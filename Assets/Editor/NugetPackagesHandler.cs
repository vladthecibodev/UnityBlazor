using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Threading.Tasks;
using static System.Array;

namespace Assets.Editor.Nuget
{
    public class NugetPackagesHandler : AssetPostprocessor
    {
        // The repositoryPath from the nuget.config, but with the real folder name
        // replaced by the package.json name of the custom package
        private static string nugetRepoAssetPath = string.Empty;
        // The real repository path
        // private static string nugetRepoPhysicalPath = string.Empty;

        private static NugetPackageItem[] allNugetPackages = Empty<NugetPackageItem>();

        public async static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] _deleted,
            string[] _moved,
            string[] _movedTo
        )
        {
            if (!await TryLoadNugetPkgData())
            {
                return;
            }

            // Select all those nuget package names that contain reimported assets
            // We consider them as reimported, so we need to clean the contents
            var reimportedNugetPackages = (
                from importedAsset in importedAssets
                let nugetPkg = ReimportedAssetPathToNugetPkg(importedAsset)
                where nugetPkg != null
                select (NugetPackageItem)nugetPkg
            ).Distinct().ToList();

            foreach (var pkg in reimportedNugetPackages)
            {
                Debug.Log($"Reimported Nuget Pkg: {pkg.PkgNameFormatted}");
                new NugetPackagesCleanUp(
                    nugetRepoAssetPath,
                    pkg
                ).CleanPackageFolder();
            }

            AssetDatabase.Refresh();
        }

        private static NugetPackageItem? ReimportedAssetPathToNugetPkg(string assetPath)
        {
            foreach (var nugetPkg in allNugetPackages)
            {
                if (assetPath.StartsWith($"{nugetRepoAssetPath}/{nugetPkg.PkgNameFormatted}"))
                {
                    return nugetPkg;
                }
            }

            return null;
        }

        // Loads the Nuget Repo paths from nuget.config
        // And also the list of all installed Nuget packages from packages.config
        private static async Task<bool> TryLoadNugetPkgData()
        {
            if (!await TryLoadNugetRepoPath())
            {
                return false;
            }

            // The list of nuget packages can change before any reimport as new nuget packages can be installed
            allNugetPackages = NugetConfigFile.LoadListFromPackagesConfig();

            return true;
        }

        private static async Task<bool> TryLoadNugetRepoPath()
        {
            // If the nuget.config file was not loaded yet, try to load it
            if (NugetConfigFile.isLoaded)
            {
                return true;
            }

            NugetConfigFile.LoadNugetConfig();

            // If the loading of the nuget config failed, then it must be non-existent
            if (!NugetConfigFile.isLoaded)
            {
                return false;
            }

            // The package info for the embedded Unity one that's targeted for nuget package installation
            var pkgWithNuget = await UnityPackageEmbedded.GetUnityPkgHoldingNugetFiles(
                NugetConfigFile.packageFolderName
            );

            // "Packages/UniBlazorCore/Plugins"
            // nugetRepoPhysicalPath = NugetConfigFile.repositoryPath;

            // Replace in "Packages/UniBlazorCore/Plugins" the real folder name e.g "UniBlazorCore"
            // With the name of the package from its package.json e.g. "com.cibodevz.uniblazor"
            // -> this is the asset path to the package that Unity uses
            nugetRepoAssetPath = NugetConfigFile.repositoryPath.Replace(
                NugetConfigFile.packageFolderName,
                pkgWithNuget.name
            );

            return true;
        }
    }
}
