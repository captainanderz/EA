namespace ProjectHorizon.ApplicationCore.Constants
{
    public static class HorizonClaimTypes
    {
        public const string SubscriptionId = "SubscriptionId";
        public const string Mfa = "Mfa";

        public static class MfaClaimValues
        {
            public const string Ok = "Ok";
            public const string NeedToConfigure = "NeedToConfigure";
            public const string NeedToEnterCode = "NeedToEnterCode";

            public static string GetValue(bool required, bool enabled, bool codeEntered) =>
                (required, enabled, codeEntered) switch
                {
                    (false, false, false) => Ok,
                    (false, false, true) => Ok,
                    (false, true, false) => Ok,
                    (false, true, true) => Ok,
                    (true, false, false) => NeedToConfigure,
                    (true, false, true) => NeedToConfigure,
                    (true, true, false) => NeedToEnterCode,
                    (true, true, true) => Ok
                };
        }
    }
}