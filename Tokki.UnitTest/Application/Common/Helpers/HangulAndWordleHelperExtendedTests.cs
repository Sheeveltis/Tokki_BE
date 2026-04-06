using FluentAssertions;
using System;
using System.Collections.Generic;
using Tokki.Application.Common.Helpers;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.Common.Helpers
{
    public class HangulAndWordleHelperExtendedTests
    {
        // ═══════════════════════════════════════════════════════════
        // TC-HELPER-HGE-01 | N | Extended SubJamos compound consonants
        // ═══════════════════════════════════════════════════════════
        [Theory]
        [InlineData('ㄵ', 'ㄴ', 'ㅈ')]
        [InlineData('ㄶ', 'ㄴ', 'ㅎ')]
        [InlineData('ㄺ', 'ㄹ', 'ㄱ')]
        [InlineData('ㄻ', 'ㄹ', 'ㅁ')]
        [InlineData('ㄼ', 'ㄹ', 'ㅂ')]
        [InlineData('ㄽ', 'ㄹ', 'ㅅ')]
        [InlineData('ㄾ', 'ㄹ', 'ㅌ')]
        [InlineData('ㄿ', 'ㄹ', 'ㅍ')]
        [InlineData('ㅀ', 'ㄹ', 'ㅎ')]
        public void GetSubJamos_ExtendedCompoundConsonant_ShouldReturnExpectedSubJamos(char jamo, char expected1, char expected2)
        {
            // Act
            var result = HangulHelper.GetSubJamos(jamo);

            // Assert
            result.Should().Contain(expected1);
            result.Should().Contain(expected2);

            // Excel Log
            QACollector.LogTestCase("Helper - Hangul", new TestCaseDetail
            {
                FunctionGroup = "HangulHelper",
                TestCaseID = $"TC-HELPER-HGE-01-{jamo}",
                Description = $"Extended compound consonant tests ensuring coverage for {jamo}",
                ExpectedResult = $"Contains {expected1} and {expected2}",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "char matches specific switch case" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-HELPER-WLE-02 | N | WordleHelper Edge Cases Initial Gray
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void CalculateFeedback_MismatchInitial_ShouldBeGrayInitial()
        {
            // Target '바' vs Guess '자' - diff initial, same vowel => Initial=Gray, Vowel=Green
            // Act
            var result = WordleHelper.CalculateFeedback("바", "자");

            // Assert
            result.Should().HaveCount(1);
            result[0].InitialStatus.Should().Be("Gray");
            result[0].VowelStatus.Should().Be("Green");

            // Excel Log
            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup = "WordleHelper",
                TestCaseID = "TC-HELPER-WLE-02",
                Description = "Different initial but same vowel yields Initial Gray, Vowel Green",
                ExpectedResult = "Initial=Gray, Vowel=Green",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Initials match condition false" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-HELPER-WLE-03 | N | WordleHelper Final Exact Match Green
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void CalculateFeedback_MatchFinal_ShouldBeGreenFinal()
        {
            // Target '강' vs Guess '장' - diff initial, same vowel, same final => Final=Green
            // Act
            var result = WordleHelper.CalculateFeedback("강", "장");

            // Assert
            result.Should().HaveCount(1);
            result[0].FinalStatus.Should().Be("Green");

            // Excel Log
            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup = "WordleHelper",
                TestCaseID = "TC-HELPER-WLE-03",
                Description = "Matching patchim directly yields FinalStatus Green",
                ExpectedResult = "FinalStatus=Green",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Finals match and != 0" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-HELPER-HGE-04 | B | English characters decomposition
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void Decompose_EnglishChar_ShouldJustReturnEnglish()
        {
            // Act
            var parts = HangulHelper.Decompose('Z');

            // Assert
            parts.Initial.Should().Be('Z');
            parts.Vowel.Should().Be(' ');

            // Excel Log
            QACollector.LogTestCase("Helper - Hangul", new TestCaseDetail
            {
                FunctionGroup = "HangulHelper",
                TestCaseID = "TC-HELPER-HGE-04",
                Description = "English character uses fallback branch logic",
                ExpectedResult = "Initial equals Z",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Char not in Hangul range" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-HELPER-WLE-05 | B | Differing lengths
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void CalculateFeedback_DifferingLengths_ShouldCalculateOnMinLength()
        {
            // Act
            var result = WordleHelper.CalculateFeedback("안녕", "안녕하세요");

            // Assert
            result.Should().HaveCount(2);

            // Excel Log
            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup = "WordleHelper",
                TestCaseID = "TC-HELPER-WLE-05",
                Description = "Differing lengths only yields feedback up to min length",
                ExpectedResult = "Count is 2",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Using Math.Min" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-HELPER-WLE-06 | B | Empty string
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void CalculateFeedback_EmptyString_ShouldReturnEmptyList()
        {
            // Act
            var result = WordleHelper.CalculateFeedback("", "안");

            // Assert
            result.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup = "WordleHelper",
                TestCaseID = "TC-HELPER-WLE-06",
                Description = "Empty string yields empty loop evaluation and zero feedback",
                ExpectedResult = "List is empty",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Length is 0" }
            });
        }
    }
}
