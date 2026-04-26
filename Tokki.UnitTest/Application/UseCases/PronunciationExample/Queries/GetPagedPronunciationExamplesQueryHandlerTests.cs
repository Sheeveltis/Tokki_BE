using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationExample.Queries.GetPagedPronunciationExamples;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationExample.Queries
{
    public class GetPagedPronunciationExamplesQueryHandlerTests
    {
        private readonly Mock<IPronunciationExampleRepository> _mockRepo;
        private readonly Mock<IUserPronunciationExampleProgressRepository> _mockProgressRepo;
        private readonly GetPagedPronunciationExamplesQueryHandler _handler;

        public GetPagedPronunciationExamplesQueryHandlerTests()
        {
            _mockRepo = new Mock<IPronunciationExampleRepository>();
            _mockProgressRepo = new Mock<IUserPronunciationExampleProgressRepository>();
            _handler = new GetPagedPronunciationExamplesQueryHandler(_mockRepo.Object, _mockProgressRepo.Object);
        }

        // GetPagedPronunciationExamplesQueryHandler_01 | A | RuleId null -> 400
        [Fact]
        public async Task Handle_RuleIdNull_Returns400()
        {
            var query = new GetPagedPronunciationExamplesQuery { PronunciationRuleId = null };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Pronunciation Example - Get Paged", new TestCaseDetail
            {
                FunctionGroup = "GetPagedPronunciationExamplesQueryHandler",
                TestCaseID = "GetPagedPronunciationExamplesQueryHandler_01",
                Description = "Mandates filtering key restricting unbounded queries natively limiting load efficiently",
                ExpectedResult = "400 rule id missing error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "RuleId null error throw" }
            });
        }

        // GetPagedPronunciationExamplesQueryHandler_02 | A | RuleId empty -> 400
        [Fact]
        public async Task Handle_RuleIdEmpty_Returns400()
        {
            var query = new GetPagedPronunciationExamplesQuery { PronunciationRuleId = "  " };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Pronunciation Example - Get Paged", new TestCaseDetail
            {
                FunctionGroup = "GetPagedPronunciationExamplesQueryHandler",
                TestCaseID = "GetPagedPronunciationExamplesQueryHandler_02",
                Description = "Mandates filtering checking whitespaces enforcing clear mappings preventing empty list returns",
                ExpectedResult = "400 formatting key invalid error structure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "RuleId formatting whitespace bypass throws safely" }
            });
        }

        // GetPagedPronunciationExamplesQueryHandler_03 | N | Paging mapping
        [Fact]
        public async Task Handle_PagingLogic_MapsReturnPerfectly()
        {
            var entities = new List<Domain.Entities.PronunciationExample>
            {
                new Domain.Entities.PronunciationExample { TargetScript = "A"}
            };
            var tuple = (entities, 50);

            _mockRepo.Setup(x => x.GetPagedAsync("R1", 2, 10, "A", It.IsAny<Tokki.Domain.Enums.PronunciationDifficulty?>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(tuple);

            var query = new GetPagedPronunciationExamplesQuery { PronunciationRuleId = "R1", PageNumber = 2, PageSize = 10, SearchTerm = "A" };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.TotalCount.Should().Be(50);
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].TargetScript.Should().Be("A");

            QACollector.LogTestCase("Pronunciation Example - Get Paged", new TestCaseDetail
            {
                FunctionGroup = "GetPagedPronunciationExamplesQueryHandler",
                TestCaseID = "GetPagedPronunciationExamplesQueryHandler_03",
                Description = "Parses complex tuples resolving into PagedResult models standardizing format",
                ExpectedResult = "Mapped count values structure",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Standard mapped structures pagination tuples resolved safely" }
            });
        }

        // GetPagedPronunciationExamplesQueryHandler_04,5,6 logic mappings ...
        [Fact] public async Task Handle_EdgeMapping1() { 
            _mockRepo.Setup(x => x.GetPagedAsync("R1", 1, 10, "", It.IsAny<Tokki.Domain.Enums.PronunciationDifficulty?>(), It.IsAny<CancellationToken>())).ReturnsAsync((new List<Domain.Entities.PronunciationExample>(), 0));
            var r = await _handler.Handle(new GetPagedPronunciationExamplesQuery{PronunciationRuleId="R1"}, default);
            r.Data.TotalCount.Should().Be(0);
            QACollector.LogTestCase("Pronunciation Example - Get Paged", new TestCaseDetail { FunctionGroup="GetPagedPronunciationExamplesQueryHandler", TestCaseID="GetPagedPronunciationExamplesQueryHandler_04", Description="Blank mapped", ExpectedResult="0 Items", StatusRound1="Passed", TestCaseType="N", TestDate=DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions=new List<string>{"Blank array format checks mappings limits"} });
        }
        [Fact] public async Task Handle_EdgeMapping2() { 
            _mockRepo.Setup(x => x.GetPagedAsync("R1", 1, 10, "", It.IsAny<Tokki.Domain.Enums.PronunciationDifficulty?>(), It.IsAny<CancellationToken>())).ReturnsAsync((new List<Domain.Entities.PronunciationExample>(), 0));
            var r = await _handler.Handle(new GetPagedPronunciationExamplesQuery{PronunciationRuleId="R1"}, default);
            r.Data.HasNextPage.Should().BeFalse();
            QACollector.LogTestCase("Pronunciation Example - Get Paged", new TestCaseDetail { FunctionGroup="GetPagedPronunciationExamplesQueryHandler", TestCaseID="GetPagedPronunciationExamplesQueryHandler_05", Description="Checks boolean property flags HasNextPages", ExpectedResult="Flags boolean check limits", StatusRound1="Passed", TestCaseType="N", TestDate=DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions=new List<string>{"Has Next Paged false limit"} });
        }
        [Fact] public async Task Handle_EdgeMapping3() { 
            var entities = new List<Domain.Entities.PronunciationExample> { new Domain.Entities.PronunciationExample() };
            _mockRepo.Setup(x => x.GetPagedAsync("R1", 1, 1, "", It.IsAny<Tokki.Domain.Enums.PronunciationDifficulty?>(), It.IsAny<CancellationToken>())).ReturnsAsync((entities, 5));
            var r = await _handler.Handle(new GetPagedPronunciationExamplesQuery{PronunciationRuleId="R1", PageSize=1}, default);
            r.Data.HasNextPage.Should().BeTrue();
            QACollector.LogTestCase("Pronunciation Example - Get Paged", new TestCaseDetail { FunctionGroup="GetPagedPronunciationExamplesQueryHandler", TestCaseID="GetPagedPronunciationExamplesQueryHandler_06", Description="Checks boolean property true limit pagination markers flawlessly", ExpectedResult="True Flag limits boundaries sets perfectly", StatusRound1="Passed", TestCaseType="N", TestDate=DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions=new List<string>{"Paging True Limits checks models bounds"} });
        }
    }
}
