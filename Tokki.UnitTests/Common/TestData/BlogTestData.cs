using System.Collections.Generic;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;
using Tokki.Application.UseCases.Blogs.Commands.UpdateBlog;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    public static class BlogTestData
    {
        public static CreateBlogCommand GetValidCreateBlogCommand()
        {
            return new CreateBlogCommand
            {
                Title = "Learn Korean With Tokki",
                Content = "Sample article content...",
                ShortDescription = "Short description of SEO standards",
                ThumbnailUrl = "https://example.com/image.jpg",
                CategoryId = "cate-xin",
                Status = BlogStatus.Published,
                Tags = new List<string> { "topik", "vocabulary" }
            };
        }

        public static CreateBlogCommand GetInvalidCategoryCommand()
        {
            return new CreateBlogCommand
            {
                Title = "Test Fail",
                CategoryId = "cate-khong-ton-tai"
            };
        }

        public static List<Tag> GetFakeTags()
        {
            return new List<Tag>
            {
                new Tag { Id = "tag-1", Name = "topik" },
                new Tag { Id = "tag-2", Name = "vocabulary" }
            };
        }
        public static UpdateBlogCommand GetValidUpdateBlogCommand(string id)
        {
            return new UpdateBlogCommand
            {
                Id = id,
                Title = "Brand New Title",
                Content = "Edited content",
                ShortDescription = "New description",
                CategoryId = "cate-moi", 
                Status = BlogStatus.Hidden,
                Tags = new List<string> { "tag-moi-1", "tag-moi-2" },
                Slug = "slug-tu-nhap"
            };
        }
        public static List<Blog> GetFakeBlogEntities()
        {
            return new List<Blog>
            {
                new Blog
                {
                    Id = "blog-1",
                    Title = "Standard Test Blog",
                    Slug = "blog-test-chuan",
                    Status = BlogStatus.Published,
                    CreatedAt = DateTimeOffset.UtcNow,
                    AuthorId = "ACC-Gum",
                    CategoryId = "cate-van-hoa",
                    
                    Category = new Category { Id = "cate-van-hoa", Name = "Culture" },
                    Tags = new List<Tag>
                    {
                        new Tag { Id = "tag-1", Name = "Tag1" },
                        new Tag { Id = "tag-2", Name = "Tag2" }
                    }
                }
            };
        }
        public static List<Blog> GetFakeBlogEntitiesWithNullCategory()
        {
            return new List<Blog>
            {
                new Blog
                {
                    Id = "blog-orphan",
                    Title = "Lost Roots Blog",
                    Category = null 
                }
            };
        }
        public static Blog GetFakeBlogDetail()
        {
            return new Blog
            {
                Id = "blog-detail-1",
                Title = "Blog details",
                Slug = "chi-tiet-blog",
                Content = "<p>The detailed content is very long...</p>",
                ThumbnailUrl = "https://img.com/1.jpg",
                ShortDescription = "Short description",
                ViewCount = 100,
                Status = BlogStatus.Published,
                CreatedAt = System.DateTimeOffset.UtcNow,
                AuthorId = "ACC-Gum",
                CategoryId = "cate-van-hoa",
                Category = new Category { Name = "Culture" },
                Tags = new System.Collections.Generic.List<Tag>
                {
                    new Tag { Name = "Tag1" },
                    new Tag { Name = "Tag2" }
                }
            };
        }

    }
}