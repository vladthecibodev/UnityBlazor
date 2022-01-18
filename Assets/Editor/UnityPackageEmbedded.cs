namespace UniBlazorCore.Internal.Nuget
{
    using System.Linq;
    using UnityEngine;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEditor.PackageManager;
    using UnityEditor.PackageManager.Requests;

    using PkgInfo = UnityEditor.PackageManager.PackageInfo;

    internal static class UnityPackageEmbedded
    {
        private static ListRequest listReq;
        private readonly static TaskCompletionSource<PkgInfo> taskCompletionSource;

        private static string packageWithNugetFolder = string.Empty;

        static UnityPackageEmbedded() => taskCompletionSource = new();

        /// <summary>
        ///     Get the packageInfo corresponding to the custom embedded pkg,
        ///     having the package folder name provided as argument
        /// </summary>
        /// 
        /// <param name="pkgFolderName">the folder name of the custom pkg, sitting in ./Packages</param>
        public static Task<PkgInfo> GetUnityPkgHoldingNugetFiles(string pkgFolderName)
        {
            packageWithNugetFolder = pkgFolderName;

            listReq = Client.List(offlineMode: true, includeIndirectDependencies: false);
            EditorApplication.update += OnUnityProgress;

            return taskCompletionSource.Task;
        }

        private static void OnUnityProgress()
        {
            if (!listReq.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= OnUnityProgress;

            var packageSearchFailure = new System.Exception($"Could not get embbeded pkg at {packageWithNugetFolder}");

            if (listReq.Status == StatusCode.Failure)
            {
                Debug.LogError($"Listing the UPM packages failed. Reason:\n${listReq.Error.message}");
                taskCompletionSource.SetException(packageSearchFailure);

                return;
            }

            var pkgCollection = listReq.Result;

            try
            {
                var pkgWithNugetFiles =
                    (
                        from pkg in pkgCollection
                        where pkg.resolvedPath.EndsWith(packageWithNugetFolder)
                        select pkg
                    ).First();

                taskCompletionSource.SetResult(pkgWithNugetFiles);
            }
            catch
            {
                Debug.LogError($"Could not find the embedded package at folder = ${packageWithNugetFolder}");
                taskCompletionSource.SetException(packageSearchFailure);
            }
        }
    }
}
