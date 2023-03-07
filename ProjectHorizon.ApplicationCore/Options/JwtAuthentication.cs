namespace ProjectHorizon.ApplicationCore.Options
{
    public class JwtAuthentication
    {
        public string Issuer { get; init; }
        public string Audience { get; init; }
        public string SecretKey { get; init; }
    }
}