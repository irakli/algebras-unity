using UnityEngine;
using UnityEditor;

namespace Algebras.Localization.Editor
{
    /// <summary>
    /// Custom editor for AlgebrasServiceProvider with OpenAI warning.
    /// </summary>
    [CustomEditor(typeof(AlgebrasServiceProvider))]
    public class AlgebrasServiceProviderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var serviceProvider = (AlgebrasServiceProvider)target;
            serializedObject.Update();

            // Draw properties manually to control visibility
            EditorGUILayout.PropertyField(serializedObject.FindProperty("apiKey"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("authenticationType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("provider"));

            // Only show model field for OpenAI
            if (serviceProvider.Provider == AlgebrasProvider.OpenAI)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("model"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("applicationName"));
            
            // Draw API settings (only for OpenAI)
            if (serviceProvider.Provider == AlgebrasProvider.OpenAI)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("apiSettings"));
            }
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("batchSettings"));

            // Show error if OpenAI is selected
            if (serviceProvider.Provider == AlgebrasProvider.OpenAI)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "OpenAI provider is not yet available.\nUse Algebras AI for now.",
                    MessageType.Error
                );
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}