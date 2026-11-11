using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations;

public class PollConfiguration : IEntityTypeConfiguration<Poll>
{
    public void Configure(EntityTypeBuilder<Poll> builder)
    {
        builder.ToTable("Polls");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.PollType).HasConversion<int>();
        builder.Property(x => x.CreatedBy).HasMaxLength(450);
        builder.Property(x => x.UpdatedBy).HasMaxLength(450);
        builder.HasIndex(x => new { x.IsActive, x.EndsAt });
        builder.HasOne(x => x.FeaturedMedia).WithMany()
            .HasForeignKey(x => x.FeaturedMediaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Options).WithOne(x => x.Poll)
            .HasForeignKey(x => x.PollId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Votes).WithOne(x => x.Poll)
            .HasForeignKey(x => x.PollId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PollOptionConfiguration : IEntityTypeConfiguration<PollOption>
{
    public void Configure(EntityTypeBuilder<PollOption> builder)
    {
        builder.ToTable("PollOptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Text).IsRequired().HasMaxLength(150);
        builder.HasIndex(x => new { x.PollId, x.SortOrder });
    }
}

public class PollVoteConfiguration : IEntityTypeConfiguration<PollVote>
{
    public void Configure(EntityTypeBuilder<PollVote> builder)
    {
        builder.ToTable("PollVotes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VoterHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.IpHash).IsRequired().HasMaxLength(64);
        // One vote per visitor per poll, enforced by the database even if two requests race.
        builder.HasIndex(x => new { x.PollId, x.VoterHash }).IsUnique();
        builder.HasIndex(x => new { x.PollId, x.OptionId });
        builder.HasIndex(x => new { x.PollId, x.IpHash });
        builder.HasOne(x => x.Option).WithMany()
            .HasForeignKey(x => x.OptionId).OnDelete(DeleteBehavior.Cascade);
    }
}
