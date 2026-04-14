using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Roadmap.Queries.GetVirtualQuiz;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap
{
    public class GetVirtualQuizQueryHandlerTests
    {
        private static GetVirtualQuizQueryHandler CreateHandler(Mock<IUserRoadmapRepository>? repo = null)
            => new GetVirtualQuizQueryHandler((repo ?? MockUserRoadmapRepository.GetMock()).Object);

        // TC-RM-GVQ-01 | A | QuestionType not found → 404
        [Fact]
        public async Task Handle_QuestionTypeNotExists_ShouldReturn404()
        {
            var repo   = MockUserRoadmapRepository.GetMock(questionTypeExists: false);
            var result = await CreateHandler(repo).Handle(new GetVirtualQuizQuery("QT-MISSING", 5), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Get Virtual Quiz", new TestCaseDetail { FunctionGroup = "GetVirtualQuiz", TestCaseID = "TC-RM-GVQ-01", Description = "QuestionType not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "QuestionTypeExistsAsync=false" } });
        }

        // TC-RM-GVQ-02 | A | No questions in type → 404
        [Fact]
        public async Task Handle_NoQuestionsAvailable_ShouldReturn404()
        {
            var repo   = MockUserRoadmapRepository.GetMock(questionTypeExists: true, randomQuestions: new List<QuestionBank>());
            var result = await CreateHandler(repo).Handle(new GetVirtualQuizQuery("QT-001", 5), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Get Virtual Quiz", new TestCaseDetail { FunctionGroup = "GetVirtualQuiz", TestCaseID = "TC-RM-GVQ-02", Description = "QuestionType exists but no questions → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetRandomQuestionsByTypeAsync returns []" } });
        }

        // TC-RM-GVQ-03 | N | Happy path: 3 questions → 3 VirtualQuizQuestionViewModels
        [Fact]
        public async Task Handle_QuestionsFound_ShouldReturnViewModels()
        {
            var qbs    = MockUserRoadmapRepository.GetSampleQuestionBanks(3);
            var repo   = MockUserRoadmapRepository.GetMock(questionTypeExists: true, randomQuestions: qbs);
            var result = await CreateHandler(repo).Handle(new GetVirtualQuizQuery("QT-001", 3), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            QACollector.LogTestCase("Roadmap - Get Virtual Quiz", new TestCaseDetail { FunctionGroup = "GetVirtualQuiz", TestCaseID = "TC-RM-GVQ-03", Description = "3 questions returned → 3 VirtualQuizQuestionViewModels", ExpectedResult = "IsSuccess=true, Count=3", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "3 questions from repo" } });
        }

        // TC-RM-GVQ-04 | N | DTO fields mapped: QuestionId, Content, Options
        [Fact]
        public async Task Handle_QuestionsFound_ShouldMapFieldsCorrectly()
        {
            var qb = new QuestionBank
            {
                QuestionBankId  = "QB-MAP-01",
                Content         = "What is 안녕?",
                MediaUrl        = null,
                QuestionOptions = new List<QuestionOption>
                {
                    new QuestionOption { OptionId = "OPT-1", KeyOption = "A", Content = "Hello" },
                    new QuestionOption { OptionId = "OPT-2", KeyOption = "B", Content = "Goodbye" }
                }
            };
            var repo   = MockUserRoadmapRepository.GetMock(questionTypeExists: true, randomQuestions: new List<QuestionBank> { qb });
            var result = await CreateHandler(repo).Handle(new GetVirtualQuizQuery("QT-001", 1), CancellationToken.None);
            var vm = result.Data![0];
            vm.QuestionId.Should().Be("QB-MAP-01");
            vm.Content.Should().Be("What is 안녕?");
            vm.Options.Should().HaveCount(2);
            vm.Options[0].KeyOption.Should().Be("A");
            QACollector.LogTestCase("Roadmap - Get Virtual Quiz", new TestCaseDetail { FunctionGroup = "GetVirtualQuiz", TestCaseID = "TC-RM-GVQ-04", Description = "DTO fields mapped: QuestionId, Content, Options", ExpectedResult = "All fields verified", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "QB fields mapped to ViewModel" } });
        }

        // TC-RM-GVQ-05 | B | GetRandomQuestionsByTypeAsync called with correct params
        [Fact]
        public async Task Handle_ValidQuery_GetRandomCalledWithCorrectParams()
        {
            var qbs  = MockUserRoadmapRepository.GetSampleQuestionBanks(2);
            var repo = MockUserRoadmapRepository.GetMock(questionTypeExists: true, randomQuestions: qbs);
            await CreateHandler(repo).Handle(new GetVirtualQuizQuery("QT-SPEC", 7), CancellationToken.None);
            repo.Verify(x => x.GetRandomQuestionsByTypeAsync("QT-SPEC", 7, It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Get Virtual Quiz", new TestCaseDetail { FunctionGroup = "GetVirtualQuiz", TestCaseID = "TC-RM-GVQ-05", Description = "GetRandomQuestionsByTypeAsync called with correct QuestionTypeId and Count", ExpectedResult = "Times.Once('QT-SPEC', 7)", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "QuestionTypeId='QT-SPEC', Count=7" } });
        }

        // TC-RM-GVQ-06 | N | Question with Passage → PassageContent mapped
        [Fact]
        public async Task Handle_QuestionWithPassage_ShouldMapPassageContent()
        {
            var qb = new QuestionBank
            {
                QuestionBankId  = "QB-PAS-01",
                Content         = "Question with passage",
                Passage         = new Passage { PassageId = "PAS-001", Content = "Passage text" },
                QuestionOptions = new List<QuestionOption>()
            };
            var repo   = MockUserRoadmapRepository.GetMock(questionTypeExists: true, randomQuestions: new List<QuestionBank> { qb });
            var result = await CreateHandler(repo).Handle(new GetVirtualQuizQuery("QT-001", 1), CancellationToken.None);
            result.Data![0].PassageContent.Should().Be("Passage text");
            QACollector.LogTestCase("Roadmap - Get Virtual Quiz", new TestCaseDetail { FunctionGroup = "GetVirtualQuiz", TestCaseID = "TC-RM-GVQ-06", Description = "Question with Passage → PassageContent mapped correctly", ExpectedResult = "PassageContent='Passage text'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "QB.Passage.Content mapped" } });
        }
    }
}
