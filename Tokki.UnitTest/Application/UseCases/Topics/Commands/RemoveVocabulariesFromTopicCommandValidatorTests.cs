using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic;
using Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic.Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics.Commands
{
    public class RemoveVocabulariesFromTopicCommandValidatorTests
    {
        private readonly RemoveVocabulariesFromTopicCommandValidator _validator;

        public RemoveVocabulariesFromTopicCommandValidatorTests()
        {
            _validator = new RemoveVocabulariesFromTopicCommandValidator();
        }

        [Fact]
        public void Validate_EmptyTopicId_ShouldHaveError()
        {
            var command = new RemoveVocabulariesFromTopicCommand { TopicId = "", VocabularyIds = new List<string> { "v1" } };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.TopicId)
                  .WithErrorMessage("ID chủ đề không được để trống.");

            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail
            {
                FunctionGroup     = "RemoveVocabulariesFromTopicCommandValidator",
                TestCaseID        = "TC-TOP-RVFT-01",
                Description       = "Empty TopicId",
                ExpectedResult    = "Error 'ID chủ đề không được để trống.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TopicId empty" }
            });
        }

        [Fact]
        public void Validate_NullVocabularyIds_ShouldHaveError()
        {
            var command = new RemoveVocabulariesFromTopicCommand { TopicId = "t1", VocabularyIds = null };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.VocabularyIds)
                  .WithErrorMessage("Danh sách từ vựng không được để trống.");

            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail
            {
                FunctionGroup     = "RemoveVocabulariesFromTopicCommandValidator",
                TestCaseID        = "TC-TOP-RVFT-02",
                Description       = "VocabularyIds is null",
                ExpectedResult    = "Error 'Danh sách từ vựng không được để trống.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyIds null" }
            });
        }

        [Fact]
        public void Validate_EmptyVocabularyIdsList_ShouldHaveError()
        {
            var command = new RemoveVocabulariesFromTopicCommand { TopicId = "t1", VocabularyIds = new List<string>() };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.VocabularyIds)
                  .WithErrorMessage("Danh sách từ vựng không được để trống.");

            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail
            {
                FunctionGroup     = "RemoveVocabulariesFromTopicCommandValidator",
                TestCaseID        = "TC-TOP-RVFT-03",
                Description       = "VocabularyIds is empty list",
                ExpectedResult    = "Error 'Danh sách từ vựng không được để trống.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyIds empty list" }
            });
        }

        [Fact]
        public void Validate_VocabularyIdsContainsEmpty_ShouldHaveError()
        {
            var command = new RemoveVocabulariesFromTopicCommand { TopicId = "t1", VocabularyIds = new List<string> { "" } };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("VocabularyIds[0]")
                  .WithErrorMessage("ID từ vựng không hợp lệ.");

            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail
            {
                FunctionGroup     = "RemoveVocabulariesFromTopicCommandValidator",
                TestCaseID        = "TC-TOP-RVFT-04",
                Description       = "VocabularyIds contains empty element",
                ExpectedResult    = "Error 'ID từ vựng không hợp lệ.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyIds contains empty string" }
            });
        }

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveError()
        {
            var command = new RemoveVocabulariesFromTopicCommand { TopicId = "t1", VocabularyIds = new List<string> { "v1" } };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail
            {
                FunctionGroup     = "RemoveVocabulariesFromTopicCommandValidator",
                TestCaseID        = "TC-TOP-RVFT-05",
                Description       = "Valid parameters",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid command" }
            });
        }

        [Fact]
        public void Validate_ValidMultipleIds_ShouldNotHaveError()
        {
            var command = new RemoveVocabulariesFromTopicCommand { TopicId = "t1", VocabularyIds = new List<string> { "v1", "v2" } };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail
            {
                FunctionGroup     = "RemoveVocabulariesFromTopicCommandValidator",
                TestCaseID        = "TC-TOP-RVFT-06",
                Description       = "Valid parameters with multiple ids",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Multiple ids" }
            });
        }
    }
}
