using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations;

public class LiveEventUpdateConfiguration : IEntityTypeConfiguration<LiveEventUpdate>
{
    public void Configure(EntityTypeBuilder<LiveEventUpdate> builder)
    {
        builder.ToTable("LiveEventUpdates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Summary).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Details).IsRequired().HasColumnType("longtext");
        builder.Property(x => x.Source).HasMaxLength(500);
        builder.Property(x => x.UpdatedBy).IsRequired().HasMaxLength(450);
        builder.HasIndex(x => new { x.LiveEventId, x.Timestamp });
    }
}
