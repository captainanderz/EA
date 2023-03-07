using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class PrivateShoppingRequestConfiguration : IEntityTypeConfiguration<PrivateShoppingRequest>
    {
        public void Configure(EntityTypeBuilder<PrivateShoppingRequest> builder)
        {
            builder
                .HasOne(request => request.PrivateApplication)
                .WithMany()
                .HasForeignKey(request => request.ApplicationId);

            builder
                .HasOne<Subscription>()
                .WithMany()
                .HasForeignKey(request => request.SubscriptionId)
                .OnDelete(DeleteBehavior.ClientCascade);

            builder
                .HasOne<ApplicationUser>(request => request.AdminResolver)
                .WithMany()
                .HasForeignKey(request => request.AdminResolverId)
                .OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}
