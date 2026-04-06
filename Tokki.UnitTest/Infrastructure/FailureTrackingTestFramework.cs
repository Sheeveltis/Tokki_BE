using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit.Abstractions;
using Xunit.Sdk;
using Tokki.UnitTest.Utilities;

namespace Tokki.UnitTest.Infrastructure
{
    /// <summary>
    /// Custom xUnit TestFramework that automatically captures test failures
    /// and logs them to QACollector with the CORRECT TestCaseID and feature name
    /// by reading the test source file and extracting metadata from the LogTestCase call.
    ///
    /// This ensures failed tests (whose LogTestCase call is never reached because
    /// an assertion throws before it) still appear in the Excel report under the
    /// correct feature sheet with the correct TC ID.
    /// </summary>
    public class FailureTrackingTestFramework : XunitTestFramework
    {
        public FailureTrackingTestFramework(IMessageSink messageSink)
            : base(messageSink)
        { }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        {
            return new FailureTrackingExecutor(
                assemblyName,
                SourceInformationProvider,
                DiagnosticMessageSink);
        }
    }

    public class FailureTrackingExecutor : XunitTestFrameworkExecutor
    {
        public FailureTrackingExecutor(
            AssemblyName assemblyName,
            ISourceInformationProvider sourceInformationProvider,
            IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        { }

        protected override void RunTestCases(
            IEnumerable<IXunitTestCase> testCases,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
        {
            var wrappedSink = new FailureInterceptingSink(executionMessageSink);
            base.RunTestCases(testCases, wrappedSink, executionOptions);
        }
    }

#pragma warning disable xUnit3000 // Not needed in .NET 9 (no cross-AppDomain)
    public class FailureInterceptingSink : IMessageSink
#pragma warning restore xUnit3000
    {
        private readonly IMessageSink _inner;

        // Base path to the Tokki.UnitTest project source
        private static readonly string _projectRoot;

        static FailureInterceptingSink()
        {
            try
            {
                // bin\Debug\net9.0 → go up 3 levels to project root
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            }
            catch
            {
                _projectRoot = "";
            }
        }

        public FailureInterceptingSink(IMessageSink inner)
        {
            _inner = inner;
        }

        public bool OnMessage(IMessageSinkMessage message)
        {
            if (message is ITestFailed failed)
            {
                try
                {
                    CaptureFailure(failed);
                }
                catch
                {
                    // Never let logging errors break the test runner
                }
            }

            return _inner.OnMessage(message);
        }

        private void CaptureFailure(ITestFailed failed)
        {
            var className = failed.TestCase?.TestMethod?.TestClass?.Class?.Name ?? "";
            var methodName = failed.TestCase?.TestMethod?.Method?.Name ?? "Unknown";

            // Build error message
            var errorMessage = "";
            if (failed.Messages != null && failed.Messages.Length > 0)
            {
                errorMessage = string.Join(" | ", failed.Messages);
            }
            if (errorMessage.Length > 500)
            {
                errorMessage = errorMessage.Substring(0, 497) + "...";
            }

            // ── Try to extract metadata from the source file ──
            var metadata = TryExtractFromSource(className, methodName);

            if (metadata != null)
            {
                // Check if already logged (test might have called LogTestCase before failing on a later assertion)
                if (QACollector.HasTestCase(metadata.FeatureName, metadata.TestCaseID))
                    return;

                QACollector.LogTestCase(metadata.FeatureName, new TestCaseDetail
                {
                    FunctionGroup     = metadata.FunctionGroup,
                    TestCaseID        = metadata.TestCaseID,
                    Description       = metadata.Description,
                    ExpectedResult    = metadata.ExpectedResult,
                    StatusRound1      = "Failed",
                    TestCaseType      = metadata.TestCaseType,
                    TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                    ErrorMessage      = errorMessage,
                    AppliedConditions = metadata.AppliedConditions
                });
            }
            // If source parsing fails, we simply skip — no phantom entries
        }

        // ═══════════════════════════════════════════════════════════════
        //  SOURCE FILE PARSING
        // ═══════════════════════════════════════════════════════════════

        private class SourceMetadata
        {
            public string FeatureName { get; set; } = "";
            public string FunctionGroup { get; set; } = "";
            public string TestCaseID { get; set; } = "";
            public string Description { get; set; } = "";
            public string ExpectedResult { get; set; } = "";
            public string TestCaseType { get; set; } = "N";
            public List<string> AppliedConditions { get; set; } = new();
        }

        /// <summary>
        /// Reads the .cs source file, finds the test method, and extracts
        /// all metadata from the QACollector.LogTestCase(...) call.
        /// </summary>
        private static SourceMetadata? TryExtractFromSource(string fullClassName, string methodName)
        {
            try
            {
                if (string.IsNullOrEmpty(_projectRoot) || string.IsNullOrEmpty(fullClassName))
                    return null;

                // Convert namespace to file path
                // e.g. Tokki.UnitTest.Application.UseCases.Accounts.ChangePasswordCommandHandlerTests
                //    → Application\UseCases\Accounts\ChangePasswordCommandHandlerTests.cs
                var relPath = fullClassName;
                if (relPath.StartsWith("Tokki.UnitTest."))
                    relPath = relPath.Substring("Tokki.UnitTest.".Length);

                relPath = relPath.Replace('.', Path.DirectorySeparatorChar) + ".cs";
                var filePath = Path.Combine(_projectRoot, relPath);

                if (!File.Exists(filePath))
                    return null;

                var sourceText = File.ReadAllText(filePath);

                // Find the method in the source
                var methodPattern = $@"(public|private|protected|internal)\s+(async\s+)?Task\s+{Regex.Escape(methodName)}\s*\(";
                var methodMatch = Regex.Match(sourceText, methodPattern);
                if (!methodMatch.Success)
                    return null;

                // Extract the content from method start to the next method or end of class
                int methodStart = methodMatch.Index;
                int searchEnd = sourceText.Length;

                // Find the next [Fact] or [Theory] which marks the next test method
                var nextMethodPattern = @"\[(Fact|Theory)\]";
                var nextMatch = Regex.Match(sourceText, nextMethodPattern, RegexOptions.None, TimeSpan.FromSeconds(1));
                // Find the NEXT occurrence after our method
                var pos = methodStart + methodMatch.Length;
                while (true)
                {
                    nextMatch = Regex.Match(sourceText.Substring(pos), nextMethodPattern);
                    if (nextMatch.Success)
                    {
                        searchEnd = pos + nextMatch.Index;
                        break;
                    }
                    break;
                }

                var methodBody = sourceText.Substring(methodStart, searchEnd - methodStart);

                // ── Extract feature name from LogTestCase("Feature Name", ...) ──
                var featureMatch = Regex.Match(methodBody, @"LogTestCase\(\s*""([^""]+)""");
                if (!featureMatch.Success) return null;

                var meta = new SourceMetadata
                {
                    FeatureName = featureMatch.Groups[1].Value
                };

                // ── Extract TestCaseID ──
                var tcIdMatch = Regex.Match(methodBody, @"TestCaseID\s*=\s*""([^""]+)""");
                if (tcIdMatch.Success) meta.TestCaseID = tcIdMatch.Groups[1].Value;

                // ── Extract FunctionGroup ──
                var fgMatch = Regex.Match(methodBody, @"FunctionGroup\s*=\s*""([^""]+)""");
                if (fgMatch.Success) meta.FunctionGroup = fgMatch.Groups[1].Value;

                // ── Extract Description ──
                var descMatch = Regex.Match(methodBody, @"Description\s*=\s*""([^""]+)""");
                if (descMatch.Success) meta.Description = descMatch.Groups[1].Value;

                // ── Extract ExpectedResult ──
                var expMatch = Regex.Match(methodBody, @"ExpectedResult\s*=\s*""([^""]+)""");
                if (expMatch.Success) meta.ExpectedResult = expMatch.Groups[1].Value;

                // ── Extract TestCaseType ──
                var typeMatch = Regex.Match(methodBody, @"TestCaseType\s*=\s*""([^""]+)""");
                if (typeMatch.Success) meta.TestCaseType = typeMatch.Groups[1].Value;

                // ── Extract AppliedConditions ──
                var condBlock = Regex.Match(methodBody,
                    @"AppliedConditions\s*=\s*new\s+List<string>\s*\{([^}]+)\}",
                    RegexOptions.Singleline);
                if (condBlock.Success)
                {
                    var condStrings = Regex.Matches(condBlock.Groups[1].Value, @"""([^""]+)""");
                    meta.AppliedConditions = condStrings.Cast<Match>().Select(m => m.Groups[1].Value).ToList();
                }

                return meta;
            }
            catch
            {
                return null;
            }
        }
    }
}
