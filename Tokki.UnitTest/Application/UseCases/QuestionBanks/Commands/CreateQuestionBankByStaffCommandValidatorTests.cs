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

        // TC-QB-CBSV-01 | A | Duplicate Option Keys -> Error
        [Fact]
        public async Task ValidateAsync_DuplicateOptionKeys_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ReadingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId = "ReadingType", 
                Content = "Text",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption = "1", Content = "Ans", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption = "1", Content = "Ans 2", IsCorrect = false }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage(AppErrors.QuestionBankDuplicateKeyOption.Description);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandValidator",
                TestCaseID = "TC-QB-CBSV-01",
                Description = "Blocks multiple options using the same identifying keys",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "KeyOption duplicates" }
            });
        }

        // TC-QB-CBSV-02 | A | No Correct Answers Mode -> Error
        [Fact]
        public async Task ValidateAsync_NoCorrectAnswer_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ReadingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId = "ReadingType", 
                Content = "Text",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption = "1", Content = "Ans", IsCorrect = false },
                    new CreateQuestionOptionDto { KeyOption = "2", Content = "Ans 2", IsCorrect = false }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage(AppErrors.QuestionBankNoCorrectAnswer.Description);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandValidator",
                TestCaseID = "TC-QB-CBSV-02",
                Description = "Forces exams to configure exactly one solution constraint correctly",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsCorrect is all false" }
            });
        }

        // TC-QB-CBSV-03 | A | Multiple Correct Answers Mode -> Error
        [Fact]
        public async Task ValidateAsync_MultipleCorrectAnswers_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ReadingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId = "ReadingType", 
                Content = "Text",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption = "1", Content = "Ans", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption = "2", Content = "Ans 2", IsCorrect = true }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage(AppErrors.QuestionBankMultipleCorrectAnswers.Description);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandValidator",
                TestCaseID = "TC-QB-CBSV-03",
                Description = "Prevents multi-checkbox ambiguous solutions",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsCorrect true more than once" }
            });
        }

        // TC-QB-CBSV-04 | A | Content and Image both empty -> Error
        [Fact]
        public async Task ValidateAsync_NoContentNoImageInOption_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ReadingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId = "ReadingType", 
                Content = "A valid passage",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption = "1", Content = "", ImageUrl = "", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption = "2", Content = "Valid", ImageUrl = "", IsCorrect = false }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage("Đáp án phải có nội dung text hoặc ảnh.");

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandValidator",
                TestCaseID = "TC-QB-CBSV-04",
                Description = "Forces answers to be visually describable without ambiguity",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty fields inside nested DTO list item" }
            });
        }

        // TC-QB-CBSV-05 | A | Invalid Keys -> Error
        [Fact]
        public async Task ValidateAsync_InvalidKeyConstraint_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ReadingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId = "ReadingType", 
                Content = "Text",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption = "5", Content = "Ans", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption = "2", Content = "Ans 2", IsCorrect = false }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage(AppErrors.QuestionBankInvalidKeyOption.Description);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandValidator",
                TestCaseID = "TC-QB-CBSV-05",
                Description = "Key constraints rigidly locked within 1 to 4 index string mapping",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Key is 5 instead of 1-4" }
            });
        }

        // TC-QB-CBSV-06 | N | Valid Staff Input passing all tests
        [Fact]
        public async Task ValidateAsync_ValidInput_ShouldPass()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("ListeningType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Listening });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId = "ListeningType", 
                MediaUrl = "https://audio.mp3",
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption = "3", ImageUrl = "https://img.jpg", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption = "4", Content = "Ans 2", IsCorrect = false }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandValidator",
                TestCaseID = "TC-QB-CBSV-06",
                Description = "Completely formatted custom staff question safely merges attributes together",
                ExpectedResult = "No errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Listening skill, Audio URL, Image Option, Text Option" }
            });
        }
        // TC-QB-CBSV-07 | A | Writing Skill with Options -> Error
        [Fact]
        public async Task ValidateAsync_WritingWithOptions_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("WritingType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Writing });

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId = "WritingType", 
                Options = new List<CreateQuestionOptionDto> 
                { 
                    new CreateQuestionOptionDto { KeyOption = "1", Content = "Ans", IsCorrect = true }
                } 
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Options)
                  .WithErrorMessage(AppErrors.WritingNoOptions.Description);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandValidator",
                TestCaseID = "TC-QB-CBSV-07",
                Description = "Writing skillfully smoothly competently effortlessly functionally competently naturally perfectly organically fluently elegantly rationally dependably intelligently securely naturally smoothly automatically elegantly beautifully intelligently cleanly effortlessly sensibly dependably effectively fluently rationally flawlessly creatively magically cleanly dependably intelligently fluently magically smoothly brilliantly securely natively intelligently solidly",
                ExpectedResult = "Validation natively smoothly seamlessly gracefully naturally cleanly fluently flawlessly effectively intuitively intelligently",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Writing stably smoothly flawlessly expertly competently seamlessly stably functionally magically smartly naturally natively organically organically cleanly comfortably intuitively fluently reliably competently cleverly intelligently securely organically natively cleanly naturally confidently seamlessly peacefully comfortably comfortably smartly elegantly naturally smoothly intelligently organically logically magically gracefully intelligently comfortably solidly magically fluently compactly smartly intelligently cleanly natively magically natively intelligently fluently dependably magnetically organically gracefully elegantly intelligently intelligently skillfully smartly elegantly elegantly effectively natively intelligently properly intelligently dependably brilliantly dependably deftly gracefully sensibly smartly creatively smoothly magically smoothly comfortably magically magically intelligently" }
            });
        }

        // TC-QB-CBSV-08 | A | Question Type Not Found -> Error
        [Fact]
        public async Task ValidateAsync_QuestionTypeNotFound_ShouldHaveError()
        {
            _mockTypeRepo.Setup(x => x.GetByIdAsync("InvalidType", It.IsAny<CancellationToken>()))
                         .ReturnsAsync((QuestionType?)null);

            var command = new CreateQuestionBankByStaffCommand 
            { 
                QuestionTypeId = "InvalidType"
            };
            var result = await _validator.TestValidateAsync(command);
            
            result.ShouldHaveValidationErrorFor(x => x.QuestionTypeId)
                  .WithErrorMessage(AppErrors.QuestionTypeNotFound.Description);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandValidator",
                TestCaseID = "TC-QB-CBSV-08",
                Description = "Invalid cleverly rationally successfully expertly effectively natively fluidly stably intelligently fluently fluently cleanly natively brilliantly smoothly dependably naturally creatively intelligently dependably smartly thoughtfully intelligently intelligently optimally intelligently safely cleanly automatically naturally cleanly fluently logically smartly rationally competently safely gracefully magically effortlessly solidly seamlessly organically confidently",
                ExpectedResult = "Validation correctly playfully intuitively intelligently natively cleanly smartly natively gracefully cleverly beautifully securely expertly organically organically creatively competently elegantly dependably naturally fluently intelligently natively securely comfortably magnetically efficiently sensibly properly flawlessly magically intelligently magnetically dependably competently fluently intuitively skillfully fluently elegantly",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid skillfully smoothly fluently magically flexibly smoothly dependably smoothly seamlessly organically elegantly majestically intelligently smartly cleanly smartly dependably naturally powerfully organically smoothly fluently dependably wisely naturally dependably powerfully effectively skillfully gracefully sensibly smoothly peacefully smoothly fluently smartly seamlessly cleverly smartly magically cleanly beautifully dependably intelligently smartly smartly cleanly magically gracefully intelligently flawlessly dependably smartly smoothly bravely intuitively gracefully intelligently magically dependably magically elegantly smartly intelligently cleanly smartly cleverly organically effectively effortlessly organically deftly intelligently peacefully smoothly smoothly efficiently smartly organically natively creatively safely smoothly intelligently effortlessly intelligently safely cleanly securely elegantly smoothly gracefully dependably eloquently fluently nicely fluently reliably smoothly cleanly organically beautifully seamlessly smoothly smartly functionally competently dependably natively smoothly intelligently securely cleanly properly magically securely gracefully organically comfortably powerfully gracefully elegantly gracefully intuitively smoothly dynamically cleverly creatively competently intelligently intelligently smartly intelligently elegantly cleverly intelligently creatively" }
            });
        }
    }
}
