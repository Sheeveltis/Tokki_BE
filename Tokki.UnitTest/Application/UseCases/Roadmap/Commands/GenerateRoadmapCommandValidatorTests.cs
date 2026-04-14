using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap.Commands
{
    public class GenerateRoadmapCommandValidatorTests
    {
        private readonly GenerateRoadmapCommandValidator _validator;

        public GenerateRoadmapCommandValidatorTests()
        {
            _validator = new GenerateRoadmapCommandValidator();
        }

        [Fact]
        public void Validate_InvalidTargetAim_ShouldHaveError()
        {
            var command = new GenerateRoadmapCommand { TargetAim = (TargetAimLevel)999, DurationDays = 30, UserId = "u1" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.TargetAim);

            QACollector.LogTestCase("Roadmap - Generate Roadmap Validator", new TestCaseDetail
            {
                FunctionGroup     = "GenerateRoadmapCommandValidator",
                TestCaseID        = "TC-RDM-GRV-01",
                Description       = "TargetAim is invalid (not in enum)",
                ExpectedResult    = "Error validation",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TargetAim invalid enum value" }
            });
        }

        [Fact]
        public void Validate_InvalidDurationDays_ShouldHaveError()
        {
            var command = new GenerateRoadmapCommand { TargetAim = TargetAimLevel.Topik_II_Level5, DurationDays = 45, UserId = "u1" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.DurationDays)
                  .WithErrorMessage("Thời gian lộ trình chỉ chấp nhận 30, 60 hoặc 90 ngày.");

            QACollector.LogTestCase("Roadmap - Generate Roadmap Validator", new TestCaseDetail
            {
                FunctionGroup     = "GenerateRoadmapCommandValidator",
                TestCaseID        = "TC-RDM-GRV-02",
                Description       = "DurationDays is invalid (not 30, 60, or 90)",
                ExpectedResult    = "Error 'Thời gian lộ trình chỉ chấp nhận 30, 60 hoặc 90 ngày.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DurationDays = 45" }
            });
        }

        [Fact]
        public void Validate_EmptyUserId_ShouldHaveError()
        {
            var command = new GenerateRoadmapCommand { TargetAim = TargetAimLevel.Topik_II_Level5, DurationDays = 60, UserId = "" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UserId)
                  .WithErrorMessage("Không xác định được người dùng.");

            QACollector.LogTestCase("Roadmap - Generate Roadmap Validator", new TestCaseDetail
            {
                FunctionGroup     = "GenerateRoadmapCommandValidator",
                TestCaseID        = "TC-RDM-GRV-03",
                Description       = "UserId is empty",
                ExpectedResult    = "Error 'Không xác định được người dùng.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserId empty" }
            });
        }

        [Theory]
        [InlineData(30)]
        [InlineData(60)]
        [InlineData(90)]
        public void Validate_ValidCommand_ShouldNotHaveError(int days)
        {
            var command = new GenerateRoadmapCommand { TargetAim = TargetAimLevel.Topik_I_Level1, DurationDays = days, UserId = "u1" };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Roadmap - Generate Roadmap Validator", new TestCaseDetail
            {
                FunctionGroup     = "GenerateRoadmapCommandValidator",
                TestCaseID        = $"TC-RDM-GRV-1{days}",
                Description       = $"Valid command with {days} days",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { $"DurationDays {days}", "Valid target/user" }
            });
        }
    }
}
