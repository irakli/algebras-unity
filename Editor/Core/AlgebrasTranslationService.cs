using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Algebras.Localization.Editor.Editor.Extension;
using Algebras.Localization.Editor.Editor.ServiceProvider;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Reporting;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Algebras.Localization.Editor.Editor.Core
{
    /// <summary>
    ///     Core service for handling Algebras translation operations.
    ///     Manages the translation workflow between Unity localization tables and Algebras API.
    /// </summary>
    public class AlgebrasTranslationService
    {
        private readonly IAlgebrasAPIClient _apiClient;
        private readonly AlgebrasServiceProvider _serviceProvider;

        /// <summary>
        ///     Initializes a new instance of AlgebrasTranslationService.
        /// </summary>
        /// <param name="serviceProvider">The service provider containing configuration.</param>
        public AlgebrasTranslationService(AlgebrasServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _apiClient = serviceProvider.Client;
        }

        /// <summary>
        ///     Pushes strings from Unity to Algebras API for translation.
        /// </summary>
        /// <param name="collection">The string table collection to translate.</param>
        /// <param name="extension">The Algebras extension with configuration.</param>
        /// <param name="targetLanguages">Languages to translate into.</param>
        /// <param name="onlyMissing">Only translate missing entries.</param>
        /// <param name="reporter">Progress reporter.</param>
        public async Task PushStringTableCollectionAsync(
            StringTableCollection collection,
            AlgebrasExtension extension,
            List<string> targetLanguages = null,
            bool onlyMissing = false,
            ITaskReporter reporter = null)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));

            // Use target languages from extension settings, or fallback to parameter, or auto-detect
            if (targetLanguages == null || targetLanguages.Count == 0)
            {
                // Check extension settings first
                if (extension.TableSettings.TargetLanguages != null &&
                    extension.TableSettings.TargetLanguages.Length > 0)
                {
                    targetLanguages = extension.TableSettings.TargetLanguages.ToList();
                }
                else
                {
                    // Fallback: translate to all available tables except the source
                    targetLanguages = new List<string>();
                    var sourceLanguage = GetSourceLanguageFromExtension(extension, collection);

                    foreach (var table in collection.StringTables)
                    {
                        var tableLanguage = table.LocaleIdentifier.Code;
                        if (tableLanguage != sourceLanguage) // Skip source language
                            targetLanguages.Add(tableLanguage);
                    }
                }
            }

            if (targetLanguages.Count == 0)
            {
                reporter?.Fail("No target languages found for translation.");
                return;
            }

            try
            {
                reporter?.Start($"Translating {collection.TableCollectionName}", "Preparing translation requests");

                // Determine source language from extension settings
                var sourceLanguage = GetSourceLanguageFromExtension(extension, collection);
                var sourceTable = GetSourceTable(collection, sourceLanguage);

                var totalOperations = targetLanguages.Count;
                var completedOperations = 0;

                foreach (var targetLanguage in targetLanguages)
                {
                    reporter?.ReportProgress($"Translating to {targetLanguage}",
                        (float)completedOperations / totalOperations);

                    await TranslateToLanguageAsync(collection, extension, sourceTable, sourceLanguage, targetLanguage,
                        onlyMissing, reporter);

                    completedOperations++;
                }

                reporter?.Completed($"Translation completed for {collection.TableCollectionName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Translation failed: {ex.Message}");
                reporter?.Fail($"Translation failed: {ex.Message}");
            }
        }

        /// <summary>
        ///     Pulls completed translations from Algebras API and updates Unity tables.
        /// </summary>
        /// <param name="collection">The string table collection to update.</param>
        /// <param name="extension">The Algebras extension with configuration.</param>
        /// <param name="targetLanguages">Languages to pull translations for.</param>
        /// <param name="reporter">Progress reporter.</param>
        public async Task PullTranslationsAsync(
            StringTableCollection collection,
            AlgebrasExtension extension,
            List<string> targetLanguages = null,
            ITaskReporter reporter = null)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));

            // For this simplified implementation, pull is the same as push
            // In a real implementation, this might check a translation server for completed translations
            await PushStringTableCollectionAsync(collection, extension, targetLanguages, false, reporter);
        }

        /// <summary>
        ///     Tests the connection to the Algebras API.
        /// </summary>
        /// <returns>True if connection is successful, false otherwise.</returns>
        public async Task<bool> TestConnectionAsync()
        {
            return await _apiClient.TestConnectionAsync();
        }

        private async Task TranslateToLanguageAsync(
            StringTableCollection collection,
            AlgebrasExtension extension,
            StringTable sourceTable,
            string sourceLanguage,
            string targetLanguage,
            bool onlyMissing,
            ITaskReporter reporter)
        {
            // Get entries that need translation
            var entriesToTranslate = GetEntriesToTranslate(collection, sourceTable, targetLanguage, onlyMissing);

            if (entriesToTranslate.Count == 0)
            {
                reporter?.ReportProgress($"No entries to translate for {targetLanguage}", 0f);
                return;
            }

            // Process translations in batches
            var batchSize = _serviceProvider.BatchSettings.BatchSize;
            var batches = CreateBatches(entriesToTranslate, batchSize);
            var maxParallelBatches = _serviceProvider.BatchSettings.MaxParallelBatches;

            var semaphore = new SemaphoreSlim(maxParallelBatches, maxParallelBatches);
            var tasks = new List<Task>();

            for (var i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                var batchIndex = i;

                var task = ProcessBatchAsync(semaphore, batch, sourceLanguage, targetLanguage, collection, extension,
                    batchIndex, batches.Count, reporter);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private async Task ProcessBatchAsync(
            SemaphoreSlim semaphore,
            List<KeyValuePair<string, string>> batch,
            string sourceLanguage,
            string targetLanguage,
            StringTableCollection collection,
            AlgebrasExtension extension,
            int batchIndex,
            int totalBatches,
            ITaskReporter reporter)
        {
            await semaphore.WaitAsync();

            try
            {
                reporter?.ReportProgress($"Processing batch {batchIndex + 1}/{totalBatches}",
                    (float)batchIndex / totalBatches);

                var texts = batch.Select(kvp => kvp.Value).ToArray();

                // Convert "Auto" to "auto" for API compatibility
                var apiSourceLanguage = sourceLanguage == "Auto" ? "auto" : sourceLanguage;
                var response = await _apiClient.TranslateBatchAsync(texts, apiSourceLanguage, targetLanguage,
                    extension.TableSettings);

                if (response.success && response.translations != null)
                {
                    // Get target table
                    var targetTable = collection.GetTable(targetLanguage) as StringTable;
                    if (targetTable == null)
                        // Create target table if it doesn't exist
                        targetTable = collection.AddNewTable(targetLanguage) as StringTable;

                    for (var i = 0; i < batch.Count && i < response.translations.Length; i++)
                    {
                        var entry = batch[i];
                        var translation = response.translations[i];

                        // Update the target table with the translation
                        targetTable.AddEntry(entry.Key, translation.translated);
                    }

                    EditorUtility.SetDirty(targetTable);
                }
                else
                {
                    Debug.LogError($"Translation batch failed: {response.error ?? "Unknown error"}");
                }

                // Respect request delay
                if (_serviceProvider.BatchSettings.RequestDelay > 0)
                    await Task.Delay(TimeSpan.FromSeconds(_serviceProvider.BatchSettings.RequestDelay));
            }
            finally
            {
                semaphore.Release();
            }
        }

        private List<KeyValuePair<string, string>> GetEntriesToTranslate(
            StringTableCollection collection,
            StringTable sourceTable,
            string targetLanguage,
            bool onlyMissing)
        {
            var entries = new List<KeyValuePair<string, string>>();
            var targetTable = collection.GetTable(targetLanguage) as StringTable;

            foreach (var sourceEntry in sourceTable.Values)
            {
                if (sourceEntry?.Value == null) continue;

                var key = sourceEntry.Key;
                var sourceText = sourceEntry.Value;

                if (onlyMissing)
                {
                    // Only include if missing in target table
                    var targetEntry = targetTable?.GetEntry(key);

                    if (targetEntry?.Value == null) entries.Add(new KeyValuePair<string, string>(key, sourceText));
                }
                else
                {
                    entries.Add(new KeyValuePair<string, string>(key, sourceText));
                }
            }

            return entries;
        }

        private List<List<KeyValuePair<string, string>>> CreateBatches(
            List<KeyValuePair<string, string>> entries,
            int batchSize)
        {
            var batches = new List<List<KeyValuePair<string, string>>>();

            for (var i = 0; i < entries.Count; i += batchSize)
            {
                var batch = entries.Skip(i).Take(batchSize).ToList();
                batches.Add(batch);
            }

            return batches;
        }

        private async Task PullLanguageTranslationsAsync(
            StringTableCollection collection,
            AlgebrasExtension extension,
            string targetLanguage,
            ITaskReporter reporter)
        {
            // For this simplified implementation, pull is the same as push
            // In a real implementation, this might check a translation server for completed translations

            await Task.CompletedTask;
        }

        private string GetSourceLanguageFromExtension(AlgebrasExtension extension, StringTableCollection collection)
        {
            var configuredSource = extension.TableSettings.SourceLanguage;

            // If set to "Auto" or empty, use the first table as source
            if (string.IsNullOrEmpty(configuredSource) || configuredSource == "Auto")
            {
                if (collection.StringTables.Count > 0) return collection.StringTables[0].LocaleIdentifier.Code;
                return "en"; // Fallback to English
            }

            return configuredSource;
        }

        private StringTable GetSourceTable(StringTableCollection collection, string sourceLanguage)
        {
            // If sourceLanguage is "Auto", use the first table
            if (sourceLanguage == "Auto" && collection.StringTables.Count > 0) return collection.StringTables[0];

            // Find table matching the source language
            foreach (var table in collection.StringTables)
                if (table.LocaleIdentifier.Code == sourceLanguage)
                    return table;

            // Fallback to first table if no match found
            return collection.StringTables.Count > 0 ? collection.StringTables[0] : null;
        }
    }
}