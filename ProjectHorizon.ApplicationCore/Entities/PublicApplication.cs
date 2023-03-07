using System.Collections.Generic;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class PublicApplication : Application
    {
        public string? PackageCacheFolderName { get; set; }

        public string? PublicRepositoryArchiveFileName { get; set; }

        //public virtual ICollection<SubscriptionPublicApplication> SubscriptionPublicApplications { get; set; }
    }
}