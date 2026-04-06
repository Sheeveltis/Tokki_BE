using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Tokki.UnitTest.Utilities
{
    public static class QACollector
    {
        private static readonly ConcurrentDictionary<string, ConcurrentBag<TestCaseDetail>> _testResults = new();

        private static Dictionary<string, int> _masterFunctions = new();
        private static readonly object _lock = new object();

        public static void RegisterMasterFunctions(params (string FunctionName, int LinesOfCode)[] functions)
        {
            lock (_lock)
            {
                _masterFunctions = functions.ToDictionary(f => f.FunctionName, f => f.LinesOfCode);
            }
        }

        /// <summary>
        /// Logs a test case result (called at the end of each [Fact]).
        /// This is the original method — works for tests that pass normally.
        /// </summary>
        public static void LogTestCase(string featureName, TestCaseDetail testCase)
        {
            var tcList = _testResults.GetOrAdd(featureName, _ => new ConcurrentBag<TestCaseDetail>());
            tcList.Add(testCase);
        }

        /// <summary>
        /// Checks if a test case with the given ID already exists in the collector.
        /// Used by FailureTrackingTestFramework to avoid duplicate entries.
        /// </summary>
        public static bool HasTestCase(string featureName, string testCaseId)
        {
            if (_testResults.TryGetValue(featureName, out var bag))
            {
                foreach (var tc in bag)
                {
                    if (tc.TestCaseID == testCaseId)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Wraps test assertions so that even when a test FAILS,
        /// the QACollector still captures the test case with StatusRound1 = "Failed"
        /// and the error message.
        ///
        /// Usage:
        /// <code>
        /// QACollector.Execute("Module - Action", new TestCaseDetail { ... }, () =>
        /// {
        ///     result.IsSuccess.Should().BeTrue();
        ///     result.StatusCode.Should().Be(200);
        /// });
        /// </code>
        /// </summary>
        public static void Execute(string featureName, TestCaseDetail testCase, Action assertions)
        {
            try
            {
                assertions();
                testCase.StatusRound1 = "Passed";
            }
            catch (Exception ex)
            {
                testCase.StatusRound1 = "Failed";
                testCase.ErrorMessage = ex.Message;
                LogTestCase(featureName, testCase);
                throw; // re-throw so xUnit still marks the test as failed
            }

            // Only reached if assertions passed
            LogTestCase(featureName, testCase);
        }

        /// <summary>
        /// Async version of Execute for async test assertions.
        /// </summary>
        public static async System.Threading.Tasks.Task ExecuteAsync(
            string featureName, TestCaseDetail testCase, Func<System.Threading.Tasks.Task> assertions)
        {
            try
            {
                await assertions();
                testCase.StatusRound1 = "Passed";
            }
            catch (Exception ex)
            {
                testCase.StatusRound1 = "Failed";
                testCase.ErrorMessage = ex.Message;
                LogTestCase(featureName, testCase);
                throw;
            }

            LogTestCase(featureName, testCase);
        }

        /// <summary>
        /// Returns all test cases that have StatusRound1 == "Failed".
        /// </summary>
        public static List<(string FeatureName, TestCaseDetail TestCase)> GetFailedTestCases()
        {
            var result = new List<(string, TestCaseDetail)>();
            foreach (var kvp in _testResults)
            {
                foreach (var tc in kvp.Value)
                {
                    if (tc.StatusRound1 == "Failed")
                    {
                        result.Add((kvp.Key, tc));
                    }
                }
            }
            return result.OrderBy(x => x.Item1).ThenBy(x => x.Item2.TestCaseID).ToList();
        }

        public static (TestCaseSummary Summary, List<FeatureSheet> Features) BuildReportData()
        {
            var summary = new TestCaseSummary();
            var featureSheets = new List<FeatureSheet>();

            var testedFeatures = _testResults.Keys.ToList();

            List<string> allFunctions;
            lock (_lock)
            {
                allFunctions = _masterFunctions.Keys.Union(testedFeatures).OrderBy(x => x).ToList();
            }

            summary.TotalSystemFunctions = allFunctions.Count;
            int functionIndex = 1;
            int fullyPassedFunctions = 0;

            foreach (var featureName in allFunctions)
            {
                bool hasTest = _testResults.TryGetValue(featureName, out var bag);
                var testCases = hasTest ? bag.ToList() : new List<TestCaseDetail>();

                if (hasTest && testCases.Count > 0 && testCases.All(tc => tc.StatusRound1 == "Passed"))
                {
                    fullyPassedFunctions++;
                }

                // Auto-generate PreCondition from test case AppliedConditions
                var preCondText = "";
                if (hasTest && testCases.Count > 0)
                {
                    var allConditions = testCases
                        .Where(tc => tc.AppliedConditions != null && tc.AppliedConditions.Count > 0)
                        .SelectMany(tc => tc.AppliedConditions)
                        .Where(c => !c.StartsWith("Return", System.StringComparison.OrdinalIgnoreCase)
                                 && !c.StartsWith("Exception", System.StringComparison.OrdinalIgnoreCase)
                                 && !c.StartsWith("Log message", System.StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.Trim())
                        .Distinct(System.StringComparer.OrdinalIgnoreCase)
                        .Take(8)
                        .ToList();

                    if (allConditions.Count > 0)
                    {
                        var numbered = allConditions.Select((c, idx) => $"{idx + 1}. {c}");
                        preCondText = string.Join("\n", numbered);
                    }
                    else
                    {
                        preCondText = "1. System is operational\n2. Test data is prepared";
                    }
                }

                summary.Functions.Add(new FunctionSummary
                {
                    No = functionIndex++,
                    FunctionName = featureName,
                    SheetName = hasTest ? featureName : "N/A",
                    Description = hasTest ? $"Automated Unit Tests for {featureName}" : "No tests implemented yet",
                    IsTested = hasTest,
                    PreCondition = preCondText
                });

                if (hasTest)
                {
                    // Resolve LOC directly for this specific feature folder
                    int loc = SourceCodeCounter.GetLinesOfCode(featureName);

                    // Fall back to master-function LOC if set manually
                    if (loc == 0)
                    {
                        lock (_lock)
                        {
                            _masterFunctions.TryGetValue(featureName, out loc);
                        }
                    }

                    featureSheets.Add(new FeatureSheet
                    {
                        FeatureName = featureName,
                        TestRequirement = $"Verify all logics in {featureName} module",
                        TotalTCs = testCases.Count,
                        LinesOfCode = loc,
                        TestCases = testCases.OrderBy(tc => tc.FunctionGroup).ThenBy(tc => tc.TestCaseID).ToList()
                    });
                }
            }

            summary.SuccessCoverage = summary.TotalSystemFunctions > 0
                ? (double)fullyPassedFunctions / summary.TotalSystemFunctions * 100
                : 0;

            return (summary, featureSheets);
        }


        public static void Clear()
        {
            _testResults.Clear();
            lock (_lock) { _masterFunctions.Clear(); }
        }
    }
}