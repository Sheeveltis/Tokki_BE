using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.Queries.Wordle;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.MiniGame.Queries
{
    public class GetTopWordleSentencesQueryHandlerTests
    {
        private readonly Mock<IMiniGameRepository> _mockRepo;
        private readonly GetTopWordleSentencesQueryHandler _handler;

        public GetTopWordleSentencesQueryHandlerTests()
        {
            _mockRepo = new Mock<IMiniGameRepository>();
            _handler = new GetTopWordleSentencesQueryHandler(_mockRepo.Object);
        }

        // TC-MG-GWS-01 | N | Returns empty list gracefully if DB yields null
        [Fact]
        public async Task Handle_SubmissionsNull_ShouldReturnEmptyList()
        {
            _mockRepo.Setup(x => x.GetTopPublicSentencesAsync("w1", 10, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((List<WordleSentenceSubmission>)null);

            var query = new GetTopWordleSentencesQuery { DailyWordleId = "w1", Top = 10 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("Wordle - Get Top Sentences", new TestCaseDetail
            {
                FunctionGroup = "GetTopWordleSentencesQueryHandler",
                TestCaseID = "TC-MG-GWS-01",
                Description = "Handles natively mapping empty returns perfectly bypassing NullExceptions",
                ExpectedResult = "Success Empty List",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Source list is Null" }
            });
        }

        // TC-MG-GWS-02 | N | Returns empty list gracefully if DB yields empty array
        [Fact]
        public async Task Handle_SubmissionsEmpty_ShouldReturnEmptyList()
        {
            _mockRepo.Setup(x => x.GetTopPublicSentencesAsync("w1", 10, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<WordleSentenceSubmission>());

            var query = new GetTopWordleSentencesQuery { DailyWordleId = "w1", Top = 10 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("Wordle - Get Top Sentences", new TestCaseDetail
            {
                FunctionGroup = "GetTopWordleSentencesQueryHandler",
                TestCaseID = "TC-MG-GWS-02",
                Description = "Recognizes any empty enumerable avoiding complex block logic issues downstream safely",
                ExpectedResult = "Success Empty List",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Source list is empty count matches" }
            });
        }

        // TC-MG-GWS-03 | N | Parses IsAnonymous logic masking identity explicitly correctly
        [Fact]
        public async Task Handle_AnonymousUserSubmission_SetsBlankIdentity()
        {
            var submit = new WordleSentenceSubmission 
            { 
                IsAnonymous = true, 
                UserId = "RealUserId",
                User = new Account { FullName = "Ngoc" },
                SentenceLikes = new List<WordleSentenceLike>()
            };

            _mockRepo.Setup(x => x.GetTopPublicSentencesAsync("w1", 10, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<WordleSentenceSubmission> { submit });

            var query = new GetTopWordleSentencesQuery { DailyWordleId = "w1", Top = 10 };
            var result = await _handler.Handle(query, CancellationToken.None);

            var first = result.Data[0];
            first.UserId.Should().BeNull();
            first.UserName.Should().Be("Người dùng ẩn danh");
            first.AvatarUrl.Should().BeNull();

            QACollector.LogTestCase("Wordle - Get Top Sentences", new TestCaseDetail
            {
                FunctionGroup = "GetTopWordleSentencesQueryHandler",
                TestCaseID = "TC-MG-GWS-03",
                Description = "Preserves user security masking data attributes wiping details successfully natively",
                ExpectedResult = "Identities are wiped string matches",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsAnonymous true" }
            });
        }

        // TC-MG-GWS-04 | N | Standard User Includes Title Identity
        [Fact]
        public async Task Handle_NormalUserSubmission_IncludesIdentity()
        {
            var submit = new WordleSentenceSubmission 
            { 
                IsAnonymous = false, 
                UserId = "U123",
                User = new Account { 
                    FullName = "Ngoc", 
                    AvatarUrl = "http", 
                    CurrentTitle = new Title { Name = "Rank 1", ColorHex = "#ff" } 
                },
                SentenceLikes = new List<WordleSentenceLike>()
            };

            _mockRepo.Setup(x => x.GetTopPublicSentencesAsync("w1", 10, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<WordleSentenceSubmission> { submit });

            var query = new GetTopWordleSentencesQuery { DailyWordleId = "w1", Top = 10 };
            var result = await _handler.Handle(query, CancellationToken.None);

            var first = result.Data[0];
            first.UserId.Should().Be("U123");
            first.UserName.Should().Be("Ngoc");
            first.TitleName.Should().Be("Rank 1");

            QACollector.LogTestCase("Wordle - Get Top Sentences", new TestCaseDetail
            {
                FunctionGroup = "GetTopWordleSentencesQueryHandler",
                TestCaseID = "TC-MG-GWS-04",
                Description = "Correctly propagates entity relationships attaching aesthetic data securely",
                ExpectedResult = "Mapped names completely",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Standard Public User data" }
            });
        }

        // TC-MG-GWS-05 | N | Fallback Empty Username Default Hook
        [Fact]
        public async Task Handle_NullUsername_AssignsFallbackName()
        {
            var submit = new WordleSentenceSubmission 
            { 
                IsAnonymous = false, 
                UserId = "U123",
                User = new Account { FullName = null }, // Missing name
                SentenceLikes = new List<WordleSentenceLike>()
            };

            _mockRepo.Setup(x => x.GetTopPublicSentencesAsync("w1", 10, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<WordleSentenceSubmission> { submit });

            var query = new GetTopWordleSentencesQuery { DailyWordleId = "w1", Top = 10 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data[0].UserName.Should().Be("Học viên Tokki");

            QACollector.LogTestCase("Wordle - Get Top Sentences", new TestCaseDetail
            {
                FunctionGroup = "GetTopWordleSentencesQueryHandler",
                TestCaseID = "TC-MG-GWS-05",
                Description = "Provides generic title substitution catching incomplete database records safely without disruption",
                ExpectedResult = "Tokki Student fallback",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Fallback full name missing" }
            });
        }

        // TC-MG-GWS-06 | N | Identifies IsLiked True Condition
        [Fact]
        public async Task Handle_UserLikesExists_MarksIsLiked()
        {
            var submit = new WordleSentenceSubmission 
            { 
                SentenceLikes = new List<WordleSentenceLike> { new WordleSentenceLike { UserId = "CurrentGuy" } }
            };

            _mockRepo.Setup(x => x.GetTopPublicSentencesAsync("w1", 10, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<WordleSentenceSubmission> { submit });

            var query = new GetTopWordleSentencesQuery { DailyWordleId = "w1", Top = 10, CurrentUserId = "CurrentGuy" };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data[0].IsLiked.Should().BeTrue();

            QACollector.LogTestCase("Wordle - Get Top Sentences", new TestCaseDetail
            {
                FunctionGroup = "GetTopWordleSentencesQueryHandler",
                TestCaseID = "TC-MG-GWS-06",
                Description = "Cross verifies foreign list memberships effectively flagging local UI markers properly",
                ExpectedResult = "IsLiked true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Current Guy matches list ID check" }
            });
        }
    }
}
