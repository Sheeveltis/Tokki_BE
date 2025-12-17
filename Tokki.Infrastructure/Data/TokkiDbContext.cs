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
        public DbSet<Vocabulary> Vocabularies { get; set; }
        public DbSet<VocabularyTopic> VocabularyTopics { get; set; }
        public DbSet<UserFavoriteVocabulary> UserFavoriteVocabularies { get; set; }
        public DbSet<VocabularyExample> VocabularyExamples { get; set; }

        public DbSet<Comment> Comments { get; set; }

        // --- QUESTION BANK DbSets ---
        public DbSet<QuestionType> QuestionTypes { get; set; }
        public DbSet<Passage> Passages { get; set; }
        public DbSet<QuestionBank> QuestionBank { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }

        public DbSet<ExamTemplate> ExamTemplates { get; set; }
        public DbSet<TemplatePart> TemplateParts { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        //Live chat
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatRoomMember> ChatRoomMembers { get; set; }
        
        public DbSet<UserVocabProgress> UserVocabProgresses { get; set; }

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

            modelBuilder.Entity<SocialLogin>()
                .HasIndex(e => new { e.Provider, e.ProviderUserId })
                .IsUnique();

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
                .OnDelete(DeleteBehavior.Cascade);

            // =========================================================
            // 5. CONFIG OTP
            // =========================================================

            modelBuilder.Entity<Otp>(entity =>
            {
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
                entity.ToTable("SystemConfigs");
                entity.HasKey(e => e.SystemConfigID);
                entity.HasIndex(e => e.Key).IsUnique();

                entity.Property(e => e.Key)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Description)
                      .HasMaxLength(255);

                entity.Property(e => e.DataType)
                      .HasMaxLength(50);

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
                entity.HasIndex(e => new { e.UserId, e.TemplateKey })
                      .IsUnique();

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

            // =========================================================
            // CONFIG VOCABULARY MODULE
            // =========================================================

            // Topic Configuration
            modelBuilder.Entity<Topic>(entity =>
            {
                entity.HasKey(t => t.TopicId);
                entity.HasIndex(t => t.TopicName);

                entity.Property(t => t.Status)
                      .HasConversion<int>();

                // Relationship với VocabularyTopics
                entity.HasMany(t => t.VocabularyTopics)
                      .WithOne(vt => vt.Topic)
                      .HasForeignKey(vt => vt.TopicId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Vocabulary Configuration
            modelBuilder.Entity<Vocabulary>(entity =>
            {
                entity.HasKey(v => v.VocabularyId);

                entity.Property(v => v.Status)
                      .HasConversion<int>();

                // Relationship với VocabularyTopics
                entity.HasMany(v => v.VocabularyTopics)
                      .WithOne(vt => vt.Vocabulary)
                      .HasForeignKey(vt => vt.VocabularyId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relationship với UserFavorites
                entity.HasMany(v => v.UserFavorites)
                      .WithOne(uf => uf.Vocabulary)
                      .HasForeignKey(uf => uf.VocabularyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // VocabularyTopic Configuration
            modelBuilder.Entity<VocabularyTopic>(entity =>
            {
                // Composite Primary Key
                entity.HasKey(vt => new { vt.VocabularyId, vt.TopicId });

                entity.Property(vt => vt.Status)
                      .HasConversion<int>();

                // Relationship với Vocabulary
                entity.HasOne(vt => vt.Vocabulary)
                      .WithMany(v => v.VocabularyTopics)
                      .HasForeignKey(vt => vt.VocabularyId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relationship với Topic
                entity.HasOne(vt => vt.Topic)
                      .WithMany(t => t.VocabularyTopics)
                      .HasForeignKey(vt => vt.TopicId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================================================
            // 7. CONFIG QUESTION BANK
            // =========================================================

            modelBuilder.Entity<QuestionType>(entity =>
            {
                entity.HasKey(qt => qt.QuestionTypeId);
                entity.HasIndex(qt => qt.Code);

                entity.Property(qt => qt.Skill)
                      .HasConversion<int>();

                entity.Property(qt => qt.IsActive)
                      .HasDefaultValue(true);
            });

            modelBuilder.Entity<Passage>(entity =>
            {
                entity.HasKey(p => p.PassageId);
                entity.HasIndex(p => p.Title);

                entity.Property(p => p.Status)
                      .HasConversion<int>();

                entity.Property(p => p.MediaType)
                      .HasConversion<int>();

                entity.Property(p => p.Status)
                      .HasDefaultValue(PassageStatus.Active);

                entity.Property(p => p.MediaType)
                      .HasDefaultValue(PassageMediaType.Text);
            });

            modelBuilder.Entity<QuestionBank>(entity =>
            {
                entity.HasKey(qb => qb.QuestionBankId);

                entity.Property(qb => qb.Skill)
                      .HasConversion<int>();

                entity.Property(qb => qb.DifficultyLevel)
                      .HasConversion<int>();

                entity.Property(qb => qb.DifficultyLevel)
                      .HasDefaultValue(DifficultyLevel.Medium);

                entity.Property(qb => qb.IsActive)
                      .HasDefaultValue(true);

                entity.HasOne(qb => qb.Passage)
                      .WithMany(p => p.QuestionBank)
                      .HasForeignKey(qb => qb.PassageId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(qb => qb.QuestionType)
                      .WithMany(qt => qt.QuestionBank)
                      .HasForeignKey(qb => qb.QuestionTypeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(qb => qb.QuestionOptions)
                      .WithOne(qo => qo.QuestionBank)
                      .HasForeignKey(qo => qo.QuestionBankId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuestionOption>(entity =>
            {
                entity.HasKey(qo => qo.OptionId);

                entity.Property(qo => qo.IsCorrect)
                      .HasDefaultValue(false);

                entity.HasOne(qo => qo.QuestionBank)
                      .WithMany(qb => qb.QuestionOptions)
                      .HasForeignKey(qo => qo.QuestionBankId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================================================
            // 8. CONFIG EXAM
            // =========================================================

            modelBuilder.Entity<ExamTemplate>(entity =>
            {
                entity.HasKey(et => et.ExamTemplateId);
                entity.HasIndex(et => et.Name);

                entity.Property(et => et.Status)
                      .HasConversion<int>();

                entity.Property(et => et.Status)
                      .HasDefaultValue(ExamTemplateStatus.Draft);

                entity.Property(et => et.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.HasMany(et => et.TemplateParts)
                      .WithOne(tp => tp.ExamTemplate)
                      .HasForeignKey(tp => tp.ExamTemplateId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(et => et.Exams)
                      .WithOne(e => e.ExamTemplate)
                      .HasForeignKey(e => e.ExamTemplateId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TemplatePart>(entity =>
            {
                entity.HasKey(tp => tp.TemplatePartId);

                entity.Property(tp => tp.Skill)
                      .HasConversion<int>();

                entity.Property(tp => tp.ExampleType)
                      .HasConversion<int>();

                entity.Property(tp => tp.ExampleType)
                      .HasDefaultValue(ExampleType.None);

                entity.HasOne(tp => tp.ExamTemplate)
                      .WithMany(et => et.TemplateParts)
                      .HasForeignKey(tp => tp.ExamTemplateId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Exam>(entity =>
            {
                entity.HasKey(e => e.ExamId);
                entity.HasIndex(e => e.Title);

                entity.Property(e => e.Type)
                      .HasConversion<int>();

                entity.Property(e => e.Status)
                      .HasConversion<int>();

                entity.Property(e => e.Status)
                      .HasDefaultValue(ExamStatus.Draft);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.ExamTemplate)
                      .WithMany(et => et.Exams)
                      .HasForeignKey(e => e.ExamTemplateId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.ExamQuestions)
                      .WithOne(eq => eq.Exam)
                      .HasForeignKey(eq => eq.ExamId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ExamQuestion>(entity =>
            {
                entity.HasKey(eq => eq.ExamQuestionId);

                entity.HasIndex(eq => new { eq.ExamId, eq.QuestionNo })
                      .IsUnique();

                entity.Property(eq => eq.Score)
                      .HasDefaultValue(2);

                entity.HasOne(eq => eq.Exam)
                      .WithMany(e => e.ExamQuestions)
                      .HasForeignKey(eq => eq.ExamId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(eq => eq.QuestionBank)
                      .WithMany()
                      .HasForeignKey(eq => eq.QuestionBankId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =========================================================
            // 9. CONFIG COMMENT
            // =========================================================

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasOne(c => c.Blog)
                      .WithMany(b => b.Comments)
                      .HasForeignKey(c => c.BlogId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                      .WithMany()
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.ParentComment)
                      .WithMany(c => c.Replies)
                      .HasForeignKey(c => c.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}