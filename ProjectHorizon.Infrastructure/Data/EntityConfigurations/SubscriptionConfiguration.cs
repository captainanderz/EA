using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public virtual void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.Property(t => t.Id)
                .HasDefaultValueSql("newid()");

            builder.HasIndex(t => t.Name)
                .IsUnique();

            builder.Property(t => t.Name)
                .HasMaxLength(40)
                .IsRequired();

            builder.Property(t => t.CompanyName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.Email)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.City)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.Country)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.ZipCode)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.VatNumber)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.State)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.CustomerNumber)
                .HasMaxLength(100);

            builder.Property(t => t.FarPayToken)
                .HasMaxLength(100);

            builder.Property(t => t.CreditCardDigits)
                .HasMaxLength(10);
        }
    }
}
