using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations
{
    public class FeaturedImageTagConfiguration : IEntityTypeConfiguration<FeaturedImageTag>
    {
        public void Configure(EntityTypeBuilder<FeaturedImageTag> builder)
        {
            builder.ToTable("FeaturedImageTags");

            builder.HasKey(x => new { x.FeaturedImageMetadataId, x.TagId });
            builder.HasQueryFilter(x => !x.FeaturedImageMetadata.IsDeleted && !x.Tag.IsDeleted);

            builder.HasOne(x => x.FeaturedImageMetadata)
                .WithMany(x => x.FeaturedImageTags)
                .HasForeignKey(x => x.FeaturedImageMetadataId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Tag)
                .WithMany(x => x.FeaturedImageTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.TagId);
            builder.HasIndex(x => new { x.TagId, x.FeaturedImageMetadataId });
        }
    }
}
