using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationRule.DTOs;
using Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRulesForUser;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationRule.Queries
{
    public class GetPronunciationRulesForUserQueryHandlerTests
    {
        private readonly Mock<IPronunciationRuleRepository> _mockRuleRepo;
        private readonly Mock<IPronunciationExampleRepository> _mockExampleRepo;
        private readonly Mock<IUserPronunciationExampleProgressRepository> _mockProgressRepo;
        private readonly GetPronunciationRulesForUserQueryHandler _handler;

        public GetPronunciationRulesForUserQueryHandlerTests()
        {
            _mockRuleRepo = new Mock<IPronunciationRuleRepository>();
            _mockExampleRepo = new Mock<IPronunciationExampleRepository>();
            _mockProgressRepo = new Mock<IUserPronunciationExampleProgressRepository>();
            
            _handler = new GetPronunciationRulesForUserQueryHandler(
                _mockRuleRepo.Object, 
                _mockExampleRepo.Object, 
                _mockProgressRepo.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsMappedPronunciationRulesWithProgress()
        {
            var query = new GetPronunciationRulesForUserQuery { UserId = "U1", PageNumber = 1, PageSize = 10 };
            
            var data = new List<Tokki.Domain.Entities.PronunciationRule>
            {
                new Tokki.Domain.Entities.PronunciationRule 
                { 
                    PronunciationRuleId ="R1", 
                    RuleName ="Test Rule",
                    Description ="Desc",
                    Content ="Content",
                    SortOrder = 1
                }
            };

            _mockRuleRepo.Setup(x => x.GetPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));
                         
            _mockExampleRepo.Setup(x => x.GetExamplesByRuleIdAsync("R1", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new List<Tokki.Domain.Entities.PronunciationExample> { new Tokki.Domain.Entities.PronunciationExample(), new Tokki.Domain.Entities.PronunciationExample() }); // 2 examples

            _mockProgressRepo.Setup(x => x.CountPracticedByUserIdAndRuleIdAsync("U1", "R1"))
                             .ReturnsAsync(1); // 1 practiced = 50%

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().RuleName.Should().Be("Test Rule");
            result.Data.Items.First().TotalExamples.Should().Be(2);
            result.Data.Items.First().PracticedCount.Should().Be(1);
            result.Data.Items.First().ProgressPercent.Should().Be(50);
            result.Data.Items.First().IsLearned.Should().BeFalse();

            QACollector.LogTestCase("PronunciationRule - Get For User", new TestCaseDetail
            {
                FunctionGroup     ="GetPronunciationRulesForUserQueryHandler",
                TestCaseID        ="GetPronunciationRulesForUser_01",
                Description       ="Valid data with progress calculation",
                ExpectedResult    ="Success and mapped correctly",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Mapping", "Progress logic" }
            });
        }

        [Fact]
        public async Task Handle_ZeroResults_ReturnsEmptyArray()
        {
            var query = new GetPronunciationRulesForUserQuery { PageNumber = 1, PageSize = 10 };

            _mockRuleRepo.Setup(x => x.GetPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((new List<Tokki.Domain.Entities.PronunciationRule>(), 0));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("PronunciationRule - Get For User", new TestCaseDetail
            {
                FunctionGroup     ="GetPronunciationRulesForUserQueryHandler",
                TestCaseID        ="GetPronunciationRulesForUser_02",
                Description       ="Empty repository",
                ExpectedResult    ="Zero items in result",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Empty return" }
            });
        }
    }
}
