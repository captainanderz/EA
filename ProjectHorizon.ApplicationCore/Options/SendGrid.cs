namespace ProjectHorizon.ApplicationCore.Options
{
    public class SendGrid
    {
        public string ApiKey { get; init; }

        public string DefaultFromEmail { get; init; }

        public string DefaultFromName { get; init; }
    }
}
