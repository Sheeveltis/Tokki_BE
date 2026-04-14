using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabularyByStaff;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary.Commands
{
    public class CreateVocabularyByStaffCommandValidatorTests
    {
        private readonly CreateVocabularyByStaffCommandValidator _validator;

        public CreateVocabularyByStaffCommandValidatorTests()
        {
            _validator = new CreateVocabularyByStaffCommandValidator();
        }

        [Fact]
        public void Validate_EmptyDefinition_ShouldHaveError()
        {
            var command = new CreateVocabularyByStaffCommand { Text = "word", Definition = "" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Definition)
                  .WithErrorMessage("Definition không được để trống.");

            QACollector.LogTestCase("Vocabulary Admin - Create By Staff Validator", new TestCaseDetail
            {
                FunctionGroup     = "CreateVocabularyByStaffCommandValidator",
                TestCaseID        = "TC-VOC-CSBV-01",
                Description       = "Empty Definition",
                ExpectedResult    = "Throws Empty Definition Error",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Definition is empty" }
            });
        }

        [Fact]
        public void Validate_PronunciationLength_ShouldHaveError()
        {
            var command = new CreateVocabularyByStaffCommand { Text = "Word", Definition = "Def", Pronunciation = new string('a', 256) };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Pronunciation)
                  .WithErrorMessage("Pronunciation không được vượt quá 255 ký tự.");

            QACollector.LogTestCase("Vocabulary Admin - Create By Staff Validator", new TestCaseDetail
            {
                FunctionGroup     = "CreateVocabularyByStaffCommandValidator",
                TestCaseID        = "TC-VOC-CSBV-02",
                Description       = "Pronunciation exceeds limits",
                ExpectedResult    = "Throws bounds length error",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "String length > 255" }
            });
        }

        [Fact]
        public void Validate_ExamplesCountExceeded_ShouldHaveError()
        {
            var command = new CreateVocabularyByStaffCommand 
            { 
                Text = "Word", 
                Definition = "Def",
                Examples = new List<VocabularyExampleDto>()
            };
            for(int i = 0; i < 11; i++) command.Examples.Add(new VocabularyExampleDto { Sentence = "S" });

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Examples)
                  .WithErrorMessage("Không thể thêm quá 10 câu ví dụ.");

            QACollector.LogTestCase("Vocabulary Admin - Create By Staff Validator", new TestCaseDetail
            {
                FunctionGroup     = "CreateVocabularyByStaffCommandValidator",
                TestCaseID        = "TC-VOC-CSBV-03",
                Description       = "Over 10 examples",
                ExpectedResult    = "Throws 10 maximum examples count error",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Examples list > 10" }
            });
        }

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveError()
        {
            var command = new CreateVocabularyByStaffCommand 
            { 
                Text = "Word", 
                Definition = "Def",
                Examples = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto { Sentence = "Hello" },
                    new VocabularyExampleDto { Sentence = "Hi" }
                }
            };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Vocabulary Admin - Create By Staff Validator", new TestCaseDetail
            {
                FunctionGroup     = "CreateVocabularyByStaffCommandValidator",
                TestCaseID        = "TC-VOC-CSBV-04",
                Description       = "Valid inputs check constraints",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid parameters" }
            });
        }
    }
}
