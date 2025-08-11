using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

namespace Algebras.Localization.Editor.Extension
{
    /// <summary>
    ///     Custom property drawer for AlgebrasTableSettings that provides language dropdowns.
    /// </summary>
    [CustomPropertyDrawer(typeof(AlgebrasTableSettings))]
    public class AlgebrasTableSettingsPropertyDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw foldout
            property.isExpanded =
                EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                    property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var currentY = position.y + EditorGUIUtility.singleLineHeight +
                               EditorGUIUtility.standardVerticalSpacing;
                var lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Get available languages from the collection
                var availableLanguages = GetAvailableLanguages(property);

                // Source Language Dropdown
                var sourceLanguageProp = property.FindPropertyRelative("sourceLanguage");
                var sourceLanguageRect =
                    new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                DrawSourceLanguageDropdown(sourceLanguageRect, sourceLanguageProp, availableLanguages);
                currentY += lineHeight;

                // Target Languages Multi-Selection
                var targetLanguagesProp = property.FindPropertyRelative("targetLanguages");
                var targetLanguagesRect =
                    new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                var targetLanguagesHeight = DrawTargetLanguagesDropdown(targetLanguagesRect, targetLanguagesProp,
                    availableLanguages, sourceLanguageProp.stringValue);
                currentY += targetLanguagesHeight + Spacing;

                // Other settings
                var normalizeRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(normalizeRect, property.FindPropertyRelative("normalizeStrings"));
                currentY += lineHeight;

                var uiModeRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(uiModeRect, property.FindPropertyRelative("uiMode"), new GUIContent("UI Mode"));
                currentY += lineHeight;

                var glossaryRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(glossaryRect, property.FindPropertyRelative("glossaryId"));
                currentY += lineHeight;

                var promptRect = new Rect(position.x, currentY, position.width,
                    EditorGUI.GetPropertyHeight(property.FindPropertyRelative("customPrompt")));
                EditorGUI.PropertyField(promptRect, property.FindPropertyRelative("customPrompt"));

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            var availableLanguages = GetAvailableLanguages(property);
            var targetLanguagesProp = property.FindPropertyRelative("targetLanguages");
            var sourceLanguage = property.FindPropertyRelative("sourceLanguage").stringValue;

            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Foldout
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Source language
            height += GetTargetLanguagesHeight(targetLanguagesProp, availableLanguages, sourceLanguage) +
                      Spacing; // Target languages
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Normalize strings
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // UI mode
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Glossary ID
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("customPrompt")); // Custom prompt

            return height;
        }

        private void DrawSourceLanguageDropdown(Rect rect, SerializedProperty sourceLanguageProp,
            List<string> availableLanguages)
        {
            var sourceOptions = new List<string> { "Auto" };
            sourceOptions.AddRange(availableLanguages);

            var currentIndex = sourceOptions.IndexOf(sourceLanguageProp.stringValue);
            if (currentIndex == -1) currentIndex = 0; // Default to "Auto"

            var newIndex = EditorGUI.Popup(rect, "Source Language", currentIndex, sourceOptions.ToArray());
            if (newIndex != currentIndex) sourceLanguageProp.stringValue = sourceOptions[newIndex];
        }

        private float DrawTargetLanguagesDropdown(Rect rect, SerializedProperty targetLanguagesProp,
            List<string> availableLanguages, string sourceLanguage)
        {
            // Filter out the source language (unless it's "Auto")
            var targetOptions = availableLanguages.Where(lang => sourceLanguage == "Auto" || lang != sourceLanguage)
                .ToList();

            // Get current selection
            var currentTargets = new List<string>();
            for (var i = 0; i < targetLanguagesProp.arraySize; i++)
                currentTargets.Add(targetLanguagesProp.GetArrayElementAtIndex(i).stringValue);

            // Show current selection as display text
            var selectionLabel = currentTargets.Count == 0 || (currentTargets.Count == 1 && currentTargets[0] == "")
                ? "All Languages"
                : string.Join(", ", currentTargets);

            if (selectionLabel.Length > 40)
                selectionLabel = selectionLabel.Substring(0, 37) + "...";

            // Create a proper popup-style button
            var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
            var buttonRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                rect.width - EditorGUIUtility.labelWidth, rect.height);

            EditorGUI.LabelField(labelRect, "Target Languages");

            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(selectionLabel), FocusType.Keyboard))
                ShowTargetLanguageMenu(targetLanguagesProp, targetOptions);

            return EditorGUIUtility.singleLineHeight;
        }

        private float GetTargetLanguagesHeight(SerializedProperty targetLanguagesProp, List<string> availableLanguages,
            string sourceLanguage)
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private void ShowTargetLanguageMenu(SerializedProperty targetLanguagesProp, List<string> targetOptions)
        {
            var menu = new GenericMenu();

            // Get current selection
            var currentTargets = new HashSet<string>();
            for (var i = 0; i < targetLanguagesProp.arraySize; i++)
                currentTargets.Add(targetLanguagesProp.GetArrayElementAtIndex(i).stringValue);

            // Add "All Languages" option
            var allSelected = currentTargets.Count == 0 || (currentTargets.Count == 1 && currentTargets.Contains(""));
            menu.AddItem(new GUIContent("All Languages"), allSelected, () =>
            {
                targetLanguagesProp.ClearArray();
                targetLanguagesProp.serializedObject.ApplyModifiedProperties();
            });

            menu.AddSeparator("");

            // Add individual language options
            foreach (var language in targetOptions)
            {
                var isSelected = currentTargets.Contains(language);
                menu.AddItem(new GUIContent(language), isSelected, () =>
                {
                    if (isSelected)
                    {
                        // Remove language
                        for (var i = targetLanguagesProp.arraySize - 1; i >= 0; i--)
                            if (targetLanguagesProp.GetArrayElementAtIndex(i).stringValue == language)
                            {
                                targetLanguagesProp.DeleteArrayElementAtIndex(i);
                                break;
                            }
                    }
                    else
                    {
                        // Add language
                        targetLanguagesProp.arraySize++;
                        targetLanguagesProp.GetArrayElementAtIndex(targetLanguagesProp.arraySize - 1).stringValue =
                            language;
                    }

                    targetLanguagesProp.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        private List<string> GetAvailableLanguages(SerializedProperty property)
        {
            var languages = new List<string>();

            // Try to get languages from all StringTableCollections in the project
            var stringTableCollections = AssetDatabase.FindAssets("t:StringTableCollection");

            foreach (var guid in stringTableCollections)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var collection = AssetDatabase.LoadAssetAtPath<StringTableCollection>(path);

                if (collection != null)
                    foreach (var table in collection.StringTables)
                    {
                        var localeCode = table.LocaleIdentifier.Code;
                        if (!languages.Contains(localeCode)) languages.Add(localeCode);
                    }
            }

            // Fallback to common languages if none found
            if (languages.Count == 0) languages.AddRange(new[] { "en", "es", "fr", "de", "ja", "ko" });

            return languages.OrderBy(l => l).ToList();
        }
    }
}