using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Tokki.Application.UseCases.Vocabulary.Commands.BulkCreateVocabulariesByStaff;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary.Commands
{
    public class BulkCreateVocabulariesByStaffCommandValidatorTests
    {
        private readonly BulkCreateVocabulariesByStaffCommandValidator _validator;

        public BulkCreateVocabulariesByStaffCommandValidatorTests()
        {
            _validator = new BulkCreateVocabulariesByStaffCommandValidator();
        }

        [Fact]
        public void Validate_VocabulariesNull_ShouldHaveError()
        {
            var command = new BulkCreateVocabulariesByStaffCommand { Vocabularies = null };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Vocabularies)
                  .WithErrorMessage("'Danh sách vocabulary' không được bỏ trống.");

            QACollector.LogTestCase("Vocabulary Admin - Bulk Create By Staff Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesByStaffCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesByStaffCommandValidator_01",
                Description       = "Vocabularies list null",
                ExpectedResult    = "Error null array",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "List is null" }
            });
        }

        [Fact]
        public void Validate_VocabulariesExceed100_ShouldHaveError()
        {
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = Enumerable.Range(1, 101).Select(i => new VocabularyCreateDto { Text = "a", Definition = "b" }).ToList()
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Vocabularies)
                  .WithErrorMessage("Không thể tạo quá 100 vocabulary trong một lần.");

            QACollector.LogTestCase("Vocabulary Admin - Bulk Create By Staff Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesByStaffCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesByStaffCommandValidator_02",
                Description       = "Vocabularies max elements",
                ExpectedResult    = "Error max array length",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Size=101" }
            });
        }

        [Fact]
        public void Validate_TextEmpty_ShouldHaveError()
        {
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto> { new VocabularyCreateDto { Text = "", Definition = "d" } }
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("Vocabularies[0].Text")
                  .WithErrorMessage("'Text' không được bỏ trống.");

            QACollector.LogTestCase("Vocabulary Admin - Bulk Create By Staff Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesByStaffCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesByStaffCommandValidator_03",
                Description       = "Missing mandatory rules",
                ExpectedResult    = "Throws error text",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Text missing" }
            });
        }

        [Fact]
        public void Validate_TextLengthExceeded_ShouldHaveError()
        {
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto> { new VocabularyCreateDto { Text = new string('t', 101), Definition = "d" } }
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("Vocabularies[0].Text");

            QACollector.LogTestCase("Vocabulary Admin - Bulk Create By Staff Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesByStaffCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesByStaffCommandValidator_04",
                Description       = "Text exceeding bounds limit",
                ExpectedResult    = "Error string max rules",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "String length > 100" }
            });
        }

        [Fact]
        public void Validate_ExamplesCountExceeded_ShouldHaveError()
        {
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto> 
                { 
                    new VocabularyCreateDto 
                    { 
                        Text = "A", 
                        Definition = "B",
                        Examples = Enumerable.Range(1, 11).Select(i => new VocabularyExampleDto { Sentence = "S" }).ToList() 
                    } 
                }
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("Vocabularies[0].Examples")
                  .WithErrorMessage("Mỗi vocabulary không thể có quá 10 câu ví dụ.");

            QACollector.LogTestCase("Vocabulary Admin - Bulk Create By Staff Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesByStaffCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesByStaffCommandValidator_05",
                Description       = "Examples > 10",
                ExpectedResult    = "Throws 10 maximum check",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Examples list > 10" }
            });
        }

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveError()
        {
            var command = new BulkCreateVocabulariesByStaffCommand
            {
                Vocabularies = new List<VocabularyCreateDto> 
                { 
                    new VocabularyCreateDto 
                    { 
                        Text = "A", 
                        Definition = "B"
                    } 
                }
            };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Vocabulary Admin - Bulk Create By Staff Validator", new TestCaseDetail
            {
                FunctionGroup     = "BulkCreateVocabulariesByStaffCommandValidator",
                TestCaseID        = "BulkCreateVocabulariesByStaffCommandValidator_06",
                Description       = "Valid inputs",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid parameters" }
            });
        }
    }
}
