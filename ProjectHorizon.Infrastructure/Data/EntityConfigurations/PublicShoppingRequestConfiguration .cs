using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class PublicShoppingRequestConfiguration : IEntityTypeConfiguration<PublicShoppingRequest>
    {
        public void Configure(EntityTypeBuilder<PublicShoppingRequest> builder)
        {
            builder.HasOne(request => request.SubscriptionPublicApplication)
                .WithMany()
                .HasForeignKey(request => new { request.SubscriptionId, request.ApplicationId });

            builder
                .HasOne<ApplicationUser>(request => request.AdminResolver)
                .WithMany()
                .HasForeignKey(request => request.AdminResolverId)
                .OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}
