using FluentAssertions;
using System;
using System.Collections.Generic;
using Tokki.Application.Common.Helpers;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.Common.Helpers
{
    public class WordleHelperTests
    {
        [Fact]
        public void CalculateFeedback_ExactMatch_ReturnsGreenOnly()
        {
            var target = "사과";
            var guess = "사과";

            var result = WordleHelper.CalculateFeedback(target, guess);

            result.Should().HaveCount(2);
            result[0].BlockColor.Should().Be("Green");
            result[0].InitialStatus.Should().Be("Green");
            result[0].VowelStatus.Should().Be("Green");
            result[0].FinalStatus.Should().Be("Green");

            result[1].BlockColor.Should().Be("Green");
            result[1].InitialStatus.Should().Be("Green");
            result[1].VowelStatus.Should().Be("Green");
            result[1].FinalStatus.Should().Be("Green");

            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup     = "WordleHelper",
                TestCaseID        = "TC-HLP-WD-01",
                Description       = "Exact match",
                ExpectedResult    = "All parts and blocks Green",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Target == Guess" }
            });
        }

        [Fact]
        public void CalculateFeedback_PartialMatch_ReturnsYellowOrGrayForBlockColor()
        {
            // Target: 사과
            // Guess: 시간
            var target = "사과";
            var guess = "시간";

            var result = WordleHelper.CalculateFeedback(target, guess);

            result.Should().HaveCount(2);
            
            // 시 (Initial: ㅅ is same as in 사, Vowel: ㅣ is different, Final: null) 
            // So Initial is Green, BlockColor is Yellow
            result[0].InitialStatus.Should().Be("Green");
            result[0].VowelStatus.Should().Be("Gray");
            result[0].BlockColor.Should().Be("Yellow");

            // 간 (Initial: ㄱ is same as in 과, Vowel: ㅏ is different, Final: ㄴ is different)
            // So Initial is Green, BlockColor is Yellow
            result[1].InitialStatus.Should().Be("Green");
            result[1].BlockColor.Should().Be("Yellow");

            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup     = "WordleHelper",
                TestCaseID        = "TC-HLP-WD-02",
                Description       = "Partial match calculates Yellow blocks correctly",
                ExpectedResult    = "Initial Green, Block Yellow",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Partial matching words" }
            });
        }

        [Fact]
        public void CalculateFeedback_NoMatch_ReturnsGray()
        {
            var target = "사과"; 
            var guess = "포도";

            var result = WordleHelper.CalculateFeedback(target, guess);

            result.Should().HaveCount(2);
            result[0].BlockColor.Should().Be("Gray");
            result[0].InitialStatus.Should().Be("Gray");
            result[0].VowelStatus.Should().Be("Gray");
            result[0].FinalStatus.Should().Be("Gray");

            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup     = "WordleHelper",
                TestCaseID        = "TC-HLP-WD-03",
                Description       = "No match calculates Gray correctly",
                ExpectedResult    = "All Gray",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Completely different words" }
            });
        }
    }
}
