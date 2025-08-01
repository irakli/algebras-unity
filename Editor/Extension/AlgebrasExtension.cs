using System;
using UnityEngine;
using UnityEditor.Localization;

namespace Algebras.Localization.Editor
{
    /// <summary>
    /// Provides an editor interface to Algebras translation service.
    /// Allows syncing localization data with AI translation services.
    /// </summary>
    [Serializable]
    [StringTableCollectionExtension]
    public class AlgebrasExtension : CollectionExtension
    {
        [SerializeField]
        private AlgebrasServiceProvider serviceProvider;
        [SerializeField] private AlgebrasTableSettings tableSettings = new AlgebrasTableSettings();

        /// <summary>
        /// The service provider that handles authentication and API communication.
        /// </summary>
        public AlgebrasServiceProvider ServiceProvider
        {
            get => serviceProvider;
            set => serviceProvider = value;
        }

        /// <summary>
        /// Settings specific to this table/collection.
        /// </summary>
        public AlgebrasTableSettings TableSettings => tableSettings;
    }

    /// <summary>
    /// Settings specific to a table/collection for translation.
    /// </summary>
    [Serializable]
    public class AlgebrasTableSettings
    {
        [SerializeField, Tooltip("Remove escaped characters like \\n, \\' from translations when they weren't in the source")]
        private bool normalizeStrings = true;

        [SerializeField, Tooltip("UI-safe mode: ensures translations won't be longer than original text (good for buttons, labels)")]
        private bool uiMode = false;

        [SerializeField, TextArea(3, 10), Tooltip("Custom instructions for the AI translator specific to this table (e.g., 'Use formal tone for UI buttons')")]
        private string customPrompt = string.Empty;

        [SerializeField, Tooltip("ID of a glossary for consistent terminology across translations")]
        private string glossaryId = string.Empty;

        [SerializeField, Tooltip("Source language for translation. 'Auto' lets the AI detect the language automatically")]
        private string sourceLanguage = "Auto";

        [SerializeField, Tooltip("Target languages to translate into. Leave empty to translate to all available languages")]
        private string[] targetLanguages = new string[0];

        /// <summary>
        /// Whether to normalize escaped characters in translations.
        /// </summary>
        public bool NormalizeStrings { get => normalizeStrings; set => normalizeStrings = value; }

        /// <summary>
        /// Whether to use UI-safe mode (preserve text length).
        /// </summary>
        public bool UIMode { get => uiMode; set => uiMode = value; }

        /// <summary>
        /// Custom prompt to use for translations of this table.
        /// </summary>
        public string CustomPrompt { get => customPrompt; set => customPrompt = value ?? string.Empty; }

        /// <summary>
        /// Glossary ID for consistent terminology in this table.
        /// </summary>
        public string GlossaryId { get => glossaryId; set => glossaryId = value ?? string.Empty; }

        /// <summary>
        /// Source language for translation. "Auto" for automatic detection.
        /// </summary>
        public string SourceLanguage { get => sourceLanguage; set => sourceLanguage = value ?? "Auto"; }

        /// <summary>
        /// Target languages to translate into. Empty array means translate to all available languages.
        /// </summary>
        public string[] TargetLanguages { get => targetLanguages; set => targetLanguages = value ?? new string[0]; }
    }
}