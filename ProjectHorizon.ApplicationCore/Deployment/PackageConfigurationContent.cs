using System.Xml.Serialization;

namespace ProjectHorizon.ApplicationCore.Deployment
{
    [XmlRoot("Configuration")]
    public class PackageConfigurationContent
    {
        [XmlElement("Name")]
        public string Name { get; set; }
        [XmlElement("Icon")]
        public string Icon { get; set; }
        [XmlElement("Version")]
        public string Version { get; set; }
        [XmlElement("Publisher")]
        public string Publisher { get; set; }
        [XmlElement("InformationUrl")]
        public string InformationUrl { get; set; }
        [XmlElement("RunAs32Bit")]
        public bool RunAs32Bit { get; set; }
        [XmlElement("Language")]
        public string Language { get; set; }
        [XmlElement("Architecture")]
        public string Architecture { get; set; }
        [XmlElement("Notes")]
        public string Notes { get; set; }
        [XmlElement("Developer")]
        public string Developer { get; set; }
        [XmlElement("Path")]
        public string Path { get; set; }
        [XmlElement("DetectionScript")]
        public string DetectionScript { get; set; }
        [XmlElement("InstallCmd")]
        public string InstallCmd { get; set; }
        [XmlElement("UninstallCmd")]
        public string UninstallCmd { get; set; }
        [XmlElement("InstallExperience")]
        public string InstallExperience { get; set; }
        [XmlElement("IsFeatured")]
        public bool IsFeatured { get; set; }
        [XmlElement("Owner")]
        public string Owner { get; set; }
        [XmlElement("PrivacyUrl")]
        public string PrivacyUrl { get; set; }
        [XmlElement("Description")]
        public string Description { get; set; }
        public byte[] IconContent { get; set; }
    }
}
