using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetByQuestionTypeId;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Queries.GetByQuestionTypeId
{
    public class GetQuestionBanksByQuestionTypeIdQueryHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _repoMock = new();

        private GetQuestionBanksByQuestionTypeIdQueryHandler CreateHandler() => new(_repoMock.Object);

        // ═══════════════════════════════════════════════════════════
        // TC-QB-QBT-01 | A | Empty QuestionTypeId -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyId_ShouldReturn400()
        {
            var query = new GetQuestionBanksByQuestionTypeIdQuery { QuestionTypeId = "  " };
            var handler = CreateHandler();

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Get By TypeId", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBanksByQuestionTypeIdQueryHandler",
                TestCaseID = "TC-QB-QBT-01",
                Description = "Returns error if QuestionTypeId is empty",
                ExpectedResult = "Return 400",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionTypeId is whitespace" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-QBT-02 | N | Success No Filter -> 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoFilter_ShouldReturnReturnAll()
        {
            var query = new GetQuestionBanksByQuestionTypeIdQuery { QuestionTypeId = "qt-1" };
            var list = new List<QuestionBank> { new QuestionBank { QuestionBankId = "1" }, new QuestionBank { QuestionBankId = "2" } };

            _repoMock.Setup(x => x.GetByQuestionTypeIdAsync("qt-1", It.IsAny<QuestionBankStatus?>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(list);

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(2);

            QACollector.LogTestCase("Question Bank - Get By TypeId", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBanksByQuestionTypeIdQueryHandler",
                TestCaseID = "TC-QB-QBT-02",
                Description = "Returns all items if no extra filters applied",
                ExpectedResult = "Return 200 with 2 items",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No CreateBy/ApprovedBy filter" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-QBT-03 | N | Success Filter CreateBy -> 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterCreateBy_ShouldReturnFiltered()
        {
            var query = new GetQuestionBanksByQuestionTypeIdQuery { QuestionTypeId = "qt-1", CreateBy = "u1" };
            var list = new List<QuestionBank> 
            { 
                new QuestionBank { QuestionBankId = "1", CreateBy = "u1" }, 
                new QuestionBank { QuestionBankId = "2", CreateBy = "u2" } 
            };

            _repoMock.Setup(x => x.GetByQuestionTypeIdAsync("qt-1", It.IsAny<QuestionBankStatus?>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(list);

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().ContainSingle(x => x.QuestionBankId == "1");

            QACollector.LogTestCase("Question Bank - Get By TypeId", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBanksByQuestionTypeIdQueryHandler",
                TestCaseID = "TC-QB-QBT-03",
                Description = "Filters by CreateBy correctly",
                ExpectedResult = "Return 200 with 1 item",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CreateBy = u1" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-QBT-04 | N | Success Filter ApprovedBy -> 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterApprovedBy_ShouldReturnFiltered()
        {
            var query = new GetQuestionBanksByQuestionTypeIdQuery { QuestionTypeId = "qt-1", ApprovedBy = "admin" };
            var list = new List<QuestionBank> 
            { 
                new QuestionBank { QuestionBankId = "1", ApprovedBy = "admin" }, 
                new QuestionBank { QuestionBankId = "2", ApprovedBy = "moderator" } 
            };

            _repoMock.Setup(x => x.GetByQuestionTypeIdAsync("qt-1", It.IsAny<QuestionBankStatus?>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(list);

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().ContainSingle(x => x.QuestionBankId == "1");

            QACollector.LogTestCase("Question Bank - Get By TypeId", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBanksByQuestionTypeIdQueryHandler",
                TestCaseID = "TC-QB-QBT-04",
                Description = "Filters by ApprovedBy correctly",
                ExpectedResult = "Return 200 with 1 item",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ApprovedBy = admin" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-QBT-05 | N | Full Details Mapping Check -> 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FullDetails_MappingCheck_ShouldReturn200()
        {
            var query = new GetQuestionBanksByQuestionTypeIdQuery { QuestionTypeId = "qt-1" };
            var list = new List<QuestionBank> 
            { 
                new QuestionBank 
                { 
                    QuestionBankId = "1", 
                    Passage = new Passage { Title = "ABC" },
                    QuestionType = new QuestionType { Name = "XYZ" },
                    QuestionOptions = new List<QuestionOption> 
                    {
                        new QuestionOption { KeyOption = "A" }
                    }
                }
            };

            _repoMock.Setup(x => x.GetByQuestionTypeIdAsync("qt-1", It.IsAny<QuestionBankStatus?>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(list);

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.First().PassageTitle.Should().Be("ABC");
            result.Data.First().QuestionTypeName.Should().Be("XYZ");
            result.Data.First().Options.Should().ContainSingle(x => x.KeyOption == "A");

            QACollector.LogTestCase("Question Bank - Get By TypeId", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBanksByQuestionTypeIdQueryHandler",
                TestCaseID = "TC-QB-QBT-05",
                Description = "Nested details mapping successfully",
                ExpectedResult = "Return 200 full data",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Passage and Type loaded in entity" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-QBT-06 | B | Empty Repo Result -> 200 Empty List
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyResult_ShouldReturnEmptyList()
        {
            var query = new GetQuestionBanksByQuestionTypeIdQuery { QuestionTypeId = "qt-1" };
            _repoMock.Setup(x => x.GetByQuestionTypeIdAsync("qt-1", It.IsAny<QuestionBankStatus?>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<QuestionBank>());

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("Question Bank - Get By TypeId", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBanksByQuestionTypeIdQueryHandler",
                TestCaseID = "TC-QB-QBT-06",
                Description = "Returns empty correctly if DB is empty",
                ExpectedResult = "Return 200 empty list",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DB returns empty items" }
            });
        }
    }
}
