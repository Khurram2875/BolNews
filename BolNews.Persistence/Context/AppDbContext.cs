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
        public DbSet<ArticleAnalytics> ArticleAnalytics { get; set; }
        public DbSet<ArticleRevision> ArticleRevisions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ArticleDiscussionComment> ArticleDiscussionComments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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


        }
        

        private static void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder)
            where T : BaseEntity
        {
            modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
