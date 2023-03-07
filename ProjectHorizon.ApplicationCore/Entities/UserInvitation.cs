using System;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class UserInvitation : BaseEntity
    {
        public virtual ApplicationUser? ApplicationUser { get; set; }

        public string? ApplicationUserId { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public int Id { get; set; }

        public string InvitationToken { get; set; }

        public string LastName { get; set; }

        public virtual Subscription Subscription { get; set; }

        public Guid SubscriptionId { get; set; }

        public bool UserHasRegistered { get; set; }

        public string UserRole { get; set; }
    }
}