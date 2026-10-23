using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;
using BolNews.Domain.Entities.Base;// Ensure this using directive is present and correct
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace BolNews.Persistence.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Article> Articles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Reporter> Reporters { get; set; }
        public DbSet<ArticleAnalytics> ArticleAnalytics { get; set; }
        public DbSet<ArticleRevision> ArticleRevisions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ArticleDiscussionComment> ArticleDiscussionComments { get; set; }
        //public DbSet<AdPlacement> AdPlacements { get; set; }
        public DbSet<EditorialPlacement> EditorialPlacements { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ArticleTag> ArticleTags { get; set; }
        public DbSet<FeaturedImageMetadata> FeaturedImageMetadata { get; set; }
        public DbSet<FeaturedImageTag> FeaturedImageTags { get; set; }
        public DbSet<MediaAsset> MediaAssets { get; set; }
        public DbSet<MediaAssetTag> MediaAssetTags { get; set; }
        public DbSet<BreakingNews> BreakingNews => Set<BreakingNews>();
        public DbSet<EditorialCategoryConfiguration> EditorialCategoryConfigurations { get; set; }
        public DbSet<ArticleInternalLinkRule> ArticleInternalLinkRules { get; set; }
        public DbSet<SiteProfile> SiteProfiles { get; set; }
        public DbSet<AdPlacement> AdPlacements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MediaAsset>()
                .Property(x => x.ContentHash)
                .HasMaxLength(64);

            modelBuilder.Entity<MediaAsset>()
                .HasIndex(x => new { x.MediaType, x.ContentHash })
                .IsUnique();

            modelBuilder.Entity<IdentityRole>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(100);
                entity.Property(x => x.NormalizedName).HasMaxLength(100);
            });

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(x => x.NormalizedUserName).HasMaxLength(100);
                entity.Property(x => x.NormalizedEmail).HasMaxLength(100);
            });

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            modelBuilder.Entity<SiteProfile>(entity =>
            {
                entity.ToTable("SiteProfiles");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.SiteName).HasMaxLength(160).IsRequired();
                entity.Property(x => x.ShortName).HasMaxLength(80).IsRequired();
                entity.Property(x => x.Tagline).HasMaxLength(300).IsRequired();
                entity.Property(x => x.HeaderLogoUrl).HasMaxLength(512).IsRequired();
                entity.Property(x => x.FooterLogoUrl).HasMaxLength(512).IsRequired();
                entity.Property(x => x.FaviconUrl).HasMaxLength(512).IsRequired();
                entity.Property(x => x.PrimaryColor).HasMaxLength(7).IsRequired();
                entity.Property(x => x.AccentColor).HasMaxLength(7).IsRequired();
                entity.Property(x => x.ThemeName).HasMaxLength(40).IsRequired();
                entity.Property(x => x.LiveStreamUrl).HasMaxLength(512).IsRequired();
                entity.Property(x => x.LiveVideoEmbedUrl).HasMaxLength(1024);
                entity.Property(x => x.LiveThumbnailUrl).HasMaxLength(512);
                entity.Property(x => x.CopyrightText).HasMaxLength(300).IsRequired();
                entity.Property(x => x.DefaultMetaTitle).HasMaxLength(200).IsRequired();
                entity.Property(x => x.DefaultMetaDescription).HasMaxLength(500).IsRequired();
                entity.Property(x => x.CustomCss).HasColumnType("longtext");
                entity.Property(x => x.AdScriptUrl).HasMaxLength(1024);
                entity.Property(x => x.WhatsAppUrl).HasMaxLength(512);
                entity.Property(x => x.FacebookUrl).HasMaxLength(512);
                entity.Property(x => x.XUrl).HasMaxLength(512);
                entity.Property(x => x.YouTubeUrl).HasMaxLength(512);
                entity.Property(x => x.InstagramUrl).HasMaxLength(512);
                entity.Property(x => x.TikTokUrl).HasMaxLength(512);
                entity.Property(x => x.UrduEditionUrl).HasMaxLength(512);
                entity.Property(x => x.OtherEditionLabel).HasMaxLength(80).IsRequired();
                entity.Property(x => x.FooterColumnOneLinks).HasColumnType("longtext");
                entity.Property(x => x.FooterColumnTwoLinks).HasColumnType("longtext");
                entity.Property(x => x.FooterColumnThreeLinks).HasColumnType("longtext");
                entity.Property(x => x.AboutContent).HasColumnType("longtext");
                entity.Property(x => x.ContactContent).HasColumnType("longtext");
                entity.Property(x => x.AdvertiseContent).HasColumnType("longtext");
                entity.Property(x => x.BlogsContent).HasColumnType("longtext");
                entity.Property(x => x.BrandedContent).HasColumnType("longtext");
                entity.Property(x => x.EditorialPolicyContent).HasColumnType("longtext");
                entity.Property(x => x.PrivacyPolicyContent).HasColumnType("longtext");
                entity.Property(x => x.TermsOfServiceContent).HasColumnType("longtext");
                entity.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            });

            modelBuilder.Entity<AdPlacement>(entity =>
            {
                entity.ToTable("AdPlacements");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
                entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
                entity.Property(x => x.ZoneKey).HasMaxLength(100).IsRequired();
                entity.Property(x => x.ElementId).HasMaxLength(128).IsRequired();
                entity.Property(x => x.ProviderUnit).HasMaxLength(200).IsRequired();
                entity.Property(x => x.ContainerClass).HasMaxLength(300).IsRequired();
                entity.Property(x => x.TargetSelector).HasMaxLength(500);
                entity.Property(x => x.TargetPosition).HasMaxLength(16).IsRequired();
                entity.Property(x => x.InsertionMode).HasMaxLength(32).IsRequired();
                entity.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.HasIndex(x => x.Key).IsUnique();
                entity.HasIndex(x => new { x.ZoneKey, x.IsEnabled, x.SortOrder });
            });

            modelBuilder.Entity<ArticleAnalytics>()
                .HasOne(a => a.Article)
                .WithMany()
                .HasForeignKey(a => a.ArticleId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false); // ✅ KEY FIX

            modelBuilder.Entity<ArticleDiscussionComment>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.ArticleId);

                entity.Property(x => x.Message)
                    .IsRequired()
                    .HasMaxLength(4000);

                entity.Property(x => x.UserId)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.HasQueryFilter(c => !c.Article.IsDeleted);

                entity.HasOne(x => x.Article)
                    .WithMany(x => x.DiscussionComments)
                    .HasForeignKey(x => x.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Soft delete filter
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var method = typeof(AppDbContext)
                        .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        ?.MakeGenericMethod(entityType.ClrType);

                    method?.Invoke(null, new object[] { modelBuilder });
                }
            }
            // Author ↔ User (1:1)
            modelBuilder.Entity<Author>()
                .HasOne(a => a.User)
                .WithOne()
                .HasForeignKey<Author>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Article ↔ Author (1:M)
            modelBuilder.Entity<Article>()
                .HasOne(a => a.Author)
                .WithMany(a => a.Articles)
                .HasForeignKey(a => a.AuthorId);

            modelBuilder.Entity<Article>()
                .HasOne(a => a.Reporter)
                .WithMany(r => r.Articles)
                .HasForeignKey(a => a.ReporterId)
                .OnDelete(DeleteBehavior.SetNull);

            //EditorialaCategoryConfiguration ↔ Category (1:M)
            modelBuilder.Entity<EditorialCategoryConfiguration>(entity =>
            {
                entity.ToTable("EditorialCategoryConfigurations");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.PlacementType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.SortOrder)
                    .IsRequired();

                entity.Property(x => x.IsActive)
                    .IsRequired();

                entity.HasOne(x => x.Category)
                    .WithMany()
                    .HasForeignKey(x => x.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new
                {
                    x.PlacementType,
                    x.IsActive,
                    x.SortOrder
                })
                .HasDatabaseName("IX_ECC_Placement_Active_Order");

                entity.HasIndex(x => new
                {
                    x.PlacementType,
                    x.CategoryId
                })
                .IsUnique();
            });

        }
        

        private static void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder)
            where T : BaseEntity
        {
            modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
