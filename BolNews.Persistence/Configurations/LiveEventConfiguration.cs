using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations;

public class LiveEventConfiguration : IEntityTypeConfiguration<LiveEvent>
{
    public void Configure(EntityTypeBuilder<LiveEvent> builder)
    {
        builder.ToTable("LiveEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(250);
        // 191 utf8mb4 characters stay within MySQL/MariaDB's 767-byte index limit.
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(191);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.EventType).HasConversion<int>();
        builder.Property(x => x.ElectionHeader).HasMaxLength(100);
        builder.Property(x => x.RaceTitle).HasMaxLength(100);
        builder.Property(x => x.RaceSubtitle).HasMaxLength(100);
        builder.HasIndex(x => new { x.IsActive, x.IsPinned, x.CreatedAt });
        builder.HasOne(x => x.FeaturedMedia).WithMany()
            .HasForeignKey(x => x.FeaturedMediaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Updates).WithOne(x => x.LiveEvent)
            .HasForeignKey(x => x.LiveEventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.LinkedArticles).WithOne(x => x.LiveEvent)
            .HasForeignKey(x => x.LiveEventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Candidates).WithOne(x => x.LiveEvent)
            .HasForeignKey(x => x.LiveEventId).OnDelete(DeleteBehavior.Cascade);
    }
}
