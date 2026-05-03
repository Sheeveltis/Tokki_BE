using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBankByStaff;
using Tokki.Domain.Enums;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;
using Tokki.Application.UseCases.QuestionBanks.DTOs;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands
{
    public class CreateQuestionBankByStaffCommandValidatorTests
    {
        private Mock<IQuestionTypeRepository> _mockTypeRepo;
        private CreateQuestionBankByStaffCommandValidator _validator;

        public CreateQuestionBankByStaffCommandValidatorTests()
        {
            _mockTypeRepo = new Mock<IQuestionTypeRepository>();
            _validator = new CreateQuestionBankByStaffCommandValidator(_mockTypeRepo.Object);
        }

        // CreateQuestionBankByStaffCommandValidator_01 | A | Duplicate Option Keys -> Error
        [Fact]
        public async Task ValidateAsync_DuplicateOptionKeys_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ReadingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId ="ReadingType", 
                Content ="Text",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption ="1", Content ="Ans", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption ="1", Content ="Ans 2", IsCorrect = false }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage(AppErrors.QuestionBankDuplicateKeyOption.Description);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankByStaffCommandValidator",
                TestCaseID ="CreateQuestionBankByStaffCommandValidator_01",
                Description ="Blocks multiple options using the same identifying keys",
                ExpectedResult ="Validation Error",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"KeyOption duplicates" }
            });
        }

        // CreateQuestionBankByStaffCommandValidator_02 | A | No Correct Answers Mode -> Error
        [Fact]
        public async Task ValidateAsync_NoCorrectAnswer_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ReadingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId ="ReadingType", 
                Content ="Text",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption ="1", Content ="Ans", IsCorrect = false },
                    new CreateQuestionOptionDto { KeyOption ="2", Content ="Ans 2", IsCorrect = false }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage(AppErrors.QuestionBankNoCorrectAnswer.Description);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankByStaffCommandValidator",
                TestCaseID ="CreateQuestionBankByStaffCommandValidator_02",
                Description ="Forces exams to configure exactly one solution constraint correctly",
                ExpectedResult ="Validation Error",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"IsCorrect is all false" }
            });
        }

        // CreateQuestionBankByStaffCommandValidator_03 | A | Multiple Correct Answers Mode -> Error
        [Fact]
        public async Task ValidateAsync_MultipleCorrectAnswers_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ReadingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId ="ReadingType", 
                Content ="Text",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption ="1", Content ="Ans", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption ="2", Content ="Ans 2", IsCorrect = true }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage(AppErrors.QuestionBankMultipleCorrectAnswers.Description);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankByStaffCommandValidator",
                TestCaseID ="CreateQuestionBankByStaffCommandValidator_03",
                Description ="Prevents multi-checkbox ambiguous solutions",
                ExpectedResult ="Validation Error",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"IsCorrect true more than once" }
            });
        }

        // CreateQuestionBankByStaffCommandValidator_04 | A | Content and Image both empty -> Error
        [Fact]
        public async Task ValidateAsync_NoContentNoImageInOption_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ReadingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId ="ReadingType", 
                Content ="A valid passage",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption ="1", Content ="", ImageUrl ="", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption ="2", Content ="Valid", ImageUrl ="", IsCorrect = false }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage("Ðáp án ph?i có n?i dung text ho?c ?nh.");

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankByStaffCommandValidator",
                TestCaseID ="CreateQuestionBankByStaffCommandValidator_04",
                Description ="Forces answers to be visually describable without ambiguity",
                ExpectedResult ="Validation Error",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Empty fields inside nested DTO list item" }
            });
        }

        // CreateQuestionBankByStaffCommandValidator_05 | A | Invalid Keys -> Error
        [Fact]
        public async Task ValidateAsync_InvalidKeyConstraint_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ReadingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId ="ReadingType", 
                Content ="Text",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption ="5", Content ="Ans", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption ="2", Content ="Ans 2", IsCorrect = false }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage(AppErrors.QuestionBankInvalidKeyOption.Description);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankByStaffCommandValidator",
                TestCaseID ="CreateQuestionBankByStaffCommandValidator_05",
                Description ="Key constraints rigidly locked within 1 to 4 index string mapping",
                ExpectedResult ="Validation Error",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Key is 5 instead of 1-4" }
            });
        }

        // CreateQuestionBankByStaffCommandValidator_06 | N | Valid Staff Input passing all tests
        [Fact]
        public async Task ValidateAsync_ValidInput_ShouldPass()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ListeningType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Listening });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId ="ListeningType", 
                MediaUrl ="https://audio.mp3",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption ="3", ImageUrl ="https://img.jpg", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption ="4", Content ="Ans 2", IsCorrect = false }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankByStaffCommandValidator",
                TestCaseID ="CreateQuestionBankByStaffCommandValidator_06",
                Description ="Completely formatted custom staff question safely merges attributes together",
                ExpectedResult ="No errors",
                StatusRound1 ="Passed",
                TestCaseType ="N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Listening skill, Audio URL, Image Option, Text Option" }
            });
        }
        // CreateQuestionBankByStaffCommandValidator_07 | A | Writing Skill with Options -> Error
        [Fact]
        public async Task ValidateAsync_WritingWithOptions_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("WritingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Writing });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId ="WritingType", 
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption ="1", Content ="Ans", IsCorrect = true }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage(AppErrors.WritingNoOptions.Description);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankByStaffCommandValidator",
                TestCaseID ="CreateQuestionBankByStaffCommandValidator_07",
                Description ="Writing",
                ExpectedResult ="Validation",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Writing" }
            });
        }

        // CreateQuestionBankByStaffCommandValidator_08 | A | Question Type Not Found -> Error
        [Fact]
        public async Task ValidateAsync_QuestionTypeNotFound_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("InvalidType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync((QuestionType?)null);

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId ="InvalidType"
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.QuestionTypeId)
                  .WithErrorMessage(AppErrors.QuestionTypeNotFound.Description);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankByStaffCommandValidator",
                TestCaseID ="CreateQuestionBankByStaffCommandValidator_08",
                Description ="Invalid",
                ExpectedResult ="Validation",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Invalid" }
            });
        }
    }
}
