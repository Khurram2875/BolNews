using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations
{
    public class ArticleConfiguration : IEntityTypeConfiguration<Article>
    {
        public void Configure(EntityTypeBuilder<Article> builder)
        {
            builder.ToTable("Articles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasIndex(x => x.Slug)
                .IsUnique();

            builder.Property(x => x.Summary)
                .HasMaxLength(1000);

            builder.Property(x => x.Content)
                .IsRequired();

            builder.Property(x => x.ViewCount)
                .HasDefaultValue(0);

            // Relationships
            builder.HasOne(x => x.Category)
                .WithMany(c => c.Articles)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Author)
                .WithMany(a => a.Articles)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Performance Indexes
            builder.HasIndex(x => x.PublishedAt);
            builder.HasIndex(x => x.IsPublished);

            // Intelligent Ranking Scores Indexes
            builder.Property(x => x.SeoScore)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            builder.Property(x => x.EditorialScore)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            builder.Property(x => x.EngagementScore)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            builder.Property(x => x.FreshnessScore)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            builder.Property(x => x.PopularityScore)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            builder.Property(x => x.CredibilityScore)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            builder.Property(x => x.OverallScore)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            builder.HasIndex(x => x.OverallScore);

            builder.HasIndex(x => x.PublishedAt);

            builder.HasIndex(x => new
            {
                x.CategoryId,
                x.OverallScore
            });
            //editorial intelligence indexes
            builder.Property(x => x.EditorialPriority)
                .HasDefaultValue(0);

            builder.Property(x => x.IsEditorsPick)
                .HasDefaultValue(false);

            builder.Property(x => x.IsFactChecked)
                .HasDefaultValue(false);
        }
    }
}
