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
        public DbSet<VipPackage> VipPackages { get; set; }
        public DbSet<SocialLogin> SocialLogins { get; set; }
        public DbSet<EmailJob> EmailJobs { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<EmailHistory> EmailHistories { get; set; }
        public DbSet<Title> Titles { get; set; }
        public DbSet<AccountTitle> AccountTitles { get; set; }
        public DbSet<UserXpHistory> UserXpHistories { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Word> Word { get; set; }
        public DbSet<Meaning> Meaning { get; set; }
        public DbSet<MeaningTopic> MeaningTopic { get; set; }
        public DbSet<UserFavoriteWord> UserFavoriteWords { get; set; }
        public DbSet<UserFavoriteTopic> UserFavoriteTopics { get; set; }

        // --- QUESTION BANK DbSets ---
        public DbSet<QuestionType> QuestionTypes { get; set; }
        public DbSet<Passage> Passages { get; set; }
        public DbSet<QuestionBank> QuestionBank { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }


        public DbSet<ExamTemplate> ExamTemplates { get; set; }
        public DbSet<TemplatePart> TemplateParts { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
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

            modelBuilder.Entity<Blog>()
                .Property(b => b.Status)
                .HasConversion<int>();

            modelBuilder.Entity<Report>()
            .Property(r => r.Status)
            .HasConversion<int>();

            modelBuilder.Entity<AccountTitle>()
            .HasKey(at => new { at.UserId, at.TitleId });

            modelBuilder.Entity<AccountTitle>()
                .HasOne(at => at.Account)
                .WithMany(a => a.UnlockedTitles)
                .HasForeignKey(at => at.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AccountTitle>()
                .HasOne(at => at.Title)
                .WithMany()
                .HasForeignKey(at => at.TitleId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Account>()
            .HasOne(a => a.CurrentTitle)
            .WithMany()
            .HasForeignKey(a => a.CurrentTitleId)
            .OnDelete(DeleteBehavior.SetNull);

            // =========================================================
            // 2. CONFIG ACCOUNT
            // =========================================================

            modelBuilder.Entity<Account>(entity =>
            {
                // Khóa chính
                entity.HasKey(e => e.UserId);

                entity.HasIndex(e => e.Email).IsUnique();

                entity.Property(e => e.Role)
                      .HasConversion<int>()
                      .HasMaxLength(20);

                entity.Property(e => e.Status)
                      .HasConversion<int>()
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
                      .HasConversion<int>()
                      .HasMaxLength(20);

                entity.Property(o => o.Type)
                      .HasConversion<int>()
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
                .HasConversion<int>();

            modelBuilder.Entity<EmailJob>()
                .Property(j => j.TargetGroup)
                .HasConversion<int>();


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
            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.HasKey(e => e.TemplateId);

                entity.HasIndex(e => e.TemplateKey).IsUnique();

                entity.Property(e => e.Subject)
                      .IsRequired()
                      .HasMaxLength(255)
                      .IsUnicode(true);

                entity.Property(e => e.Body)
                      .IsRequired()
                      .IsUnicode(true);

                entity.Property(e => e.Description)
                      .HasMaxLength(500)
                      .IsUnicode(true);
            });


            modelBuilder.Entity<Word>(entity =>
            {
                entity.HasKey(w => w.WordId);

                // Index cho Text để tìm kiếm nhanh
                entity.HasIndex(w => w.Text);

                // Convert enum Status sang int
                entity.Property(w => w.Status)
                      .HasConversion<int>();

                // Relationship: Word -> Meanings (One-to-Many)
                entity.HasMany(w => w.Meanings)
                      .WithOne(m => m.Word)
                      .HasForeignKey(m => m.WordId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Meaning Configuration
            modelBuilder.Entity<Meaning>(entity =>
            {
                entity.HasKey(m => m.MeaningId);

                // Convert enum Status sang int
                entity.Property(m => m.Status)
                      .HasConversion<int>();

                // Relationship với MeaningTopics
                entity.HasMany(m => m.MeaningTopics)
                      .WithOne(mt => mt.Meaning)
                      .HasForeignKey(mt => mt.MeaningId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Topic Configuration
            modelBuilder.Entity<Topic>(entity =>
            {
                entity.HasKey(t => t.TopicId);

                // Index cho TopicName để tìm kiếm nhanh
                entity.HasIndex(t => t.TopicName);

                // Convert enum Status sang int
                entity.Property(t => t.Status)
                      .HasConversion<int>();

                // Relationship với MeaningTopics
                entity.HasMany(t => t.MeaningTopics)
                      .WithOne(mt => mt.Topic)
                      .HasForeignKey(mt => mt.TopicId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // MeaningTopic Configuration (Many-to-Many junction table)
            modelBuilder.Entity<MeaningTopic>(entity =>
            {
                // Composite Primary Key
                entity.HasKey(mt => new { mt.MeaningId, mt.TopicId });

                // Convert enum Status sang int
                entity.Property(mt => mt.Status)
                      .HasConversion<int>();

                // Relationship với Meaning
                entity.HasOne(mt => mt.Meaning)
                      .WithMany(m => m.MeaningTopics)
                      .HasForeignKey(mt => mt.MeaningId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relationship với Topic
                entity.HasOne(mt => mt.Topic)
                      .WithMany(t => t.MeaningTopics)
                      .HasForeignKey(mt => mt.TopicId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<UserFavoriteWord>(entity =>
            {
                entity.HasKey(e => e.FavoriteWordId);

                entity.HasOne(e => e.Word)
                      .WithMany(w => w.UserFavorites)
                      .HasForeignKey(e => e.WordId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Meaning)
                      .WithMany(m => m.UserFavorites)
                      .HasForeignKey(e => e.MeaningId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Đảm bảo 1 user chỉ favorite 1 word 1 lần
                entity.HasIndex(e => new { e.UserId, e.WordId })
                      .IsUnique();
            });

            // UserFavoriteTopic
            modelBuilder.Entity<UserFavoriteTopic>(entity =>
            {
                entity.HasKey(e => e.FavoriteTopicId);

                entity.HasOne(e => e.Topic)
                      .WithMany(t => t.UserFavorites)
                      .HasForeignKey(e => e.TopicId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Đảm bảo 1 user chỉ favorite 1 topic 1 lần
                entity.HasIndex(e => new { e.UserId, e.TopicId })
                      .IsUnique();
            });

            // =========================================================
            // 7. CONFIG QUESTION BANK
            // =========================================================

            // QuestionType Configuration
            modelBuilder.Entity<QuestionType>(entity =>
            {
                entity.HasKey(qt => qt.QuestionTypeId);

                // Index cho Code để tìm kiếm nhanh
                entity.HasIndex(qt => qt.Code);

                // Convert enum Skill sang int
                entity.Property(qt => qt.Skill)
                      .HasConversion<int>();

                // Default value cho IsActive
                entity.Property(qt => qt.IsActive)
                      .HasDefaultValue(true);
            });

            // Passage Configuration
            modelBuilder.Entity<Passage>(entity =>
            {
                entity.HasKey(p => p.PassageId);

                // Index cho Title để tìm kiếm nhanh
                entity.HasIndex(p => p.Title);

                // Convert enum Status sang int
                entity.Property(p => p.Status)
                      .HasConversion<int>();

                // Convert enum MediaType sang int
                entity.Property(p => p.MediaType)
                      .HasConversion<int>();

                // Default values
                entity.Property(p => p.Status)
                      .HasDefaultValue(PassageStatus.Active);

                entity.Property(p => p.MediaType)
                      .HasDefaultValue(PassageMediaType.Text);
            });

            // QuestionBank Configuration
            modelBuilder.Entity<QuestionBank>(entity =>
            {
                entity.HasKey(qb => qb.QuestionBankId);

                // Convert enum Skill sang int
                entity.Property(qb => qb.Skill)
                      .HasConversion<int>();

                // Convert enum DifficultyLevel sang int
                entity.Property(qb => qb.DifficultyLevel)
                      .HasConversion<int>();

                // Default values
                entity.Property(qb => qb.DifficultyLevel)
                      .HasDefaultValue(DifficultyLevel.Medium);

                entity.Property(qb => qb.IsActive)
                      .HasDefaultValue(true);

                // Relationship với Passage (Optional)
                entity.HasOne(qb => qb.Passage)
                      .WithMany(p => p.QuestionBank)
                      .HasForeignKey(qb => qb.PassageId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship với QuestionType (Optional)
                entity.HasOne(qb => qb.QuestionType)
                      .WithMany(qt => qt.QuestionBank)
                      .HasForeignKey(qb => qb.QuestionTypeId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship với QuestionOptions (One-to-Many)
                entity.HasMany(qb => qb.QuestionOptions)
                      .WithOne(qo => qo.QuestionBank)
                      .HasForeignKey(qo => qo.QuestionBankId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // QuestionOption Configuration
            modelBuilder.Entity<QuestionOption>(entity =>
            {
                entity.HasKey(qo => qo.OptionId);

                // Default value cho IsCorrect
                entity.Property(qo => qo.IsCorrect)
                      .HasDefaultValue(false);

                // Relationship với QuestionBank
                entity.HasOne(qo => qo.QuestionBank)
                      .WithMany(qb => qb.QuestionOptions)
                      .HasForeignKey(qo => qo.QuestionBankId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            // ExamTemplate Configuration
            modelBuilder.Entity<ExamTemplate>(entity =>
            {
                entity.HasKey(et => et.ExamTemplateId);

                // Index cho Name để tìm kiếm nhanh
                entity.HasIndex(et => et.Name);

                // Convert enum Status sang int
                entity.Property(et => et.Status)
                      .HasConversion<int>();

                // Default value cho Status và CreatedAt
                entity.Property(et => et.Status)
                      .HasDefaultValue(ExamTemplateStatus.Draft);

                entity.Property(et => et.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Relationship với TemplateParts
                entity.HasMany(et => et.TemplateParts)
                      .WithOne(tp => tp.ExamTemplate)
                      .HasForeignKey(tp => tp.ExamTemplateId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relationship với Exams
                entity.HasMany(et => et.Exams)
                      .WithOne(e => e.ExamTemplate)
                      .HasForeignKey(e => e.ExamTemplateId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // TemplatePart Configuration
            modelBuilder.Entity<TemplatePart>(entity =>
            {
                entity.HasKey(tp => tp.TemplatePartId);

                // Convert enum Skill sang int
                entity.Property(tp => tp.Skill)
                      .HasConversion<int>();

                // Convert enum ExampleType sang int
                entity.Property(tp => tp.ExampleType)
                      .HasConversion<int>();

                // Default value cho ExampleType
                entity.Property(tp => tp.ExampleType)
                      .HasDefaultValue(ExampleType.None);

                // Relationship với ExamTemplate
                entity.HasOne(tp => tp.ExamTemplate)
                      .WithMany(et => et.TemplateParts)
                      .HasForeignKey(tp => tp.ExamTemplateId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Exam Configuration
            modelBuilder.Entity<Exam>(entity =>
            {
                entity.HasKey(e => e.ExamId);

                // Index cho Title để tìm kiếm nhanh
                entity.HasIndex(e => e.Title);

                // Convert enum Type sang int
                entity.Property(e => e.Type)
                      .HasConversion<int>();

                // Convert enum Status sang int
                entity.Property(e => e.Status)
                      .HasConversion<int>();

                // Default values
                entity.Property(e => e.Status)
                      .HasDefaultValue(ExamStatus.Draft);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Relationship với ExamTemplate
                entity.HasOne(e => e.ExamTemplate)
                      .WithMany(et => et.Exams)
                      .HasForeignKey(e => e.ExamTemplateId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship với ExamQuestions
                entity.HasMany(e => e.ExamQuestions)
                      .WithOne(eq => eq.Exam)
                      .HasForeignKey(eq => eq.ExamId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ExamQuestion Configuration
            modelBuilder.Entity<ExamQuestion>(entity =>
            {
                entity.HasKey(eq => eq.ExamQuestionId);

                // Unique constraint: 1 đề không thể có 2 câu cùng số thứ tự
                entity.HasIndex(eq => new { eq.ExamId, eq.QuestionNo })
                      .IsUnique();

                // Default value cho Score
                entity.Property(eq => eq.Score)
                      .HasDefaultValue(2);

                // Relationship với Exam
                entity.HasOne(eq => eq.Exam)
                      .WithMany(e => e.ExamQuestions)
                      .HasForeignKey(eq => eq.ExamId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relationship với QuestionBank
                entity.HasOne(eq => eq.QuestionBank)
                      .WithMany()
                      .HasForeignKey(eq => eq.QuestionBankId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}