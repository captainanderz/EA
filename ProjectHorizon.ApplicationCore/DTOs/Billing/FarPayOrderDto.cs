namespace ProjectHorizon.ApplicationCore.DTOs.Billing
{
    public class FarPayOrderDto
    {
        public string ExternalID { get; set; }

        public string AcceptUrl { get; set; }

        public string CancelUrl { get; set; }

        public string CallbackUrl { get; set; }

        public string Lang { get; set; }

        public string PaymentTypes { get; set; }

        public int Agreement { get; set; }

        public Customer Customer { get; set; }
    }
}
