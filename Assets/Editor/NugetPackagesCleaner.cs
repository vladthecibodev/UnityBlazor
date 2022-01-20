using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Assets.Editor.Nuget
{
    // Cleans installed nuget packages s.t. they work with Unity
    // Inspired (read "shameless code snatching") by GlitchEnzo's NuGetForUnity project
    // https://github.com/GlitchEnzo/NuGetForUnity/blob/master/Assets/NuGet/Editor/NugetHelper.cs
    internal class NugetPackagesCleanUp
    {
        // The Asset path where all nuget packages live
        private readonly string nugetPkgsAssetPath;
        // The NugetPackage to be cleaned
        private readonly NugetPackageItem nugetPackage;

        public NugetPackagesCleanUp(string nugetPkgsAssetPath, NugetPackageItem nugetPackage)
        {
            this.nugetPkgsAssetPath = nugetPkgsAssetPath;
            this.nugetPackage = nugetPackage;
        }

        public void CleanPackageFolder()
        {
            var pkgName = nugetPackage.PkgNameFormatted;
            var packageAssetPath = $"{nugetPkgsAssetPath}/{pkgName}";

            var assetRemove = AssetRemover(packageAssetPath);

            // Unity has no use for the build directory
            assetRemove("build");
            assetRemove("src");

            // No support for runtime dll platforms
            assetRemove("runtimes");

            // Delete documentation folders
            assetRemove("docs");
            assetRemove("documentation");

            assetRemove("editorconfig");

            // Delete ref folder, as it is just used for compile-time reference and does not contain implementations.
            // Leaving it results in "assembly loading" and "multiple pre-compiled assemblies with same name" errors
            assetRemove("ref");

            // No ideea what to do with tools
            assetRemove("tools");

            assetRemove("package");
            assetRemove("_rels");

            // Remove the included .nupkg of this package
            assetRemove($"{pkgName}.nupkg");
            // If any .nuspec escapes
            assetRemove($"{nugetPackage.id}.nuspec");

            assetRemove("Icon.png");
            assetRemove("useSharedDesignerContext.txt");

            CleanAssemblies(packageAssetPath);
        }

        public void CleanAssemblies(string packageAssetPath)
        {
            CleanLib(packageAssetPath);
            CleanAnalyzers(packageAssetPath);
            RemoveLocalizations(packageAssetPath);
        }

        private void CleanLib(string packageAssetPath)
        {
            var libPath = $"{packageAssetPath}/lib";
            if (!AssetDatabase.IsValidFolder(libPath))
            {
                return;
            }

            // Gets the full paths of the subFolders Assets
            var fwDirsPaths = AssetDatabase.GetSubFolders(libPath);
            // Strip the path from the Asset Paths -> keep only the folder name
            var fwDirectories = fwDirsPaths.Select(path => path.Substring(path.LastIndexOf('/') + 1));

            // Pick only the netstandard folders
            var netstandardDirs = fwDirectories
                .Where(dirname => dirname.StartsWith("netstandard"))
                .OrderByDescending(dirname => dirname)
                .Take(2);

            if (!netstandardDirs.Any())
            {
                Debug.LogError($"There is no netstandard folder for nuget package's lib at\n{libPath}");
                return;
            }

            // If the netstandard2.0 (or lower) is available and we picked 2.1 instead
            // then prefer netstandard2.0 for now
            var pickedNetstandardDir = netstandardDirs.First();
            if (pickedNetstandardDir.EndsWith("2.1") && netstandardDirs.Count() == 2)
            {
                pickedNetstandardDir = netstandardDirs.Skip(1).First();
            }
            
            // Remove all the other framework folders, other than the picked one
            foreach (var fwDir in fwDirsPaths)
            {
                if (fwDir.EndsWith(pickedNetstandardDir))
                {
                    continue;
                }

                AssetDatabase.DeleteAsset(fwDir);
            }
        }

        private void CleanAnalyzers(string packageAssetPath)
        {
            var analyzerPath = $"{packageAssetPath}/analyzers/dotnet";
            if (!AssetDatabase.IsValidFolder(analyzerPath))
            {
                return;
            }

            // Delete the Visual Basic folder
            var vbPath = $"{analyzerPath}/vb";
            if (AssetDatabase.IsValidFolder(vbPath))
            {
                AssetDatabase.DeleteAsset(vbPath);
            }
        }

        // Remove all *.resources.dll for localized assemblies
        // As Unity will complain about multiple assemblies with the same name
        private void RemoveLocalizations(string packageAssetPath)
        {
            var localizedDllGuids = AssetDatabase.FindAssets(".resources", new[] { packageAssetPath });
            // Unity does not like appending the format to the searched asset name
            // so make sure the filenames are ending in resources.dll
            var localizedParentFolders = localizedDllGuids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => path.EndsWith(".resources.dll"))
                // And map the Asset's Paths to their parent folders' Asset Paths
                .Select(path => path.Substring(0, path.LastIndexOf("/")));

            // Delete all parent folders that contain localized assemblies
            foreach (var parentFolder in localizedParentFolders)
            {
                AssetDatabase.DeleteAsset(parentFolder);
            }
        }

        private Action<string> AssetRemover(string packageAssetPath) => 
            (relativePath) => AssetDatabase.DeleteAsset($"{packageAssetPath}/{relativePath}");
    }
}
