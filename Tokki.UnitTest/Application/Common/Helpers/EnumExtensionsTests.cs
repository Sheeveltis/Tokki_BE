using FluentAssertions;
using System;
using System.ComponentModel;
using Tokki.Application.Common.Helpers;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.Common.Helpers
{
    public class EnumExtensionsTests
    {
        private enum TestEnum
        {
            [Description("First Item")]
            First = 1,

            Second = 2,

            [Description("")]
            EmptyDescription = 3
        }

        // TC-CH-EE-01 | N | Enum with Description Attribute
        [Fact]
        public void GetDescription_WithDescriptionAttribute_ReturnsDescription()
        {
            var value = TestEnum.First;
            var result = value.GetDescription();
            
            result.Should().Be("First Item");

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "EnumExtensions",
                TestCaseID = "TC-CH-EE-01",
                Description = "Enum with [Description] returns attribute value",
                ExpectedResult = "First Item",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Attribute exists" }
            });
        }

        // TC-CH-EE-02 | N | Enum without Description Attribute
        [Fact]
        public void GetDescription_WithoutDescriptionAttribute_ReturnsToString()
        {
            var value = TestEnum.Second;
            var result = value.GetDescription();
            
            result.Should().Be("Second");

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "EnumExtensions",
                TestCaseID = "TC-CH-EE-02",
                Description = "Enum without [Description] falls back to ToString()",
                ExpectedResult = "Second",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Attribute is null" }
            });
        }

        // TC-CH-EE-03 | A | Enum casted from out-of-bounds int
        [Fact]
        public void GetDescription_UndefinedEnumValue_ReturnsIntString()
        {
            var value = (TestEnum)999;
            var result = value.GetDescription();
            
            result.Should().Be("999");

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "EnumExtensions",
                TestCaseID = "TC-CH-EE-03",
                Description = "Undefined enum value parsed cleanly to ToString representation",
                ExpectedResult = "999",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Attribute lookup returns null" }
            });
        }

        // TC-CH-EE-04 | N | Enum with Empty String Description Attribute
        [Fact]
        public void GetDescription_EmptyDescriptionAttribute_ReturnsEmptyString()
        {
            var value = TestEnum.EmptyDescription;
            var result = value.GetDescription();
            
            result.Should().Be("");

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "EnumExtensions",
                TestCaseID = "TC-CH-EE-04",
                Description = "Blank description returns blank space",
                ExpectedResult = "empty string",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Attribute = empty string" }
            });
        }

        // TC-CH-EE-05 | A | Throws NRE if invoked on null directly somehow (Using object wrapper implicitly)
        [Fact]
        public void GetDescription_NullEnumObject_ShouldThrowNullReference()
        {
            Enum nullEnum = null!;
            Action act = () => nullEnum.GetDescription();
            
            act.Should().Throw<NullReferenceException>();

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "EnumExtensions",
                TestCaseID = "TC-CH-EE-05",
                Description = "Throws implicitly due to value.GetType() call on null",
                ExpectedResult = "NullReferenceException",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "null object reference" }
            });
        }

        // TC-CH-EE-06 | N | Verify edge case checking for base Enum behavior (System.Enum generic)
        [Fact]
        public void GetDescription_DayOfWeekSystemEnum_ShouldFallback()
        {
            var day = DayOfWeek.Monday;
            var result = day.GetDescription();
            
            result.Should().Be("Monday"); // Built-in enum has no description attribute mostly

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "EnumExtensions",
                TestCaseID = "TC-CH-EE-06",
                Description = "Operates seamlessly on any existing System.Enum",
                ExpectedResult = "Monday",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "System native enum" }
            });
        }
    }
}
