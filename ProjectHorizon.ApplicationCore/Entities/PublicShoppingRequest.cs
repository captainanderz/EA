namespace ProjectHorizon.ApplicationCore.Entities
{
    public class PublicShoppingRequest : ShoppingRequest
    {
        public virtual SubscriptionPublicApplication SubscriptionPublicApplication { get; set; }
    }
}
