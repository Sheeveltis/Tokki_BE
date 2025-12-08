using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Collections.Generic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Infrastructure.Data
{
    public class TokkiDbContext : DbContext
    {
        public TokkiDbContext(DbContextOptions<TokkiDbContext> options) : base(options) { }

        // --- DbSets ---
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Session> Session { get; set; }
        public DbSet<Otp> OtpCodes { get; set; }
        public DbSet<SystemConfig> SystemConfig { get; set; }

        public DbSet<SocialLogin> SocialLogins { get; set; } // Đổi tên cho khớp với Entity (ExternalLogins cũng đc nhưng SocialLogins chuẩn hơn)
        public DbSet<EmailJob> EmailJobs { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<EmailHistory> EmailHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================================
            // 1. CONFIG BLOG & CATEGORY & TAG
            // =========================================================

            // Blog - Tag (Many-to-Many)
            modelBuilder.Entity<Blog>()
                .HasMany(b => b.Tags)
                .WithMany(t => t.Blogs)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogTags",
                    j => j.HasOne<Tag>().WithMany().HasForeignKey("TagsId"),
                    j => j.HasOne<Blog>().WithMany().HasForeignKey("BlogsId"),
                    j => j.HasKey("BlogsId", "TagsId")
                );

            // Blog - Category (One-to-Many)
            modelBuilder.Entity<Blog>()
                .HasOne(b => b.Category)
                .WithMany(c => c.Blogs)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique Slugs
            modelBuilder.Entity<Blog>().HasIndex(b => b.Slug).IsUnique();
            modelBuilder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();

            // Convert Enum BlogStatus (nếu có)
            modelBuilder.Entity<Blog>()
                .Property(b => b.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Report>()
            .Property(r => r.Status)
            .HasConversion<int>();

            // =========================================================
            // 2. CONFIG ACCOUNT
            // =========================================================

            modelBuilder.Entity<Account>(entity =>
            {
                // Khóa chính
                entity.HasKey(e => e.UserId);

                entity.HasIndex(e => e.Email).IsUnique();

                entity.Property(e => e.Role)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.Property(e => e.Status)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.Property(e => e.DateOfBirth)
                      .HasColumnType("date");
            });

            // =========================================================
            // 3. CONFIG SOCIAL LOGIN
            // =========================================================

            // SỬA LỖI: Đổi ProviderKey -> ProviderUserId
            // Ràng buộc 1: (Provider + ProviderUserId) phải duy nhất toàn hệ thống
            modelBuilder.Entity<SocialLogin>()
                .HasIndex(e => new { e.Provider, e.ProviderUserId })
                .IsUnique();

            // Ràng buộc 2: (UserId + Provider) phải duy nhất (1 user chỉ link 1 GG, 1 FB)
            modelBuilder.Entity<SocialLogin>()
                .HasIndex(e => new { e.UserId, e.Provider })
                .IsUnique();

            // =========================================================
            // 4. CONFIG SESSION
            // =========================================================

            modelBuilder.Entity<Session>()
                .HasOne(us => us.Account)
                .WithMany(a => a.Sessions)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user -> Xóa hết session

            // =========================================================
            // 5. CONFIG OTP
            // =========================================================

            modelBuilder.Entity<Otp>(entity =>
            {
                // Khóa ngoại
                entity.HasOne(o => o.Account)
                      .WithMany()
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(o => o.Status)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                // Cấu hình ENUM Type -> string
                entity.Property(o => o.Type)
                      .HasConversion<string>()
                      .HasMaxLength(30);
            });

            // =========================================================
            // 6. CONFIG SYSTEM CONFIG
            // =========================================================
            modelBuilder.Entity<SystemConfig>(entity =>
            {
                entity.ToTable("SystemConfigs"); // Đặt tên bảng

                // Khóa chính
                entity.HasKey(e => e.SystemConfigID);

                // QUAN TRỌNG: Cột Key phải là duy nhất (không được trùng tên cấu hình)
                entity.HasIndex(e => e.Key).IsUnique();

                // Cấu hình độ dài và bắt buộc (khớp với Data Annotation bên Entity)
                entity.Property(e => e.Key)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Description)
                      .HasMaxLength(255);

                entity.Property(e => e.DataType)
                      .HasMaxLength(50);

                // Giá trị mặc định cho IsActive
                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);
            });


            modelBuilder.Entity<EmailJob>()
                .Property(j => j.Status)
                .HasConversion<string>();

            modelBuilder.Entity<EmailJob>()
                .Property(j => j.TargetGroup)
                .HasConversion<string>();

            modelBuilder.Entity<EmailJob>().Property(j => j.Status).HasConversion<string>();

            // =========================================================
            // CONFIG EMAIL HISTORY
            // =========================================================
            modelBuilder.Entity<EmailHistory>(entity =>
            {
                // Unique constraint: 1 user chỉ nhận 1 email cho mỗi template
                entity.HasIndex(e => new { e.UserId, e.TemplateKey })
                      .IsUnique();

                // Foreign key
                entity.HasOne(e => e.Account)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}