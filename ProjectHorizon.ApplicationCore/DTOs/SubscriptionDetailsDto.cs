using System;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class SubscriptionDetailsDto : BillingInfoDto
    {
        public Guid SubscriptionId { get; set; }

        public string State { get; set; }

        public string CreditCardDigits { get; set; }

        public int? DeviceCount { get; set; }

        public decimal DueAmount { get; set; }

        public DateTime DueDate { get; set; }

        public int DaysUntilPayment { get; set; }

        public string LogoSmall { get; set; } = string.Empty;

        public string ShopGroupPrefix { get; set; } = string.Empty;
    }
}
