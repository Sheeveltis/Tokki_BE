using System.Collections.Generic;
using System.Linq;

namespace Tokki.UnitTest.Utilities
{
    public class TestCaseSummary
    {
        public string ProjectName { get; set; } = "Tokki Project";
        public string ProjectCode { get; set; } = "TK-001";
        public string Environment { get; set; } = 
            "1. Framework: .NET 9.0 (net9.0)\n" +
            "2. Testing Framework: xUnit 2.9.2\n" +
            "3. Mocking Library: Moq 4.20.72\n" +
            "4. Assertion Library: FluentAssertions 8.8.0\n" +
            "5. Architecture: CQRS with MediatR (Commands & Queries)\n" +
            "6. Test Runner: Microsoft.NET.Test.Sdk 17.12.0\n" +
            "7. Code Coverage: Coverlet 8.0.1 (XPlat Code Coverage)\n" +
            "8. Report Generator: EPPlus 8.5.0\n" +
            "9. IDE: Visual Studio 2022 / Rider\n" +
            "10. OS: Windows 11";
        public List<FunctionSummary> Functions { get; set; } = new();

        public int TotalSystemFunctions { get; set; } = 10;

        public double TestCoverage => TotalSystemFunctions > 0
            ? (double)Functions.Count(f => f.IsTested) / TotalSystemFunctions * 100
            : 0;

        public double SuccessCoverage { get; set; }
    }

    public class FunctionSummary
    {
        public int No { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public string SheetName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PreCondition { get; set; } = string.Empty;

        public bool IsTested { get; set; }
    }

    public class FeatureSheet
    {
        public string FeatureName { get; set; } = string.Empty;
        public string TestRequirement { get; set; } = string.Empty;
        public int TotalTCs { get; set; }
        public int LinesOfCode { get; set; } = 0;
        public int LackOfTestCases { get; set; } = 0;
        public List<TestCaseDetail> TestCases { get; set; } = new();
    }

    public class TestCaseDetail
    {
        public string FunctionGroup { get; set; } = string.Empty;
        public string TestCaseID { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Procedure { get; set; } = string.Empty;
        public string ExpectedResult { get; set; } = string.Empty;
        public string PreCondition { get; set; } = string.Empty;
        public string StatusRound1 { get; set; } = "Pending"; 
        public string TestDate { get; set; } = string.Empty;
        public string TestCaseType { get; set; } = "N";
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> AppliedConditions { get; set; } = new();
    }
}