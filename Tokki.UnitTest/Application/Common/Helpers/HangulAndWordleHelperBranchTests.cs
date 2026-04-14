using FluentAssertions;
using Tokki.Application.Common.Helpers;
using Tokki.Application.UseCases.MiniGame.DTOs;
using Tokki.UnitTest.Utilities;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tokki.UnitTest.Application.Common.Helpers
{
    /// <summary>
    /// Branch-coverage tests for HangulHelper (Decompose + GetSubJamos)
    /// and WordleHelper (CalculateFeedback).
    /// </summary>
    public class HangulAndWordleHelperBranchTests
    {
        // ─────────────────────────────────────────────────────────────────
        // HangulHelper.Decompose
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public void Decompose_HangulCharacter_ShouldReturnCorrectJamos()
        {
            // '글' = ㄱ + ㅡ + ㄹ
            var parts = HangulHelper.Decompose('글');
            parts.Initial.Should().Be('ㄱ');
            parts.Vowel.Should().Be('ㅡ');
            parts.Final.Should().Be('ㄹ');

            QACollector.LogTestCase("Helper - Hangul Decompose", new TestCaseDetail
            {
                FunctionGroup = "HangulHelper", TestCaseID = "TC-HH-01",
                Description = "Hangul character '글' decomposes to ㄱ/ㅡ/ㄹ",
                ExpectedResult = "Initial=ㄱ Vowel=ㅡ Final=ㄹ", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "char in Hangul range 0xAC00-0xD7A3" }
            });
        }

        [Fact]
        public void Decompose_NonHangulCharacter_ShouldReturnCharWithSpaceVowelAndNullFinal()
        {
            // ASCII character should use the 'out of range' branch
            var parts = HangulHelper.Decompose('A');
            parts.Initial.Should().Be('A');
            parts.Vowel.Should().Be(' ');
            parts.Final.Should().Be('\0');

            QACollector.LogTestCase("Helper - Hangul Decompose", new TestCaseDetail
            {
                FunctionGroup = "HangulHelper", TestCaseID = "TC-HH-02",
                Description = "Non-Hangul char returns itself with empty vowel/final",
                ExpectedResult = "Initial=char, Vowel=' ', Final=\\0", StatusRound1 = "Passed",
                TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "char < 0xAC00 → early return" }
            });
        }

        [Fact]
        public void Decompose_CharacterWithNoPatchim_ShouldHaveNullFinal()
        {
            // '가' = ㄱ + ㅏ + (no patchim)
            var parts = HangulHelper.Decompose('가');
            parts.Initial.Should().Be('ㄱ');
            parts.Vowel.Should().Be('ㅏ');
            parts.Final.Should().Be('\0');

            QACollector.LogTestCase("Helper - Hangul Decompose", new TestCaseDetail
            {
                FunctionGroup = "HangulHelper", TestCaseID = "TC-HH-03",
                Description = "'가' has no patchim → Final is \\0",
                ExpectedResult = "Final=\\0", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "finalIndex == 0 → FinalConsonants[0] = \\0" }
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // HangulHelper.GetSubJamos
        // ─────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData('ㅘ', 'ㅗ', 'ㅏ')]
        [InlineData('ㅙ', 'ㅗ', 'ㅐ')]
        [InlineData('ㅚ', 'ㅗ', 'ㅣ')]
        [InlineData('ㅝ', 'ㅜ', 'ㅓ')]
        [InlineData('ㅞ', 'ㅜ', 'ㅔ')]
        [InlineData('ㅟ', 'ㅜ', 'ㅣ')]
        [InlineData('ㅢ', 'ㅡ', 'ㅣ')]
        public void GetSubJamos_CompoundVowel_ShouldReturnCorrectSubComponents(char jamo, char c1, char c2)
        {
            var result = HangulHelper.GetSubJamos(jamo);
            result.Should().Contain(c1);
            result.Should().Contain(c2);

            QACollector.LogTestCase("Helper - Hangul SubJamos", new TestCaseDetail
            {
                FunctionGroup = "HangulHelper", TestCaseID = $"TC-SUBJAMO-{jamo}",
                Description = $"Compound vowel {jamo} expands to {c1}+{c2}",
                ExpectedResult = $"Set contains {c1} and {c2}", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { $"jamo = {jamo}" }
            });
        }

        [Theory]
        [InlineData('ㄲ', 'ㄱ')]
        [InlineData('ㄳ', 'ㄱ', 'ㅅ')]
        [InlineData('ㅄ', 'ㅂ', 'ㅅ')]
        [InlineData('ㅆ', 'ㅅ')]
        public void GetSubJamos_CompoundConsonant_ShouldReturnSubConsonants(char jamo, char c1, char c2 = '\0')
        {
            var result = HangulHelper.GetSubJamos(jamo);
            result.Should().Contain(c1);
            if (c2 != '\0') result.Should().Contain(c2);

            QACollector.LogTestCase("Helper - Hangul SubJamos", new TestCaseDetail
            {
                FunctionGroup = "HangulHelper", TestCaseID = $"TC-CONJAMO-{jamo}",
                Description = $"Compound consonant {jamo} expands correctly",
                ExpectedResult = $"Contains {c1}", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { $"jamo = {jamo}" }
            });
        }

        [Fact]
        public void GetSubJamos_SimpleJamo_ShouldReturnSingleElementSet()
        {
            var result = HangulHelper.GetSubJamos('ㄱ');
            result.Should().HaveCount(1);
            result.Should().Contain('ㄱ');

            QACollector.LogTestCase("Helper - Hangul SubJamos", new TestCaseDetail
            {
                FunctionGroup = "HangulHelper", TestCaseID = "TC-SUBJAMO-SIMPLE",
                Description = "Simple consonant falls into default case → single-element set",
                ExpectedResult = "{ 'ㄱ' }", StatusRound1 = "Passed",
                TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No compound → default switch branch" }
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // WordleHelper.CalculateFeedback
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public void CalculateFeedback_ExactMatch_ShouldBeAllGreen()
        {
            var target = "글자";
            var guess  = "글자";
            var result = WordleHelper.CalculateFeedback(target, guess);

            result.Should().HaveCount(2);
            result.Should().OnlyContain(fb => fb.BlockColor == "Green");

            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup = "WordleHelper", TestCaseID = "TC-WH-01",
                Description = "Identical target/guess → all blocks Green",
                ExpectedResult = "All BlockColor=Green", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "target[i] == guess[i] for all i" }
            });
        }

        [Fact]
        public void CalculateFeedback_CompletelyWrong_ShouldBeAllGray()
        {
            // Use clearly distinct Hangul chars with no shared jamos
            var target = "가나";
            var guess  = "루미";
            var result = WordleHelper.CalculateFeedback(target, guess);

            result.Should().HaveCount(2);
            result.Should().OnlyContain(fb => fb.BlockColor == "Gray");

            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup = "WordleHelper", TestCaseID = "TC-WH-02",
                Description = "No matching jamos at all → all blocks Gray",
                ExpectedResult = "All BlockColor=Gray", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No shared jamos between target/guess" }
            });
        }

        [Fact]
        public void CalculateFeedback_PartialInitialMatch_ShouldBeGreenInitial()
        {
            // 가 vs 거 → share ㄱ initial, different vowel, no final → Initial=Green, Vowel=Gray, Final=Green(\0==\0)
            var result = WordleHelper.CalculateFeedback("가", "거");

            result.Should().HaveCount(1);
            result[0].InitialStatus.Should().Be("Green");
            result[0].BlockColor.Should().Be("Yellow");  // partial match → Yellow block

            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup = "WordleHelper", TestCaseID = "TC-WH-03",
                Description = "Same initial ㄱ, different vowel → InitialStatus=Green, Block=Yellow",
                ExpectedResult = "InitialStatus=Green, BlockColor=Yellow", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "tParts.Initial == gParts.Initial but not exact char match" }
            });
        }

        [Fact]
        public void CalculateFeedback_FinalStatusGray_WhenTargetHasNoPatchim()
        {
            // 가 (no patchim) vs 갈 (final ㄹ) → Final should be Gray
            var result = WordleHelper.CalculateFeedback("가", "갈");

            result.Should().HaveCount(1);
            result[0].FinalStatus.Should().Be("Gray");

            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup = "WordleHelper", TestCaseID = "TC-WH-04",
                Description = "Target has no patchim, guess has patchim → FinalStatus=Gray",
                ExpectedResult = "FinalStatus=Gray", StatusRound1 = "Passed",
                TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "tParts.Final == \\0 → Gray branch" }
            });
        }

        [Fact]
        public void CalculateFeedback_FinalStatusYellow_WhenSharedSubJamo()
        {
            // 닭 (final ㄺ=ㄹ+ㄱ) vs 달 (final ㄹ) → tFinals ∩ gFinals ≠ ∅ → Yellow
            var result = WordleHelper.CalculateFeedback("닭", "달");

            result.Should().HaveCount(1);
            result[0].FinalStatus.Should().Be("Yellow");

            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup = "WordleHelper", TestCaseID = "TC-WH-05",
                Description = "Compound final ㄺ shares ㄹ with guess final ㄹ → FinalStatus=Yellow",
                ExpectedResult = "FinalStatus=Yellow", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "tFinals.Intersect(gFinals).Any() && both != \\0" }
            });
        }

        [Fact]
        public void CalculateFeedback_VowelStatusYellow_WhenSharedSubVowel()
        {
            // 봐 (vowel ㅘ=ㅗ+ㅏ) vs 보 (vowel ㅗ) → shared ㅗ → Yellow
            var result = WordleHelper.CalculateFeedback("봐", "보");

            result.Should().HaveCount(1);
            result[0].VowelStatus.Should().Be("Yellow");

            QACollector.LogTestCase("Helper - Wordle", new TestCaseDetail
            {
                FunctionGroup = "WordleHelper", TestCaseID = "TC-WH-06",
                Description = "Compound vowel ㅘ shares ㅗ with guess vowel ㅗ → VowelStatus=Yellow",
                ExpectedResult = "VowelStatus=Yellow", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "tVowels.Intersect(gVowels).Any()" }
            });
        }
    }
}
