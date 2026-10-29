using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations;

public class LiveEventCandidateConfiguration : IEntityTypeConfiguration<LiveEventCandidate>
{
    public void Configure(EntityTypeBuilder<LiveEventCandidate> builder)
    {
        builder.ToTable("LiveEventCandidates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Party).HasConversion<int>();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Percentage).HasPrecision(5, 2);
        builder.HasIndex(x => new { x.LiveEventId, x.SortOrder });
    }
}
