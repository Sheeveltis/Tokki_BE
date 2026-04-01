using System;
using System.IO;
using System.Linq;
using Tokki.UnitTest.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Tokki.UnitTest.Tools
{
    // ─────────────────────────────────────────────────────────────────────────
    // Z_GenerateReportTask  (prefix "Z_" ensures this class sorts LAST
    // alphabetically, so xUnit schedules it after all other test classes
    // when parallelization is disabled.)
    // ─────────────────────────────────────────────────────────────────────────
    public class Z_GenerateReportTask
    {
        private readonly ITestOutputHelper _output;

        public Z_GenerateReportTask(ITestOutputHelper output)
        {
            _output = output;
        }

        private void Log(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _output.WriteLine(line);
            Console.WriteLine(line);

            // Also write to a debug log file on Desktop for guaranteed visibility
            try
            {
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Tokki_TestReport_Debug.log");
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch { /* ignore log write failures */ }
        }

        [Fact]
        public void Export_QA_Report_To_Downloads()
        {
            try
            {
                Log("=== Export_QA_Report_To_Downloads START ===");

                var (summary, features) = QACollector.BuildReportData();

                var totalTCs = features.Sum(f => f.TestCases.Count);
                Log($"Functions: {summary.Functions.Count}, Features with tests: {features.Count}, Total TCs: {totalTCs}");

                if (features.Count > 0)
                {
                    foreach (var f in features)
                        Log($"  Feature: '{f.FeatureName}' → {f.TestCases.Count} TCs");
                }

                if (summary.Functions.Count == 0)
                {
                    Log("WARNING: No data collected! QACollector._testResults is empty.");
                    Log("This means LogTestCase() was never called before this test ran.");
                    Log("Make sure you click 'Run All' in Test Explorer, NOT run individual files.");
                    return;
                }

                // Determine output directory
                string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string downloadsPath   = Path.Combine(userProfilePath, "Downloads");
                string desktopPath     = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string baseDir         = Directory.Exists(downloadsPath) ? downloadsPath : desktopPath;

                string filePath = Path.Combine(baseDir, $"Tokki_QA_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");

                Log($"Writing QA report to: {filePath}");
                ExcelReportGenerator.ExportReport(filePath, summary, features);

                if (File.Exists(filePath))
                    Log($"✅ QA Report created: {filePath}");
                else
                    Log($"❌ QA Report NOT found after write: {filePath}");

                Assert.True(File.Exists(filePath), $"Excel file was NOT created at: {filePath}");
            }
            catch (Exception ex)
            {
                Log($"❌ EXCEPTION in Export_QA_Report: {ex.GetType().Name}: {ex.Message}");
                Log(ex.StackTrace ?? "");
                throw; // re-throw so test fails visibly
            }
        }

        [Fact]
        public void Export_Full_Standard_Report()
        {
            try
            {
                Log("=== Export_Full_Standard_Report START ===");

                var (summary, features) = QACollector.BuildReportData();

                var totalTCs = features.Sum(f => f.TestCases.Count);
                Log($"Functions: {summary.Functions.Count}, Features with tests: {features.Count}, Total TCs: {totalTCs}");

                if (summary.Functions.Count == 0)
                {
                    Log("WARNING: No data collected. Skipping report generation.");
                    return;
                }

                var header = new ProjectReportHeader
                {
                    ProjectName = "TOKKI LEARNING MANAGEMENT SYSTEM",
                    ProjectCode = "TK_CAPSTONE_2026",
                    Creator     = "Project Team G1"
                };

                string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string downloadsPath   = Path.Combine(userProfilePath, "Downloads");
                string desktopPath     = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string baseDir         = Directory.Exists(downloadsPath) ? downloadsPath : desktopPath;

                string fileName = $"Project_Test_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                string filePath = Path.Combine(baseDir, fileName);

                Log($"Writing Standard report to: {filePath}");
                ExcelReportGenerator.ExportStandardReport(filePath, header, summary, features);

                if (File.Exists(filePath))
                    Log($"✅ Standard Report created: {filePath}");
                else
                    Log($"❌ Standard Report NOT found after write: {filePath}");

                Assert.True(File.Exists(filePath), $"Excel file was NOT created at: {filePath}");
            }
            catch (Exception ex)
            {
                Log($"❌ EXCEPTION in Export_Full_Standard_Report: {ex.GetType().Name}: {ex.Message}");
                Log(ex.StackTrace ?? "");
                throw;
            }
        }
    }
}