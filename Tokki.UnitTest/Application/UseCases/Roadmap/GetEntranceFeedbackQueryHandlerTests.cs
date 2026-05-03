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

        // GetEntranceFeedback_01 | A | HasPendingWritingAnswers → 202
        [Fact]
        public async Task Handle_PendingWritingAnswers_ShouldReturn202()
        {
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.HasPendingWritingAnswersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var result = await CreateHandler(examRepo).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(202);
            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "GetEntranceFeedback_01", Description = "Pending writing answers → 202", ExpectedResult = "IsSuccess=false, 202", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "HasPendingWritingAnswersAsync=true" } });
        }

        // GetEntranceFeedback_02 | A | UserExam result not found via mediator → 404
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
            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "GetEntranceFeedback_02", Description = "Mediator returns failure for exam result → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetUserExamResultQuery → failure" } });
        }

        // GetEntranceFeedback_03 | A | IncorrectQuestionTypes returns null → 404
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
            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "GetEntranceFeedback_03", Description = "GetIncorrectQuestionTypes returns null → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "QuestionTypes=null → failure" } });
        }

        // GetEntranceFeedback_04 | N | Happy path: all data available → EntranceFeedbackResult returned
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
            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "GetEntranceFeedback_04", Description = "Happy path: AiFeedback and 3 DurationOptions returned", ExpectedResult = "IsSuccess=true, AiFeedback='Excellent!', DurationOptions.Count=3", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All data available", "AiFeedback and options returned" } });
        }

        // GetEntranceFeedback_05 | N | AI returns empty feedback → fallback message generated
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
            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "GetEntranceFeedback_05", Description = "AI returns empty string → fallback message used", ExpectedResult = "IsSuccess=true, AiFeedback non-empty (fallback)", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "AI returns ''", "fallback feedback generated" } });
        }

        // GetEntranceFeedback_06 | B | HasPendingWritingAnswersAsync called with correct UserExamId
        [Fact]
        public async Task Handle_ValidQuery_HasPendingCalledWithCorrectId()
        {
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.HasPendingWritingAnswersAsync("UE-SPECIFIC", It.IsAny<CancellationToken>())).ReturnsAsync(true);
            await CreateHandler(examRepo).Handle(new GetEntranceFeedbackQuery { UserExamId = "UE-SPECIFIC", UserId = "USER-001", TargetAim = TargetAimLevel.Topik_I_Level1, SelfDeclaredLevel = CurrentTopikLevel.Level_1 }, CancellationToken.None);
            examRepo.Verify(x => x.HasPendingWritingAnswersAsync("UE-SPECIFIC", It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "GetEntranceFeedback_06", Description = "HasPendingWritingAnswersAsync called with exact UserExamId", ExpectedResult = "Times.Once with 'UE-SPECIFIC'", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "UserExamId passed correctly" } });
        }
        // GetEntranceFeedback_07 | N | Branch: TargetAim Topik_II, Score>=120 -> Level_3, 4 weak types -> 60Days
        [Fact]
        public async Task Handle_TargetAimTopikII_ShouldCalculateLevelAndDurationOptionsCorrectly()
        {
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.HasPendingWritingAnswersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            
            var weakTypes = new List<QuestionType>
            {
                new QuestionType { Skill = QuestionSkill.Reading, QuestionTypeId = "1", Code = "R1", Name = "R1" },
                new QuestionType { Skill = QuestionSkill.Reading, QuestionTypeId = "2", Code = "R2", Name = "R2" },
                new QuestionType { Skill = QuestionSkill.Listening, QuestionTypeId = "3", Code = "L1", Name = "L1" },
                new QuestionType { Skill = QuestionSkill.Listening, QuestionTypeId = "4", Code = "L2", Name = "L2" }
            };
            examRepo.Setup(x => x.GetIncorrectQuestionTypesByExamIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(weakTypes);
            examRepo.Setup(x => x.SaveSelfDeclaredLevelAsync(It.IsAny<string>(), It.IsAny<CurrentTopikLevel>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            
            // Total score: 50 + 50 + 20 = 120 -> Level 3
            var skillResult = new UserExamResultResponse { Listening = new SkillScoreDto { Score = 50 }, Reading = new SkillScoreDto { Score = 50 }, Writing = new SkillScoreDto { Score = 20 } };
            var mediator = new Mock<IMediator>();
            mediator.Setup(x => x.Send(It.IsAny<GetUserExamResultQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(OperationResult<UserExamResultResponse>.Success(skillResult));
            
            var aiService = new Mock<IAiRoadmapService>();
            aiService.Setup(x => x.GenerateEntranceFeedbackAsync(It.IsAny<TargetAimLevel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<int>()))
                     .ReturnsAsync("Mock feedback");

            var handler = CreateHandler(examRepo, aiService, mediator);
            var query = new GetEntranceFeedbackQuery
            {
                UserId = "USER-001", UserExamId = "UE-001",
                TargetAim = TargetAimLevel.Topik_II_Level3, // Target is II
                SelfDeclaredLevel = CurrentTopikLevel.Level_4 // Declared 4, Calculated 3 -> Final 3
            };

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.SuggestedCurrentLevel.Should().Be(CurrentTopikLevel.Level_3);
            
            // 4 weak types -> recommend 60 days
            var recommendedOption = result.Data.DurationOptions.Find(o => o.Recommended);
            recommendedOption.Should().NotBeNull();
            recommendedOption!.Days.Should().Be(60);

            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "GetEntranceFeedback_07", Description = "Topik II branch with Score>=120 mapping Level 3 and 4 weak types mapping 60 Days", ExpectedResult = "Level=3, 60 Days recommended", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "CalculateLevel Topik II branch", "recommend60 = true" } });
        }

        // GetEntranceFeedback_08 | N | Branch: TargetAim Topik_I, Score>=140 -> Level_2, 9 weak types -> 90Days
        [Fact]
        public async Task Handle_TargetAimTopikI_ShouldCalculateLevelAndDurationOptionsCorrectly()
        {
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.HasPendingWritingAnswersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            
            var weakTypes = new List<QuestionType>
            {
                new QuestionType { Skill = QuestionSkill.Reading, QuestionTypeId = "1", Code = "R1", Name = "R1" },
                new QuestionType { Skill = QuestionSkill.Reading, QuestionTypeId = "2", Code = "R2", Name = "R2" },
                new QuestionType { Skill = QuestionSkill.Reading, QuestionTypeId = "3", Code = "R3", Name = "R3" },
                new QuestionType { Skill = QuestionSkill.Listening, QuestionTypeId = "4", Code = "L1", Name = "L1" },
                new QuestionType { Skill = QuestionSkill.Listening, QuestionTypeId = "5", Code = "L2", Name = "L2" },
                new QuestionType { Skill = QuestionSkill.Listening, QuestionTypeId = "6", Code = "L3", Name = "L3" },
                new QuestionType { Skill = QuestionSkill.Writing, QuestionTypeId = "7", Code = "W1", Name = "W1" },
                new QuestionType { Skill = QuestionSkill.Writing, QuestionTypeId = "8", Code = "W2", Name = "W2" },
                new QuestionType { Skill = QuestionSkill.Writing, QuestionTypeId = "9", Code = "W3", Name = "W3" }
            };
            examRepo.Setup(x => x.GetIncorrectQuestionTypesByExamIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(weakTypes);
            examRepo.Setup(x => x.SaveSelfDeclaredLevelAsync(It.IsAny<string>(), It.IsAny<CurrentTopikLevel>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            
            // Topik I ignores writing for level mapping. Score: 70 + 70 = 140 -> Level 2
            var skillResult = new UserExamResultResponse { Listening = new SkillScoreDto { Score = 70 }, Reading = new SkillScoreDto { Score = 70 }, Writing = new SkillScoreDto { Score = 20 } };
            var mediator = new Mock<IMediator>();
            mediator.Setup(x => x.Send(It.IsAny<GetUserExamResultQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(OperationResult<UserExamResultResponse>.Success(skillResult));
            
            var aiService = new Mock<IAiRoadmapService>();
            aiService.Setup(x => x.GenerateEntranceFeedbackAsync(It.IsAny<TargetAimLevel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<int>()))
                     .ReturnsAsync("Mock feedback");

            var handler = CreateHandler(examRepo, aiService, mediator);
            var query = new GetEntranceFeedbackQuery
            {
                UserId = "USER-001", UserExamId = "UE-001",
                TargetAim = TargetAimLevel.Topik_I_Level2, // Target is I
                SelfDeclaredLevel = CurrentTopikLevel.Level_1 // Declared 1, Calculated 2 -> Final 1
            };

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.SuggestedCurrentLevel.Should().Be(CurrentTopikLevel.Level_1); // min(1, 2)
            
            // 9 weak types -> recommend 90 days
            var recommendedOption = result.Data.DurationOptions.Find(o => o.Recommended);
            recommendedOption.Should().NotBeNull();
            recommendedOption!.Days.Should().Be(90);

            QACollector.LogTestCase("Roadmap - Get Entrance Feedback", new TestCaseDetail { FunctionGroup = "GetEntranceFeedback", TestCaseID = "GetEntranceFeedback_08", Description = "Topik I branch with TopikIScore>=140 mapping Level 2 and 9 weak types mapping 90 Days", ExpectedResult = "Level=1 (min check), 90 Days recommended", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "CalculateLevel Topik I branch", "recommend90 = true" } });
        }
    }
}
