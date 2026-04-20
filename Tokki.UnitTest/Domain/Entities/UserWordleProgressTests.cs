using FluentAssertions;
using System;
using System.Collections.Generic;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.DomainTests.Entities
{
    public class UserWordleProgressTests
    {
        [Fact]
        public void Guesses_ValidJson_ReturnsList()
        {
            var progress = new UserWordleProgress
            {
                GuessesJson = "[\"사과\", \"나무\"]"
            };

            var guesses = progress.Guesses;

            guesses.Should().NotBeNull();
            guesses.Should().HaveCount(2);
            guesses[0].Should().Be("사과");
            guesses[1].Should().Be("나무");

            QACollector.LogTestCase("Domain - User Wordle Progress", new TestCaseDetail
            {
                FunctionGroup     = "UserWordleProgress",
                TestCaseID        = "UserWordleProgress_01",
                Description       = "Deserialize array correctly",
                ExpectedResult    = "Deserialized accurately",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "JSON mapped validation" }
            });
        }

        [Fact]
        public void Guesses_EmptyOrNullJson_ReturnsEmptyList()
        {
            var progressNull = new UserWordleProgress { GuessesJson = null! };
            var progressEmpty = new UserWordleProgress { GuessesJson = "" };

            progressNull.Guesses.Should().NotBeNull().And.BeEmpty();
            progressEmpty.Guesses.Should().NotBeNull().And.BeEmpty();

            QACollector.LogTestCase("Domain - User Wordle Progress", new TestCaseDetail
            {
                FunctionGroup     = "UserWordleProgress",
                TestCaseID        = "UserWordleProgress_02",
                Description       = "Null or empty json fallback",
                ExpectedResult    = "Empty list",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Fallback to empty list" }
            });
        }

        [Fact]
        public void Guesses_ValidListAssignment_UpdatesJson()
        {
            var progress = new UserWordleProgress();
            var list = new List<string> { "가방", "바다" };

            progress.Guesses = list;

            progress.GuessesJson.Should().Be("[\"\\uAC00\\uBC29\",\"\\uBC14\\uB2E4\"]");

            QACollector.LogTestCase("Domain - User Wordle Progress", new TestCaseDetail
            {
                FunctionGroup     = "UserWordleProgress",
                TestCaseID        = "UserWordleProgress_03",
                Description       = "Serialize array assignments neatly",
                ExpectedResult    = "Serialized output string correctly",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "JSON mapped assigned string validation seamlessly" }
            });
        }
    }
}
