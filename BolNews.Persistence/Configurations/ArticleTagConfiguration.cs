using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations
{
    public class ArticleTagConfiguration : IEntityTypeConfiguration<ArticleTag>
    {
        public void Configure(EntityTypeBuilder<ArticleTag> builder)
        {
            builder.ToTable("ArticleTags");

            builder.HasKey(x => new { x.ArticleId, x.TagId });
            builder.HasQueryFilter(x => !x.Article.IsDeleted && !x.Tag.IsDeleted);

            builder.HasOne(x => x.Article)
                .WithMany(x => x.ArticleTags)
                .HasForeignKey(x => x.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Tag)
                .WithMany(x => x.ArticleTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.TagId);
            builder.HasIndex(x => new { x.TagId, x.ArticleId });
        }
    }
}
