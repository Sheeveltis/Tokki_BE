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
                Description = "Query due reviews cho user hợp lệ với limit → trả về danh sách vocab đến hạn",
                ExpectedResult = "Return 200, Data.Count = 2",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid UserId",
                    "Limit = 10",
                    "2 vocab đến hạn review",
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
                Description = "User không có vocab nào đến hạn review hôm nay → trả về empty list",
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
                Description = "Limit = 0 (boundary) → không lấy item nào, trả về empty list",
                ExpectedResult = "Return 200, Data = empty list",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Limit = 0 (boundary: giá trị tối thiểu)",
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
                Description = "Limit = 100 (default value) → trả về đúng 100 items",
                ExpectedResult = "Return 200, Data.Count = 100, Limit được truyền đúng vào repository",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Limit = 100 (default boundary)",
                    "Repository trả về 100 items",
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
                Description = "Repository throw exception khi query due reviews → exception propagate lên caller",
                ExpectedResult = "Exception được throw với message 'Database connection lost'",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Repository throws Exception",
                    "Handler không có try/catch",
                    "Exception propagates to caller"
                }
            });
        }
    }
}