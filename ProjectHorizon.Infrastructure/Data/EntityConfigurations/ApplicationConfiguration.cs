using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public abstract class ApplicationConfiguration<TBase> : IEntityTypeConfiguration<TBase>
        where TBase : Application
    {
        public virtual void Configure(EntityTypeBuilder<TBase> builder)
        {
            builder.Property(t => t.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(t => t.Version)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.Publisher)
                .HasMaxLength(200);

            builder.Property(t => t.InformationUrl)
                .HasMaxLength(1000);

            builder.Property(t => t.Notes)
                .HasMaxLength(1000);

            builder.Property(t => t.Language)
                .HasMaxLength(100);

            builder.Property(t => t.Architecture)
                .HasMaxLength(20);
        }
    }
}