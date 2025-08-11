using System;
using UnityEngine;
using UnityEngine.Localization.Metadata;

namespace Algebras.Localization
{
    /// <summary>
    ///     Runtime metadata for storing translation information in localization entries.
    ///     This can be attached to StringTableEntry objects to track translation details.
    /// </summary>
    [Serializable]
    [Metadata(AllowedTypes = MetadataType.StringTableEntry)]
    public class AlgebrasTranslationMetadata : IMetadata
    {
        [SerializeField] private float m_Confidence;
        [SerializeField] private string m_Model;
        [SerializeField] private string m_Timestamp;
        [SerializeField] private bool m_NeedsReview;

        /// <summary>
        ///     Creates a new instance with default values.
        /// </summary>
        public AlgebrasTranslationMetadata()
        {
            m_Confidence = 0f;
            m_Model = string.Empty;
            m_Timestamp = string.Empty;
            m_NeedsReview = false;
        }

        /// <summary>
        ///     Creates a new instance with specified values.
        /// </summary>
        /// <param name="confidence">Translation confidence score.</param>
        /// <param name="model">AI model used.</param>
        /// <param name="timestamp">Translation timestamp.</param>
        /// <param name="needsReview">Whether review is needed.</param>
        public AlgebrasTranslationMetadata(float confidence, string model, string timestamp, bool needsReview = false)
        {
            Confidence = confidence;
            Model = model;
            Timestamp = timestamp;
            NeedsReview = needsReview;
        }

        /// <summary>
        ///     AI confidence score for this translation (0-1).
        /// </summary>
        public float Confidence
        {
            get => m_Confidence;
            set => m_Confidence = Mathf.Clamp01(value);
        }

        /// <summary>
        ///     Name of the AI model used for translation.
        /// </summary>
        public string Model
        {
            get => m_Model;
            set => m_Model = value ?? string.Empty;
        }

        /// <summary>
        ///     Timestamp when the translation was generated.
        /// </summary>
        public string Timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value ?? string.Empty;
        }

        /// <summary>
        ///     Whether this translation needs human review.
        /// </summary>
        public bool NeedsReview
        {
            get => m_NeedsReview;
            set => m_NeedsReview = value;
        }

        /// <summary>
        ///     Updates the metadata with new translation information.
        /// </summary>
        /// <param name="confidence">New confidence score.</param>
        /// <param name="model">AI model used.</param>
        /// <param name="timestamp">Translation timestamp.</param>
        public void UpdateTranslation(float confidence, string model, string timestamp)
        {
            Confidence = confidence;
            Model = model;
            Timestamp = timestamp;
            NeedsReview = confidence < 0.7f; // Auto-flag low confidence for review
        }

        /// <summary>
        ///     Marks this translation as reviewed by a human.
        /// </summary>
        public void MarkAsReviewed()
        {
            NeedsReview = false;
        }

        /// <summary>
        ///     Gets a human-readable description of the translation quality.
        /// </summary>
        /// <returns>Quality description string.</returns>
        public string GetQualityDescription()
        {
            return Confidence switch
            {
                >= 0.9f => "High Quality",
                >= 0.7f => "Good Quality",
                >= 0.5f => "Medium Quality",
                _ => "Low Quality"
            };
        }

        /// <summary>
        ///     Returns a string representation of this metadata.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            return $"AlgebrasTranslation: {GetQualityDescription()} ({Confidence:P1}) - {Model} - {Timestamp}";
        }
    }
}