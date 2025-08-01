using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Localization.Reporting;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;

namespace Algebras.Localization.Editor
{
    /// <summary>
    /// Custom property drawer for AlgebrasExtension providing simple interface.
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
                EditorGUI.HelpBox(rect, "Service Provider is required for translation operations.", MessageType.Warning);
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
            float height = 0f;

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
                height += EditorStyles.helpBox.CalcHeight(new GUIContent(errorMessage), EditorGUIUtility.currentViewWidth) + k_Spacing;
            }

            // Spacing before buttons
            height += k_SectionSpacing;

            // Action Buttons
            height += k_ButtonHeight + k_Spacing;

            return height;
        }

        private void DrawActionButtons(Rect rect, AlgebrasExtension extension)
        {
            // Single translate button taking full width
            var translateRect = new Rect(rect.x, rect.y, rect.width, k_ButtonHeight);

            bool canTranslate = extension?.ServiceProvider != null && extension.ServiceProvider.IsConfigurationValid();

            GUI.enabled = canTranslate;

            if (GUI.Button(translateRect, "Translate"))
            {
                PerformTranslateOperation(extension);
            }

            GUI.enabled = true;
        }

        private AlgebrasExtension GetExtension(SerializedProperty property)
        {
            // Get the actual object being drawn
            var target = property.serializedObject.targetObject;

            // If it's a StringTableCollection, look for the extension
            if (target is StringTableCollection collection)
            {
                foreach (var ext in collection.Extensions)
                {
                    if (ext is AlgebrasExtension algebrasExt)
                        return algebrasExt;
                }
            }

            return null;
        }

        private async void PerformTranslateOperation(AlgebrasExtension extension)
        {
            if (extension?.TargetCollection == null) return;

            var collection = extension.TargetCollection as StringTableCollection;
            if (collection == null) return;

            var translationService = new AlgebrasTranslationService(extension.ServiceProvider);
            var reporter = new SimpleTaskReporter();

            try
            {
                // Use push operation to translate missing entries
                await translationService.PushStringTableCollectionAsync(collection, extension, null, true, reporter);
                EditorUtility.SetDirty(collection);
                Debug.Log("Translation completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Translation failed: {ex.Message}");
                EditorUtility.DisplayDialog("Translation Error", $"Translation operation failed:\n{ex.Message}", "OK");
            }
        }

    }

    /// <summary>
    /// Simple task reporter for translation operations.
    /// </summary>
    public class SimpleTaskReporter : ITaskReporter
    {
        public bool Started { get; private set; }
        public float CurrentProgress { get; private set; }
        public string Description { get; private set; }
        public string Status { get; private set; }

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