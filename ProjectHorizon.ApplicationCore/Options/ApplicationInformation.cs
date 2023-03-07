namespace ProjectHorizon.ApplicationCore.Options
{
    public class ApplicationInformation
    {
        public string TermsVersion { get; set; } = string.Empty;

        public bool AskForPaymentAfterRegister { get; set; } = true;

        public string ClientId { get; set; } = string.Empty;

        public string ApiScope { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;
    }
}
