using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class PublicApplicationConfiguration : ApplicationConfiguration<PublicApplication>
    {
        public override void Configure(EntityTypeBuilder<PublicApplication> builder)
        {
            builder.Property(t => t.PublicRepositoryArchiveFileName)
                .HasMaxLength(1000);

            builder.Property(t => t.PackageCacheFolderName)
                .HasMaxLength(1000);



            base.Configure(builder);
        }
    }
}
