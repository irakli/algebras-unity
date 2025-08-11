using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Algebras.Localization.Editor.Editor.Core;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Reporting;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Algebras.Localization.Editor.Editor.Extension
{
    /// <summary>
    ///     Custom property drawer for AlgebrasExtension providing simple interface.
    /// </summary>
    [CustomPropertyDrawer(typeof(AlgebrasExtension))]
    public class AlgebrasExtensionPropertyDrawer : PropertyDrawer
    {
        private const float k_ButtonHeight = 20f;
        private const float k_Spacing = 2f;
        private const float k_SectionSpacing = 8f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // Header
            EditorGUI.LabelField(rect, "Algebras", EditorStyles.boldLabel);
            rect.y += EditorGUIUtility.singleLineHeight + k_SectionSpacing;

            // Service Provider
            var serviceProviderProp = property.FindPropertyRelative("serviceProvider");
            EditorGUI.PropertyField(rect, serviceProviderProp, new GUIContent("Localization Service Provider"));
            rect.y += EditorGUIUtility.singleLineHeight + k_Spacing;

            // Table Settings
            var tableSettingsProp = property.FindPropertyRelative("tableSettings");
            EditorGUI.PropertyField(rect, tableSettingsProp, new GUIContent("Table Settings"), true);
            rect.y += EditorGUI.GetPropertyHeight(tableSettingsProp, true) + k_Spacing;

            // Get the actual extension object to check validation
            var extension = GetExtension(property);

            // Validation warning
            if (serviceProviderProp.objectReferenceValue == null)
            {
                EditorGUI.HelpBox(rect, "Service Provider is required for translation operations.",
                    MessageType.Warning);
                rect.y += EditorGUIUtility.singleLineHeight + k_Spacing;
            }
            else if (extension?.ServiceProvider != null && !extension.ServiceProvider.IsConfigurationValid())
            {
                var errors = extension.ServiceProvider.GetValidationErrors();
                var errorMessage = string.Join("\n", errors);
                var helpBoxHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(errorMessage), rect.width);
                var errorRect = new Rect(rect.x, rect.y, rect.width, helpBoxHeight);
                EditorGUI.HelpBox(errorRect, errorMessage, MessageType.Error);
                rect.y += helpBoxHeight + k_Spacing;
            }

            // Add some spacing before buttons
            rect.y += k_SectionSpacing;

            // Action Buttons
            DrawActionButtons(rect, extension);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = 0f;

            // Header
            height += EditorGUIUtility.singleLineHeight + k_SectionSpacing;

            // Service Provider
            height += EditorGUIUtility.singleLineHeight + k_Spacing;

            // Table Settings
            var tableSettingsProp = property.FindPropertyRelative("tableSettings");
            height += EditorGUI.GetPropertyHeight(tableSettingsProp, true) + k_Spacing;

            // Validation messages
            var serviceProviderProp = property.FindPropertyRelative("serviceProvider");
            var extension = GetExtension(property);

            if (serviceProviderProp.objectReferenceValue == null)
            {
                height += EditorGUIUtility.singleLineHeight + k_Spacing;
            }
            else if (extension?.ServiceProvider != null && !extension.ServiceProvider.IsConfigurationValid())
            {
                var errors = extension.ServiceProvider.GetValidationErrors();
                var errorMessage = string.Join("\n", errors);
                height += EditorStyles.helpBox.CalcHeight(new GUIContent(errorMessage),
                    EditorGUIUtility.currentViewWidth) + k_Spacing;
            }

            // Spacing before buttons
            height += k_SectionSpacing;

            // Action Buttons area
            // Mode label + dropdown + explanation + spacing + button
            height += EditorGUIUtility.singleLineHeight + k_Spacing; // Mode label
            height += EditorGUIUtility.singleLineHeight + k_Spacing; // Mode dropdown
            height += EditorGUIUtility.singleLineHeight * 2 + k_Spacing; // Explanation (estimated)

            // Check if we need to show glossary warning
            if (extension?.TableSettings?.TranslationMode == TranslationMode.Batch &&
                !string.IsNullOrEmpty(extension?.TableSettings?.GlossaryId))
                height += EditorGUIUtility.singleLineHeight + k_Spacing; // Warning message

            height += k_Spacing; // Extra spacing before button
            height += k_ButtonHeight; // Translate button

            return height;
        }

        private void DrawActionButtons(Rect rect, AlgebrasExtension extension)
        {
            var currentRect = rect;

            // Translation Mode Selection
            var modeRect = new Rect(currentRect.x, currentRect.y, currentRect.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(modeRect, "Translation Mode", EditorStyles.boldLabel);
            currentRect.y += EditorGUIUtility.singleLineHeight + k_Spacing;

            // Mode dropdown
            var modeDropdownRect = new Rect(currentRect.x, currentRect.y, currentRect.width,
                EditorGUIUtility.singleLineHeight);
            var newMode = (TranslationMode)EditorGUI.EnumPopup(modeDropdownRect,
                extension?.TableSettings?.TranslationMode ?? TranslationMode.Batch);
            if (extension?.TableSettings != null && newMode != extension.TableSettings.TranslationMode)
            {
                extension.TableSettings.TranslationMode = newMode;
                EditorUtility.SetDirty(extension.TargetCollection);
            }

            currentRect.y += EditorGUIUtility.singleLineHeight + k_Spacing;

            // Mode explanation
            var explanationRect = new Rect(currentRect.x, currentRect.y, currentRect.width,
                EditorGUIUtility.singleLineHeight * 2);
            var explanation = newMode == TranslationMode.Single
                ? "Single Mode: Individual translations with glossary support. Slower but more accurate for terminology."
                : "Batch Mode: Bulk translations with parallel processing. Faster but no glossary support.";

            var helpBoxHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(explanation), currentRect.width);
            explanationRect.height = helpBoxHeight;
            EditorGUI.HelpBox(explanationRect, explanation, MessageType.Info);
            currentRect.y += helpBoxHeight + k_Spacing;

            // Glossary warning for batch mode
            if (newMode == TranslationMode.Batch && !string.IsNullOrEmpty(extension?.TableSettings?.GlossaryId))
            {
                var warningRect = new Rect(currentRect.x, currentRect.y, currentRect.width,
                    EditorGUIUtility.singleLineHeight);
                var warningHeight = EditorStyles.helpBox.CalcHeight(
                    new GUIContent("Warning: Glossary is configured but not supported in Batch mode."),
                    currentRect.width);
                warningRect.height = warningHeight;
                EditorGUI.HelpBox(warningRect, "Warning: Glossary is configured but not supported in Batch mode.",
                    MessageType.Warning);
                currentRect.y += warningHeight + k_Spacing;
            }

            // Add spacing before translate button
            currentRect.y += k_Spacing;

            // Translate button with mode-specific label
            var translateRect = new Rect(currentRect.x, currentRect.y, currentRect.width, k_ButtonHeight);
            var buttonText = newMode == TranslationMode.Single ? "Translate (Single Mode)" : "Translate (Batch Mode)";

            var canTranslate = extension?.ServiceProvider != null && extension.ServiceProvider.IsConfigurationValid();

            GUI.enabled = canTranslate;

            if (GUI.Button(translateRect, buttonText)) PerformTranslateOperation(extension);

            GUI.enabled = true;
        }

        private AlgebrasExtension GetExtension(SerializedProperty property)
        {
            // Get the actual object being drawn
            var target = property.serializedObject.targetObject;

            // If it's a StringTableCollection, look for the extension
            if (target is StringTableCollection collection)
                foreach (var ext in collection.Extensions)
                    if (ext is AlgebrasExtension algebrasExt)
                        return algebrasExt;

            return null;
        }

        private async void PerformTranslateOperation(AlgebrasExtension extension)
        {
            if (extension?.TargetCollection == null) return;

            var collection = extension.TargetCollection as StringTableCollection;
            if (collection == null) return;

            var mode = extension.TableSettings?.TranslationMode ?? TranslationMode.Batch;

            try
            {
                if (mode == TranslationMode.Single)
                    await PerformSingleModeTranslation(extension, collection);
                else
                    await PerformBatchModeTranslation(extension, collection);

                EditorUtility.SetDirty(collection);
                Debug.Log($"Translation completed successfully using {mode} mode.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Translation failed: {ex.Message}");
                EditorUtility.DisplayDialog("Translation Error", $"Translation operation failed:\n{ex.Message}", "OK");
            }
        }

        private async Task PerformBatchModeTranslation(AlgebrasExtension extension, StringTableCollection collection)
        {
            var translationService = new AlgebrasTranslationService(extension.ServiceProvider);
            var reporter = new SimpleTaskReporter();

            // Use existing batch translation service
            await translationService.PushStringTableCollectionAsync(collection, extension, null, true, reporter);
        }

        private async Task PerformSingleModeTranslation(AlgebrasExtension extension, StringTableCollection collection)
        {
            var apiClient = extension.ServiceProvider.Client;
            var reporter = new SimpleTaskReporter();

            reporter.Start("Single Mode Translation", "Preparing translation...");

            // Determine the actual source language and get source table
            var configuredSourceLang = extension.TableSettings.SourceLanguage;
            StringTable sourceTable = null;

            // If source language is "Auto" or empty, use first table as source
            if (string.IsNullOrEmpty(configuredSourceLang) || configuredSourceLang == "Auto")
            {
                sourceTable = collection.StringTables.Count > 0 ? collection.StringTables[0] : null;
                configuredSourceLang = sourceTable?.LocaleIdentifier.Code ?? "en";
            }
            else
            {
                // Find the configured source table
                sourceTable = collection.GetTable(configuredSourceLang) as StringTable;
                if (sourceTable == null)
                    // Fallback to first table if configured source not found
                    sourceTable = collection.StringTables.Count > 0 ? collection.StringTables[0] : null;
            }

            if (sourceTable == null)
            {
                reporter.Completed("No source table found for translation.");
                return;
            }

            Debug.Log($"[Single Mode] Using source table: {sourceTable.LocaleIdentifier.Code}");

            // Get all missing entries that need translation
            var missingEntries = new List<(string key, string sourceText, string targetLanguage)>();

            foreach (var table in collection.StringTables)
            {
                // Skip the source table itself
                if (table.LocaleIdentifier.Code == sourceTable.LocaleIdentifier.Code)
                    continue;

                Debug.Log($"[Single Mode] Checking target table: {table.LocaleIdentifier.Code}");

                foreach (var entry in collection.SharedData.Entries)
                {
                    var sourceEntry = sourceTable.GetEntry(entry.Id);
                    var targetTable = table;
                    var targetEntry = targetTable?.GetEntry(entry.Id);

                    if (sourceEntry != null && !string.IsNullOrEmpty(sourceEntry.Value) &&
                        (targetEntry == null || string.IsNullOrEmpty(targetEntry.Value)))
                    {
                        missingEntries.Add((entry.Key, sourceEntry.Value, table.LocaleIdentifier.Code));
                        Debug.Log($"[Single Mode] Found missing entry: '{entry.Key}' in {table.LocaleIdentifier.Code}");
                    }
                }
            }

            if (missingEntries.Count == 0)
            {
                reporter.Completed("No missing translations found.");
                return;
            }

            reporter.ReportProgress($"Translating {missingEntries.Count} entries...", 0f);

            // Translate each entry individually using single mode
            for (var i = 0; i < missingEntries.Count; i++)
            {
                var (key, sourceText, targetLang) = missingEntries[i];

                try
                {
                    var response = await apiClient.TranslateSingleAsync(
                        sourceText,
                        extension.TableSettings.SourceLanguage == "Auto"
                            ? "auto"
                            : extension.TableSettings.SourceLanguage.ToLower(),
                        targetLang,
                        extension.TableSettings);

                    if (response.success && response.translations.Length > 0)
                    {
                        var translatedText = response.translations[0].translated;

                        // Find the target table and update the entry
                        var targetTable = collection.GetTable(targetLang) as StringTable;
                        if (targetTable != null)
                        {
                            targetTable.AddEntry(key, translatedText);

                            Debug.Log(
                                $"[Single Mode] Translated '{sourceText}' -> '{translatedText}' (key: {key}, lang: {targetLang})");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Single Mode] Failed to translate key '{key}': {response.error}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Single Mode] Exception translating key '{key}': {ex.Message}");
                }

                // Update progress
                var progress = (float)(i + 1) / missingEntries.Count;
                reporter.ReportProgress($"Translated {i + 1}/{missingEntries.Count} entries", progress);
            }

            reporter.Completed($"Single mode translation completed for {missingEntries.Count} entries.");
        }
    }

    /// <summary>
    ///     Simple task reporter for translation operations.
    /// </summary>
    public class SimpleTaskReporter : ITaskReporter
    {
        public string Description { get; private set; }
        public string Status { get; private set; }
        public bool Started { get; private set; }
        public float CurrentProgress { get; private set; }

        public void Start(string description, string initialStatus)
        {
            Started = true;
            Description = description;
            Status = initialStatus;
            Debug.Log($"[Algebras] {description}: {initialStatus}");
        }

        public void ReportProgress(string status, float progress)
        {
            Status = status;
            CurrentProgress = progress;
            Debug.Log($"[Algebras] {Description}: {status} ({progress:P0})");
        }

        public void Completed(string message)
        {
            Debug.Log($"[Algebras] {Description}: Completed - {message}");
        }

        public void Fail(string error)
        {
            Debug.LogError($"[Algebras] {Description}: Failed - {error}");
        }
    }
}