using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabulary;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary.Commands
{
    public class CreateVocabularyCommandValidatorTests
    {
        private readonly CreateVocabularyCommandValidator _validator;

        public CreateVocabularyCommandValidatorTests()
        {
            _validator = new CreateVocabularyCommandValidator();
        }

        [Fact]
        public void Validate_EmptyText_ShouldHaveError()
        {
            var command = new CreateVocabularyCommand { Text = "", Definition = "Def" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Text)
                  .WithErrorMessage("Text không du?c d? tr?ng.");

            QACollector.LogTestCase("Vocabulary - Create Validator", new TestCaseDetail
            {
                FunctionGroup     = "CreateVocabularyCommandValidator",
                TestCaseID        = "CreateVocabularyCommandValidator_01",
                Description       = "Empty Text",
                ExpectedResult    = "Throws Empty validation error",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Text empty string" }
            });
        }

        [Fact]
        public void Validate_LongDefinition_ShouldHaveError()
        {
            var command = new CreateVocabularyCommand { Text = "Word", Definition = new string('A', 501) };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Definition)
                  .WithErrorMessage("Definition không du?c vu?t quá 500 ký t?.");

            QACollector.LogTestCase("Vocabulary - Create Validator", new TestCaseDetail
            {
                FunctionGroup     = "CreateVocabularyCommandValidator",
                TestCaseID        = "CreateVocabularyCommandValidator_02",
                Description       = "Definition limit exceeded",
                ExpectedResult    = "Throws Length Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Definition > 500" }
            });
        }

        [Fact]
        public void Validate_DuplicateExamples_ShouldHaveError()
        {
            var command = new CreateVocabularyCommand 
            { 
                Text = "Word", 
                Definition = "Def",
                Examples = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto { Sentence = "Hello" },
                    new VocabularyExampleDto { Sentence = "  hello" }
                }
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("Examples")
                  .WithErrorMessage("Danh sách câu ví d? b? trùng: hello");

            QACollector.LogTestCase("Vocabulary - Create Validator", new TestCaseDetail
            {
                FunctionGroup     = "CreateVocabularyCommandValidator",
                TestCaseID        = "CreateVocabularyCommandValidator_03",
                Description       = "Duplicate examples tracking",
                ExpectedResult    = "Throws custom duplicate error",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Duplicate examples" }
            });
        }

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveError()
        {
            var command = new CreateVocabularyCommand 
            { 
                Text = "Word", 
                Definition = "Def",
                Examples = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto { Sentence = "Hello", Translation = "Xin chao" },
                }
            };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Vocabulary - Create Validator", new TestCaseDetail
            {
                FunctionGroup     = "CreateVocabularyCommandValidator",
                TestCaseID        = "CreateVocabularyCommandValidator_04",
                Description       = "Valid inputs check",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid object" }
            });
        }
    }
}
