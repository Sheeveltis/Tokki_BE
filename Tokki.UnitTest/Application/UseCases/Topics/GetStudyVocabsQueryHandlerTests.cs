using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Queries.GetStudyVocabs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class GetStudyVocabsQueryHandlerTests
    {
        private GetStudyVocabsQueryHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IUserVocabProgressRepository>? progressRepo = null)
        {
            return new GetStudyVocabsQueryHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (progressRepo ?? MockUserVocabProgressRepository.GetMock()).Object);
        }

        [Fact]
        public async Task Handle_TopicHasNoVocab_ShouldReturn404()
        {
            var query = new GetStudyVocabsQuery
            {
                TopicId = "TOPIC-001",
                UserId = "USER-001",
                Count = 10
            };

            var mockTopicRepo = MockTopicRepository.GetMock(
                returnedTopic: MockTopicRepository.GetSampleTopic());

            // GetVocabulariesByTopicIdAsync trả về empty → 404
            mockTopicRepo.Setup(x => x.GetVocabulariesByTopicIdAsync(It.IsAny<string>()))
                         .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary>());

            var handler = CreateHandler(topicRepo: mockTopicRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Topic - Get Study Vocabs", new TestCaseDetail
            {
                FunctionGroup = "Get Study Vocabs",
                TestCaseID = "TC-TOPIC-GSV-01",
                Description = "Topic chưa có vocab nào → return 404",
                ExpectedResult = "Return 404 'Topic này chưa có từ vựng nào'",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "GetVocabulariesByTopicIdAsync returns empty",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_HasUnlearnedVocabs_ShouldReturnUnlearnedFirst()
        {
            var query = new GetStudyVocabsQuery
            {
                TopicId = "TOPIC-001",
                UserId = "USER-001",
                Count = 5
            };

            var allVocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                new Tokki.Domain.Entities.Vocabulary { VocabularyId = "VOCAB-001", Text = "안녕", Definition = "Xin chào", Status = VocabularyStatus.Active },
                new Tokki.Domain.Entities.Vocabulary { VocabularyId = "VOCAB-002", Text = "감사", Definition = "Cảm ơn", Status = VocabularyStatus.Active },
                new Tokki.Domain.Entities.Vocabulary { VocabularyId = "VOCAB-003", Text = "미안", Definition = "Xin lỗi", Status = VocabularyStatus.Active }
            };

            var mockTopicRepo = MockTopicRepository.GetMock(
                returnedTopic: MockTopicRepository.GetSampleTopic());

            mockTopicRepo.Setup(x => x.GetVocabulariesByTopicIdAsync(It.IsAny<string>()))
                         .ReturnsAsync(allVocabs);

            var mockProgressRepo = MockUserVocabProgressRepository.GetMock();

            // VOCAB-001 đã học, còn VOCAB-002 và VOCAB-003 chưa học
            mockProgressRepo.Setup(x => x.GetLearnedVocabIdsByTopicAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                            .ReturnsAsync(new List<string> { "VOCAB-001" });

            var handler = CreateHandler(topicRepo: mockTopicRepo, progressRepo: mockProgressRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeEmpty();

            // Tất cả vocab trả về phải là vocab chưa học
            result.Data.Should().NotContain(v => v.VocabularyId == "VOCAB-001");
            result.Message.Should().Contain("Danh sách từ mới");

            QACollector.LogTestCase("Topic - Get Study Vocabs", new TestCaseDetail
            {
                FunctionGroup = "Get Study Vocabs",
                TestCaseID = "TC-TOPIC-GSV-02",
                Description = "User đã học 1 vocab, còn 2 chưa học → trả về 2 vocab chưa học",
                ExpectedResult = "Return 200, không chứa vocab đã học, message 'Danh sách từ mới'",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "3 vocab trong topic",
                    "1 vocab đã học (VOCAB-001)",
                    "Trả về chỉ vocab chưa học",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_AllVocabsLearned_ShouldReturnRandomReviewMode()
        {
            var query = new GetStudyVocabsQuery
            {
                TopicId = "TOPIC-001",
                UserId = "USER-001",
                Count = 3
            };

            var allVocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                new Tokki.Domain.Entities.Vocabulary { VocabularyId = "VOCAB-001", Text = "안녕", Definition = "Xin chào", Status = VocabularyStatus.Active },
                new Tokki.Domain.Entities.Vocabulary { VocabularyId = "VOCAB-002", Text = "감사", Definition = "Cảm ơn", Status = VocabularyStatus.Active }
            };

            var mockTopicRepo = MockTopicRepository.GetMock(
                returnedTopic: MockTopicRepository.GetSampleTopic());

            mockTopicRepo.Setup(x => x.GetVocabulariesByTopicIdAsync(It.IsAny<string>()))
                         .ReturnsAsync(allVocabs);

            var mockProgressRepo = MockUserVocabProgressRepository.GetMock();

            // Tất cả vocab đã học
            mockProgressRepo.Setup(x => x.GetLearnedVocabIdsByTopicAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                            .ReturnsAsync(new List<string> { "VOCAB-001", "VOCAB-002" });

            var handler = CreateHandler(topicRepo: mockTopicRepo, progressRepo: mockProgressRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeEmpty();
            result.Message.Should().Contain("ôn tập ngẫu nhiên");

            QACollector.LogTestCase("Topic - Get Study Vocabs", new TestCaseDetail
            {
                FunctionGroup = "Get Study Vocabs",
                TestCaseID = "TC-TOPIC-GSV-03",
                Description = "User đã học hết toàn bộ vocab → chuyển sang chế độ ôn tập ngẫu nhiên",
                ExpectedResult = "Return 200, message chứa 'ôn tập ngẫu nhiên'",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Tất cả vocab đã học (boundary: unlearnedVocabs.Count = 0)",
                    "Fallback sang random toàn bộ vocab",
                    "Return 200"
                }
            });
        }
    }
}