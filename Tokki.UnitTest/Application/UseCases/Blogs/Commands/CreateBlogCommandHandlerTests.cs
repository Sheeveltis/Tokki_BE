using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Blogs
{
    public class CreateBlogCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static CreateBlogCommandHandler CreateHandler(
            Mock<IBlogRepository>? repo = null,
            Mock<IIdGeneratorService>? idGen = null,
            Mock<ILogger<CreateBlogCommandHandler>>? logger = null)
        {
            repo ??= MockBlogRepository.GetMock();
            
            if (idGen == null)
            {
                idGen = new Mock<IIdGeneratorService>();
                idGen.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("BLOG-1234");
            }
            
            if (logger == null)
            {
                logger = new Mock<ILogger<CreateBlogCommandHandler>>();
            }

            var mockNotifService = new Mock<Tokki.Application.IServices.INotificationService>();
            var mockSysConfig = new Mock<ISystemConfigRepository>();
            var mockNotifHelper = new Tokki.Application.Common.Helpers.AppNotificationHelper(mockNotifService.Object, mockSysConfig.Object);
            var bgJobClient = new Mock<Hangfire.IBackgroundJobClient>();

            return new CreateBlogCommandHandler(repo.Object, idGen.Object, bgJobClient.Object, mockNotifHelper, logger.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Blog_01 | A | Category Not Found → Return 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CategoryNotFound_ShouldReturn404()
        {
            var command = new CreateBlogCommand { Title = "Test Title", CategoryId = "INVALID-CAT" };
            
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.CategoryNotFound);

            QACollector.LogTestCase("Blog - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Blog",
                TestCaseID        = "Create_Blog_01",
                Description       = "Attempt to create a blog with an invalid category ID",
                ExpectedResult    = "Return 404 CategoryNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CategoryExistsAsync returns false", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Blog_02 | N | Valid request → Creates blog as Draft
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateDraftBlog()
        {
            var command = new CreateBlogCommand 
            { 
                Title = "Test Title", 
                CategoryId = "VALID-CAT",
                Content = "Content",
                ShortDescription = "Desc",
                CreatedBy = "U1"
            };
            
            var mockRepo = MockBlogRepository.GetMock();
            
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("BLOG-1234");
            
            mockRepo.Verify(x => x.AddAsync(It.Is<Tokki.Domain.Entities.Blog>(b => b.Status == BlogStatus.Draft)), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Blog - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Blog",
                TestCaseID        = "Create_Blog_02",
                Description       = "Provide valid data and category",
                ExpectedResult    = "Return 201 with generated Blog ID as string",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CategoryExistsAsync returns true", "Save changes successfully", "Return 201" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Blog_03 | N | Tag Creation triggers exact amount of tags
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WithTags_ShouldCallTagGenerator()
        {
            var command = new CreateBlogCommand 
            { 
                Title = "Test Title", 
                CategoryId = "VALID-CAT",
                Tags = new List<string> { "C#", "NET9" }
            };
            
            var mockRepo = MockBlogRepository.GetMock();
            
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            mockRepo.Verify(x => x.GetOrCreateTagsAsync(It.Is<List<string>>(t => t.Count == 2), It.IsAny<bool>()), Times.Once);

            QACollector.LogTestCase("Blog - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Blog",
                TestCaseID        = "Create_Blog_03",
                Description       = "Create blog with a defined list of tags",
                ExpectedResult    = "GetOrCreateTagsAsync receives correct tag list",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Has Tags list", "Call GetOrCreateTagsAsync" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Blog_04 | N | DB Exception triggers 500 ServerError
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DatabaseThrowsException_ShouldReturn500()
        {
            var command = new CreateBlogCommand { Title = "Test", CategoryId = "VALID-CAT" };
            var mockRepo = MockBlogRepository.GetMock();
            
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Mock DB Failure"));

            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Message.Should().Contain("Lỗi SQL chi tiết: Mock DB Failure");
            result.Errors.Should().Contain(AppErrors.ServerError);

            QACollector.LogTestCase("Blog - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Blog",
                TestCaseID        = "Create_Blog_04",
                Description       = "Simulate database exception during save",
                ExpectedResult    = "Return 500 ServerError with detailed message",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SaveChangesAsync throws Exception", "Return 500" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Blog_05 | B | Title generates correct Slug
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TitleWithAccents_ShouldGenerateCorrectSlug()
        {
            var command = new CreateBlogCommand { Title = "Tiếng Việt Cố Lên", CategoryId = "VALID-CAT" };
            var mockRepo = MockBlogRepository.GetMock();
            
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            mockRepo.Verify(x => x.AddAsync(It.Is<Tokki.Domain.Entities.Blog>(b => 
                b.Slug == "tieng-viet-co-len-BLOG-1234")), Times.Once);

            QACollector.LogTestCase("Blog - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Blog",
                TestCaseID        = "Create_Blog_05",
                Description       = "Title contains Vietnamese accents",
                ExpectedResult    = "Slug should be normalized to asci format",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Check Slug normalization" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Blog_06 | N | Verify ViewCount initializes to 0
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Initialization_ShouldEnsureViewCountZero()
        {
            var command = new CreateBlogCommand { Title = "Title", CategoryId = "VALID-CAT" };
            var mockRepo = MockBlogRepository.GetMock();
            
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            mockRepo.Verify(x => x.AddAsync(It.Is<Tokki.Domain.Entities.Blog>(b => b.ViewCount == 0)), Times.Once);

            QACollector.LogTestCase("Blog - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Blog",
                TestCaseID        = "Create_Blog_06",
                Description       = "Verify new blog ViewCount starts at 0",
                ExpectedResult    = "ViewCount is 0 in entity",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Check ViewCount initialized" }
            });
        }
    }
}
