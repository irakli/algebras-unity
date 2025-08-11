using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Algebras.Localization.Editor.Extension;
using Algebras.Localization.Editor.ServiceProvider;
using Algebras.Localization.Editor.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Algebras.Localization.Editor.Core
{
    /// <summary>
    ///     Implementation of IAlgebrasAPIClient for communicating with Algebras translation service.
    /// </summary>
    public class AlgebrasAPIClient : IAlgebrasAPIClient
    {
        private readonly AlgebrasServiceProvider _serviceProvider;

        /// <summary>
        ///     Initializes a new instance of AlgebrasAPIClient.
        /// </summary>
        /// <param name="serviceProvider">The service provider containing configuration.</param>
        public AlgebrasAPIClient(AlgebrasServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        ///     Translates a batch of texts from source to target language.
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
            var response = await SendTranslationRequestAsync(request);

            // Apply string normalization if successful
            if (response.success && response.translations != null)
            {
                var finalOptions = options ?? CreateDefaultOptions(tableSettings);
                var normalizedTranslations = StringNormalizer.NormalizeTranslations(
                    texts,
                    response.translations.Select(t => t.translated).ToArray(),
                    finalOptions.normalize_strings
                );

                // Update results with normalized translations
                for (int i = 0; i < response.translations.Length && i < normalizedTranslations.Length; i++)
                {
                    response.translations[i].translated = normalizedTranslations[i];
                }
            }

            return response;
        }

        /// <summary>
        ///     Translates a single text from source to target language.
        ///     Currently routes through batch API for backward compatibility.
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
        ///     Translates a single text using the true single mode API endpoint with glossary support.
        /// </summary>
        public async Task<TranslationResponse> TranslateSingleAsync(
            string text,
            string sourceLanguage,
            string targetLanguage,
            AlgebrasTableSettings tableSettings,
            TranslationRequest.TranslationOptions options = null)
        {
            if (string.IsNullOrEmpty(text))
                return CreateErrorResponse("No text provided for translation");

            var singleRequest =
                CreateSingleTranslationRequest(text, sourceLanguage, targetLanguage, tableSettings, options);
            var response = await SendSingleTranslationRequestAsync(singleRequest, text);

            // Apply string normalization if successful
            if (response.success && response.translations.Length > 0)
            {
                var finalOptions = options ?? CreateDefaultOptions(tableSettings);
                var normalizedTranslation = StringNormalizer.NormalizeTranslation(
                    text,
                    response.translations[0].translated,
                    finalOptions.normalize_strings
                );

                // Update the response with normalized text
                response.translations[0].translated = normalizedTranslation;
            }

            return response;
        }

        /// <summary>
        ///     Tests the connection to the translation service.
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var defaultSettings = new AlgebrasTableSettings();
                // Test using single mode API to verify new endpoint works
                var testResponse = await TranslateSingleAsync("test", "en", "es", defaultSettings);
                return testResponse.success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Algebras API connection test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Gets information about available models.
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

        private AlgebrasSingleRequest CreateSingleTranslationRequest(
            string text,
            string sourceLanguage,
            string targetLanguage,
            AlgebrasTableSettings tableSettings,
            TranslationRequest.TranslationOptions options)
        {
            var finalOptions = options ?? CreateDefaultOptions(tableSettings);

            return new AlgebrasSingleRequest
            {
                sourceLanguage = sourceLanguage,
                targetLanguage = targetLanguage,
                textContent = text,
                fileContent = "", // Always empty for text-only translations
                glossaryId = finalOptions.glossary_id ?? "",
                prompt = finalOptions.custom_prompt ?? "",
                flag = finalOptions.ui_safe ? "true" : "false" // Convert bool to string for single mode
            };
        }

        private async Task<TranslationResponse> SendSingleTranslationRequestAsync(AlgebrasSingleRequest request,
            string originalText)
        {
            try
            {
                if (_serviceProvider.Provider == AlgebrasProvider.OpenAI)
                    return CreateErrorResponse(
                        "OpenAI provider is not yet implemented. Please use Algebras AI provider.");

                var endpoint = GetSingleTranslationEndpoint();

                using (var webRequest = new UnityWebRequest(endpoint, "POST"))
                {
                    // Debug the request values before creating form data
                    Debug.Log($"[Single API] sourceLanguage: '{request.sourceLanguage}'");
                    Debug.Log($"[Single API] targetLanguage: '{request.targetLanguage}'");
                    Debug.Log($"[Single API] textContent: '{request.textContent}'");
                    Debug.Log($"[Single API] fileContent: '{request.fileContent}'");
                    Debug.Log($"[Single API] prompt: '{request.prompt}'");
                    Debug.Log($"[Single API] flag: '{request.flag}'");
                    Debug.Log($"[Single API] glossaryId: '{request.glossaryId}'");

                    // Create multipart form data with explicit null checks
                    var formData = new List<IMultipartFormSection>();

                    // Add required fields with safety checks
                    if (!string.IsNullOrEmpty(request.sourceLanguage))
                        formData.Add(new MultipartFormDataSection("sourceLanguage", request.sourceLanguage));

                    if (!string.IsNullOrEmpty(request.targetLanguage))
                        formData.Add(new MultipartFormDataSection("targetLanguage", request.targetLanguage));

                    if (!string.IsNullOrEmpty(request.textContent))
                        formData.Add(new MultipartFormDataSection("textContent", request.textContent));

                    // Optional fields - only add if not null/empty
                    if (!string.IsNullOrEmpty(request.fileContent))
                        formData.Add(new MultipartFormDataSection("fileContent", request.fileContent));

                    if (!string.IsNullOrEmpty(request.prompt))
                        formData.Add(new MultipartFormDataSection("prompt", request.prompt));

                    if (!string.IsNullOrEmpty(request.flag))
                        formData.Add(new MultipartFormDataSection("flag", request.flag));

                    if (!string.IsNullOrEmpty(request.glossaryId))
                        formData.Add(new MultipartFormDataSection("glossaryId", request.glossaryId));

                    Debug.Log($"[Single API] Created {formData.Count} form sections");

                    webRequest.uploadHandler = new UploadHandlerRaw(
                        UnityWebRequest.SerializeFormSections(formData, Encoding.UTF8.GetBytes("----boundary----")));
                    webRequest.downloadHandler = new DownloadHandlerBuffer();

                    // Set headers for multipart/form-data
                    webRequest.SetRequestHeader("Content-Type", "multipart/form-data; boundary=----boundary----");
                    webRequest.SetRequestHeader("Accept", "application/json");
                    SetAuthenticationHeaders(webRequest);

                    // Send request
                    var operation = webRequest.SendWebRequest();

                    // Wait for completion
                    while (!operation.isDone) await Task.Yield();

                    // Handle response
                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        var responseText = webRequest.downloadHandler.text;
                        Debug.Log($"Algebras Single API response: {responseText}");

                        var singleResponse = JsonUtility.FromJson<AlgebrasSingleResponse>(responseText);

                        // Convert to legacy format
                        return new TranslationResponse
                        {
                            success = true,
                            translations = new[]
                            {
                                new TranslationResponse.TranslationResult
                                {
                                    original = originalText,
                                    translated = singleResponse.data ?? "",
                                    confidence = 1.0f // Single mode doesn't provide confidence scores
                                }
                            }
                        };
                    }

                    var error = $"HTTP {webRequest.responseCode}: {webRequest.error}";
                    if (webRequest.downloadHandler != null) error += $"\nResponse: {webRequest.downloadHandler.text}";
                    Debug.LogError($"Algebras Single API request failed: {error}");
                    return CreateErrorResponse(error);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Algebras Single API request exception: {ex.Message}");
                return CreateErrorResponse(ex.Message);
            }
        }

        private async Task<TranslationResponse> SendTranslationRequestAsync(TranslationRequest request)
        {
            try
            {
                if (_serviceProvider.Provider == AlgebrasProvider.OpenAI)
                    return CreateErrorResponse(
                        "OpenAI provider is not yet implemented. Please use Algebras AI provider.");
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
                    var bodyRaw = Encoding.UTF8.GetBytes(json);
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    webRequest.downloadHandler = new DownloadHandlerBuffer();

                    // Set headers
                    webRequest.SetRequestHeader("Content-Type", "application/json");
                    webRequest.SetRequestHeader("Accept", "application/json");
                    SetAuthenticationHeaders(webRequest);

                    // Send request
                    var operation = webRequest.SendWebRequest();

                    // Wait for completion
                    while (!operation.isDone) await Task.Yield();

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
                            var sortedTranslations =
                                new AlgebrasBatchResponse.AlgebrasTranslation[batchResponse.data.translations.Length];
                            for (var i = 0; i < batchResponse.data.translations.Length; i++)
                            {
                                var translation = batchResponse.data.translations[i];
                                if (translation.index < sortedTranslations.Length)
                                    sortedTranslations[translation.index] = translation;
                            }

                            for (var i = 0;
                                 i < legacyResponse.translations.Length && i < sortedTranslations.Length;
                                 i++)
                                legacyResponse.translations[i] = new TranslationResponse.TranslationResult
                                {
                                    original = i < request.texts.Length ? request.texts[i] : "",
                                    translated = sortedTranslations[i]?.content ?? "",
                                    confidence = 1.0f // Algebras AI doesn't return confidence in batch mode
                                };
                        }

                        return legacyResponse;
                    }

                    var error = $"HTTP {webRequest.responseCode}: {webRequest.error}";
                    if (webRequest.downloadHandler != null) error += $"\nResponse: {webRequest.downloadHandler.text}";
                    Debug.LogError($"Algebras API request failed: {error}");
                    return CreateErrorResponse(error);
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

        private string GetSingleTranslationEndpoint()
        {
            return "https://platform.algebras.ai/api/v1/translation/translate";
        }

        private void SetAuthenticationHeaders(UnityWebRequest webRequest)
        {
            webRequest.SetRequestHeader("X-Api-Key", _serviceProvider.ApiKey);
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