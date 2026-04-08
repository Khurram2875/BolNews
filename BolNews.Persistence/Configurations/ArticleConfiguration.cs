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
        }
    }
}
