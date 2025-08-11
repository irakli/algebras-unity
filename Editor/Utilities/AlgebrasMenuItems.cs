using Algebras.Localization.Editor.ServiceProvider;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Algebras.Localization.Editor.Utilities
{
    /// <summary>
    ///     Menu items for Algebras Localization package.
    /// </summary>
    public static class AlgebrasMenuItems
    {
        private const string MenuRoot = "Assets/Create/Localization/";
        private const string WindowMenuRoot = "Window/Algebras Localization/";

        /// <summary>
        ///     Creates a new Algebras Service Provider asset.
        /// </summary>
        [MenuItem(MenuRoot + "Algebras Service")]
        public static void CreateAlgebrasServiceProvider()
        {
            var asset = ScriptableObject.CreateInstance<AlgebrasServiceProvider>();

            var path = EditorUtility.SaveFilePanelInProject(
                "Create Algebras Service",
                "AlgebrasService",
                "asset",
                "Create a new Algebras Service Provider asset");

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);

                Debug.Log($"Created Algebras Service Provider at {path}");
            }
            else
            {
                Object.DestroyImmediate(asset);
            }
        }

        /// <summary>
        ///     Opens the Algebras Documentation.
        /// </summary>
        [MenuItem(WindowMenuRoot + "Documentation")]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://docs.algebras.ai/unity");
        }

        /// <summary>
        ///     Opens the Algebras Dashboard.
        /// </summary>
        [MenuItem(WindowMenuRoot + "Dashboard")]
        public static void OpenDashboard()
        {
            Application.OpenURL("https://dashboard.algebras.ai");
        }

        /// <summary>
        ///     Shows information about the Algebras Localization package.
        /// </summary>
        [MenuItem(WindowMenuRoot + "About")]
        public static void ShowAbout()
        {
            var packageInfo =
                PackageInfo.FindForAssetPath("Packages/com.algebras.localization");
            var version = packageInfo?.version ?? "Unknown";

            var message = "Algebras Localization for Unity\n\n" +
                          $"Version: {version}\n" +
                          "AI-powered localization extension for Unity's Localization package.\n\n" +
                          "Features:\n" +
                          "• Automatic AI translation using Algebras AI or OpenAI\n" +
                          "• Batch processing with progress tracking\n" +
                          "• Smart caching to reduce API costs\n" +
                          "• Translation confidence scoring\n" +
                          "• Review workflow for quality control\n\n";

            EditorUtility.DisplayDialog("About Algebras Localization", message, "OK");
        }
    }
}