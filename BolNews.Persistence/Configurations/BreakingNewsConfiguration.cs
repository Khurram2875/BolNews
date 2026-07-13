using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Persistence.Configurations
{
    public class BreakingNewsConfiguration : IEntityTypeConfiguration<BreakingNews>
    {
        public void Configure(EntityTypeBuilder<BreakingNews> builder)
        {
            builder.ToTable("BreakingNews");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Text)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.DisplayOrder)
                .HasDefaultValue(0);

            builder.Property(x => x.DisplayOrder)
                .HasDefaultValue(100);

            builder.Property(x => x.Text)
               .HasMaxLength(500)
               .IsRequired();

            builder.Property(x => x.TickerStyle)
                .HasConversion<int>();

            builder.Property(x => x.IsPinned)
                .HasDefaultValue(false);

            builder.HasOne(x => x.Article)
                .WithMany(a => a.BreakingNews)
                .HasForeignKey(x => x.ArticleId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany(u => u.CreatedBreakingNews)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.UpdatedByUser)
                .WithMany(u => u.UpdatedBreakingNews)
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.IsActive);

            builder.HasIndex(x => x.DisplayOrder);

            builder.HasIndex(x => new
            {
                x.IsActive,
                x.DisplayOrder
            });
        }
    }
}
