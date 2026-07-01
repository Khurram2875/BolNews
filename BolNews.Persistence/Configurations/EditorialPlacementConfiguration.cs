using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations
{
    public class EditorialPlacementConfiguration : IEntityTypeConfiguration<EditorialPlacement>
    {
        public void Configure(EntityTypeBuilder<EditorialPlacement> builder)
        {
            builder.ToTable("EditorialPlacements");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PlacementKey)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.SortOrder)
                .HasDefaultValue(0);

            builder.HasOne(x => x.Article)
                .WithMany()
                .HasForeignKey(x => x.ArticleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ArticleId);

            builder.HasIndex(x => new
            {
                x.PlacementKey,
                x.IsDeleted,
                x.SortOrder
            });

            builder.HasIndex(x => new
            {
                x.PlacementKey,
                x.ArticleId,
                x.IsDeleted
            });
        }
    }
}
