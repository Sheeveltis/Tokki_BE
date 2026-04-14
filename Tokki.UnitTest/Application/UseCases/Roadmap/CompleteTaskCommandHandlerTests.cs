using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Roadmap.Commands.CompleteTask;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap
{
    public class CompleteTaskCommandHandlerTests
    {
        private static CompleteTaskCommandHandler CreateHandler(Mock<IUserRoadmapRepository>? repo = null)
            => new CompleteTaskCommandHandler((repo ?? MockUserRoadmapRepository.GetMock()).Object);

        // TC-RM-COM-01 | A | Task not found → 404
        [Fact]
        public async Task Handle_TaskNotFound_ShouldReturn404()
        {
            var repo   = MockUserRoadmapRepository.GetMock(task: null);
            var result = await CreateHandler(repo).Handle(new CompleteTaskCommand { TaskId = "TASK-MISSING", UserId = "USER-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Complete Task", new TestCaseDetail { FunctionGroup = "CompleteTask", TestCaseID = "TC-RM-COM-01", Description = "Task not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTaskByIdAsync returns null" } });
        }

        // TC-RM-COM-02 | A | Task belongs to different user → 403
        [Fact]
        public async Task Handle_DifferentUser_ShouldReturn403()
        {
            var task   = MockUserRoadmapRepository.GetSampleTask(userId: "OWNER-001");
            var repo   = MockUserRoadmapRepository.GetMock(task: task);
            var result = await CreateHandler(repo).Handle(new CompleteTaskCommand { TaskId = task.TaskId, UserId = "OTHER-USER" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            QACollector.LogTestCase("Roadmap - Complete Task", new TestCaseDetail { FunctionGroup = "CompleteTask", TestCaseID = "TC-RM-COM-02", Description = "Wrong user → 403, SaveChanges never called", ExpectedResult = "IsSuccess=false, 403", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "UserId mismatch" } });
        }

        // TC-RM-COM-03 | N | Task not yet completed → marked as completed, saved, success
        [Fact]
        public async Task Handle_IncompleteTask_ShouldMarkCompleteAndSave()
        {
            var task   = MockUserRoadmapRepository.GetSampleTask(userId: "USER-001", completed: false);
            var repo   = MockUserRoadmapRepository.GetMock(task: task);
            var result = await CreateHandler(repo).Handle(new CompleteTaskCommand { TaskId = task.TaskId, UserId = "USER-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            task.IsCompleted.Should().BeTrue();
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Complete Task", new TestCaseDetail { FunctionGroup = "CompleteTask", TestCaseID = "TC-RM-COM-03", Description = "Incomplete task → IsCompleted=true, saved, success", ExpectedResult = "IsSuccess=true, task.IsCompleted=true", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "task.IsCompleted was false → set true" } });
        }

        // TC-RM-COM-04 | N | Already completed task → no SaveChanges call (idempotent)
        [Fact]
        public async Task Handle_AlreadyCompletedTask_ShouldNotSaveAgain()
        {
            var task   = MockUserRoadmapRepository.GetSampleTask(userId: "USER-001", completed: true);
            var repo   = MockUserRoadmapRepository.GetMock(task: task);
            var result = await CreateHandler(repo).Handle(new CompleteTaskCommand { TaskId = task.TaskId, UserId = "USER-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            QACollector.LogTestCase("Roadmap - Complete Task", new TestCaseDetail { FunctionGroup = "CompleteTask", TestCaseID = "TC-RM-COM-04", Description = "Already completed task → SaveChanges not called (idempotent)", ExpectedResult = "IsSuccess=true, SaveChanges Times.Never", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "task.IsCompleted already true", "SaveChanges skipped" } });
        }

        // TC-RM-COM-05 | B | GetTaskByIdAsync called once with correct TaskId
        [Fact]
        public async Task Handle_ValidCommand_GetTaskByIdCalledWithCorrectId()
        {
            const string taskId = "TASK-SPECIFIC-01";
            var task   = MockUserRoadmapRepository.GetSampleTask(taskId: taskId, userId: "USER-001");
            var repo   = MockUserRoadmapRepository.GetMock(task: task);
            await CreateHandler(repo).Handle(new CompleteTaskCommand { TaskId = taskId, UserId = "USER-001" }, CancellationToken.None);
            repo.Verify(x => x.GetTaskByIdAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Complete Task", new TestCaseDetail { FunctionGroup = "CompleteTask", TestCaseID = "TC-RM-COM-05", Description = "Boundary: GetTaskByIdAsync called with exact TaskId", ExpectedResult = "GetTaskByIdAsync('TASK-SPECIFIC-01') Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TaskId matches exactly" } });
        }

        // TC-RM-COM-06 | A | Repository SaveChanges throws → exception propagates
        [Fact]
        public async Task Handle_SaveThrows_ShouldPropagateException()
        {
            var task   = MockUserRoadmapRepository.GetSampleTask(userId: "USER-001", completed: false);
            var repo   = MockUserRoadmapRepository.GetMock(task: task);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));
            var act = async () => await CreateHandler(repo).Handle(new CompleteTaskCommand { TaskId = task.TaskId, UserId = "USER-001" }, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();
            QACollector.LogTestCase("Roadmap - Complete Task", new TestCaseDetail { FunctionGroup = "CompleteTask", TestCaseID = "TC-RM-COM-06", Description = "SaveChangesAsync throws → exception propagates", ExpectedResult = "Exception thrown", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "SaveChangesAsync throws" } });
        }
    }
}
