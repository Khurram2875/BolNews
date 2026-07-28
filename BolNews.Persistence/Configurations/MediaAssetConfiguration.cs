using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAssets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Caption).HasMaxLength(500);
        builder.Property(x => x.AltText).HasMaxLength(300);
        builder.Property(x => x.Credit).HasMaxLength(200);
        builder.Property(x => x.OriginalFileName).HasMaxLength(260);
        builder.HasIndex(x => new { x.MediaType, x.CreatedAt });
        builder.HasIndex(x => x.Caption);
        builder.HasMany(x => x.FeaturedForArticles).WithOne(x => x.FeaturedMedia)
            .HasForeignKey(x => x.FeaturedMediaId).OnDelete(DeleteBehavior.SetNull);
    }
}
