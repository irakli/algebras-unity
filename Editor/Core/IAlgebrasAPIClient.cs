using System;
using System.Threading.Tasks;
using Algebras.Localization.Editor.Extension;

namespace Algebras.Localization.Editor.Core
{
    /// <summary>
    ///     Request model for Algebras AI batch translation API calls.
    /// </summary>
    [Serializable]
    public class AlgebrasBatchRequest
    {
        public string[] texts;
        public string sourceLanguage;
        public string targetLanguage;
        public string prompt = "";
        public bool flag; // ui_safe mode
    }

    /// <summary>
    ///     Request model for Algebras AI single translation API calls.
    ///     Uses multipart/form-data format.
    /// </summary>
    [Serializable]
    public class AlgebrasSingleRequest
    {
        public string sourceLanguage;
        public string targetLanguage;
        public string textContent;
        public string fileContent = ""; // Always empty for text-only translations
        public string glossaryId = "";
        public string prompt = "";
        public string flag = "false"; // ui_safe mode as string
    }

    /// <summary>
    ///     Legacy request model for backwards compatibility.
    /// </summary>
    [Serializable]
    public class TranslationRequest
    {
        public string[] texts;
        public string source_language;
        public string target_language;
        public TranslationOptions options;

        [Serializable]
        public class TranslationOptions
        {
            public bool ui_safe;
            public string glossary_id;
            public string custom_prompt;
            public bool normalize_strings;
            public float temperature;
            public int max_tokens;
        }
    }

    /// <summary>
    ///     Response model for Algebras AI batch translation API calls.
    /// </summary>
    [Serializable]
    public class AlgebrasBatchResponse
    {
        public AlgebrasData data;
        public bool success;
        public string error;

        [Serializable]
        public class AlgebrasData
        {
            public AlgebrasTranslation[] translations;
        }

        [Serializable]
        public class AlgebrasTranslation
        {
            public string content;
            public int index;
        }
    }

    /// <summary>
    ///     Response model for Algebras AI single translation API calls.
    /// </summary>
    [Serializable]
    public class AlgebrasSingleResponse
    {
        public string data;
        public bool success;
        public string error;
    }

    /// <summary>
    ///     Legacy response model for backwards compatibility.
    /// </summary>
    [Serializable]
    public class TranslationResponse
    {
        public TranslationResult[] translations;
        public string model;
        public string timestamp;
        public bool success;
        public string error;

        [Serializable]
        public class TranslationResult
        {
            public string original;
            public string translated;
            public float confidence;
        }
    }

    /// <summary>
    ///     Interface for Algebras translation API client.
    /// </summary>
    public interface IAlgebrasAPIClient
    {
        /// <summary>
        ///     Translates a batch of texts from source to target language.
        /// </summary>
        /// <param name="texts">Array of texts to translate.</param>
        /// <param name="sourceLanguage">Source language code.</param>
        /// <param name="targetLanguage">Target language code.</param>
        /// <param name="tableSettings">Table-specific settings for translation.</param>
        /// <param name="options">Translation options.</param>
        /// <returns>Translation response containing results.</returns>
        Task<TranslationResponse> TranslateBatchAsync(
            string[] texts,
            string sourceLanguage,
            string targetLanguage,
            AlgebrasTableSettings tableSettings,
            TranslationRequest.TranslationOptions options = null);

        /// <summary>
        ///     Translates a single text from source to target language.
        ///     Currently routes through batch API for consistency.
        /// </summary>
        /// <param name="text">Text to translate.</param>
        /// <param name="sourceLanguage">Source language code.</param>
        /// <param name="targetLanguage">Target language code.</param>
        /// <param name="tableSettings">Table-specific settings for translation.</param>
        /// <param name="options">Translation options.</param>
        /// <returns>Translation response containing result.</returns>
        Task<TranslationResponse> TranslateAsync(
            string text,
            string sourceLanguage,
            string targetLanguage,
            AlgebrasTableSettings tableSettings,
            TranslationRequest.TranslationOptions options = null);

        /// <summary>
        ///     Translates a single text using the true single mode API endpoint.
        ///     This method supports glossary and uses multipart/form-data format.
        /// </summary>
        /// <param name="text">Text to translate.</param>
        /// <param name="sourceLanguage">Source language code.</param>
        /// <param name="targetLanguage">Target language code.</param>
        /// <param name="tableSettings">Table-specific settings for translation.</param>
        /// <param name="options">Translation options.</param>
        /// <returns>Translation response containing result.</returns>
        Task<TranslationResponse> TranslateSingleAsync(
            string text,
            string sourceLanguage,
            string targetLanguage,
            AlgebrasTableSettings tableSettings,
            TranslationRequest.TranslationOptions options = null);

        /// <summary>
        ///     Tests the connection to the translation service.
        /// </summary>
        /// <returns>True if connection is successful, false otherwise.</returns>
        Task<bool> TestConnectionAsync();

        /// <summary>
        ///     Gets information about available models (for OpenAI provider).
        /// </summary>
        /// <returns>Array of available model names.</returns>
        Task<string[]> GetAvailableModelsAsync();
    }
}