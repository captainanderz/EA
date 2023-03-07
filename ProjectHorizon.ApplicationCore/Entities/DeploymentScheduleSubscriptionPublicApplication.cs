using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class DeploymentScheduleSubscriptionPublicApplication : DeploymentScheduleApplication
    {
        public Guid SubscriptionId { get; set; }

        public virtual Subscription Subscription { get; set; } = null!;

        public virtual SubscriptionPublicApplication Application { get; set; }
    }
}
