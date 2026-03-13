using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Tokki.UnitTest.Utilities
{
    public static class ExcelReportGenerator
    {
        public static void ExportReport(string outputPath, TestCaseSummary summary, List<FeatureSheet> features)
        {
            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");
            using var package = new ExcelPackage();

            // ================= 1. VẼ SHEET TỔNG HỢP =================
            var wsSummary = package.Workbook.Worksheets.Add("TEST CASE LIST");

            wsSummary.Cells["D1:E1"].Merge = true;
            wsSummary.Cells["D1"].Value = "TEST CASE LIST";
            wsSummary.Cells["D1"].Style.Font.Bold = true;
            wsSummary.Cells["D1"].Style.Font.Size = 16;
            wsSummary.Cells["D1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            wsSummary.Cells["A3"].Value = "Project Name";
            wsSummary.Cells["D3"].Value = summary.ProjectName;
            wsSummary.Cells["A4"].Value = "Project Code";
            wsSummary.Cells["D4"].Value = summary.ProjectCode;
            wsSummary.Cells["A5"].Value = "Test Environment Setup Description";
            wsSummary.Cells["D5"].Value = summary.Environment;
            wsSummary.Cells["D5"].Style.WrapText = true;

            string[] summaryHeaders = { "No", "Function Name", "Sheet Name", "Description", "Pre-Condition" };
            int rowSum = 8;
            for (int i = 0; i < summaryHeaders.Length; i++)
            {
                var cell = wsSummary.Cells[rowSum, i + 1];
                cell.Value = summaryHeaders[i];
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.DarkBlue);
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Font.Bold = true;
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            rowSum++;
            foreach (var func in summary.Functions)
            {
                wsSummary.Cells[rowSum, 1].Value = func.No;
                wsSummary.Cells[rowSum, 2].Value = func.FunctionName;
                wsSummary.Cells[rowSum, 3].Formula = $"HYPERLINK(\"#'{func.SheetName}'!A1\", \"{func.SheetName}\")";
                wsSummary.Cells[rowSum, 3].Style.Font.Color.SetColor(Color.Blue);
                wsSummary.Cells[rowSum, 3].Style.Font.UnderLine = true;
                wsSummary.Cells[rowSum, 4].Value = func.Description;
                wsSummary.Cells[rowSum, 5].Value = func.PreCondition;
                rowSum++;
            }
            wsSummary.Cells.AutoFitColumns();

            // ================= 2. VẼ CÁC SHEET FEATURE CHI TIẾT =================
            foreach (var feature in features)
            {
                var wsDetail = package.Workbook.Worksheets.Add(feature.FeatureName);

                wsDetail.Cells["A2"].Value = "Feature"; wsDetail.Cells["B2"].Value = feature.FeatureName;
                wsDetail.Cells["A3"].Value = "Test requirement"; wsDetail.Cells["B3"].Value = feature.TestRequirement;
                wsDetail.Cells["A4"].Value = "Number of TCs"; wsDetail.Cells["B4"].Value = feature.TotalTCs;

                wsDetail.Cells["A6"].Value = "Round 1";
                wsDetail.Cells["A7"].Value = "Round 2";
                wsDetail.Cells["B5"].Value = "Passed"; wsDetail.Cells["C5"].Value = "Failed";
                wsDetail.Cells["D5"].Value = "Pending"; wsDetail.Cells["E5"].Value = "N/A";

                string[] detailHeaders = { "Test Case ID", "Test Case Description", "Test Case Procedure", "Expected Results", "Pre-conditions", "Round 1", "Test date" };
                int rowDet = 10;
                for (int i = 0; i < detailHeaders.Length; i++)
                {
                    var cell = wsDetail.Cells[rowDet, i + 1];
                    cell.Value = detailHeaders[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.OliveDrab);
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.Font.Bold = true;
                }

                rowDet++;

                var groupedTCs = feature.TestCases.GroupBy(x => x.FunctionGroup);

                foreach (var group in groupedTCs)
                {
                    var funcCell = wsDetail.Cells[rowDet, 1, rowDet, detailHeaders.Length];
                    funcCell.Merge = true;
                    funcCell.Value = group.Key;
                    funcCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    funcCell.Style.Fill.BackgroundColor.SetColor(Color.LightCyan);
                    funcCell.Style.Font.Bold = true;
                    rowDet++;

                    foreach (var tc in group)
                    {
                        wsDetail.Cells[rowDet, 1].Value = tc.TestCaseID;
                        wsDetail.Cells[rowDet, 2].Value = tc.Description;
                        wsDetail.Cells[rowDet, 3].Value = tc.Procedure;
                        wsDetail.Cells[rowDet, 4].Value = tc.ExpectedResult;
                        wsDetail.Cells[rowDet, 5].Value = tc.PreCondition;
                        wsDetail.Cells[rowDet, 6].Value = tc.StatusRound1;
                        wsDetail.Cells[rowDet, 7].Value = tc.TestDate;

                        wsDetail.Cells[rowDet, 2, rowDet, 5].Style.WrapText = true;
                        rowDet++;
                    }
                }

                wsDetail.Column(2).Width = 30;
                wsDetail.Column(3).Width = 40;
                wsDetail.Column(4).Width = 40;
                wsDetail.Column(5).Width = 25;
            }

            // Lưu file ra ổ cứng
            FileInfo fileInfo = new FileInfo(outputPath);
            package.SaveAs(fileInfo);
        }
        public static void ExportStandardReport(string outputPath, ProjectReportHeader header, TestCaseSummary summary, List<FeatureSheet> features)
        {
            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");

            using (var package = new ExcelPackage())
            {
                // ================= SHEET 1: COVER =================
                var wsCover = package.Workbook.Worksheets.Add("Cover");
                wsCover.Cells["B2:G2"].Merge = true;
                wsCover.Cells["B2"].Value = "TEST REPORT DOCUMENT";
                wsCover.Cells["B2"].Style.Font.Size = 20;
                wsCover.Cells["B2"].Style.Font.Bold = true;
                wsCover.Cells["B2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                wsCover.Cells["B4"].Value = "Project Name"; wsCover.Cells["C4"].Value = header.ProjectName;
                wsCover.Cells["F4"].Value = "Creator"; wsCover.Cells["G4"].Value = header.Creator;
                wsCover.Cells["B5"].Value = "Project Code"; wsCover.Cells["C5"].Value = header.ProjectCode;
                wsCover.Cells["F5"].Value = "Issue Date"; wsCover.Cells["G5"].Value = DateTime.Now.ToString("dd/MM/yyyy");
                wsCover.Cells["B4:G6"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                // ================= SHEET 2: TEST CASE LIST =================
                var wsList = package.Workbook.Worksheets.Add("Test Cases");
                wsList.Cells["B2"].Value = "TEST CASE LIST";
                wsList.Cells["B2"].Style.Font.Size = 18;
                wsList.Cells["B2"].Style.Font.Bold = true;

                string[] listHeaders = { "No", "Function Name", "Sheet Name", "Description" };
                for (int i = 0; i < listHeaders.Length; i++)
                {
                    var cell = wsList.Cells[8, i + 2];
                    cell.Value = listHeaders[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.SetBackground(Color.Navy);
                    cell.Style.Font.Color.SetColor(Color.White);
                }

                int rowList = 9;
                foreach (var f in summary.Functions)
                {
                    wsList.Cells[rowList, 2].Value = f.No;
                    wsList.Cells[rowList, 3].Value = f.FunctionName;
                    wsList.Cells[rowList, 4].Value = f.SheetName;
                    wsList.Cells[rowList, 5].Value = f.Description;
                    rowList++;
                }

                // ================= SHEET 3: TEST STATISTICS (Ảnh 2 sếp gửi) =================
                var wsStat = package.Workbook.Worksheets.Add("Test Statistics");
                wsStat.Cells["B2:H2"].Merge = true;
                wsStat.Cells["B2"].Value = "TEST STATISTICS";
                wsStat.Cells["B2"].Style.Font.Size = 18;
                wsStat.Cells["B2"].Style.Font.Bold = true;
                wsStat.Cells["B2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Header Table Statistics
                string[] statHeaders = { "No", "Module Name", "Passed", "Failed", "Pending", "N/A", "Total TCs" };
                for (int i = 0; i < statHeaders.Length; i++)
                {
                    var cell = wsStat.Cells[10, i + 2];
                    cell.Value = statHeaders[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.SetBackground(Color.Navy);
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.Font.Bold = true;
                }

                int rowStat = 11;
                foreach (var f in features)
                {
                    wsStat.Cells[rowStat, 2].Value = rowStat - 10;
                    wsStat.Cells[rowStat, 3].Value = f.FeatureName;
                    wsStat.Cells[rowStat, 4].Value = f.TestCases.Count(x => x.StatusRound1 == "Passed");
                    wsStat.Cells[rowStat, 5].Value = f.TestCases.Count(x => x.StatusRound1 == "Failed");
                    wsStat.Cells[rowStat, 6].Value = f.TestCases.Count(x => x.StatusRound1 == "Pending");
                    wsStat.Cells[rowStat, 7].Value = 0; // N/A
                    wsStat.Cells[rowStat, 8].Value = f.TotalTCs;
                    rowStat++;
                }

                // Vùng Test Coverage (Ảnh 2 - Hàng 16, 17)
                wsStat.Cells[rowStat + 2, 3].Value = "Test coverage";
                wsStat.Cells[rowStat + 2, 6].Value = summary.TestCoverage / 100;
                wsStat.Cells[rowStat + 2, 6].Style.Numberformat.Format = "0.00%";
                wsStat.Cells[rowStat + 2, 6].Style.Font.Bold = true;
                wsStat.Cells[rowStat + 2, 6].Style.Font.Color.SetColor(Color.Blue);

                wsStat.Cells[rowStat + 3, 3].Value = "Test successful coverage";
                wsStat.Cells[rowStat + 3, 6].Value = summary.SuccessCoverage / 100;
                wsStat.Cells[rowStat + 3, 6].Style.Numberformat.Format = "0.00%";

                foreach (var feature in features)
                {
                    var wsF = package.Workbook.Worksheets.Add(feature.FeatureName);

                    DrawFeatureHeader(wsF, feature, header);

                    DrawFeatureMatrix(wsF, feature);

                    wsF.Cells[wsF.Dimension.Address].AutoFitColumns(15, 50);
                }

                package.SaveAs(new FileInfo(outputPath));
            }
        }

        private static void DrawFeatureMatrix(ExcelWorksheet ws, FeatureSheet feature)
        {
            // Start column for test cases (A, B, C, D are merged for labels, so data starts at E = 5)
            int startCol = 5;

            // ================= ROW 9: MATRIX HEADER (UTCIDs) =================
            ws.Cells["A9:D9"].Merge = true;
            ws.Cells["A9:D9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells["A9:D9"].Style.Fill.SetBackground(Color.Navy);

            for (int i = 0; i < feature.TestCases.Count; i++)
            {
                var cell = ws.Cells[9, startCol + i];
                cell.Value = feature.TestCases[i].TestCaseID;
                cell.Style.TextRotation = 90;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.SetBackground(Color.Navy);
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Font.Bold = true;
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            // ================= DYNAMIC DATA PREPARATION =================
            var allApplied = feature.TestCases
                .Where(tc => tc.AppliedConditions != null)
                .SelectMany(x => x.AppliedConditions)
                .Distinct()
                .ToList();

            // Separate Confirms (Returns/Exceptions/Logs) from general Conditions
            var confirmsList = allApplied
                .Where(x => x.StartsWith("Return", StringComparison.OrdinalIgnoreCase) ||
                            x.StartsWith("Exception", StringComparison.OrdinalIgnoreCase) ||
                            x.StartsWith("Log message", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var conditionsList = allApplied.Except(confirmsList).ToList();

            // Fallbacks to prevent empty blocks and Excel merging errors
            if (conditionsList.Count == 0) conditionsList.Add("No specific condition");
            if (confirmsList.Count == 0) confirmsList.Add("No specific confirm");

            int currentRow = 10;

            // ================= BLOCK 1: CONDITIONS =================
            int startCondRow = currentRow;
            foreach (var cond in conditionsList)
            {
                ws.Cells[currentRow, 2, currentRow, 4].Merge = true;
                ws.Cells[currentRow, 2].Value = cond;

                for (int i = 0; i < feature.TestCases.Count; i++)
                {
                    if (feature.TestCases[i].AppliedConditions != null && feature.TestCases[i].AppliedConditions.Contains(cond))
                    {
                        ws.Cells[currentRow, startCol + i].Value = "O";
                        ws.Cells[currentRow, startCol + i].Style.Font.Bold = true;
                        ws.Cells[currentRow, startCol + i].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                }
                currentRow++;
            }
            int endCondRow = currentRow - 1;

            // ================= BLOCK 2: CONFIRMS =================
            int startConfRow = currentRow;
            foreach (var conf in confirmsList)
            {
                ws.Cells[currentRow, 2, currentRow, 4].Merge = true;
                ws.Cells[currentRow, 2].Value = conf;

                for (int i = 0; i < feature.TestCases.Count; i++)
                {
                    if (feature.TestCases[i].AppliedConditions != null && feature.TestCases[i].AppliedConditions.Contains(conf))
                    {
                        ws.Cells[currentRow, startCol + i].Value = "O";
                        ws.Cells[currentRow, startCol + i].Style.Font.Bold = true;
                        ws.Cells[currentRow, startCol + i].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                }
                currentRow++;
            }
            int endConfRow = currentRow - 1;

            // ================= BLOCK 3: RESULTS =================
            int startResRow = currentRow;

            // Row 1: Type
            ws.Cells[currentRow, 2, currentRow, 4].Merge = true;
            ws.Cells[currentRow, 2].Value = "Type(N : Normal, A : Abnormal, B : Boundary)";
            for (int i = 0; i < feature.TestCases.Count; i++)
            {
                // UPDATE: Pull TestCaseType from DTO (N, A, or B)
                ws.Cells[currentRow, startCol + i].Value = feature.TestCases[i].TestCaseType;
                ws.Cells[currentRow, startCol + i].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            currentRow++;

            // Row 2: Passed/Failed
            ws.Cells[currentRow, 2, currentRow, 4].Merge = true;
            ws.Cells[currentRow, 2].Value = "Passed/Failed";
            for (int i = 0; i < feature.TestCases.Count; i++)
            {
                var status = feature.TestCases[i].StatusRound1 == "Passed" ? "P" : "F";
                ws.Cells[currentRow, startCol + i].Value = status;
                ws.Cells[currentRow, startCol + i].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            currentRow++;

            // Row 3: Executed Date
            ws.Cells[currentRow, 2, currentRow, 4].Merge = true;
            ws.Cells[currentRow, 2].Value = "Executed Date";
            for (int i = 0; i < feature.TestCases.Count; i++)
            {
                ws.Cells[currentRow, startCol + i].Value = feature.TestCases[i].TestDate;
                ws.Cells[currentRow, startCol + i].Style.TextRotation = 90;
                ws.Cells[currentRow, startCol + i].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            currentRow++;

            // Row 4: Defect ID
            ws.Cells[currentRow, 2, currentRow, 4].Merge = true;
            ws.Cells[currentRow, 2].Value = "Defect ID";
            int endResRow = currentRow;

            // ================= FORMAT COLUMN A (VERTICAL LABELS) =================
            var blocks = new[] {
            (startCondRow, endCondRow, "Condition"),
            (startConfRow, endConfRow, "Confirm"),
            (startResRow, endResRow, "Result")
        };

            foreach (var block in blocks)
            {
                if (block.Item1 <= block.Item2)
                {
                    var range = ws.Cells[block.Item1, 1, block.Item2, 1];
                    range.Merge = true;
                    range.Value = block.Item3;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.SetBackground(Color.Navy);
                    range.Style.Font.Color.SetColor(Color.White);
                    range.Style.Font.Bold = true;
                    range.Style.TextRotation = 90;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }
            }

            // ================= APPLY BORDERS =================
            var matrixRange = ws.Cells[9, 1, endResRow, startCol + feature.TestCases.Count - 1];
            matrixRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            matrixRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            matrixRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            matrixRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }
        private static void DrawFeatureHeader(ExcelWorksheet ws, FeatureSheet feature, ProjectReportHeader header)
        {
            // ================= ROW 2: Function Code & Name =================
            // Function Code (2), Function Code Value (2), Function Name (7), Function Name Value (9) -> Total: 20 cols (A to T)
            ws.Cells["A2:B2"].Merge = true; ws.Cells["A2"].Value = "Function Code";
            ws.Cells["C2:D2"].Merge = true; ws.Cells["C2"].Value = feature.FeatureName;
            ws.Cells["E2:K2"].Merge = true; ws.Cells["E2"].Value = "Function Name";
            ws.Cells["L2:T2"].Merge = true; ws.Cells["L2"].Value = feature.FeatureName;

            // ================= ROW 3: Creator & Executor =================
            // Tương tự Row 2: (2), (2), (7), (9)
            ws.Cells["A3:B3"].Merge = true; ws.Cells["A3"].Value = "Created By";
            ws.Cells["C3:D3"].Merge = true; ws.Cells["C3"].Value = header.Creator;
            ws.Cells["E3:K3"].Merge = true; ws.Cells["E3"].Value = "Executed By";
            ws.Cells["L3:T3"].Merge = true; ws.Cells["L3"].Value = header.Executor;

            // ================= ROW 4: Lines of code & Lack of test cases =================
            // Tương tự Row 2: (2), (2), (7), (9)
            ws.Cells["A4:B4"].Merge = true; ws.Cells["A4"].Value = "Lines of code";
            ws.Cells["C4:D4"].Merge = true; ws.Cells["C4"].Value = feature.LinesOfCode > 0 ? feature.LinesOfCode.ToString() : "";

            ws.Cells["E4:K4"].Merge = true; ws.Cells["E4"].Value = "Lack of test cases";

            // --- Tính toán Logic Lack of test cases (KLOC) ---
            double expectedTCs = (feature.LinesOfCode / 1000.0) * header.NormalTestCasesPerKLOC;
            int lack = feature.TotalTCs - (int)Math.Ceiling(expectedTCs);
            feature.LackOfTestCases = lack >= 0 ? 0 : lack;

            ws.Cells["L4:T4"].Merge = true;
            ws.Cells["L4"].Value = feature.LinesOfCode > 0 ? feature.LackOfTestCases.ToString() : "";

            // ================= ROW 5: Test requirement =================
            // Test requirement (2), Content (18) -> Total 20 cols
            ws.Cells["A5:B5"].Merge = true; ws.Cells["A5"].Value = "Test requirement";
            ws.Cells["C5:T5"].Merge = true; ws.Cells["C5"].Value = feature.TestRequirement;

            // Apply bold font to header labels
            ws.Cells["A2:A5"].Style.Font.Bold = true;
            ws.Cells["E2:E4"].Style.Font.Bold = true;

            // ================= ROW 6: Statistics Headers =================
            // Passed (2), Failed (2), Untested (6), N/A/B (3), Total Test Cases (7) -> Total 20 cols (A to T)
            ws.Cells["A6:B6"].Merge = true; ws.Cells["A6"].Value = "Passed";
            ws.Cells["C6:D6"].Merge = true; ws.Cells["C6"].Value = "Failed";
            ws.Cells["E6:J6"].Merge = true; ws.Cells["E6"].Value = "Untested";
            ws.Cells["K6:M6"].Merge = true; ws.Cells["K6"].Value = "N/A/B";
            ws.Cells["N6:T6"].Merge = true; ws.Cells["N6"].Value = "Total Test Cases";
            ws.Cells["A6:T6"].Style.Font.Bold = true;

            // ================= ROW 7: Statistics Values =================
            ws.Cells["A7:B7"].Merge = true; ws.Cells["A7"].Value = feature.TestCases.Count(x => x.StatusRound1 == "Passed");
            ws.Cells["C7:D7"].Merge = true; ws.Cells["C7"].Value = feature.TestCases.Count(x => x.StatusRound1 == "Failed");
            ws.Cells["E7:J7"].Merge = true; ws.Cells["E7"].Value = feature.TestCases.Count(x => x.StatusRound1 == "Pending");

            // Tự động đếm số lượng Test Case theo loại N (Normal), A (Abnormal), B (Boundary)
            ws.Cells["K7"].Value = feature.TestCases.Count(x => x.TestCaseType == "N");
            ws.Cells["L7"].Value = feature.TestCases.Count(x => x.TestCaseType == "A");
            ws.Cells["M7"].Value = feature.TestCases.Count(x => x.TestCaseType == "B");

            ws.Cells["N7:T7"].Merge = true; ws.Cells["N7"].Value = feature.TotalTCs;

            // ================= APPLY STYLES & BORDERS =================
            var fullHeaderRange = ws.Cells["A2:T7"];
            fullHeaderRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            fullHeaderRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            fullHeaderRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            fullHeaderRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            // Center alignment for statistics
            ws.Cells["A6:T7"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }
    }
}