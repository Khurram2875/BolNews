using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations
{
    public class ReporterConfiguration : IEntityTypeConfiguration<Reporter>
    {
        public void Configure(EntityTypeBuilder<Reporter> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.SourceName)
                .HasMaxLength(200);

            builder.Property(x => x.Slug)
                .HasMaxLength(250);

            builder.HasIndex(x => x.Slug);
        }
    }
}
