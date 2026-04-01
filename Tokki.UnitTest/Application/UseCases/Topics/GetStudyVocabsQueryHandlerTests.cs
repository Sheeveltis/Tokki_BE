using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Queries.GetStudyVocabs;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class GetStudyVocabsQueryHandlerTests
    {
        private static Mock<ITopicRepository> GetTopicMock(List<Tokki.Domain.Entities.Vocabulary>? vocabs = null)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetVocabulariesByTopicIdAsync(It.IsAny<string>())).ReturnsAsync(vocabs ?? new List<Tokki.Domain.Entities.Vocabulary>());
            return m;
        }

        private static Mock<IUserVocabProgressRepository> GetProgressMock(List<string>? learnedIds = null)
        {
            var m = new Mock<IUserVocabProgressRepository>();
            m.Setup(x => x.GetLearnedVocabIdsByTopicAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(learnedIds ?? new List<string>());
            return m;
        }

        private static GetStudyVocabsQueryHandler CreateHandler(
            Mock<ITopicRepository>?              topicRepo    = null,
            Mock<IUserVocabProgressRepository>?  progressRepo = null)
            => new GetStudyVocabsQueryHandler(
                (topicRepo    ?? GetTopicMock()).Object,
                (progressRepo ?? GetProgressMock()).Object);

        private static List<Tokki.Domain.Entities.Vocabulary> SampleVocabs(int count = 3)
        {
            var list = new List<Tokki.Domain.Entities.Vocabulary>();
            for (int i = 1; i <= count; i++)
                list.Add(new Tokki.Domain.Entities.Vocabulary { VocabularyId = $"V-{i:000}", Text = $"word{i}", Pronunciation = $"[w{i}]", Definition = $"def{i}" });
            return list;
        }

        private static GetStudyVocabsQuery MakeQuery(string topicId = "T-001", string userId = "U-001", int count = 5) =>
            new GetStudyVocabsQuery { TopicId = topicId, UserId = userId, Count = count };

        // TC-TOPIC-GVOC-01 | A | No vocabs in topic → 404
        [Fact]
        public async Task Handle_NoVocabsInTopic_ShouldReturn404()
        {
            var result = await CreateHandler(GetTopicMock(new List<Tokki.Domain.Entities.Vocabulary>())).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Get Study Vocabs", new TestCaseDetail { FunctionGroup = "GetStudyVocabs", TestCaseID = "TC-TOPIC-GVOC-01", Description = "No vocabs in topic → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetVocabulariesByTopicIdAsync returns empty" } });
        }

        // TC-TOPIC-GVOC-02 | N | No learned vocabs → all unlearned returned (up to Count)
        [Fact]
        public async Task Handle_NoneLearnedYet_ShouldReturnUnlearnedVocabs()
        {
            var vocabs = SampleVocabs(5);
            var result = await CreateHandler(GetTopicMock(vocabs), GetProgressMock(new List<string>()))
                .Handle(MakeQuery(count: 3), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            QACollector.LogTestCase("Topic - Get Study Vocabs", new TestCaseDetail { FunctionGroup = "GetStudyVocabs", TestCaseID = "TC-TOPIC-GVOC-02", Description = "No learned vocabs → 3 unlearned returned (Count=3)", ExpectedResult = "IsSuccess=true, Data.Count=3", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "unlearned filtered to Count" } });
        }

        // TC-TOPIC-GVOC-03 | N | All vocabs learned → fallback to random all vocabs
        [Fact]
        public async Task Handle_AllVocabsLearned_FallbackToRandomAll()
        {
            var vocabs     = SampleVocabs(3);
            var learnedIds = new List<string> { "V-001", "V-002", "V-003" };
            var result     = await CreateHandler(GetTopicMock(vocabs), GetProgressMock(learnedIds))
                .Handle(MakeQuery(count: 2), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2); // fallback pulls from all
            QACollector.LogTestCase("Topic - Get Study Vocabs", new TestCaseDetail { FunctionGroup = "GetStudyVocabs", TestCaseID = "TC-TOPIC-GVOC-03", Description = "All learned → fallback to random all, Count=2", ExpectedResult = "IsSuccess=true, Data.Count=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "unlearnedVocabs.Count==0 → fallback" } });
        }

        // TC-TOPIC-GVOC-04 | N | DTO fields mapped correctly (VocabularyId, Text, Pronunciation)
        [Fact]
        public async Task Handle_ValidRequest_DtoFieldsMappedCorrectly()
        {
            var vocabs = new List<Tokki.Domain.Entities.Vocabulary> { new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V-001", Text = "안녕", Pronunciation = "[an-nyeong]", Definition = "hello", ImgURL = "img.png", AudioURL = "audio.mp3" } };
            var result = await CreateHandler(GetTopicMock(vocabs), GetProgressMock()).Handle(MakeQuery(count: 1), CancellationToken.None);
            var dto = result.Data![0];
            dto.VocabularyId.Should().Be("V-001");
            dto.Text.Should().Be("안녕");
            dto.Pronunciation.Should().Be("[an-nyeong]");
            QACollector.LogTestCase("Topic - Get Study Vocabs", new TestCaseDetail { FunctionGroup = "GetStudyVocabs", TestCaseID = "TC-TOPIC-GVOC-04", Description = "DTO fields: VocabularyId, Text, Pronunciation mapped", ExpectedResult = "All fields correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Vocabulary entity→VocabBasicInfoDTO" } });
        }

        // TC-TOPIC-GVOC-05 | B | GetVocabulariesByTopicIdAsync called with correct topicId
        [Fact]
        public async Task Handle_WithTopicId_RepoCalledWithCorrectTopicId()
        {
            var repo = GetTopicMock(SampleVocabs());
            await CreateHandler(repo).Handle(MakeQuery("T-XYZ"), CancellationToken.None);
            repo.Verify(x => x.GetVocabulariesByTopicIdAsync("T-XYZ"), Times.Once);
            QACollector.LogTestCase("Topic - Get Study Vocabs", new TestCaseDetail { FunctionGroup = "GetStudyVocabs", TestCaseID = "TC-TOPIC-GVOC-05", Description = "GetVocabulariesByTopicIdAsync called with 'T-XYZ'", ExpectedResult = "Times.Once with correct ID", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TopicId forwarded" } });
        }

        // TC-TOPIC-GVOC-06 | N | Count parameter respected
        [Fact]
        public async Task Handle_Count2_ResultHasMaxCount2()
        {
            var result = await CreateHandler(GetTopicMock(SampleVocabs(10)), GetProgressMock())
                .Handle(MakeQuery(count: 2), CancellationToken.None);
            result.Data.Should().HaveCount(2);
            QACollector.LogTestCase("Topic - Get Study Vocabs", new TestCaseDetail { FunctionGroup = "GetStudyVocabs", TestCaseID = "TC-TOPIC-GVOC-06", Description = "Count=2 limits result to 2 items", ExpectedResult = "Data.Count=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { ".Take(Count) applied" } });
        }
    }
}