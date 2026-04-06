using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBankById;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Queries.GetQuestionBankById
{
    public class GetQuestionBankByIdQueryHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _repoMock = new();

        private GetQuestionBankByIdQueryHandler CreateHandler() => new(_repoMock.Object);

        // ═══════════════════════════════════════════════════════════
        // TC-QB-ID-01 | A | NotFound -> 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NotFound_ShouldReturn404()
        {
            var query = new GetQuestionBankByIdQuery { QuestionBankId = "qb-1" };
            _repoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((QuestionBank?)null);

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBankByIdQueryHandler",
                TestCaseID = "TC-QB-ID-01",
                Description = "Returns error if entity not found",
                ExpectedResult = "Return 404",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdWithDetailsAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-ID-02 | N | Full Loading -> 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FullLoading_ShouldReturn200()
        {
            var query = new GetQuestionBankByIdQuery { QuestionBankId = "qb-1" };
            var qb = new QuestionBank 
            { 
                QuestionBankId = "qb-1",
                Passage = new Passage { Title = "ABC" },
                QuestionType = new QuestionType { Name = "XYZ" },
                QuestionOptions = new List<QuestionOption> 
                {
                    new QuestionOption { KeyOption = "B" },
                    new QuestionOption { KeyOption = "A" }
                }
            };
            _repoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.PassageTitle.Should().Be("ABC");
            result.Data.QuestionTypeName.Should().Be("XYZ");
            result.Data.Options[0].KeyOption.Should().Be("A"); // Ordered correctly

            QACollector.LogTestCase("Question Bank - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBankByIdQueryHandler",
                TestCaseID = "TC-QB-ID-02",
                Description = "Returns DTO with all nested models",
                ExpectedResult = "Return 200 Data",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Navigation properties exist" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-ID-03 | N | Null Navigation Properties -> 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullNavigations_ShouldReturn200()
        {
            var query = new GetQuestionBankByIdQuery { QuestionBankId = "qb-1" };
            var qb = new QuestionBank 
            { 
                QuestionBankId = "qb-1",
                Passage = null,
                QuestionType = null,
                QuestionOptions = new List<QuestionOption>()
            };
            _repoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.PassageTitle.Should().BeNull();
            result.Data.Options.Should().BeEmpty();

            QACollector.LogTestCase("Question Bank - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBankByIdQueryHandler",
                TestCaseID = "TC-QB-ID-03",
                Description = "Returns 200 safely when navigations are null",
                ExpectedResult = "Return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Passage = null, QuestionType = null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-ID-04 | E | Exception In Repo -> 500 equivalent if external, but we mock the query 
        // We will just do a standard exception catch if one exists, otherwise xunit catches it.
        // Wait, the handler does NOT have a try/catch, so it throws up to middleware. This is standard in MediatR.
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepoThrows_ShouldThrowException()
        {
            var query = new GetQuestionBankByIdQuery { QuestionBankId = "qb-1" };
            _repoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("DB Failure"));

            var handler = CreateHandler();

            Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>().WithMessage("DB Failure");

            QACollector.LogTestCase("Question Bank - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBankByIdQueryHandler",
                TestCaseID = "TC-QB-ID-04",
                Description = "Throws implicitly if Repo throws",
                ExpectedResult = "Throws Exception",
                StatusRound1 = "Passed",
                TestCaseType = "E",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repo throws Exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-ID-05 | A | Empty ID defaults -> repo might handle or we fail
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyId_Returns404IfRepoNull()
        {
            var query = new GetQuestionBankByIdQuery { QuestionBankId = "" };
            _repoMock.Setup(x => x.GetByIdWithDetailsAsync("", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((QuestionBank?)null);

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBankByIdQueryHandler",
                TestCaseID = "TC-QB-ID-05",
                Description = "Returns error if empty id triggers repo null",
                ExpectedResult = "Return 404",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdWithDetailsAsync throws/null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-ID-06 | N | Success Content Mapping
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_OptionsMappedCorrectly_Return200()
        {
            var query = new GetQuestionBankByIdQuery { QuestionBankId = "qb-1" };
            var qb = new QuestionBank 
            { 
                QuestionBankId = "qb-1",
                Content = "question",
                MediaUrl = "http"
            };
            _repoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Content.Should().Be("question");
            result.Data.MediaUrl.Should().Be("http");

            QACollector.LogTestCase("Question Bank - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBankByIdQueryHandler",
                TestCaseID = "TC-QB-ID-06",
                Description = "Maps base properties correctly",
                ExpectedResult = "Return 200 Data",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid simple question" }
            });
        }
    }
}
