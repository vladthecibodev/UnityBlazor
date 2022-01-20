namespace Assets.Editor.Nuget
{
    using System.IO;
    using System.Xml.Linq;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEngine;

    internal static class NugetConfigFile
    {
        public static bool isLoaded = false;
        // The loaded repositoryPath from nuget.config without the leading "./"
        // e.g. "Packages/UniBlazorCore/Plugins"
        public static string repositoryPath = string.Empty;
        // Just the folder name of the package
        // e.g. "UniBlazorCore"
        public static string packageFolderName = string.Empty;

        public static void LoadNugetConfig()
        {
            XDocument nugetConfig = LoadConfigFile("nuget.config");

            // Get the <add /> element containing the "repositoryPath" key
            var repositoryPathQ =
                from addEl in nugetConfig.Root.Element("config").Elements("add")
                where addEl.Attribute("key").Value == "repositoryPath"
                select addEl;

            try
            {
                repositoryPath = repositoryPathQ.First().Attribute("value").Value;
            }
            catch
            {
                Debug.LogError("nuget.config does not contain a \"repositoryPath\" configuration element");
                return;
            }

            var regexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;
            Regex startRegex = new(@"^\./", regexOptions);
            Regex packageNameRegex = new(@"^Packages/(\w+)/\w");

            repositoryPath = startRegex.Replace(repositoryPath, "");
            packageFolderName = packageNameRegex.Match(repositoryPath).Groups.Last().Value;

            isLoaded = true;
        }

        // Gets the array containing which nuget packages defined in packages.config
        // Which are installed in the project
        public static NugetPackageItem[] LoadListFromPackagesConfig()
        {
            XDocument packagesConfig = LoadConfigFile("packages.config");

            // Select all XML "packages" elements describing a Nuget package
            return (
                from package in packagesConfig.Root.Elements("package")
                select new NugetPackageItem()
                {
                    id = package.Attribute("id").Value,
                    version = package.Attribute("version").Value,
                }
            ).ToArray();
        }

        private static XDocument LoadConfigFile(string filename) => XDocument.Load(
            Path.Combine(Application.dataPath, $"../{filename}")
        );
    }
}
