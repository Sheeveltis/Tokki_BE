using FluentAssertions;
using System;
using System.Collections.Generic;
using Tokki.Application.Common.Helpers;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.Common.Helpers
{
    public class HangulHelperTests
    {
        [Fact]
        public void Decompose_ValidHangulChar_ReturnsCorrectParts()
        {
            // '글' = ㄱ + ㅡ + ㄹ
            var parts = HangulHelper.Decompose('글');

            parts.Initial.Should().Be('ㄱ');
            parts.Vowel.Should().Be('ㅡ');
            parts.Final.Should().Be('ㄹ');

            QACollector.LogTestCase("Helper - Hangul Decompose", new TestCaseDetail
            {
                FunctionGroup     = "HangulHelper",
                TestCaseID        = "TC-HLP-HAN-01",
                Description       = "Valid Hangul character is correctly decomposed",
                ExpectedResult    = "Returns exact Initial, Vowel, Final Jamo",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Decompose '글'" }
            });
        }

        [Fact]
        public void Decompose_NonHangulChar_ReturnsSelfWithEmptyParts()
        {
            var parts = HangulHelper.Decompose('A');

            parts.Initial.Should().Be('A');
            parts.Vowel.Should().Be(' ');
            parts.Final.Should().Be('\0');

            QACollector.LogTestCase("Helper - Hangul Decompose", new TestCaseDetail
            {
                FunctionGroup     = "HangulHelper",
                TestCaseID        = "TC-HLP-HAN-02",
                Description       = "Non Hangul character fallback",
                ExpectedResult    = "Returns initial=self, vowel=' ', final='\\0'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Decompose 'A'" }
            });
        }

        [Theory]
        [InlineData('ㅘ', new[] { 'ㅗ', 'ㅏ' })]
        [InlineData('ㄺ', new[] { 'ㄹ', 'ㄱ' })]
        [InlineData('ㄱ', new[] { 'ㄱ' })] // Default char
        public void GetSubJamos_ValidJamo_ReturnsCorrectSubJamos(char jamo, char[] expected)
        {
            var result = HangulHelper.GetSubJamos(jamo);

            result.Should().BeEquivalentTo(expected);

            QACollector.LogTestCase("Helper - Hangul SubJamos", new TestCaseDetail
            {
                FunctionGroup     = "HangulHelper",
                TestCaseID        = $"TC-HLP-HAN-03-{jamo}",
                Description       = $"Get sub jamos for {jamo}",
                ExpectedResult    = $"Returns {expected.Length} sub jamos",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { $"Sub Jamos validation" }
            });
        }
    }
}
