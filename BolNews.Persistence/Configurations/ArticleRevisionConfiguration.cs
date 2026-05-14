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
    public class ArticleRevisionConfiguration
    : IEntityTypeConfiguration<ArticleRevision>
    {
        public void Configure(EntityTypeBuilder<ArticleRevision> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.MetaTitle)
                .HasMaxLength(500);

            builder.Property(x => x.MetaDescription)
                .HasMaxLength(1000);

            builder.Property(x => x.WorkflowState)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.ChangedByUserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Article)
                .WithMany(a => a.Revisions)
                .HasForeignKey(x => x.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new
            {
                x.ArticleId,
                x.RevisionNumber
            })
            .IsUnique();
        }
    }
}
