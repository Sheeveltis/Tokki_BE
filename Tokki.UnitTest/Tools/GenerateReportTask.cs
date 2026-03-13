using System;
using System.IO;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Tools
{
    [Collection("Sequential Report Export")]
    public class GenerateReportTask
    {
        [Fact]
        public void Export_QA_Report_To_Downloads()
        {
            System.Threading.Thread.Sleep(1000);

            var (summary, features) = QACollector.BuildReportData();

            if (summary.Functions.Count == 0) return;

            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string downloadsPath = Path.Combine(userProfilePath, "Downloads");
            string filePath = Path.Combine(downloadsPath, $"Tokki_QA_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");

            ExcelReportGenerator.ExportReport(filePath, summary, features);

            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public void Export_Full_Standard_Report()
        {
            System.Threading.Thread.Sleep(1000);

            var (summary, features) = QACollector.BuildReportData();

            if (summary.Functions.Count == 0) return;

            var header = new ProjectReportHeader
            {
                ProjectName = "TOKKI LEARNING MANAGEMENT SYSTEM",
                ProjectCode = "TK_CAPSTONE_2026",
                Creator = "Project Team G1"
            };

            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string downloadsPath = Path.Combine(userProfilePath, "Downloads");
            string fileName = $"Project_Test_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            string filePath = Path.Combine(downloadsPath, fileName);

            ExcelReportGenerator.ExportStandardReport(filePath, header, summary, features);

            Assert.True(File.Exists(filePath));
        }
    }
}