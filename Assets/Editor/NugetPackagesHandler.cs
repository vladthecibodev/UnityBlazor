using UnityEngine;
using UnityEditor;

namespace UniBlazorCore.Internal.Nuget
{
    public class NugetPackagesHandler : AssetPostprocessor
    {
        // The repositoryPath from the nuget.config, but with the real folder name
        // replaced by the package.json name of the custom package
        public static string nugetRepoAssetPath = string.Empty;
        public async static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            // If the nuget.config file was not loaded yet, try to load it
            if (!NugetConfigFile.isLoaded)
            {
                NugetConfigFile.LoadNugetConfig();

                // If the loading of the nuget config failed, then it must be non-existent
                if (!NugetConfigFile.isLoaded)
                {
                    return;
                }

                // The package info for the embedded Unity one that's targeted for nuget package installation
                var pkgWithNuget = await UnityPackageEmbedded.GetUnityPkgHoldingNugetFiles(NugetConfigFile.packageFolderName);
                nugetRepoAssetPath = NugetConfigFile.repositoryPath.Replace(NugetConfigFile.packageFolderName, pkgWithNuget.name);

                Debug.Log(nugetRepoAssetPath);
            }

            foreach (string str in importedAssets)
            {
                Debug.Log("Reimported Asset: " + str);
            }
            foreach (string str in deletedAssets)
            {
                Debug.Log($"Deleted Asset: {str}");
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
            }
        }

        
    }
}
