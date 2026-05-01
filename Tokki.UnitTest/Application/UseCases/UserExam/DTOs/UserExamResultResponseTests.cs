using FluentAssertions;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam.DTOs
{
    public class UserExamResultResponseTests
    {
        [Fact]
        public void TotalScore_CalculatedCorrectly_SumOfScores()
        {
            var response = new UserExamResultResponse
            {
                Listening = new SkillScoreDto { Score = 20.5 },
                Reading = new SkillScoreDto { Score = 30.0 },
                Writing = new SkillScoreDto { Score = 10.5 }
            };

            var total = response.TotalScore;

            total.Should().Be(61.0);

            QACollector.LogTestCase("UserExam - DTO", new TestCaseDetail
            {
                FunctionGroup     = "UserExamResultResponse",
                TestCaseID        = "UserExamResultResponse_01",
                Description       = "Verifies TotalScore correctly adds L, R, W scores",
                ExpectedResult    = "Total score equals 61.0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid scores provided" }
            });
        }

        [Fact]
        public void IsGraded_AllSkillsGraded_ReturnsTrue()
        {
            var response = new UserExamResultResponse
            {
                Listening = new SkillScoreDto { IsGraded = true },
                Reading = new SkillScoreDto { IsGraded = true },
                Writing = new SkillScoreDto { IsGraded = true }
            };

            response.IsGraded.Should().BeTrue();

            QACollector.LogTestCase("UserExam - DTO", new TestCaseDetail
            {
                FunctionGroup     = "UserExamResultResponse",
                TestCaseID        = "UserExamResultResponse_02",
                Description       = "Verifies IsGraded correctly tests all flawlessly tests true",
                ExpectedResult    = "Returns true",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "All skills Graded" }
            });
        }

        [Fact]
        public void IsGraded_OneSkillNotGraded_ReturnsFalse()
        {
            var response = new UserExamResultResponse
            {
                Listening = new SkillScoreDto { IsGraded = true },
                Reading = new SkillScoreDto { IsGraded = false },
                Writing = new SkillScoreDto { IsGraded = true }
            };

            response.IsGraded.Should().BeFalse();

            QACollector.LogTestCase("UserExam - DTO", new TestCaseDetail
            {
                FunctionGroup     = "UserExamResultResponse",
                TestCaseID        = "UserExamResultResponse_03",
                Description       = "Verifies IsGraded returns false if one is not graded",
                ExpectedResult    = "Returns false",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "One skill IsGraded=false" }
            });
        }
    }
}
