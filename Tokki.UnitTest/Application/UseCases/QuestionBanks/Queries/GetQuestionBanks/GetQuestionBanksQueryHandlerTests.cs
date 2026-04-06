using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBanks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Queries.GetQuestionBanks
{
    public class GetQuestionBanksQueryHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _repoMock = new();

        private GetQuestionBanksQueryHandler CreateHandler() => new(_repoMock.Object);

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GL-01 | N | Success No Filter -> 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoFilter_ShouldReturnPagedValid()
        {
            var query = new GetQuestionBanksQuery { PageNumber = 1, PageSize = 10 };
            var list = new List<QuestionBank> { new QuestionBank { QuestionBankId = "1" }, new QuestionBank { QuestionBankId = "2" } };

            _repoMock.Setup(x => x.GetPagedAsync(1, 10, null, null, null, null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((list, 50)); // Total 50

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(50); // Original total from repo

            QACollector.LogTestCase("Question Bank - Get List", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBanksQueryHandler",
                TestCaseID = "TC-QB-GL-01",
                Description = "Returns paged items correctly if no extra filters applied",
                ExpectedResult = "Return 200 with items and totalCount 50",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No CreateBy/ApprovedBy filter" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GL-02 | N | Success Filter CreateBy -> 200 (Total count patched)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterCreateBy_ShouldReturnFilteredAndPatchedCount()
        {
            var query = new GetQuestionBanksQuery { PageNumber = 1, PageSize = 10, CreateBy = "u1" };
            var list = new List<QuestionBank> 
            { 
                new QuestionBank { QuestionBankId = "1", CreateBy = "u1" }, 
                new QuestionBank { QuestionBankId = "2", CreateBy = "u2" },
                new QuestionBank { QuestionBankId = "3", CreateBy = "u1" }
            };

            _repoMock.Setup(x => x.GetPagedAsync(1, 10, null, null, null, null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((list, 50));

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().HaveCount(2); // Filtered local
            result.Data.TotalCount.Should().Be(2); // Patched total to local count

            QACollector.LogTestCase("Question Bank - Get List", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBanksQueryHandler",
                TestCaseID = "TC-QB-GL-02",
                Description = "Filters by CreateBy and patches TotalCount",
                ExpectedResult = "Return 200 with TotalCount matching items count",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CreateBy = u1" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GL-03 | N | Success Filter ApprovedBy -> 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterApprovedBy_ShouldReturnFiltered()
        {
            var query = new GetQuestionBanksQuery { PageNumber = 1, PageSize = 10, ApprovedBy = "admin" };
            var list = new List<QuestionBank> 
            { 
                new QuestionBank { QuestionBankId = "1", ApprovedBy = "admin" }, 
                new QuestionBank { QuestionBankId = "2", ApprovedBy = "moderator" } 
            };

            _repoMock.Setup(x => x.GetPagedAsync(1, 10, null, null, null, null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((list, 50));

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().ContainSingle(x => x.QuestionBankId == "1");
            result.Data.TotalCount.Should().Be(1);

            QACollector.LogTestCase("Question Bank - Get List", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBanksQueryHandler",
                TestCaseID = "TC-QB-GL-03",
                Description = "Filters by ApprovedBy correctly and patches TotalCount",
                ExpectedResult = "Return 200 with 1 item",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ApprovedBy = admin" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GL-04 | N | Options Array Ordered Check
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_OptionsAreOrdered_ShouldReturn200()
        {
            var query = new GetQuestionBanksQuery { PageNumber = 1, PageSize = 10 };
            var list = new List<QuestionBank> 
            { 
                new QuestionBank 
                { 
                    QuestionBankId = "1", 
                    QuestionOptions = new List<QuestionOption> 
                    {
                        new QuestionOption { KeyOption = "C" },
                        new QuestionOption { KeyOption = "A" },
                        new QuestionOption { KeyOption = "B" }
                    }
                }
            };

            _repoMock.Setup(x => x.GetPagedAsync(1, 10, null, null, null, null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((list, 1));

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var ops = result.Data.Items.First().Options.ToList();
            ops[0].KeyOption.Should().Be("A");
            ops[1].KeyOption.Should().Be("B");
            ops[2].KeyOption.Should().Be("C");

            QACollector.LogTestCase("Question Bank - Get List", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBanksQueryHandler",
                TestCaseID = "TC-QB-GL-04",
                Description = "Options are mapped and ordered correctly",
                ExpectedResult = "Return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Nested options sorted by KeyOption" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GL-05 | B | Empty Repo Result -> 200 Empty List
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyResult_ShouldReturnEmptyList()
        {
            var query = new GetQuestionBanksQuery { PageNumber = 1, PageSize = 10 };
            _repoMock.Setup(x => x.GetPagedAsync(1, 10, null, null, null, null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((new List<QuestionBank>(), 0));

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Question Bank - Get List", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBanksQueryHandler",
                TestCaseID = "TC-QB-GL-05",
                Description = "Returns empty safely if DB is empty",
                ExpectedResult = "Return 200 empty items",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedAsync returns 0 limit" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GL-06 | N | Filtering Filters Out All Items -> 200 Empty Patched
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterAllOut_ShouldReturnEmptyPatchedCount()
        {
            var query = new GetQuestionBanksQuery { PageNumber = 1, PageSize = 10, CreateBy = "not-exist" };
            var list = new List<QuestionBank> 
            { 
                new QuestionBank { QuestionBankId = "1", CreateBy = "u1" }
            };

            _repoMock.Setup(x => x.GetPagedAsync(1, 10, null, null, null, null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((list, 50));

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().BeEmpty(); // Because filtered
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Question Bank - Get List", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionBanksQueryHandler",
                TestCaseID = "TC-QB-GL-06",
                Description = "Returns empty gracefully when local filter eliminates all items",
                ExpectedResult = "Return 200 with TotalCount=0",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CreateBy local filter unmatched" }
            });
        }
    }
}
