using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class SubscriptionConsent : BaseEntity
    {
        public long Id { get; set; }

        public Guid SubscriptionId { get; set; }

        public string TenantId { get; set; } = null!;

        public virtual Subscription Subscription { get; set; } = null!;
    }
}
