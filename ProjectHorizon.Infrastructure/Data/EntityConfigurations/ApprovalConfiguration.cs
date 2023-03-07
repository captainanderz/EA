using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
    {
        public virtual void Configure(EntityTypeBuilder<Approval> builder)
        {
            builder.Property(t => t.UserDecision)
                .HasMaxLength(50);

            builder
                .HasOne(a => a.SubscriptionPublicApplication)
                .WithMany()
                .HasForeignKey(a => new { a.SubscriptionId, a.PublicApplicationId })
                .OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}
