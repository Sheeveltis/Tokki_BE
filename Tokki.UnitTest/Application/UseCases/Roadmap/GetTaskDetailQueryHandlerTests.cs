using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Roadmap.Queries;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap
{
    public class GetTaskDetailQueryHandlerTests
    {
        private static GetTaskDetailQueryHandler CreateHandler(Mock<IUserRoadmapRepository>? repo = null)
            => new GetTaskDetailQueryHandler((repo ?? MockUserRoadmapRepository.GetMock()).Object);

        // GetTaskDetail_01 | A | Task not found → 404
        [Fact]
        public async Task Handle_TaskNotFound_ShouldReturn404()
        {
            var repo   = MockUserRoadmapRepository.GetMock(task: null);
            var result = await CreateHandler(repo).Handle(new GetTaskDetailQuery { TaskId = "TASK-MISSING" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Get Task Detail", new TestCaseDetail { FunctionGroup = "GetTaskDetail", TestCaseID = "GetTaskDetail_01", Description = "Task not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTaskByIdAsync returns null" } });
        }

        // GetTaskDetail_02 | N | Task found → TaskDetailResult mapped correctly
        [Fact]
        public async Task Handle_TaskFound_ShouldReturnMappedDetail()
        {
            var task = MockUserRoadmapRepository.GetSampleTask("TASK-001", "USER-001");
            task.AiGeneratedContent = "Learn grammar basics";
            var repo   = MockUserRoadmapRepository.GetMock(task: task);
            var result = await CreateHandler(repo).Handle(new GetTaskDetailQuery { TaskId = "TASK-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TaskId.Should().Be("TASK-001");
            result.Data.Title.Should().Be("Study Korean");
            result.Data.Content.Should().Be("Learn grammar basics");
            result.Data.IsCompleted.Should().BeFalse();
            QACollector.LogTestCase("Roadmap - Get Task Detail", new TestCaseDetail { FunctionGroup = "GetTaskDetail", TestCaseID = "GetTaskDetail_02", Description = "Task found → TaskDetailResult fields mapped correctly", ExpectedResult = "IsSuccess=true, all fields mapped", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Task exists", "all fields checked" } });
        }

        // GetTaskDetail_03 | N | Task with QuestionType → Skill field mapped
        [Fact]
        public async Task Handle_TaskWithQuestionType_ShouldMapSkill()
        {
            var task = MockUserRoadmapRepository.GetSampleTask("TASK-003");
            task.QuestionType = new QuestionType { QuestionTypeId = "QT-001", Skill = QuestionSkill.Reading };
            var repo   = MockUserRoadmapRepository.GetMock(task: task);
            var result = await CreateHandler(repo).Handle(new GetTaskDetailQuery { TaskId = "TASK-003" }, CancellationToken.None);
            result.Data!.Skill.Should().Be("Reading");
            QACollector.LogTestCase("Roadmap - Get Task Detail", new TestCaseDetail { FunctionGroup = "GetTaskDetail", TestCaseID = "GetTaskDetail_03", Description = "Task with QuestionType → Skill='Reading'", ExpectedResult = "Data.Skill='Reading'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "QuestionType set, Skill=Reading" } });
        }

        // GetTaskDetail_04 | N | Task without QuestionType → Skill null
        [Fact]
        public async Task Handle_TaskWithoutQuestionType_ShouldHaveNullSkill()
        {
            var task = MockUserRoadmapRepository.GetSampleTask("TASK-004");
            task.QuestionType = null;
            var repo   = MockUserRoadmapRepository.GetMock(task: task);
            var result = await CreateHandler(repo).Handle(new GetTaskDetailQuery { TaskId = "TASK-004" }, CancellationToken.None);
            result.Data!.Skill.Should().BeNull();
            QACollector.LogTestCase("Roadmap - Get Task Detail", new TestCaseDetail { FunctionGroup = "GetTaskDetail", TestCaseID = "GetTaskDetail_04", Description = "Task with null QuestionType → Skill=null", ExpectedResult = "Data.Skill=null", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "QuestionType=null", "Skill field = null" } });
        }

        // GetTaskDetail_05 | B | Completed task returns IsCompleted=true
        [Fact]
        public async Task Handle_CompletedTask_ShouldReturnIsCompletedTrue()
        {
            var task = MockUserRoadmapRepository.GetSampleTask("TASK-005", completed: true);
            var repo   = MockUserRoadmapRepository.GetMock(task: task);
            var result = await CreateHandler(repo).Handle(new GetTaskDetailQuery { TaskId = "TASK-005" }, CancellationToken.None);
            result.Data!.IsCompleted.Should().BeTrue();
            QACollector.LogTestCase("Roadmap - Get Task Detail", new TestCaseDetail { FunctionGroup = "GetTaskDetail", TestCaseID = "GetTaskDetail_05", Description = "Completed task → IsCompleted=true in result", ExpectedResult = "Data.IsCompleted=true", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "task.IsCompleted=true" } });
        }

        // GetTaskDetail_06 | B | GetTaskByIdAsync called with correct TaskId
        [Fact]
        public async Task Handle_ValidQuery_GetTaskByIdCalledWithCorrectId()
        {
            const string taskId = "TASK-EXACT-01";
            var task = MockUserRoadmapRepository.GetSampleTask(taskId: taskId);
            var repo = MockUserRoadmapRepository.GetMock(task: task);
            await CreateHandler(repo).Handle(new GetTaskDetailQuery { TaskId = taskId }, CancellationToken.None);
            repo.Verify(x => x.GetTaskByIdAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Get Task Detail", new TestCaseDetail { FunctionGroup = "GetTaskDetail", TestCaseID = "GetTaskDetail_06", Description = "GetTaskByIdAsync called with exact TaskId", ExpectedResult = "GetTaskByIdAsync Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TaskId passed correctly" } });
        }
    }
}
