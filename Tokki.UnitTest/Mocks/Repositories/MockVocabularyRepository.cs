using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockVocabularyRepository
    {
        public static Mock<IVocabularyRepository> GetMock(
            Vocabulary? returnedVocab = null,
            Vocabulary? returnedVocabWithChildren = null,
            List<Vocabulary>? returnedByText = null,
            Vocabulary? returnedByTextAndDefinition = null)
        {
            var mockRepo = new Mock<IVocabularyRepository>();

            // GetByIdAsync — dùng cho Approve/Reject/Submit
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(returnedVocab);

            // GetByIdWithChildrenAsync — dùng cho Delete/Update
            mockRepo.Setup(x => x.GetByIdWithChildrenAsync(It.IsAny<string>()))
                    .ReturnsAsync(returnedVocabWithChildren ?? returnedVocab);

            // GetAllByTextAsync — dùng cho Create/CreateByStaff (check duplicate)
            mockRepo.Setup(x => x.GetAllByTextAsync(It.IsAny<string>()))
                    .ReturnsAsync(returnedByText ?? new List<Vocabulary>());

            // GetByTextAndDefinitionAsync — dùng cho BulkCreate
            mockRepo.Setup(x => x.GetByTextAndDefinitionAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .ReturnsAsync(returnedByTextAndDefinition);

            // AddAsync
            mockRepo.Setup(x => x.AddAsync(It.IsAny<Vocabulary>()))
                    .Returns(Task.CompletedTask);

            // UpdateAsync
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<Vocabulary>()))
                    .Returns(Task.CompletedTask);

           

            return mockRepo;
        }

        // ===== Sample Data Builders =====

        public static Vocabulary GetSampleVocabulary(
            string vocabId = "VOCAB-001",
            VocabularyStatus status = VocabularyStatus.Active)
        {
            return new Vocabulary
            {
                VocabularyId = vocabId,
                Text = "안녕하세요",
                Pronunciation = "an-nyeong-ha-se-yo",
                Definition = "Xin chào",
                CreateBy = "USER-001",
                Status = status,
                VocabularyTopics = new List<VocabularyTopic>(),
                VocabularyExamples = new List<VocabularyExample>()
            };
        }

        public static Vocabulary GetSampleVocabPendingApproval(string vocabId = "VOCAB-002")
        {
            return new Vocabulary
            {
                VocabularyId = vocabId,
                Text = "감사합니다",
                Definition = "Cảm ơn",
                CreateBy = "STAFF-001",
                Status = VocabularyStatus.PendingApproval,
                VocabularyTopics = new List<VocabularyTopic>(),
                VocabularyExamples = new List<VocabularyExample>()
            };
        }

        public static Vocabulary GetSampleVocabDraft(string vocabId = "VOCAB-003")
        {
            return new Vocabulary
            {
                VocabularyId = vocabId,
                Text = "미안합니다",
                Definition = "Xin lỗi",
                CreateBy = "STAFF-001",
                Status = VocabularyStatus.Draft,
                VocabularyTopics = new List<VocabularyTopic>(),
                VocabularyExamples = new List<VocabularyExample>()
            };
        }

        public static Vocabulary GetSampleVocabDeleted(string vocabId = "VOCAB-004")
        {
            return new Vocabulary
            {
                VocabularyId = vocabId,
                Text = "반갑습니다",
                Definition = "Rất vui được gặp bạn",
                CreateBy = "USER-001",
                Status = VocabularyStatus.Deleted,
                VocabularyTopics = new List<VocabularyTopic>(),
                VocabularyExamples = new List<VocabularyExample>()
            };
        }

        public static Vocabulary GetSampleVocabWithChildren(
            string vocabId = "VOCAB-001",
            VocabularyStatus status = VocabularyStatus.Active)
        {
            return new Vocabulary
            {
                VocabularyId = vocabId,
                Text = "안녕하세요",
                Definition = "Xin chào",
                CreateBy = "USER-001",
                Status = status,
                VocabularyTopics = new List<VocabularyTopic>
                {
                    new VocabularyTopic { VocabularyId = vocabId, TopicId = "TOPIC-001", Status = VocabularyTopicStatus.Active }
                },
                VocabularyExamples = new List<VocabularyExample>
                {
                    new VocabularyExample { ExampleId = "EX-001", VocabularyId = vocabId, Sentence = "안녕하세요!", Status = VocabularyExampleStatus.Active }
                }
            };
        }
    }
}