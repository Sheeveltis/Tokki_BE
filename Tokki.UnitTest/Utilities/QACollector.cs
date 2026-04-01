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

        public static void LogTestCase(string featureName, TestCaseDetail testCase)
        {
            var tcList = _testResults.GetOrAdd(featureName, _ => new ConcurrentBag<TestCaseDetail>());
            tcList.Add(testCase);
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

                summary.Functions.Add(new FunctionSummary
                {
                    No = functionIndex++,
                    FunctionName = featureName,
                    SheetName = hasTest ? featureName : "N/A",
                    Description = hasTest ? $"Automated Unit Tests for {featureName}" : "No tests implemented yet",
                    IsTested = hasTest
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