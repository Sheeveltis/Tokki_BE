using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank;
using Tokki.Domain.Enums;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;
using Tokki.Application.UseCases.QuestionBanks.DTOs;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands
{
    public class CreateQuestionBankCommandValidatorTests
    {
        private Mock<IQuestionTypeRepository> _mockTypeRepo;
        private CreateQuestionBankCommandValidator _validator;

        public CreateQuestionBankCommandValidatorTests()
        {
            _mockTypeRepo = new Mock<IQuestionTypeRepository>();
            _validator = new CreateQuestionBankCommandValidator(_mockTypeRepo.Object);
        }

        // TC-QB-CBV-01 | A | Empty QuestionTypeId -> Error
        [Fact]
        public async Task ValidateAsync_EmptyQuestionTypeId_ShouldHaveError()
        {
            var command = new CreateQuestionBankCommand { QuestionTypeId = "" };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.QuestionTypeId)
                .WithErrorMessage("'Loại câu hỏi' must not be empty.");

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandValidator",
                TestCaseID = "TC-QB-CBV-01",
                Description = "Empty TypeId is blocked unconditionally before DB query",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionTypeId is empty" }
            });
        }

        // TC-QB-CBV-02 | A | TypeId Not Found in DB -> Error
        [Fact]
        public async Task ValidateAsync_TypeIdNotFound_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((QuestionType)null);

            var command = new CreateQuestionBankCommand { QuestionTypeId = "InvalidType" };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.QuestionTypeId)
                  .WithErrorMessage(AppErrors.QuestionTypeNotFound.Description);

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandValidator",
                TestCaseID = "TC-QB-CBV-02",
                Description = "Database lookup prevents linking bad question types",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DB returns null" }
            });
        }

        // TC-QB-CBV-03 | A | TypeId Inactive -> Error
        [Fact]
        public async Task ValidateAsync_TypeIdInactive_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("Inactive", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = false, Skill = QuestionSkill.Listening });

            var command = new CreateQuestionBankCommand { QuestionTypeId = "Inactive" };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.QuestionTypeId)
                  .WithErrorMessage("Loại câu hỏi đang bị vô hiệu hóa.");

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandValidator",
                TestCaseID = "TC-QB-CBV-03",
                Description = "Database lookup prevents using disabled configuration templates",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsActive is false" }
            });
        }

        // TC-QB-CBV-04 | A | Listening Skill missing MediaUrl -> Error
        [Fact]
        public async Task ValidateAsync_ListeningSkillWithoutMedia_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ListeningType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Listening });

            var command = new CreateQuestionBankCommand { QuestionTypeId = "ListeningType", MediaUrl = "" };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.MediaUrl)
                  .WithErrorMessage("Câu hỏi Listening bắt buộc phải có MediaUrl.");

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandValidator",
                TestCaseID = "TC-QB-CBV-04",
                Description = "Enforces skill boundaries requiring media for listening banks",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "MediaUrl empty on Listening" }
            });
        }

        // TC-QB-CBV-05 | A | Reading Skill missing Content -> Error
        [Fact]
        public async Task ValidateAsync_ReadingSkillWithoutContent_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ReadingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });

            var command = new CreateQuestionBankCommand { QuestionTypeId = "ReadingType", Content = "" };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Content)
                  .WithErrorMessage("Câu hỏi Reading bắt buộc phải có Content.");

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandValidator",
                TestCaseID = "TC-QB-CBV-05",
                Description = "Enforces skill boundaries requiring passage string for reading banks",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Content empty on Reading" }
            });
        }

        // TC-QB-CBV-06 | A | Writing Skill containing options -> Error
        [Fact]
        public async Task ValidateAsync_WritingSkillWithOptions_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("WritingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Writing });

            var command = new CreateQuestionBankCommand 
            { 
                QuestionTypeId = "WritingType", 
                Options = new List<CreateQuestionOptionDto> { new CreateQuestionOptionDto() } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage(AppErrors.WritingNoOptions.Description);

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandValidator",
                TestCaseID = "TC-QB-CBV-06",
                Description = "Enforces writing tasks to be strictly essay type without multiple choice data",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Options not empty on Writing" }
            });
        }

        // TC-QB-CBV-07 | N | Fully Valid Request -> Pass
        [Fact]
        public async Task ValidateAsync_ValidReadingWithCorrectOptions_ShouldPass()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ReadingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });

            var command = new CreateQuestionBankCommand 
            { 
                QuestionTypeId = "ReadingType", 
                Content = "A valid passage",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption = "1", Content = "Ans 1", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption = "2", Content = "Ans 2", IsCorrect = false }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandValidator",
                TestCaseID = "TC-QB-CBV-07",
                Description = "Valid reading configuration matching exact constraints safely parses logic",
                ExpectedResult = "No errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid skill & multi-choice structure" }
            });
        }
    }
}
