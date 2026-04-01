using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.Queries.GetEntranceFeedback;
using Tokki.Application.UseCases.UserExam.Queries.GetUserExamResult;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap
{
    public class GetEntranceFeedbackQueryHandlerTests
    {
        private static GetEntranceFeedbackQueryHandler CreateHandler(
            Mock<IUserExamRepository>? examRepo  = null,
            Mock<IAiRoadmapService>?   aiService = null,
            Mock<IMediator>?           mediator  = null)
        {
            var mockExam    = examRepo  ?? new Mock<IUserExamRepository>();
            var mockAi      = aiService ?? new Mock<IAiRoadmapService>();
            var mockMediator = mediator ?? new Mock<IMediator>();
            return new GetEntranceFeedbackQueryHandler(mockExam.Object, mockAi.Object, mockMediator.Object);
        }

        private static GetEntranceFeedbackQuery MakeQuery(string examId = "UE-001") => new GetEntranceFeedbackQuery
        {
            UserId           = "USER-001",
            UserExamId       = examId,
            TargetAim        = TargetAimLevel.Topik_I_Level1,
            SelfDeclaredLevel = CurrentTopikLevel.Level_1
        };

        // TC-RM-GEF-01 | A | HasPendingWritingAnswers → 202
        [Fact]
        public async Task Handle_PendingWritingAnswers_ShouldReturn202()
        {
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.HasPendingWritingAnswersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var result = await CreateHandler(examRepo).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(202);
            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "TC-RM-GEF-01", Description = "Pending writing answers → 202", ExpectedResult = "IsSuccess=false, 202", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "HasPendingWritingAnswersAsync=true" } });
        }

        // TC-RM-GEF-02 | A | UserExam result not found via mediator → 404
        [Fact]
        public async Task Handle_ExamResultNotFound_ShouldReturn404()
        {
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.HasPendingWritingAnswersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            var mediator = new Mock<IMediator>();
            mediator.Setup(x => x.Send(It.IsAny<GetUserExamResultQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(OperationResult<UserExamResultResponse>.Failure("Not found", 404));
            var result = await CreateHandler(examRepo, mediator: mediator).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "TC-RM-GEF-02", Description = "Mediator returns failure for exam result → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetUserExamResultQuery → failure" } });
        }

        // TC-RM-GEF-03 | A | IncorrectQuestionTypes returns null → 404
        [Fact]
        public async Task Handle_NullQuestionTypes_ShouldReturn404()
        {
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.HasPendingWritingAnswersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            examRepo.Setup(x => x.GetIncorrectQuestionTypesByExamIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((List<QuestionType>?)null);
            examRepo.Setup(x => x.SaveSelfDeclaredLevelAsync(It.IsAny<string>(), It.IsAny<CurrentTopikLevel>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var skillResult = new UserExamResultResponse { Listening = new SkillScoreDto { Score = 50 }, Reading = new SkillScoreDto { Score = 50 }, Writing = new SkillScoreDto { Score = 0 } };
            var mediator = new Mock<IMediator>();
            mediator.Setup(x => x.Send(It.IsAny<GetUserExamResultQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(OperationResult<UserExamResultResponse>.Success(skillResult));
            var result = await CreateHandler(examRepo, mediator: mediator).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "TC-RM-GEF-03", Description = "GetIncorrectQuestionTypes returns null → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "QuestionTypes=null → failure" } });
        }

        // TC-RM-GEF-04 | N | Happy path: all data available → EntranceFeedbackResult returned
        [Fact]
        public async Task Handle_HappyPath_ShouldReturnFeedbackResult()
        {
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.HasPendingWritingAnswersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            examRepo.Setup(x => x.GetIncorrectQuestionTypesByExamIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuestionType>());
            examRepo.Setup(x => x.SaveSelfDeclaredLevelAsync(It.IsAny<string>(), It.IsAny<CurrentTopikLevel>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var skillResult = new UserExamResultResponse { Listening = new SkillScoreDto { Score = 50 }, Reading = new SkillScoreDto { Score = 50 }, Writing = new SkillScoreDto { Score = 0 } };
            var mediator = new Mock<IMediator>();
            mediator.Setup(x => x.Send(It.IsAny<GetUserExamResultQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(OperationResult<UserExamResultResponse>.Success(skillResult));
            var aiService = new Mock<IAiRoadmapService>();
            aiService.Setup(x => x.GenerateEntranceFeedbackAsync(It.IsAny<TargetAimLevel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<int>()))
                     .ReturnsAsync("Excellent!");
            var result = await CreateHandler(examRepo, aiService, mediator).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AiFeedback.Should().Be("Excellent!");
            result.Data.DurationOptions.Should().HaveCount(3);
            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "TC-RM-GEF-04", Description = "Happy path: AiFeedback and 3 DurationOptions returned", ExpectedResult = "IsSuccess=true, AiFeedback='Excellent!', DurationOptions.Count=3", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All data available", "AiFeedback and options returned" } });
        }

        // TC-RM-GEF-05 | N | AI returns empty feedback → fallback message generated
        [Fact]
        public async Task Handle_AiEmptyFeedback_ShouldUseFallbackMessage()
        {
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.HasPendingWritingAnswersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            examRepo.Setup(x => x.GetIncorrectQuestionTypesByExamIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuestionType>());
            examRepo.Setup(x => x.SaveSelfDeclaredLevelAsync(It.IsAny<string>(), It.IsAny<CurrentTopikLevel>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var skillResult = new UserExamResultResponse { Listening = new SkillScoreDto { Score = 50 }, Reading = new SkillScoreDto { Score = 50 }, Writing = new SkillScoreDto { Score = 0 } };
            var mediator = new Mock<IMediator>();
            mediator.Setup(x => x.Send(It.IsAny<GetUserExamResultQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(OperationResult<UserExamResultResponse>.Success(skillResult));
            var aiService = new Mock<IAiRoadmapService>();
            aiService.Setup(x => x.GenerateEntranceFeedbackAsync(It.IsAny<TargetAimLevel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<int>()))
                     .ReturnsAsync(string.Empty);
            var result = await CreateHandler(examRepo, aiService, mediator).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.AiFeedback.Should().NotBeNullOrEmpty();
            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "TC-RM-GEF-05", Description = "AI returns empty string → fallback message used", ExpectedResult = "IsSuccess=true, AiFeedback non-empty (fallback)", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "AI returns ''", "fallback feedback generated" } });
        }

        // TC-RM-GEF-06 | B | HasPendingWritingAnswersAsync called with correct UserExamId
        [Fact]
        public async Task Handle_ValidQuery_HasPendingCalledWithCorrectId()
        {
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.HasPendingWritingAnswersAsync("UE-SPECIFIC", It.IsAny<CancellationToken>())).ReturnsAsync(true);
            await CreateHandler(examRepo).Handle(new GetEntranceFeedbackQuery { UserExamId = "UE-SPECIFIC", UserId = "USER-001", TargetAim = TargetAimLevel.Topik_I_Level1, SelfDeclaredLevel = CurrentTopikLevel.Level_1 }, CancellationToken.None);
            examRepo.Verify(x => x.HasPendingWritingAnswersAsync("UE-SPECIFIC", It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "TC-RM-GEF-06", Description = "HasPendingWritingAnswersAsync called with exact UserExamId", ExpectedResult = "Times.Once with 'UE-SPECIFIC'", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "UserExamId passed correctly" } });
        }
    }
}
