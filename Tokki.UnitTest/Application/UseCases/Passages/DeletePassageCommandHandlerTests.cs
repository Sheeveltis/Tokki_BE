using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Passages.Commands.DeletePassage;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Passages
{
    public class DeletePassageCommandHandlerTests
    {
        private static DeletePassageCommandHandler CreateHandler(
            Mock<IPassageRepository>? passageRepo = null,
            Mock<IQuestionBankRepository>? qbRepo = null)
        {
            return new DeletePassageCommandHandler(
                (passageRepo ?? new Mock<IPassageRepository>()).Object,
                (qbRepo     ?? new Mock<IQuestionBankRepository>()).Object);
        }

        private static Passage ActivePassage(string id) => new()
        {
            PassageId = id,
            Title     = "Test Passage",
            Status    = PassageStatus.Active
        };

        // TC-01: Passage not found → 404
        [Fact]
        public async Task Handle_PassageNotFound_ShouldReturn404()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetByIdAsync("P-999", It.IsAny<CancellationToken>())).ReturnsAsync((Passage?)null);

            var result = await CreateHandler(repo)
                .Handle(new DeletePassageCommand { PassageId = "P-999" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Passage - Delete", new TestCaseDetail
            {
                FunctionGroup = "DeletePassage", TestCaseID = "TC-PAS-DEL-01",
                Description = "Passage not found → 404 NotFound",
                ExpectedResult = "Return 404", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "passage == null => 404" }
            });
        }

        // TC-02: Passage in use by QuestionBank → 409
        [Fact]
        public async Task Handle_PassageInUse_ShouldReturn409()
        {
            var passageRepo = new Mock<IPassageRepository>();
            passageRepo.Setup(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(ActivePassage("P-001"));

            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.AnyUsingPassageAsync("P-001", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateHandler(passageRepo, qbRepo)
                .Handle(new DeletePassageCommand { PassageId = "P-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Passage - Delete", new TestCaseDetail
            {
                FunctionGroup = "DeletePassage", TestCaseID = "TC-PAS-DEL-02",
                Description = "Passage used by QuestionBank → 409 Conflict",
                ExpectedResult = "Return 409 PassageInUse", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AnyUsingPassageAsync == true => 409" }
            });
        }

        // TC-03: Already hidden → idempotent 200
        [Fact]
        public async Task Handle_AlreadyHidden_ShouldReturnIdempotent200()
        {
            var passageRepo = new Mock<IPassageRepository>();
            var hidden = new Passage { PassageId = "P-001", Status = PassageStatus.Hidden };
            passageRepo.Setup(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>())).ReturnsAsync(hidden);

            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.AnyUsingPassageAsync("P-001", It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await CreateHandler(passageRepo, qbRepo)
                .Handle(new DeletePassageCommand { PassageId = "P-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            passageRepo.Verify(x => x.UpdateAsync(It.IsAny<Passage>()), Times.Never);

            QACollector.LogTestCase("Passage - Delete", new TestCaseDetail
            {
                FunctionGroup = "DeletePassage", TestCaseID = "TC-PAS-DEL-03",
                Description = "Already Hidden → idempotent 200 without UpdateAsync",
                ExpectedResult = "Return 200, UpdateAsync NOT called", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "status==Hidden => 200 no-op" }
            });
        }

        // TC-04: Happy path → status set to Hidden, 200
        [Fact]
        public async Task Handle_ActivePassage_ShouldSetHiddenAndReturn200()
        {
            var passageRepo = new Mock<IPassageRepository>();
            var passage     = ActivePassage("P-001");
            passageRepo.Setup(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>())).ReturnsAsync(passage);
            passageRepo.Setup(x => x.UpdateAsync(It.IsAny<Passage>())).Returns(Task.CompletedTask);
            passageRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.AnyUsingPassageAsync("P-001", It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await CreateHandler(passageRepo, qbRepo)
                .Handle(new DeletePassageCommand { PassageId = "P-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            passage.Status.Should().Be(PassageStatus.Hidden);
            passageRepo.Verify(x => x.UpdateAsync(passage), Times.Once);

            QACollector.LogTestCase("Passage - Delete", new TestCaseDetail
            {
                FunctionGroup = "DeletePassage", TestCaseID = "TC-PAS-DEL-04",
                Description = "Active passage, not in use → Status=Hidden, UpdateAsync called, 200",
                ExpectedResult = "Return 200, Status=Hidden", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "not in use, active => set Hidden => 200" }
            });
        }

        // TC-05: SaveChanges throws → 500
        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldReturn500()
        {
            var passageRepo = new Mock<IPassageRepository>();
            var passage     = ActivePassage("P-001");
            passageRepo.Setup(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>())).ReturnsAsync(passage);
            passageRepo.Setup(x => x.UpdateAsync(It.IsAny<Passage>())).Returns(Task.CompletedTask);
            passageRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));

            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.AnyUsingPassageAsync("P-001", It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await CreateHandler(passageRepo, qbRepo)
                .Handle(new DeletePassageCommand { PassageId = "P-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Passage - Delete", new TestCaseDetail
            {
                FunctionGroup = "DeletePassage", TestCaseID = "TC-PAS-DEL-05",
                Description = "SaveChangesAsync throws → catch → 500",
                ExpectedResult = "Return 500 ServerError", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SaveChangesAsync throws => 500" }
            });
        }

        // TC-06: PassageId trimmed before use
        [Fact]
        public async Task Handle_PassageIdWithSpaces_ShouldBeTrimmed()
        {
            var passageRepo = new Mock<IPassageRepository>();
            passageRepo.Setup(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>()))
                       .ReturnsAsync((Passage?)null);

            await CreateHandler(passageRepo)
                .Handle(new DeletePassageCommand { PassageId = "  P-001  " }, CancellationToken.None);

            passageRepo.Verify(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Passage - Delete", new TestCaseDetail
            {
                FunctionGroup = "DeletePassage", TestCaseID = "TC-PAS-DEL-06",
                Description = "PassageId with whitespace → trimmed before GetByIdAsync call",
                ExpectedResult = "GetByIdAsync('P-001') called", StatusRound1 = "Passed",
                TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.PassageId.Trim() used" }
            });
        }
    }
}
