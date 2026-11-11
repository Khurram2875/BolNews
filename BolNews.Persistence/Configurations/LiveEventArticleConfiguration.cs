using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations;

public class LiveEventArticleConfiguration : IEntityTypeConfiguration<LiveEventArticle>
{
    public void Configure(EntityTypeBuilder<LiveEventArticle> builder)
    {
        builder.ToTable("LiveEventArticles");
        builder.HasKey(x => new { x.LiveEventId, x.ArticleId });
        builder.HasOne(x => x.Article).WithMany().HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ArticleId);
    }
}
