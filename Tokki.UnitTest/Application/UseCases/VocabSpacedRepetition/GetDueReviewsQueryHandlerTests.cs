using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;
using Tokki.Application.UseCases.VocabSpacedRepetition.Queries.GetDueReviews;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.VocabSpacedRepetition
{
    public class GetDueReviewsQueryHandlerTests
    {
        private GetDueReviewsQueryHandler CreateHandler(
            Mock<IUserVocabProgressRepository>? progressRepo = null)
        {
            return new GetDueReviewsQueryHandler(
                (progressRepo ?? MockUserVocabProgressRepository.GetMock()).Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnDueReviewList()
        {
            // Arrange
            var query = new GetDueReviewsQuery
            {
                UserId = "USER-001",
                Limit = 10
            };

            var fakeResult = MockUserVocabProgressRepository.GetSampleDueReviews();

            var mockProgressRepo = MockUserVocabProgressRepository.GetMock(
                dueReviews: fakeResult);

            var handler = CreateHandler(progressRepo: mockProgressRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(2);

            QACollector.LogTestCase("VocabSR - Get Due Reviews", new TestCaseDetail
            {
                FunctionGroup = "Get Due Reviews",
                TestCaseID = "TC-GDR-01",
                Description = "Query due reviews for users valid with limit → returns a list of due vocabs",
                ExpectedResult = "Return 200, Data.Count = 2",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid UserId",
                    "Limit = 10",
                    "2 vocabs due for review",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_NoDueReviews_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new GetDueReviewsQuery
            {
                UserId = "USER-001",
                Limit = 10
            };

            var mockProgressRepo = MockUserVocabProgressRepository.GetMock(
                dueReviews: new List<ReviewItemDTO>());

            var handler = CreateHandler(progressRepo: mockProgressRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("VocabSR - Get Due Reviews", new TestCaseDetail
            {
                FunctionGroup = "Get Due Reviews",
                TestCaseID = "TC-GDR-02",
                Description = "User has no vocabs due for review today → returns an empty list",
                ExpectedResult = "Return 200, Data = empty list",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid UserId",
                    "No due vocab items",
                    "Return 200 empty list"
                }
            });
        }

        [Fact]
        public async Task Handle_LimitEqualsZero_ShouldReturnEmptyList()
        {
            // Arrange — Limit = 0 (boundary: giá trị tối thiểu)
            var query = new GetDueReviewsQuery
            {
                UserId = "USER-001",
                Limit = 0
            };

            var mockProgressRepo = MockUserVocabProgressRepository.GetMock(
                dueReviews: new List<ReviewItemDTO>());

            var handler = CreateHandler(progressRepo: mockProgressRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("VocabSR - Get Due Reviews", new TestCaseDetail
            {
                FunctionGroup = "Get Due Reviews",
                TestCaseID = "TC-GDR-03",
                Description = "Limit = 0 (boundary) → does not take any items, returns an empty list",
                ExpectedResult = "Return 200, Data = empty list",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Limit = 0 (boundary: minimum value)",
                    "Return 200 empty list"
                }
            });
        }

        [Fact]
        public async Task Handle_LimitEqualsDefault100_ShouldRespectLimit()
        {
            // Arrange — Limit = 100 (default value)
            var query = new GetDueReviewsQuery
            {
                UserId = "USER-001",
                Limit = 100
            };

            // Tạo 100 ReviewItemDTO
            var dueReviews = new List<ReviewItemDTO>();
            for (int i = 1; i <= 100; i++)
            {
                dueReviews.Add(new ReviewItemDTO { VocabularyId = $"VOCAB-{i:D3}" });
            }

            var mockProgressRepo = MockUserVocabProgressRepository.GetMock(
                dueReviews: dueReviews);

            var handler = CreateHandler(progressRepo: mockProgressRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(100);

            // Verify GetDueReviewsAsync được gọi với đúng Limit = 100
            mockProgressRepo.Verify(x => x.GetDueReviewsAsync(
                "USER-001",
                It.IsAny<DateTime>(),
                100,
                It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("VocabSR - Get Due Reviews", new TestCaseDetail
            {
                FunctionGroup = "Get Due Reviews",
                TestCaseID = "TC-GDR-04",
                Description = "Limit = 100 (default value) → returns exactly 100 items",
                ExpectedResult = "Return 200, Data.Count = 100, Limit is passed correctly to the repository",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Limit = 100 (default boundary)",
                    "Repository returns 100 items",
                    "GetDueReviewsAsync called with Limit = 100",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var query = new GetDueReviewsQuery
            {
                UserId = "USER-001",
                Limit = 10
            };

            var mockProgressRepo = MockUserVocabProgressRepository.GetMock();
            mockProgressRepo.Setup(x => x.GetDueReviewsAsync(
                        It.IsAny<string>(),
                        It.IsAny<DateTime>(),
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Database connection lost"));

            var handler = CreateHandler(progressRepo: mockProgressRepo);

            // Act
            var act = async () => await handler.Handle(query, CancellationToken.None);

            // Assert — handler không catch exception → propagate lên caller
            await act.Should().ThrowAsync<Exception>()
                     .WithMessage("Database connection lost");

            QACollector.LogTestCase("VocabSR - Get Due Reviews", new TestCaseDetail
            {
                FunctionGroup = "Get Due Reviews",
                TestCaseID = "TC-GDR-05",
                Description = "Repository throws exception when query due reviews → exception propagates to caller",
                ExpectedResult = "Exception is thrown with message 'Database connection lost'",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Repository throws Exception",
                    "Handler does not have try/catch",
                    "Exception propagates to caller"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCallRepositoryWithUtcNowPlus7()
        {
            // Arrange
            var query = new GetDueReviewsQuery
            {
                UserId = "USER-001",
                Limit = 10
            };

            DateTime? capturedTime = null;
            var mockProgressRepo = MockUserVocabProgressRepository.GetMock();
            mockProgressRepo.Setup(x => x.GetDueReviewsAsync(
                        "USER-001",
                        It.IsAny<DateTime>(),
                        10,
                        It.IsAny<CancellationToken>()))
                    .Callback<string, DateTime, int, CancellationToken>((u, t, l, c) => capturedTime = t)
                    .ReturnsAsync(new List<ReviewItemDTO>());

            var handler = CreateHandler(progressRepo: mockProgressRepo);

            // Act
            var before = DateTime.UtcNow.AddHours(7).AddSeconds(-1);
            var result = await handler.Handle(query, CancellationToken.None);
            var after = DateTime.UtcNow.AddHours(7).AddSeconds(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedTime.Should().NotBeNull();
            capturedTime.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);

            QACollector.LogTestCase("VocabSR - Get Due Reviews", new TestCaseDetail
            {
                FunctionGroup = "Get Due Reviews",
                TestCaseID = "TC-GDR-06",
                Description = "Validates the handler shifts UtcNow timestamp by +7 hours when querying DB",
                ExpectedResult = "Repository receives time between (UtcNow+7 - 1s) and (UtcNow+7 + 1s)",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Time captured via Callback",
                    "Time roughly equals UtcNow + 7",
                    "Success result"
                }
            });
        }
    }
}