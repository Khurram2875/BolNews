using BolNews.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolNews.Persistence.Configurations
{
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.ToTable("Tags");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.NormalizedName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(120);

            builder.HasIndex(x => x.NormalizedName)
                .IsUnique();

            builder.HasIndex(x => x.Slug)
                .IsUnique();
        }
    }
}
