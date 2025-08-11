using System;
using System.Collections.Generic;
using Algebras.Localization.Editor.Editor.Core;
using UnityEngine;

namespace Algebras.Localization.Editor.Editor.ServiceProvider
{
    /// <summary>
    ///     Authentication type for Algebras translation service.
    /// </summary>
    public enum AlgebrasAuthenticationType
    {
        /// <summary>
        ///     No authentication specified.
        /// </summary>
        None,

        /// <summary>
        ///     Use API key authentication.
        /// </summary>
        ApiKey
    }

    /// <summary>
    ///     Translation provider options.
    /// </summary>
    public enum AlgebrasProvider
    {
        /// <summary>
        ///     Use Algebras AI translation service.
        /// </summary>
        AlgebrasAI,

        /// <summary>
        ///     Use OpenAI translation service.
        /// </summary>
        OpenAI
    }

    /// <summary>
    ///     Configuration for connecting to Algebras translation service.
    /// </summary>
    public interface IAlgebrasTranslationService
    {
        /// <summary>
        ///     The API client that will be used for translation requests.
        /// </summary>
        IAlgebrasAPIClient Client { get; }
    }

    /// <summary>
    ///     Global API settings for Algebras service (OpenAI-specific).
    /// </summary>
    [Serializable]
    public class AlgebrasAPISettings
    {
        [SerializeField]
        [Tooltip("Controls randomness in translation (0-1). Lower values = more consistent. OpenAI only.")]
        [Range(0f, 1f)]
        private float temperature = 0.3f;

        [SerializeField]
        [Tooltip("Maximum tokens for AI response. Higher = longer translations possible. OpenAI only.")]
        [Range(1, 4096)]
        private int maxTokens = 2048;

        /// <summary>
        ///     Temperature setting for AI translation (0-1, lower = more consistent).
        ///     Only used for OpenAI provider.
        /// </summary>
        public float Temperature
        {
            get => temperature;
            set => temperature = Mathf.Clamp01(value);
        }

        /// <summary>
        ///     Maximum tokens for AI responses. Only used for OpenAI provider.
        /// </summary>
        public int MaxTokens
        {
            get => maxTokens;
            set => maxTokens = Mathf.Max(1, value);
        }
    }

    /// <summary>
    ///     Batch processing settings for translation requests.
    /// </summary>
    [Serializable]
    public class AlgebrasBatchSettings
    {
        [SerializeField] [Tooltip("Number of texts to translate in each batch request")] [Range(1, 100)]
        private int batchSize = 20;

        [SerializeField]
        [Tooltip("Maximum number of simultaneous batch requests for better performance")]
        [Range(1, 10)]
        private int maxParallelBatches = 5;

        [SerializeField] [Tooltip("Delay between requests in seconds to respect API rate limits")] [Range(0f, 2f)]
        private float requestDelay = 0.1f;

        /// <summary>
        ///     Number of strings to process in each batch.
        /// </summary>
        public int BatchSize
        {
            get => batchSize;
            set => batchSize = Mathf.Max(1, value);
        }

        /// <summary>
        ///     Maximum number of parallel batch requests.
        /// </summary>
        public int MaxParallelBatches
        {
            get => maxParallelBatches;
            set => maxParallelBatches = Mathf.Max(1, value);
        }

        /// <summary>
        ///     Delay between requests to respect rate limits (seconds).
        /// </summary>
        public float RequestDelay
        {
            get => requestDelay;
            set => requestDelay = Mathf.Max(0f, value);
        }
    }

    /// <summary>
    ///     The Algebras service provider performs authentication and manages connection settings
    ///     for the Algebras translation service. It also includes general translation properties
    ///     such as batch settings and translation options.
    /// </summary>
    [CreateAssetMenu(fileName = "Algebras Service", menuName = "Localization/Algebras Service")]
    [HelpURL("https://docs.algebras.ai/unity/service-provider")]
    public class AlgebrasServiceProvider : ScriptableObject, IAlgebrasTranslationService, ISerializationCallbackReceiver
    {
        private const string AlgebrasAPIEndpoint = "https://platform.algebras.ai";

        [SerializeField] [Tooltip("Your Algebras AI API key. Get one from https://platform.algebras.ai")]
        private string apiKey = string.Empty;

        [SerializeField] [Tooltip("Authentication method to use with the API")]
        private AlgebrasAuthenticationType authenticationType = AlgebrasAuthenticationType.ApiKey;

        [SerializeField] [Tooltip("Translation provider: Algebras AI (recommended) or OpenAI (coming soon)")]
        private AlgebrasProvider provider = AlgebrasProvider.AlgebrasAI;

        [SerializeField] [Tooltip("AI model to use for translation (OpenAI only)")]
        private string model = "gpt-4";

        [SerializeField] [Tooltip("Application name sent with API requests for identification")]
        private string applicationName = string.Empty;

        [SerializeField] private AlgebrasAPISettings apiSettings = new();
        [SerializeField] private AlgebrasBatchSettings batchSettings = new();

        private IAlgebrasAPIClient _client;

        /// <summary>
        ///     The authentication methodology to use.
        /// </summary>
        public AlgebrasAuthenticationType Authentication
        {
            get => authenticationType;
            set => authenticationType = value;
        }

        /// <summary>
        ///     The translation provider to use.
        /// </summary>
        public AlgebrasProvider Provider
        {
            get => provider;
            set => provider = value;
        }

        /// <summary>
        ///     The API key for authentication.
        /// </summary>
        public string ApiKey
        {
            get => apiKey;
            set => apiKey = value ?? string.Empty;
        }

        /// <summary>
        ///     The API endpoint URL.
        /// </summary>
        public string ApiEndpoint => AlgebrasAPIEndpoint;

        /// <summary>
        ///     The model to use for translation (relevant for OpenAI provider).
        /// </summary>
        public string Model
        {
            get => model;
            set => model = value ?? "gpt-4";
        }

        /// <summary>
        ///     The application name sent with requests.
        /// </summary>
        public string ApplicationName
        {
            get => applicationName;
            set => applicationName = value ?? "Unity Algebras Localization";
        }

        /// <summary>
        ///     Global API settings (OpenAI-specific).
        /// </summary>
        public AlgebrasAPISettings APISettings => apiSettings;

        /// <summary>
        ///     Batch processing settings.
        /// </summary>
        public AlgebrasBatchSettings BatchSettings => batchSettings;

        private void OnValidate()
        {
            // Ensure non-null settings
            apiSettings ??= new AlgebrasAPISettings();
            batchSettings ??= new AlgebrasBatchSettings();
        }

        /// <summary>
        ///     The API client that will be used for translation requests.
        /// </summary>
        public IAlgebrasAPIClient Client
        {
            get
            {
                if (_client == null)
                    _client = CreateClient();
                return _client;
            }
        }

        public void OnBeforeSerialize()
        {
            // Validation before serialization
        }

        public void OnAfterDeserialize()
        {
            // Ensure settings are not null after deserialization
            apiSettings ??= new AlgebrasAPISettings();
            batchSettings ??= new AlgebrasBatchSettings();
        }

        /// <summary>
        ///     Validates the current configuration.
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise.</returns>
        public bool IsConfigurationValid()
        {
            if (authenticationType == AlgebrasAuthenticationType.ApiKey)
                if (string.IsNullOrWhiteSpace(apiKey))
                    return false;


            return true;
        }

        /// <summary>
        ///     Gets validation error messages for the current configuration.
        /// </summary>
        /// <returns>Array of validation error messages, or empty array if valid.</returns>
        public string[] GetValidationErrors()
        {
            var errors = new List<string>();

            if (authenticationType == AlgebrasAuthenticationType.ApiKey && string.IsNullOrWhiteSpace(apiKey))
                errors.Add("API Key is required when using API Key authentication.");

            if (provider == AlgebrasProvider.OpenAI)
            {
                errors.Add("OpenAI provider is not yet available. Use Algebras AI.");
                provider = AlgebrasProvider.AlgebrasAI;
            }

            return errors.ToArray();
        }

        private IAlgebrasAPIClient CreateClient()
        {
            // This will be implemented when we create the API client
            return new AlgebrasAPIClient(this);
        }
    }
}