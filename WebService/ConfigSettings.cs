namespace WebService
{
    using System.Fabric;
    using System.Fabric.Description;

    public class ConfigSettings
    {
        public string RequestsServiceName { get; private set; }

        public string MatchmakerServiceName { get; private set; }

        public string SimulationServiceName { get; private set; }

        public int ReverseProxyPort { get; private set; }

        public ConfigSettings(StatelessServiceContext serviceContext)
        {
            serviceContext.CodePackageActivationContext.ConfigurationPackageModifiedEvent += CodePackageActivationContext_ConfigurationPackageModifiedEvent;
            UpdateConfigSettings(serviceContext.CodePackageActivationContext.GetConfigurationPackageObject("Config").Settings);
        }


        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            UpdateConfigSettings(e.NewPackage.Settings);
        }

        private void UpdateConfigSettings(ConfigurationSettings settings)
        {
            ConfigurationSection section = settings.Sections["MyConfigSection"];
            RequestsServiceName = section.Parameters["RequestsServiceName"].Value;
            MatchmakerServiceName = section.Parameters["MatchmakerServiceName"].Value;
            SimulationServiceName = section.Parameters["SimulationServiceName"].Value;
            ReverseProxyPort = int.Parse(section.Parameters["ReverseProxyPort"].Value);
        }
    }
}
