using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Passages.Commands.UpdatePassage;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Passages
{
    public class UpdatePassageCommandHandlerTests
    {
        private static UpdatePassageCommandHandler CreateHandler(
            Mock<IPassageRepository>? passageRepo = null)
        {
            return new UpdatePassageCommandHandler(
                (passageRepo ?? new Mock<IPassageRepository>()).Object);
        }

        private static Passage ExistingTextPassage(string id = "P-001") => new()
        {
            PassageId = id,
            Title     = "Old Title",
            Content   = "Old content",
            MediaType = PassageMediaType.Text,
            Status    = PassageStatus.Active
        };

        // TC-01: Not found → 404
        [Fact]
        public async Task Handle_PassageNotFound_ShouldReturn404()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetByIdAsync("P-999", It.IsAny<CancellationToken>())).ReturnsAsync((Passage?)null);

            var cmd    = new UpdatePassageCommand { PassageId = "P-999", MediaType = PassageMediaType.Text };
            var result = await CreateHandler(repo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Passage - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdatePassage", TestCaseID = "TC-PAS-UPD-01",
                Description = "Passage not found → 404",
                ExpectedResult = "Return 404", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "passage == null => 404" }
            });
        }

        // TC-02: Text type but empty content → 400
        [Fact]
        public async Task Handle_TextTypeNoContent_ShouldReturn400()
        {
            var repo    = new Mock<IPassageRepository>();
            var passage = ExistingTextPassage();
            passage.Content = null; // no old content
            repo.Setup(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>())).ReturnsAsync(passage);

            var cmd = new UpdatePassageCommand
            {
                PassageId = "P-001",
                Title     = "Title",
                Content   = "",          // blank → no update, old also null
                MediaType = PassageMediaType.Text
            };

            var result = await CreateHandler(repo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Passage - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdatePassage", TestCaseID = "TC-PAS-UPD-02",
                Description = "MediaType=Text but no content → 400 ValidationFailed",
                ExpectedResult = "Return 400", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "mediaType=Text, content empty => 400" }
            });
        }

        // TC-03: Image type but no ImageUrl → 400
        [Fact]
        public async Task Handle_ImageTypeNoImageUrl_ShouldReturn400()
        {
            var repo    = new Mock<IPassageRepository>();
            var passage = new Passage { PassageId = "P-001", MediaType = PassageMediaType.Image, Title = "T" };
            repo.Setup(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>())).ReturnsAsync(passage);

            var cmd = new UpdatePassageCommand
            {
                PassageId = "P-001",
                Title     = "Title",
                ImageUrl  = "",
                MediaType = PassageMediaType.Image
            };

            var result = await CreateHandler(repo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Passage - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdatePassage", TestCaseID = "TC-PAS-UPD-03",
                Description = "MediaType=Image but no ImageUrl → 400",
                ExpectedResult = "Return 400", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "mediaType=Image, imageUrl empty => 400" }
            });
        }

        // TC-04: Duplicate title → 409
        [Fact]
        public async Task Handle_DuplicateTitle_ShouldReturn409()
        {
            var repo    = new Mock<IPassageRepository>();
            var passage = ExistingTextPassage();
            repo.Setup(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>())).ReturnsAsync(passage);
            repo.Setup(x => x.IsTitleExistsAsync("New Title", "P-001")).ReturnsAsync(true);

            var cmd = new UpdatePassageCommand
            {
                PassageId = "P-001",
                Title     = "New Title",
                Content   = "content",
                MediaType = PassageMediaType.Text
            };

            var result = await CreateHandler(repo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Passage - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdatePassage", TestCaseID = "TC-PAS-UPD-04",
                Description = "Different title that already exists → 409 Conflict",
                ExpectedResult = "Return 409 DuplicateTitle", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "title changed, IsTitleExistsAsync => 409" }
            });
        }

        // TC-05: Happy path → 200 with updated ID
        [Fact]
        public async Task Handle_ValidUpdate_ShouldReturn200WithId()
        {
            var repo    = new Mock<IPassageRepository>();
            var passage = ExistingTextPassage("P-001");
            repo.Setup(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>())).ReturnsAsync(passage);
            repo.Setup(x => x.IsTitleExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(x => x.UpdateAsync(It.IsAny<Passage>())).Returns(Task.CompletedTask);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var cmd = new UpdatePassageCommand
            {
                PassageId = "P-001",
                Title     = "New Unique Title",
                Content   = "New content",
                MediaType = PassageMediaType.Text
            };

            var result = await CreateHandler(repo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("P-001");
            passage.Title.Should().Be("New Unique Title");

            QACollector.LogTestCase("Passage - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdatePassage", TestCaseID = "TC-PAS-UPD-05",
                Description = "Valid update → Title/Content updated, Return 200 with PassageId",
                ExpectedResult = "Return 200, Data='P-001'", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "valid update => UpdateAsync, SaveChanges => 200" }
            });
        }

        // TC-06: Exception during save → 500
        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldReturn500()
        {
            var repo    = new Mock<IPassageRepository>();
            var passage = ExistingTextPassage("P-001");
            repo.Setup(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>())).ReturnsAsync(passage);
            repo.Setup(x => x.IsTitleExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(x => x.UpdateAsync(It.IsAny<Passage>())).Returns(Task.CompletedTask);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));

            var cmd = new UpdatePassageCommand
            {
                PassageId = "P-001",
                Title     = "New Title",
                Content   = "content",
                MediaType = PassageMediaType.Text
            };

            var result = await CreateHandler(repo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Passage - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdatePassage", TestCaseID = "TC-PAS-UPD-06",
                Description = "SaveChangesAsync throws → catch → 500",
                ExpectedResult = "Return 500", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SaveChangesAsync throws => 500" }
            });
        }
    }
}
