using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.Commands.CreateUserTakeExam;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class CreateUserTakeExamCommandHandlerTests
    {
        private CreateUserTakeExamCommandHandler CreateHandler(
            Mock<IUserExamRepository>? repo = null)
        {
            return new CreateUserTakeExamCommandHandler(
                (repo ?? new Mock<IUserExamRepository>()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_ExamNotFound_ShouldReturn404()
        {
            var command = new CreateUserTakeExamCommand
            {
                UserId = "USER-001",
                ExamId = "EXAM-INVALID"
            };

            var mockRepo = new Mock<IUserExamRepository>();
            mockRepo.Setup(x => x.GetInProgressSessionAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Domain.Entities.UserExam?)null);

            mockRepo.Setup(x => x.GetExamWithFullStructureAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Domain.Entities.Exam?)null);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("UserExam - Create Session", new TestCaseDetail
            {
                FunctionGroup = "Create User Take Exam",
                TestCaseID = "TC-UEXM-CRE-01",
                Description = "Tạo session với ExamId không tồn tại",
                ExpectedResult = "Return 404",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid ExamId",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_InProgressSessionExists_ShouldReturnExistingSession()
        {
            // Đã có session InProgress → trả về session cũ, không tạo mới
            var command = new CreateUserTakeExamCommand
            {
                UserId = "USER-001",
                ExamId = "EXAM-001"
            };

            var existingSession = new Domain.Entities.UserExam
            {
                UserExamId = "SESSION-EXISTING",
                UserId = "USER-001",
                ExamId = "EXAM-001",
                Status = UserExamStatus.InProgress
            };

            var mockRepo = new Mock<IUserExamRepository>();
            mockRepo.Setup(x => x.GetInProgressSessionAsync(
                        "USER-001",
                        "EXAM-001",
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingSession);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.UserExamId.Should().Be("SESSION-EXISTING");

            // AddSessionAsync không được gọi vì dùng session cũ
            mockRepo.Verify(
                x => x.AddSessionAsync(
                    It.IsAny<Domain.Entities.UserExam>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            QACollector.LogTestCase("UserExam - Create Session", new TestCaseDetail
            {
                FunctionGroup = "Create User Take Exam",
                TestCaseID = "TC-UEXM-CRE-02",
                Description = "Đã có session InProgress → trả về session cũ, không tạo mới",
                ExpectedResult = "Return Success với UserExamId = SESSION-EXISTING",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Existing InProgress session (boundary: idempotent)",
                    "AddSessionAsync NOT called",
                    "Return existing session"
                }
            });
        }

        // ⚠️ NOTE: Test này có thể FAIL vì cần mock phức tạp cho ExamTemplate.TemplateParts
        [Fact]
        public async Task Handle_ValidExam_ShouldCreateNewSessionAndReturn200()
        {
            var command = new CreateUserTakeExamCommand
            {
                UserId = "USER-001",
                ExamId = "EXAM-001"
            };

            var mockRepo = new Mock<IUserExamRepository>();

            // Không có session cũ
            mockRepo.Setup(x => x.GetInProgressSessionAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Domain.Entities.UserExam?)null);

            // Exam có đầy đủ structure
            var exam = new Tokki.Domain.Entities.Exam
            {
                ExamId = "EXAM-001",
                ExamTemplate = new ExamTemplate
                {
                    TemplateParts = new List<TemplatePart>
                    {
                        new TemplatePart
                        {
                            Skill = QuestionSkill.Reading,
                            QuestionFrom = 1,
                            QuestionTo = 2
                        }
                    }
                },
                ExamQuestions = new List<ExamQuestion>
                {
                    new ExamQuestion
                    {
                        QuestionNo = 1,
                        QuestionBankId = "QB-001"
                    },
                    new ExamQuestion
                    {
                        QuestionNo = 2,
                        QuestionBankId = "QB-002"
                    }
                }
            };

            mockRepo.Setup(x => x.GetExamWithFullStructureAsync(
                        "EXAM-001",
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(exam);

            mockRepo.Setup(x => x.AddSessionAsync(
                        It.IsAny<Domain.Entities.UserExam>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.UserExamId.Should().NotBeNullOrEmpty();

            mockRepo.Verify(
                x => x.AddSessionAsync(
                    It.IsAny<Domain.Entities.UserExam>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            QACollector.LogTestCase("UserExam - Create Session", new TestCaseDetail
            {
                FunctionGroup = "Create User Take Exam",
                TestCaseID = "TC-UEXM-CRE-03",
                Description = "Tạo session mới cho exam hợp lệ → AddSessionAsync được gọi",
                ExpectedResult = "Return Success, AddSessionAsync called once",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No existing session",
                    "Valid Exam with questions",
                    "AddSessionAsync called once",
                    "Return Success"
                }
            });
        }
    }
}