using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Tokki.Application.UseCases.Vocabulary.Commands.BulkCreateVocabularies;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary.Commands
{
    public class BulkCreateVocabulariesCommandValidatorTests
    {
        private readonly BulkCreateVocabulariesCommandValidator _validator;

        public BulkCreateVocabulariesCommandValidatorTests()
        {
            _validator = new BulkCreateVocabulariesCommandValidator();
        }

        [Fact]
        public void Validate_VocabulariesNull_ShouldHaveError()
        {
            var command = new BulkCreateVocabulariesCommand { Vocabularies = null };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Vocabularies)
                  .WithErrorMessage("'Danh sách vocabulary' không được bỏ trống.");

            QACollector.LogTestCase("Vocabulary - Bulk Create Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesCommandValidator_01",
                Description       = "Vocabularies list null",
                ExpectedResult    = "Throws Empty validation error",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "List is null" }
            });
        }

        [Fact]
        public void Validate_VocabulariesEmpty_ShouldHaveError()
        {
            var command = new BulkCreateVocabulariesCommand { Vocabularies = new List<VocabularyCreateDto>() };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Vocabularies)
                  .WithErrorMessage("'Danh sách vocabulary' không được bỏ trống.");

            QACollector.LogTestCase("Vocabulary - Bulk Create Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesCommandValidator_02",
                Description       = "Vocabularies list empty",
                ExpectedResult    = "Throws validation error",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "List is empty" }
            });
        }

        [Fact]
        public void Validate_VocabulariesExceed100_ShouldHaveError()
        {
            var command = new BulkCreateVocabulariesCommand
            {
                Vocabularies = Enumerable.Range(1, 101).Select(i => new VocabularyCreateDto { Text = "a", Definition = "b" }).ToList()
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Vocabularies)
                  .WithErrorMessage("Không thể tạo quá 100 vocabulary trong một lần.");

            QACollector.LogTestCase("Vocabulary - Bulk Create Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesCommandValidator_03",
                Description       = "Vocabularies count > 100 checks",
                ExpectedResult    = "Error max count 100",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "List count=101" }
            });
        }

        [Fact]
        public void Validate_TextEmpty_ShouldHaveError()
        {
            var command = new BulkCreateVocabulariesCommand
            {
                Vocabularies = new List<VocabularyCreateDto> { new VocabularyCreateDto { Text = "", Definition = "d" } }
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("Vocabularies[0].Text")
                  .WithErrorMessage("'Text' không được bỏ trống.");

            QACollector.LogTestCase("Vocabulary - Bulk Create Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesCommandValidator_04",
                Description       = "Text property is empty",
                ExpectedResult    = "Error empty Text",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Text missing" }
            });
        }

        [Fact]
        public void Validate_DefinitionExceedLength_ShouldHaveError()
        {
            var command = new BulkCreateVocabulariesCommand
            {
                Vocabularies = new List<VocabularyCreateDto> { new VocabularyCreateDto { Text = "t", Definition = new string('a', 501) } }
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("Vocabularies[0].Definition")
                  .WithErrorMessage("Chiều dài của 'Definition' phải lớn hơn hoặc bằng 0 ký tự và ít hơn hoặc bằng 500 ký tự. Bạn đã nhập 501 ký tự.");

            QACollector.LogTestCase("Vocabulary - Bulk Create Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesCommandValidator_05",
                Description       = "Definition length > 500 characters",
                ExpectedResult    = "Error max length breached",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Definition exceeds length limit boundaries" }
            });
        }

        [Fact]
        public void Validate_ExamplesExceed10_ShouldHaveError()
        {
            var command = new BulkCreateVocabulariesCommand
            {
                Vocabularies = new List<VocabularyCreateDto> 
                { 
                    new VocabularyCreateDto 
                    { 
                        Text = "t", 
                        Definition = "d",
                        Examples = Enumerable.Range(1, 11).Select(i => new VocabularyExampleDto { Sentence = "s" }).ToList() 
                    } 
                }
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("Vocabularies[0].Examples")
                  .WithErrorMessage("Mỗi vocabulary không thể có quá 10 câu ví dụ.");

            QACollector.LogTestCase("Vocabulary - Bulk Create Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesCommandValidator_06",
                Description       = "Examples list > 10",
                ExpectedResult    = "Error examples max count",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Examples list > 10" }
            });
        }

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveError()
        {
            var command = new BulkCreateVocabulariesCommand
            {
                Vocabularies = new List<VocabularyCreateDto> 
                { 
                    new VocabularyCreateDto 
                    { 
                        Text = "Word", 
                        Definition = "Definition", 
                        Pronunciation = "wɜrd",
                        Examples = new List<VocabularyExampleDto> { new VocabularyExampleDto { Sentence = "S", Translation = "T" } }
                    } 
                }
            };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Vocabulary - Bulk Create Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesCommandValidator_07",
                Description       = "Valid mapping attributes",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid all properties parsed" }
            });
        }
    }
}
