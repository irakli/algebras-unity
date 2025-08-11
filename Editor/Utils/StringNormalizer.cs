using System.Collections.Generic;

namespace Algebras.Localization.Editor.Utils
{
    /// <summary>
    ///     Utility class for normalizing translated strings by removing unnecessary escape characters.
    ///     Based on the Algebras CLI normalization logic.
    /// </summary>
    public static class StringNormalizer
    {
        /// <summary>
        ///     Common escaped characters that may be unnecessarily added by translation APIs.
        /// </summary>
        private static readonly Dictionary<string, string> EscapeMappings = new()
        {
            { "\\'", "'" }, // Escaped apostrophe → apostrophe
            { "\\\"", "\"" }, // Escaped quote → quote  
            { "\\\\", "\\" }, // Escaped backslash → backslash
            { "\\n", "\n" }, // Escaped newline → actual newline
            { "\\t", "\t" }, // Escaped tab → actual tab
            { "\\r", "\r" } // Escaped carriage return → actual carriage return
        };

        /// <summary>
        ///     Normalize a translated string by removing escaped characters that weren't present in the source text.
        ///     This helps fix over-escaping that can occur in AI translations.
        /// </summary>
        /// <param name="sourceText">The original source text</param>
        /// <param name="translatedText">The translated text from the API</param>
        /// <param name="isEnabled">Whether normalization is enabled (from settings)</param>
        /// <returns>Normalized translated text</returns>
        public static string NormalizeTranslation(string sourceText, string translatedText, bool isEnabled = true)
        {
            // Skip normalization if disabled
            if (!isEnabled)
                return translatedText;

            // Skip if either text is null or empty
            if (string.IsNullOrEmpty(sourceText) || string.IsNullOrEmpty(translatedText))
                return translatedText;

            var normalizedText = translatedText;

            // Process each escape mapping
            foreach (var mapping in EscapeMappings)
            {
                var escapedChar = mapping.Key;
                var unescapedChar = mapping.Value;

                // Only normalize if:
                // 1. The escaped version exists in the translated text
                // 2. The escaped version does NOT exist in the source text
                // This prevents removing intentional escaping from the source
                if (normalizedText.Contains(escapedChar) && !sourceText.Contains(escapedChar))
                    normalizedText = normalizedText.Replace(escapedChar, unescapedChar);
            }

            return normalizedText;
        }

        /// <summary>
        ///     Normalize an array of translated texts.
        /// </summary>
        /// <param name="sourceTexts">The original source texts</param>
        /// <param name="translatedTexts">The translated texts from the API</param>
        /// <param name="isEnabled">Whether normalization is enabled</param>
        /// <returns>Array of normalized translated texts</returns>
        public static string[] NormalizeTranslations(string[] sourceTexts, string[] translatedTexts,
            bool isEnabled = true)
        {
            if (!isEnabled || sourceTexts == null || translatedTexts == null)
                return translatedTexts;

            var normalizedTexts = new string[translatedTexts.Length];

            for (var i = 0; i < translatedTexts.Length; i++)
            {
                var sourceText = i < sourceTexts.Length ? sourceTexts[i] : "";
                normalizedTexts[i] = NormalizeTranslation(sourceText, translatedTexts[i]);
            }

            return normalizedTexts;
        }
    }
}