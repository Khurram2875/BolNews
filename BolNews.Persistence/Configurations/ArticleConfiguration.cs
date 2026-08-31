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
            builder.HasIndex(x => new
            {
                x.IsPublished,
                x.PublishedAt
            });
            // Editorial Workflow Indexes
            builder.HasIndex(x => x.WorkflowStatus);

            builder.HasIndex(x => x.ReviewerUserId);

            builder.HasIndex(x => x.FactCheckerUserId);

            builder.HasIndex(x => x.AuthorId);
            // Scheduled Publishing / Embargo
            builder.HasIndex(x => x.ScheduledPublishAt);

            builder.HasIndex(x => x.EmbargoUntil);
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

            builder.HasIndex(a => new
            {
                a.IsPublished,
                a.IsDeleted,
                a.CategoryId,
                a.PublishedAt
            })
                .HasDatabaseName("IX_Articles_Published_Deleted_Category_PublishedAt");

            builder.HasIndex(a => new
            {
                a.IsPublished,
                a.IsDeleted,
                a.OverallScore,
                a.PublishedAt
            })
            .HasDatabaseName("IX_Articles_Published_Deleted_OverallScore_PublishedAt");

            builder.HasIndex(a => new 
            { a.Slug, a.IsDeleted })
            .HasDatabaseName("IX_Articles_Slug_IsDeleted");
            
            //wordpress data indexes with articles
            builder.HasIndex(a => new 
            { a.SourceSystem, a.SourceId })
            .IsUnique();

            builder.Property(a => a.SourceSystem)
                .HasMaxLength(50);

            builder.Property(a => a.SourceId)
                .HasMaxLength(100);
        }
    }
}
