namespace ProjectHorizon.ApplicationCore.Entities
{
    public class GraphConfig : BaseEntity
    {
        public int Id { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Tenant { get; set; }
    }
}
