using FluentAssertions;
using System;
using Tokki.Application.Common.Helpers;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.Common.Helpers
{
    public class LevelEngineTests
    {
        // LevelEngine_01 | A | TotalXP < 0 returns Level 1
        [Fact]
        public void GetLevel_NegativeXp_ShouldReturnLevel1()
        {
            var level = LevelEngine.GetLevel(-50);
            level.Should().Be(1);

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "LevelEngine",
                TestCaseID = "LevelEngine_01",
                Description = "Negative XP triggers early return for Level 1",
                ExpectedResult = "1",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "totalXp < BASE_XP" }
            });
        }

        // LevelEngine_02 | A | TotalXP = 0 returns Level 1
        [Fact]
        public void GetLevel_ZeroXp_ShouldReturnLevel1()
        {
            var level = LevelEngine.GetLevel(0);
            level.Should().Be(1);

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "LevelEngine",
                TestCaseID = "LevelEngine_02",
                Description = "Zero XP triggers early return for Level 1",
                ExpectedResult = "1",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "totalXp < BASE_XP" }
            });
        }

        // LevelEngine_03 | N | TotalXP = 100 (BASE_XP) returns Level 2
        [Fact]
        public void GetLevel_BaseXpExact_ShouldReturnLevel2()
        {
            var level = LevelEngine.GetLevel(100);
            level.Should().Be(2);

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "LevelEngine",
                TestCaseID = "LevelEngine_03",
                Description = "Exactly BASE_XP crosses Level 2 boundary (Math.Floor(1) + 1)",
                ExpectedResult = "2",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "totalXp == BASE_XP" }
            });
        }

        // LevelEngine_04 | N | TotalXP = High Amount (e.g. 800) returns accurate level (Level 5) 
        // 800 / 100 = 8. Math.Pow(8, 1/1.5) = Pow(8, 0.666..) = 4. Floor(4) + 1 = 5
        [Fact]
        public void GetLevel_HighXp_ShouldCalculateCurveProperly()
        {
            var level = LevelEngine.GetLevel(800);
            level.Should().Be(5);

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "LevelEngine",
                TestCaseID = "LevelEngine_04",
                Description = "Calculates mathematical progression curve scaling appropriately",
                ExpectedResult = "5",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "TotalXp = 800" }
            });
        }

        // LevelEngine_05 | A | GetTotalXpRequiredForLevel(1 or 0) returns 0 
        [Fact]
        public void GetTotalXpRequiredForLevel_LevelZeroAndOne_ShouldReturnZero()
        {
            LevelEngine.GetTotalXpRequiredForLevel(0).Should().Be(0);
            LevelEngine.GetTotalXpRequiredForLevel(1).Should().Be(0);

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "LevelEngine",
                TestCaseID = "LevelEngine_05",
                Description = "Under level 2 threshold requires 0 xp",
                ExpectedResult = "0 XP required",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "level <= 1" }
            });
        }

        // LevelEngine_06 | N | GetTotalXpRequiredForLevel(5) returns 800
        // Because Base(100) * Pow(5-1, 1.5) = 100 * Pow(4, 1.5) = 100 * 8 = 800
        [Fact]
        public void GetTotalXpRequiredForLevel_LevelFive_ShouldReturn800()
        {
            var requiredXp = LevelEngine.GetTotalXpRequiredForLevel(5);
            requiredXp.Should().Be(800);

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "LevelEngine",
                TestCaseID = "LevelEngine_06",
                Description = "Calculates reverse mathematical formula required to attain explicit level",
                ExpectedResult = "800",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Level == 5" }
            });
        }
    }
}
