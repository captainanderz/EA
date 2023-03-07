namespace ProjectHorizon.ApplicationCore.Deployment
{
    public class PackageInfo
    {
        public PackageConfigurationContent PackageConfigurationContent { get; set; }

        public string DetectApplicationPowerShellScriptContent { get; set; }

        public string RequirementPowerShellScriptContent { get; set; }
    }
}
