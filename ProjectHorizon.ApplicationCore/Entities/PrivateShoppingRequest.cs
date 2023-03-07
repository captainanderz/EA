namespace ProjectHorizon.ApplicationCore.Entities
{
    public class PrivateShoppingRequest : ShoppingRequest
    {
        public virtual PrivateApplication PrivateApplication { get; set; }
    }
}
