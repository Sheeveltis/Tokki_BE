using MediatR;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockUserRoadmapRepository
    {
        public static Mock<IUserRoadmapRepository> GetMock(
            UserRoadmap?      activeRoadmap     = null,
            RoadmapDailyTask? task              = null,
            RoadmapWeek?      week              = null,
            RoadmapWeek?      weekByIndex       = null,
            Exam?             entranceExam      = null,
            bool              questionTypeExists = false,
            List<QuestionBank>? randomQuestions  = null,
            UserExam?         userExam           = null,
            List<ExamQuestion>? examQuestions    = null)
        {
            var mock = new Mock<IUserRoadmapRepository>();

            mock.Setup(x => x.GetActiveRoadmapByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeRoadmap);

            mock.Setup(x => x.GetTaskByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            mock.Setup(x => x.GetWeekByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(week);

            mock.Setup(x => x.GetWeekByIndexAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(weekByIndex);

            mock.Setup(x => x.GetEntranceExamByConfigKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entranceExam);

            mock.Setup(x => x.QuestionTypeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(questionTypeExists);

            mock.Setup(x => x.GetRandomQuestionsByTypeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(randomQuestions ?? new List<QuestionBank>());

            mock.Setup(x => x.GetUserExamByExamIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userExam);

            mock.Setup(x => x.GetExamQuestionsForGradingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(examQuestions ?? new List<ExamQuestion>());

            mock.Setup(x => x.AddUserExamAsync(It.IsAny<UserExam>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.AddUserExamAnswersAsync(It.IsAny<List<UserExamAnswer>>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.AddAsync(It.IsAny<UserRoadmap>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            mock.Setup(x => x.GetValidQuestionTypeIdsByLevelAsync(It.IsAny<CurrentTopikLevel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());

            mock.Setup(x => x.GetQuestionTypeMenuAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionTypeMenuItem>());

            mock.Setup(x => x.GetGrammarMenuAsync(It.IsAny<List<string>>(), It.IsAny<CurrentTopikLevel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GrammarMenuItem>());

            return mock;
        }

        // ── Sample Data ───────────────────────────────────────────
        public static UserRoadmap GetSampleActiveRoadmap(string userId = "USER-001", string roadmapId = "RM-001")
        {
            var roadmap = new UserRoadmap
            {
                UserRoadmapId     = roadmapId,
                UserId            = userId,
                CurrentStatus     = UserRoadmapStatus.Active,
                TargetAim         = TargetAimLevel.Topik_I_Level1,
                CurrentLevel      = CurrentTopikLevel.Level_1,
                OverallAiAssessment = "Good start",
                Weeks             = new List<RoadmapWeek>()
            };
            return roadmap;
        }

        public static RoadmapDailyTask GetSampleTask(
            string taskId    = "TASK-001",
            string userId    = "USER-001",
            string roadmapId = "RM-001",
            bool   completed = false)
        {
            var roadmap = GetSampleActiveRoadmap(userId, roadmapId);
            var week = new RoadmapWeek
            {
                RoadmapWeekId = "WEEK-001",
                UserRoadmap   = roadmap,
                WeekIndex     = 1,
                DailyTasks    = new List<RoadmapDailyTask>()
            };
            roadmap.Weeks.Add(week);
            return new RoadmapDailyTask
            {
                TaskId      = taskId,
                RoadmapWeekId = week.RoadmapWeekId,
                RoadmapWeek = week,
                Title       = "Study Korean",
                TaskType    = RoadmapTaskType.LearnTheory,
                IsCompleted = completed,
                DayIndex    = 1
            };
        }

        public static RoadmapWeek GetSampleWeek(
            string weekId    = "WEEK-001",
            string userId    = "USER-001",
            string roadmapId = "RM-001",
            int    weekIndex  = 1,
            string? examId   = null)
        {
            var roadmap = GetSampleActiveRoadmap(userId, roadmapId);
            var week = new RoadmapWeek
            {
                RoadmapWeekId = weekId,
                UserRoadmap   = roadmap,
                WeekIndex     = weekIndex,
                WeeklyExamId  = examId,
                Status        = RoadmapWeekStatus.InProgress,
                DailyTasks    = new List<RoadmapDailyTask>()
            };
            roadmap.Weeks.Add(week);
            return week;
        }

        public static Exam GetSampleEntranceExam(string examId = "EXAM-001") => new Exam
        {
            ExamId   = examId,
            Title    = "TOPIK I Entrance Exam",
            Duration = 100
        };

        public static List<QuestionBank> GetSampleQuestionBanks(int count = 3) =>
            Enumerable.Range(1, count).Select(i => new QuestionBank
            {
                QuestionBankId = $"QB-00{i}",
                Content        = $"Question {i}",
                QuestionOptions = new List<QuestionOption>
                {
                    new QuestionOption { OptionId = $"OPT-{i}-1", KeyOption = "A", Content = "Option A", IsCorrect = true },
                    new QuestionOption { OptionId = $"OPT-{i}-2", KeyOption = "B", Content = "Option B", IsCorrect = false }
                }
            }).ToList();
    }
}
