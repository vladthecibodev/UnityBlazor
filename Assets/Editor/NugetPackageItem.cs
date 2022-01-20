namespace Assets.Editor.Nuget
{
    // A representataion of the installed nuget packages found in packages.config
    internal struct NugetPackageItem
    {
        public string id;
        public string version;

        public string PkgNameFormatted => $"{id}.{version}";
    };
}
