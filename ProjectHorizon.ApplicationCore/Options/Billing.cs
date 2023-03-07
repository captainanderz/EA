namespace ProjectHorizon.ApplicationCore.Options
{
    public class Billing
    {
        public int DayOfMonth { get; init; }

        public decimal MonthlySubscriptionPrice { get; init; }

        public decimal PricePerEndpoint { get; init; }
    }
}
