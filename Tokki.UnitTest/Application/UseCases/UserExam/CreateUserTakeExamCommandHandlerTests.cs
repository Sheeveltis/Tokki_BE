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
                TestCaseID = "Create_User_Take_Exam_01",
                Description = "Create session with non-existent ExamId",
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
                TestCaseID = "Create_User_Take_Exam_02",
                Description = "There is already an InProgress session → return the old session, do not create a new one",
                ExpectedResult = "Return Success with UserExamId = SESSION-EXISTING",
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
                TestCaseID = "Create_User_Take_Exam_03",
                Description = "Create a new session for a valid exam → AddSessionAsync is called",
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

        [Fact]
        public async Task Handle_ExamWithWritingQuestions_ShouldCreateWritingAnswers()
        {
            // Arrange
            var command = new CreateUserTakeExamCommand
            {
                UserId = "USER-002",
                ExamId = "EXAM-WRITING"
            };

            var exam = new Tokki.Domain.Entities.Exam
            {
                ExamId = "EXAM-WRITING",
                ExamTemplate = new ExamTemplate
                {
                    TemplateParts = new List<TemplatePart>
                    {
                        new TemplatePart
                        {
                            Skill = QuestionSkill.Writing,
                            QuestionFrom = 1,
                            QuestionTo = 2
                        }
                    }
                },
                ExamQuestions = new List<ExamQuestion>
                {
                    new ExamQuestion { QuestionNo = 1, QuestionBankId = "QB-W01" },
                    new ExamQuestion { QuestionNo = 2, QuestionBankId = "QB-W02" }
                }
            };

            var mockRepo = new Mock<IUserExamRepository>();
            mockRepo.Setup(x => x.GetInProgressSessionAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Domain.Entities.UserExam?)null);

            mockRepo.Setup(x => x.GetExamWithFullStructureAsync("EXAM-WRITING", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(exam);

            Domain.Entities.UserExam? captured = null;
            mockRepo.Setup(x => x.AddSessionAsync(It.IsAny<Domain.Entities.UserExam>(), It.IsAny<CancellationToken>()))
                    .Callback<Domain.Entities.UserExam, CancellationToken>((s, _) => captured = s)
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(repo: mockRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.UserExamWritingAnswers.Should().HaveCount(2);
            captured.UserExamAnswers.Should().BeEmpty();

            QACollector.LogTestCase("UserExam - Create Session", new TestCaseDetail
            {
                FunctionGroup = "Create User Take Exam",
                TestCaseID = "Create_User_Take_Exam_04",
                Description = "Exam has only Writing questions → creates UserExamWritingAnswer for each question",
                ExpectedResult = "2 UserExamWritingAnswers created, UserExamAnswers empty",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "All questions belong to Writing part",
                    "UserExamWritingAnswers.Count = 2",
                    "UserExamAnswers is empty"
                }
            });
        }

        [Fact]
        public async Task Handle_ExamWithMixedSkills_ShouldCreateBothAnswerTypes()
        {
            // Arrange
            var command = new CreateUserTakeExamCommand
            {
                UserId = "USER-003",
                ExamId = "EXAM-MIXED"
            };

            var exam = new Tokki.Domain.Entities.Exam
            {
                ExamId = "EXAM-MIXED",
                ExamTemplate = new ExamTemplate
                {
                    TemplateParts = new List<TemplatePart>
                    {
                        new TemplatePart { Skill = QuestionSkill.Reading,  QuestionFrom = 1, QuestionTo = 2 },
                        new TemplatePart { Skill = QuestionSkill.Writing,  QuestionFrom = 3, QuestionTo = 4 }
                    }
                },
                ExamQuestions = new List<ExamQuestion>
                {
                    new ExamQuestion { QuestionNo = 1, QuestionBankId = "QB-R01" },
                    new ExamQuestion { QuestionNo = 2, QuestionBankId = "QB-R02" },
                    new ExamQuestion { QuestionNo = 3, QuestionBankId = "QB-W01" },
                    new ExamQuestion { QuestionNo = 4, QuestionBankId = "QB-W02" }
                }
            };

            var mockRepo = new Mock<IUserExamRepository>();
            mockRepo.Setup(x => x.GetInProgressSessionAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Domain.Entities.UserExam?)null);

            mockRepo.Setup(x => x.GetExamWithFullStructureAsync("EXAM-MIXED", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(exam);

            Domain.Entities.UserExam? captured = null;
            mockRepo.Setup(x => x.AddSessionAsync(It.IsAny<Domain.Entities.UserExam>(), It.IsAny<CancellationToken>()))
                    .Callback<Domain.Entities.UserExam, CancellationToken>((s, _) => captured = s)
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(repo: mockRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.UserExamAnswers.Should().HaveCount(2);
            captured.UserExamWritingAnswers.Should().HaveCount(2);

            QACollector.LogTestCase("UserExam - Create Session", new TestCaseDetail
            {
                FunctionGroup = "Create User Take Exam",
                TestCaseID = "Create_User_Take_Exam_05",
                Description = "Exam has Reading and Writing questions → creates both UserExamAnswer and UserExamWritingAnswer",
                ExpectedResult = "2 UserExamAnswers + 2 UserExamWritingAnswers",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Questions 1-2 are Reading, Questions 3-4 are Writing",
                    "UserExamAnswers.Count = 2",
                    "UserExamWritingAnswers.Count = 2"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidExam_ShouldSetCurrentSkillToFirstPartOrderedByQuestionFrom()
        {
            // Arrange – Writing part has lower QuestionFrom so it should be CurrentSkill
            var command = new CreateUserTakeExamCommand
            {
                UserId = "USER-004",
                ExamId = "EXAM-ORDER"
            };

            var exam = new Tokki.Domain.Entities.Exam
            {
                ExamId = "EXAM-ORDER",
                ExamTemplate = new ExamTemplate
                {
                    TemplateParts = new List<TemplatePart>
                    {
                        // Reading starts at 5 (later)
                        new TemplatePart { Skill = QuestionSkill.Reading,  QuestionFrom = 5, QuestionTo = 6 },
                        // Listening starts at 1 (first) → should become CurrentSkill
                        new TemplatePart { Skill = QuestionSkill.Listening, QuestionFrom = 1, QuestionTo = 4 }
                    }
                },
                ExamQuestions = new List<ExamQuestion>
                {
                    new ExamQuestion { QuestionNo = 1, QuestionBankId = "QB-L01" },
                    new ExamQuestion { QuestionNo = 5, QuestionBankId = "QB-R01" }
                }
            };

            var mockRepo = new Mock<IUserExamRepository>();
            mockRepo.Setup(x => x.GetInProgressSessionAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Domain.Entities.UserExam?)null);

            mockRepo.Setup(x => x.GetExamWithFullStructureAsync("EXAM-ORDER", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(exam);

            Domain.Entities.UserExam? captured = null;
            mockRepo.Setup(x => x.AddSessionAsync(It.IsAny<Domain.Entities.UserExam>(), It.IsAny<CancellationToken>()))
                    .Callback<Domain.Entities.UserExam, CancellationToken>((s, _) => captured = s)
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(repo: mockRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.CurrentSkill.Should().Be(QuestionSkill.Listening);

            QACollector.LogTestCase("UserExam - Create Session", new TestCaseDetail
            {
                FunctionGroup = "Create User Take Exam",
                TestCaseID = "Create_User_Take_Exam_06",
                Description = "CurrentSkill is assigned to the skill of the TemplatePart with the smallest QuestionFrom",
                ExpectedResult = "CurrentSkill = Listening (QuestionFrom=1 < Reading QuestionFrom=5)",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "TemplateParts added in reverse order (Reading first, Listening second)",
                    "OrderBy(QuestionFrom) picks Listening as first skill",
                    "CurrentSkill = QuestionSkill.Listening"
                }
            });
        }
    }
}