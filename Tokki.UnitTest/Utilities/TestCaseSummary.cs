using System.Collections.Generic;
using System.Linq;

namespace Tokki.UnitTest.Utilities
{
    public class TestCaseSummary
    {
        public string ProjectName { get; set; } = "A Comprehensive Korean Learning Platform from Beginner to TOPIK, integrating Gamification Strategies, SRS Flashcards, and AI-powered Pronunciation Practice";
        public string ProjectCode { get; set; } = "TK_CAPSTONE_2026";
        public string Environment { get; set; } = 
            "- Framework: .NET 9.0 (net9.0)\n" +
            "- Testing Framework: xUnit 2.9.2\n" +
            "- Mocking Library: Moq 4.20.72\n" +
            "- Assertion Library: FluentAssertions 8.8.0\n" +
            "- Architecture: CQRS with MediatR (Commands & Queries)\n" +
            "- Test Runner: Microsoft.NET.Test.Sdk 17.12.0\n" +
            "- Code Coverage: Coverlet 8.0.1 (XPlat Code Coverage)\n" +
            "- Report Generator: EPPlus 8.5.0\n" +
            "- IDE: Visual Studio 2022 / Rider";
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