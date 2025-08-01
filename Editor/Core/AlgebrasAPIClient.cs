using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Algebras.Localization.Editor
{
    /// <summary>
    /// Implementation of IAlgebrasAPIClient for communicating with Algebras translation service.
    /// </summary>
    public class AlgebrasAPIClient : IAlgebrasAPIClient
    {
        private readonly AlgebrasServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of AlgebrasAPIClient.
        /// </summary>
        /// <param name="serviceProvider">The service provider containing configuration.</param>
        public AlgebrasAPIClient(AlgebrasServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Translates a batch of texts from source to target language.
        /// </summary>
        public async Task<TranslationResponse> TranslateBatchAsync(
            string[] texts,
            string sourceLanguage,
            string targetLanguage,
            AlgebrasTableSettings tableSettings,
            TranslationRequest.TranslationOptions options = null)
        {
            if (texts == null || texts.Length == 0)
                return CreateErrorResponse("No texts provided for translation");

            var request = CreateTranslationRequest(texts, sourceLanguage, targetLanguage, tableSettings, options);
            return await SendTranslationRequestAsync(request);
        }

        /// <summary>
        /// Translates a single text from source to target language.
        /// </summary>
        public async Task<TranslationResponse> TranslateAsync(
            string text,
            string sourceLanguage,
            string targetLanguage,
            AlgebrasTableSettings tableSettings,
            TranslationRequest.TranslationOptions options = null)
        {
            if (string.IsNullOrEmpty(text))
                return CreateErrorResponse("No text provided for translation");

            return await TranslateBatchAsync(new[] { text }, sourceLanguage, targetLanguage, tableSettings, options);
        }

        /// <summary>
        /// Tests the connection to the translation service.
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var defaultSettings = new AlgebrasTableSettings();
                var testResponse = await TranslateAsync("test", "en", "es", defaultSettings);
                return testResponse.success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Algebras API connection test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets information about available models.
        /// </summary>
        public async Task<string[]> GetAvailableModelsAsync()
        {
            await Task.CompletedTask;
            return new[] { "algebras-translator-v1" };
        }

        private TranslationRequest CreateTranslationRequest(
            string[] texts,
            string sourceLanguage,
            string targetLanguage,
            AlgebrasTableSettings tableSettings,
            TranslationRequest.TranslationOptions options)
        {
            var request = new TranslationRequest
            {
                texts = texts,
                source_language = sourceLanguage,
                target_language = targetLanguage,
                options = options ?? CreateDefaultOptions(tableSettings)
            };

            return request;
        }

        private TranslationRequest.TranslationOptions CreateDefaultOptions(AlgebrasTableSettings tableSettings)
        {
            var apiSettings = _serviceProvider.APISettings;
            return new TranslationRequest.TranslationOptions
            {
                ui_safe = tableSettings.UIMode,
                glossary_id = tableSettings.GlossaryId,
                custom_prompt = tableSettings.CustomPrompt,
                normalize_strings = tableSettings.NormalizeStrings,
                temperature = apiSettings.Temperature,
                max_tokens = apiSettings.MaxTokens
            };
        }

        private async Task<TranslationResponse> SendTranslationRequestAsync(TranslationRequest request)
        {
            try
            {
                if (_serviceProvider.Provider == AlgebrasProvider.OpenAI)
                {
                    return CreateErrorResponse("OpenAI provider is not yet implemented. Please use Algebras AI provider.");
                }
                // Convert to Algebras AI batch request format
                var batchRequest = new AlgebrasBatchRequest
                {
                    texts = request.texts,
                    sourceLanguage = request.source_language,
                    targetLanguage = request.target_language,
                    prompt = request.options?.custom_prompt ?? "",
                    flag = request.options?.ui_safe ?? false
                };

                var json = JsonUtility.ToJson(batchRequest);
                var endpoint = GetTranslationEndpoint();

                using (var webRequest = new UnityWebRequest(endpoint, "POST"))
                {
                    // Set request body
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    webRequest.downloadHandler = new DownloadHandlerBuffer();

                    // Set headers
                    webRequest.SetRequestHeader("Content-Type", "application/json");
                    webRequest.SetRequestHeader("Accept", "application/json");
                    SetAuthenticationHeaders(webRequest);

                    // Send request
                    var operation = webRequest.SendWebRequest();

                    // Wait for completion
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    // Handle response
                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        var responseText = webRequest.downloadHandler.text;
                        Debug.Log($"Algebras API response: {responseText}");

                        var batchResponse = JsonUtility.FromJson<AlgebrasBatchResponse>(responseText);

                        // Convert to legacy format
                        var legacyResponse = new TranslationResponse
                        {
                            success = true,
                            translations = new TranslationResponse.TranslationResult[request.texts.Length]
                        };

                        if (batchResponse.data?.translations != null)
                        {
                            // Sort by index and convert to legacy format
                            var sortedTranslations = new AlgebrasBatchResponse.AlgebrasTranslation[batchResponse.data.translations.Length];
                            for (int i = 0; i < batchResponse.data.translations.Length; i++)
                            {
                                var translation = batchResponse.data.translations[i];
                                if (translation.index < sortedTranslations.Length)
                                {
                                    sortedTranslations[translation.index] = translation;
                                }
                            }

                            for (int i = 0; i < legacyResponse.translations.Length && i < sortedTranslations.Length; i++)
                            {
                                legacyResponse.translations[i] = new TranslationResponse.TranslationResult
                                {
                                    original = i < request.texts.Length ? request.texts[i] : "",
                                    translated = sortedTranslations[i]?.content ?? "",
                                    confidence = 1.0f // Algebras AI doesn't return confidence in batch mode
                                };
                            }
                        }

                        return legacyResponse;
                    }
                    else
                    {
                        var error = $"HTTP {webRequest.responseCode}: {webRequest.error}";
                        if (webRequest.downloadHandler != null)
                        {
                            error += $"\nResponse: {webRequest.downloadHandler.text}";
                        }
                        Debug.LogError($"Algebras API request failed: {error}");
                        return CreateErrorResponse(error);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Algebras API request exception: {ex.Message}");
                return CreateErrorResponse(ex.Message);
            }
        }

        private string GetTranslationEndpoint()
        {
            return "https://platform.algebras.ai/api/v1/translation/translate-batch";
        }

        private void SetAuthenticationHeaders(UnityWebRequest webRequest)
        {
            switch (_serviceProvider.Authentication)
            {
                case AlgebrasAuthenticationType.ApiKey:
                    webRequest.SetRequestHeader("X-Api-Key", _serviceProvider.ApiKey);
                    break;
            }

            webRequest.SetRequestHeader("User-Agent", $"{_serviceProvider.ApplicationName}/1.0.0");
        }

        private TranslationResponse CreateErrorResponse(string error)
        {
            return new TranslationResponse
            {
                success = false,
                error = error,
                translations = new TranslationResponse.TranslationResult[0]
            };
        }
    }
}