using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations;

public class SiteProfileVersionConfiguration : IEntityTypeConfiguration<SiteProfileVersion>
{
    public void Configure(EntityTypeBuilder<SiteProfileVersion> builder)
    {
        builder.ToTable("SiteProfileVersions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.Note).IsRequired().HasMaxLength(200);
        builder.Property(x => x.SnapshotJson).IsRequired().HasColumnType("longtext");
        builder.HasIndex(x => x.CreatedAt);
    }
}
