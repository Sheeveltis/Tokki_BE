using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Infrastructure.Data
{
    public class TokkiDbContext : DbContext
    {
        public TokkiDbContext(DbContextOptions<TokkiDbContext> options) : base(options) { }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Blog>()
                .HasMany(b => b.Tags)
                .WithMany(t => t.Blogs)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogTags", 
                    j => j.HasOne<Tag>().WithMany().HasForeignKey("TagsId"),
                    j => j.HasOne<Blog>().WithMany().HasForeignKey("BlogsId"),
                    j => j.HasKey("BlogsId", "TagsId")
                );

            modelBuilder.Entity<Blog>()
                .HasOne(b => b.Category)
                .WithMany(c => c.Blogs)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Blog>().HasIndex(b => b.Slug).IsUnique();
            modelBuilder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        }
    }
}
