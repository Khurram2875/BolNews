using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations;

public class MediaAssetTagConfiguration : IEntityTypeConfiguration<MediaAssetTag>
{
    public void Configure(EntityTypeBuilder<MediaAssetTag> builder)
    {
        builder.ToTable("MediaAssetTags");
        builder.HasKey(x => new { x.MediaAssetId, x.TagId });
        builder.HasOne(x => x.MediaAsset).WithMany(x => x.MediaAssetTags).HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Tag).WithMany(x => x.MediaAssetTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TagId, x.MediaAssetId });
    }
}
