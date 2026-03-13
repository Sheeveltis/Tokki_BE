using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Exam.Commands.AddQuestionToExam;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories; // <-- Include this namespace for the Factory Mocks
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class AddQuestionToExamCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidData_ShouldUpdateSuccessfully()
        {
            // Arrange
            var command = new AddQuestionToExamCommand
            {
                QuestionBankId = "QB-123",
                ExamId = "EX-456",
                QuestionNo = 1
            };

            // Keep reference to assert changes later
            var existingSlot = new ExamQuestion { ExamId = command.ExamId, QuestionNo = command.QuestionNo, QuestionBankId = "OLD-ID" };

            // 1. USE FACTORY MOCKS (Clean and concise)
            var mockQuestionBankRepo = MockQuestionBankRepository.GetMock(new QuestionBank { QuestionBankId = command.QuestionBankId });
            var mockExamQuestionRepo = MockExamQuestionRepository.GetMock(existingSlot);
            var mockLogger = new Mock<ILogger<AddQuestionToExamCommandHandler>>();

            var handler = new AddQuestionToExamCommandHandler(
                mockExamQuestionRepo.Object,
                mockQuestionBankRepo.Object,
                mockLogger.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            existingSlot.QuestionBankId.Should().Be(command.QuestionBankId); // Verify the ID was updated

            // Log for Excel Report
            QACollector.LogTestCase("Exam - Add Question", new TestCaseDetail
            {
                FunctionGroup = "Add Question To Exam",
                TestCaseID = "TC-EXAM-ADD-01",
                Description = "Successfully replace an existing question in the exam with a new one",
                ExpectedResult = "Slot updated and return Success",
                StatusRound1 = "Passed",
                TestCaseType = "N", // Normal case
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {
                    "Valid QuestionBank ID",
                    "Valid Exam Slot",
                    "Return Success"
                }
            });
        }

        [Fact]
        public async Task Handle_QuestionBankNotFound_ShouldReturn404()
        {
            // Arrange
            var command = new AddQuestionToExamCommand { QuestionBankId = "QB-INVALID", ExamId = "EX-456", QuestionNo = 1 };

            // 1. USE FACTORY MOCKS (Pass null to simulate not found)
            var mockQuestionBankRepo = MockQuestionBankRepository.GetMock(null);
            var mockExamQuestionRepo = MockExamQuestionRepository.GetMock();
            var mockLogger = new Mock<ILogger<AddQuestionToExamCommandHandler>>();

            var handler = new AddQuestionToExamCommandHandler(
                mockExamQuestionRepo.Object,
                mockQuestionBankRepo.Object,
                mockLogger.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Be(AppErrors.QuestionBankNotFound.Description);

            // Log for Excel Report
            QACollector.LogTestCase("Exam - Add Question", new TestCaseDetail
            {
                FunctionGroup = "Add Question To Exam",
                TestCaseID = "TC-EXAM-ADD-02",
                Description = "Attempt to add a question that does not exist in QuestionBank",
                ExpectedResult = "Return 404 QuestionBankNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A", // Abnormal case
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {
                    "Invalid QuestionBank ID",
                    "Return 404 Not Found"
                }
            });
        }

        [Fact]
        public async Task Handle_ExamSlotNotFound_ShouldReturn404()
        {
            // Arrange
            var command = new AddQuestionToExamCommand { QuestionBankId = "QB-123", ExamId = "EX-456", QuestionNo = 99 }; // Invalid slot 99

            // 1. USE FACTORY MOCKS
            var mockQuestionBankRepo = MockQuestionBankRepository.GetMock(new QuestionBank { QuestionBankId = command.QuestionBankId });
            var mockExamQuestionRepo = MockExamQuestionRepository.GetMock(null); // Simulate slot not found
            var mockLogger = new Mock<ILogger<AddQuestionToExamCommandHandler>>();

            var handler = new AddQuestionToExamCommandHandler(
                mockExamQuestionRepo.Object,
                mockQuestionBankRepo.Object,
                mockLogger.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Contain($"Không tìm thấy câu hỏi số {command.QuestionNo}");

            // Log for Excel Report
            QACollector.LogTestCase("Exam - Add Question", new TestCaseDetail
            {
                FunctionGroup = "Add Question To Exam",
                TestCaseID = "TC-EXAM-ADD-03",
                Description = "Attempt to update a question slot that does not exist in the exam",
                ExpectedResult = "Return 404 Slot Not Found",
                StatusRound1 = "Passed",
                TestCaseType = "A", // Abnormal case
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {
                    "Valid QuestionBank ID",
                    "Invalid Exam Slot",
                    "Return 404 Not Found"
                }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrowsException_ShouldReturn500()
        {
            // Arrange
            var command = new AddQuestionToExamCommand { QuestionBankId = "QB-123", ExamId = "EX-456", QuestionNo = 1 };

            // We still use the factory, but we override the setup to throw an Exception
            var mockQuestionBankRepo = MockQuestionBankRepository.GetMock();
            mockQuestionBankRepo.Setup(x => x.GetByIdAsync(command.QuestionBankId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database connection lost"));

            var mockExamQuestionRepo = MockExamQuestionRepository.GetMock();
            var mockLogger = new Mock<ILogger<AddQuestionToExamCommandHandler>>();

            var handler = new AddQuestionToExamCommandHandler(
                mockExamQuestionRepo.Object,
                mockQuestionBankRepo.Object,
                mockLogger.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Message.Should().Be(AppErrors.ServerError.Description);

            // Log for Excel Report
            QACollector.LogTestCase("Exam - Add Question", new TestCaseDetail
            {
                FunctionGroup = "Add Question To Exam",
                TestCaseID = "TC-EXAM-ADD-04",
                Description = "System throws unhandled exception during database operation",
                ExpectedResult = "Catch exception and return 500 Server Error",
                StatusRound1 = "Passed",
                TestCaseType = "A", // Abnormal case
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {
                    "Exception thrown by Database",
                    "Return 500 Server Error"
                }
            });
        }
    }
}