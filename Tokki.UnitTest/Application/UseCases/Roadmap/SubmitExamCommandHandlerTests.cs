using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Exam.Commands.SubmitExam;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using UserExamEntity = Tokki.Domain.Entities.UserExam;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap
{
    public class SubmitExamCommandHandlerTests
    {
        private static Mock<IIdGeneratorService> GetIdGenMock(string id = "UE-GEN-001")
        {
            var m = new Mock<IIdGeneratorService>();
            m.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns(id);
            return m;
        }

        private static SubmitExamCommandHandler CreateHandler(
            Mock<IUserRoadmapRepository>?            roadmapRepo = null,
            Mock<IUserWeaknessRepository>?           weakRepo    = null,
            Mock<IRoadmapKnowledgeProfileRepository>? profileRepo = null,
            Mock<IIdGeneratorService>?               idGen       = null)
        {
            var mockWeak    = weakRepo    ?? new Mock<IUserWeaknessRepository>();
            var mockProfile = profileRepo ?? new Mock<IRoadmapKnowledgeProfileRepository>();
            mockWeak.Setup(x => x.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserWeakness>());
            mockWeak.Setup(x => x.AddAsync(It.IsAny<UserWeakness>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockWeak.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockProfile.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((RoadmapKnowledgeProfile?)null);
            mockProfile.Setup(x => x.AddAsync(It.IsAny<RoadmapKnowledgeProfile>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockProfile.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return new SubmitExamCommandHandler(
                (roadmapRepo ?? MockUserRoadmapRepository.GetMock()).Object,
                mockWeak.Object,
                mockProfile.Object,
                (idGen ?? GetIdGenMock()).Object);
        }

        // SubmitExam_01 | A | No exam questions → 404
        [Fact]
        public async Task Handle_NoExamQuestions_ShouldReturn404()
        {
            var repo   = MockUserRoadmapRepository.GetMock(examQuestions: new List<ExamQuestion>());
            var result = await CreateHandler(roadmapRepo: repo).Handle(
                new SubmitExamCommand { ExamId = "EXAM-001", UserId = "USER-001", Answers = new List<UserAnswerDto>() },
                CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Submit Exam", new TestCaseDetail { FunctionGroup = "SubmitExam", TestCaseID = "SubmitExam_01", Description = "No exam questions → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetExamQuestionsForGradingAsync returns []" } });
        }

        // SubmitExam_02 | N | Happy path: 1 correct answer → totalScore > 0, 200
        [Fact]
        public async Task Handle_OneCorrectAnswer_ShouldReturnPositiveScore()
        {
            var eq = new ExamQuestion
            {
                ExamQuestionId = "EQ-001", ExamId = "EXAM-001", QuestionBankId = "QB-001", Score = 10,
                QuestionBank = new QuestionBank
                {
                    QuestionBankId = "QB-001", QuestionTypeId = "QT-001",
                    QuestionOptions = new List<QuestionOption>
                    {
                        new QuestionOption { OptionId = "OPT-CORRECT", IsCorrect = true }
                    }
                }
            };
            var repo = MockUserRoadmapRepository.GetMock(examQuestions: new List<ExamQuestion> { eq });
            var result = await CreateHandler(roadmapRepo: repo).Handle(
                new SubmitExamCommand
                {
                    ExamId = "EXAM-001", UserId = "USER-001",
                    Answers = new List<UserAnswerDto>
                    {
                        new UserAnswerDto { QuestionId = "QB-001", SelectedOptionId = "OPT-CORRECT" }
                    }
                },
                CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(10);
            QACollector.LogTestCase("Roadmap - Submit Exam", new TestCaseDetail { FunctionGroup = "SubmitExam", TestCaseID = "SubmitExam_02", Description = "1 correct answer → Score=10, 200", ExpectedResult = "IsSuccess=true, Data=10", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Correct answer submitted", "Score=10" } });
        }

        // SubmitExam_03 | N | Wrong answer → score = 0
        [Fact]
        public async Task Handle_WrongAnswer_ShouldReturnZeroScore()
        {
            var eq = new ExamQuestion
            {
                ExamQuestionId = "EQ-001", ExamId = "EXAM-001", QuestionBankId = "QB-001", Score = 10,
                QuestionBank = new QuestionBank
                {
                    QuestionBankId = "QB-001", QuestionTypeId = "QT-001",
                    QuestionOptions = new List<QuestionOption>
                    {
                        new QuestionOption { OptionId = "OPT-CORRECT", IsCorrect = true },
                        new QuestionOption { OptionId = "OPT-WRONG",   IsCorrect = false }
                    }
                }
            };
            var repo = MockUserRoadmapRepository.GetMock(examQuestions: new List<ExamQuestion> { eq });
            var result = await CreateHandler(roadmapRepo: repo).Handle(
                new SubmitExamCommand
                {
                    ExamId = "EXAM-001", UserId = "USER-001",
                    Answers = new List<UserAnswerDto>
                    {
                        new UserAnswerDto { QuestionId = "QB-001", SelectedOptionId = "OPT-WRONG" }
                    }
                },
                CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(0);
            QACollector.LogTestCase("Roadmap - Submit Exam", new TestCaseDetail { FunctionGroup = "SubmitExam", TestCaseID = "SubmitExam_03", Description = "Wrong answer → Score=0", ExpectedResult = "IsSuccess=true, Data=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Wrong option selected", "Score=0" } });
        }

        // SubmitExam_04 | N | Empty answers list → score = 0, success
        [Fact]
        public async Task Handle_EmptyAnswers_ShouldReturnZero()
        {
            var eq = new ExamQuestion
            {
                ExamQuestionId = "EQ-001", ExamId = "EXAM-001", QuestionBankId = "QB-001", Score = 10,
                QuestionBank = new QuestionBank { QuestionBankId = "QB-001", QuestionTypeId = "QT-001", QuestionOptions = new List<QuestionOption>() }
            };
            var repo = MockUserRoadmapRepository.GetMock(examQuestions: new List<ExamQuestion> { eq });
            var result = await CreateHandler(roadmapRepo: repo).Handle(
                new SubmitExamCommand { ExamId = "EXAM-001", UserId = "USER-001", Answers = new List<UserAnswerDto>() },
                CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(0);
            QACollector.LogTestCase("Roadmap - Submit Exam", new TestCaseDetail { FunctionGroup = "SubmitExam", TestCaseID = "SubmitExam_04", Description = "Empty answers → Score=0, success", ExpectedResult = "IsSuccess=true, Score=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Answers=[]", "no grading", "Score=0" } });
        }

        // SubmitExam_05 | B | AddUserExamAsync and AddUserExamAnswersAsync both called
        [Fact]
        public async Task Handle_ValidSubmit_ShouldCallAddUserExamAndAnswers()
        {
            var eq = new ExamQuestion
            {
                ExamQuestionId = "EQ-001", ExamId = "EXAM-001", QuestionBankId = "QB-001", Score = 5,
                QuestionBank = new QuestionBank { QuestionBankId = "QB-001", QuestionTypeId = "QT-001", QuestionOptions = new List<QuestionOption>() }
            };
            var repo = MockUserRoadmapRepository.GetMock(examQuestions: new List<ExamQuestion> { eq });
            await CreateHandler(roadmapRepo: repo).Handle(
                new SubmitExamCommand { ExamId = "EXAM-001", UserId = "USER-001", Answers = new List<UserAnswerDto>() },
                CancellationToken.None);
            repo.Verify(x => x.AddUserExamAsync(It.IsAny<UserExamEntity>()), Times.Once);
            repo.Verify(x => x.AddUserExamAnswersAsync(It.IsAny<List<UserExamAnswer>>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Submit Exam", new TestCaseDetail { FunctionGroup = "SubmitExam", TestCaseID = "SubmitExam_05", Description = "AddUserExamAsync and AddUserExamAnswersAsync both called once", ExpectedResult = "Both Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "UserExam and Answers persisted" } });
        }

        // SubmitExam_06 | N | UserExam.Status = Completed after submission
        [Fact]
        public async Task Handle_ValidSubmit_UserExamStatusShouldBeCompleted()
        {
            var eq = new ExamQuestion
            {
                ExamQuestionId = "EQ-001", ExamId = "EXAM-001", QuestionBankId = "QB-001", Score = 5,
                QuestionBank = new QuestionBank { QuestionBankId = "QB-001", QuestionTypeId = "QT-001", QuestionOptions = new List<QuestionOption>() }
            };
            UserExamEntity? capturedExam = null;
            var repo = MockUserRoadmapRepository.GetMock(examQuestions: new List<ExamQuestion> { eq });
            repo.Setup(x => x.AddUserExamAsync(It.IsAny<UserExamEntity>()))
                .Callback<UserExamEntity>(ue => capturedExam = ue)
                .Returns(Task.CompletedTask);
            await CreateHandler(roadmapRepo: repo).Handle(
                new SubmitExamCommand { ExamId = "EXAM-001", UserId = "USER-001", Answers = new List<UserAnswerDto>() },
                CancellationToken.None);
            capturedExam.Should().NotBeNull();
            capturedExam!.Status.Should().Be(UserExamStatus.Completed);
            QACollector.LogTestCase("Roadmap - Submit Exam", new TestCaseDetail { FunctionGroup = "SubmitExam", TestCaseID = "SubmitExam_06", Description = "UserExam created with Status=Completed", ExpectedResult = "UserExam.Status=Completed", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "UserExamStatus.Completed set on creation" } });
        }
    }
}
