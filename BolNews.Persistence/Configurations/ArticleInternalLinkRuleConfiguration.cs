using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations;

public class ArticleInternalLinkRuleConfiguration : IEntityTypeConfiguration<ArticleInternalLinkRule>
{
    public void Configure(EntityTypeBuilder<ArticleInternalLinkRule> builder)
    {
        builder.ToTable("ArticleInternalLinkRules");
        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.RuleType)
            .IsRequired();

        builder.Property(rule => rule.AnchorText)
            .HasMaxLength(500);

        builder.Property(rule => rule.TargetUrl)
            .HasMaxLength(2048);

        builder.Property(rule => rule.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.HasOne(rule => rule.SourceArticle)
            .WithMany()
            .HasForeignKey(rule => rule.SourceArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rule => rule.TargetArticle)
            .WithMany()
            .HasForeignKey(rule => rule.TargetArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rule => rule.SourceArticleId)
            .HasDatabaseName("IX_ArticleInternalLinkRules_SourceArticleId");

        builder.HasIndex(rule => rule.TargetArticleId)
            .HasDatabaseName("IX_ArticleInternalLinkRules_TargetArticleId");
    }
}
