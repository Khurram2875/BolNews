using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations
{
    public class FeaturedImageMetadataConfiguration : IEntityTypeConfiguration<FeaturedImageMetadata>
    {
        public void Configure(EntityTypeBuilder<FeaturedImageMetadata> builder)
        {
            builder.ToTable("FeaturedImageMetadata");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AltText)
                .HasMaxLength(300);

            builder.Property(x => x.Caption)
                .HasMaxLength(500);

            builder.Property(x => x.Credit)
                .HasMaxLength(200);

            builder.HasIndex(x => x.ArticleId)
                .IsUnique();

            builder.HasOne(x => x.Article)
                .WithOne(x => x.FeaturedImageMetadata)
                .HasForeignKey<FeaturedImageMetadata>(x => x.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
