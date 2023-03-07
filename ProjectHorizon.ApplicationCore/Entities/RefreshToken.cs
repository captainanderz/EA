using System;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class RefreshToken : BaseEntity
    {
        public virtual ApplicationUser ApplicationUser { get; set; }

        public string ApplicationUserId { get; set; }

        public DateTime ExpiresOn { get; set; }

        public int Id { get; set; }

        public bool IsActive => RevokedOn == null && !IsExpired;

        public bool IsExpired => DateTime.UtcNow >= ExpiresOn;

        public string? ReplacedByToken { get; set; }

        public DateTime? RevokedOn { get; set; }

        public string Token { get; set; }
    }
}