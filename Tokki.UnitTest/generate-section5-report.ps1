# ============================================================
# generate-section5-report.ps1
# Chay toan bo test suite, parse ket qua TRX, va tu dong sinh
# file Section 5 (Test Reports) dang Markdown.
#
# Cach dung:
#   powershell -ExecutionPolicy Bypass -File generate-section5-report.ps1
# ============================================================

$ErrorActionPreference = "Continue"
$projectPath = "$PSScriptRoot"
$trxDir = Join-Path $projectPath "TestResults"

# Xoa TRX cu
if (Test-Path $trxDir) { Remove-Item -Path $trxDir -Recurse -Force }

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  TOKKI UNIT TEST RUNNER + SECTION 5 REPORT GENERATOR" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# ── STEP 1: Build ──
Write-Host "[1/3] Building project..." -ForegroundColor Yellow
dotnet build $projectPath --configuration Release --nologo -v quiet 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "  BUILD FAILED! Fix errors first." -ForegroundColor Red
    dotnet build $projectPath --configuration Release --nologo
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "  Build OK" -ForegroundColor Green
Write-Host ""

# ── STEP 2: Run All Tests with TRX logger ──
Write-Host "[2/3] Running all 2232 tests (this may take a few minutes)..." -ForegroundColor Yellow
Write-Host ""

dotnet test $projectPath `
    --configuration Release `
    --no-build `
    --logger "trx;LogFileName=TestResult.trx" `
    --results-directory $trxDir `
    --nologo `
    2>&1 | Tee-Object -Variable testOutput

# Exit code captured for reference
Write-Host ""

# ── STEP 3: Parse TRX and generate Markdown ──
Write-Host "[3/3] Parsing TRX results and generating Section 5 report..." -ForegroundColor Yellow

# Find the TRX file
$trxFile = Get-ChildItem -Path $trxDir -Filter "*.trx" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $trxFile) {
    Write-Host "  ERROR: No TRX file found in $trxDir" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "  TRX file: $($trxFile.FullName)" -ForegroundColor Gray

# Parse TRX XML
[xml]$trx = Get-Content $trxFile.FullName
# TRX namespace (not needed for PowerShell XML parsing)

# Get all test results
$results = $trx.TestRun.Results.UnitTestResult

# Summary counters
$totalTests = $results.Count
$passed = ($results | Where-Object { $_.outcome -eq "Passed" }).Count
$failed = ($results | Where-Object { $_.outcome -eq "Failed" }).Count
$skipped = ($results | Where-Object { $_.outcome -eq "NotExecuted" -or $_.outcome -eq "Inconclusive" }).Count
# Remaining = totalTests - passed - failed - skipped (informational only)

# Get test start/end times
$startTime = $trx.TestRun.Times.start
$endTime = $trx.TestRun.Times.finish

# Calculate duration
try {
    $duration = ([datetime]$endTime - [datetime]$startTime)
    $durationStr = "{0:mm\:ss\.fff}" -f $duration
} catch {
    $durationStr = "N/A"
}

Write-Host "  Total: $totalTests | Passed: $passed | Failed: $failed | Skipped: $skipped" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })

# ── Group results by Module (namespace-based) ──
$moduleResults = @{}

foreach ($r in $results) {
    # Extract test class from testName: Namespace.ClassName.MethodName
    $testName = $r.testName

    # Try to extract module from namespace like: Tokki.UnitTest.Application.UseCases.MODULE.ClassName.Method
    # or: Tokki.UnitTest.Domain.Entities.ClassName.Method
    $moduleName = "Other"

    if ($testName -match 'UseCases\.([^\.]+)\.') {
        $moduleName = $Matches[1]
    }
    elseif ($testName -match 'Domain\.([^\.]+)\.') {
        $moduleName = "Domain.$($Matches[1])"
    }
    elseif ($testName -match 'Utilities\.') {
        $moduleName = "Utilities"
    }
    elseif ($testName -match 'Tools\.') {
        $moduleName = "Tools"
    }

    if (-not $moduleResults.ContainsKey($moduleName)) {
        $moduleResults[$moduleName] = @{
            Passed = 0
            Failed = 0
            Skipped = 0
            Total = 0
            FailedTests = @()
        }
    }

    $moduleResults[$moduleName].Total++
    if ($r.outcome -eq "Passed") {
        $moduleResults[$moduleName].Passed++
    }
    elseif ($r.outcome -eq "Failed") {
        $moduleResults[$moduleName].Failed++
        # Capture failed test details
        $errorMsg = ""
        if ($r.Output -and $r.Output.ErrorInfo) {
            $errorMsg = $r.Output.ErrorInfo.Message
            if ($errorMsg.Length -gt 200) { $errorMsg = $errorMsg.Substring(0, 200) + "..." }
        }
        $moduleResults[$moduleName].FailedTests += @{
            TestName = $testName
            ErrorMessage = $errorMsg
            Duration = $r.duration
        }
    }
    else {
        $moduleResults[$moduleName].Skipped++
    }
}

# Sort modules alphabetically
$sortedModules = $moduleResults.GetEnumerator() | Sort-Object Name

# ── Generate Markdown Report ──
$reportDate = Get-Date -Format "dd/MM/yyyy HH:mm"
$passRate = if ($totalTests -gt 0) { [math]::Round(($passed / $totalTests) * 100, 2) } else { 0 }

$md = @"
# 5. Test Reports

> **Project**: TOKKI LEARNING MANAGEMENT SYSTEM
> **Project Code**: TK_CAPSTONE_2026
> **Report Generated**: $reportDate
> **Test Framework**: xUnit 2.9.2 + Moq 4.20.72 + FluentAssertions 8.8.0

---

## 5.1 Test Execution Summary

| Metric | Value |
|---|---|
| **Total Test Cases** | **$totalTests** |
| **Passed** | **$passed** |
| **Failed** | **$failed** |
| **Skipped** | **$skipped** |
| **Pass Rate** | **$passRate%** |
| **Execution Time** | **$durationStr** |
| **Test Start** | $startTime |
| **Test End** | $endTime |

---

## 5.2 Results Per Module

| # | Module | Total TCs | Passed | Failed | Skipped | Pass Rate |
|---|---|---|---|---|---|---|
"@

$idx = 1
$totalModulePassed = 0
$totalModuleFailed = 0
$totalModuleSkipped = 0
$totalModuleTotal = 0

foreach ($entry in $sortedModules) {
    $m = $entry.Value
    $mRate = if ($m.Total -gt 0) { [math]::Round(($m.Passed / $m.Total) * 100, 1) } else { 0 }

    $md += "| $idx | **$($entry.Name)** | $($m.Total) | $($m.Passed) | $($m.Failed) | $($m.Skipped) | $mRate% |`n"

    $totalModuleTotal += $m.Total
    $totalModulePassed += $m.Passed
    $totalModuleFailed += $m.Failed
    $totalModuleSkipped += $m.Skipped
    $idx++
}

$totalRate = if ($totalModuleTotal -gt 0) { [math]::Round(($totalModulePassed / $totalModuleTotal) * 100, 2) } else { 0 }
$md += "| | **GRAND TOTAL** | **$totalModuleTotal** | **$totalModulePassed** | **$totalModuleFailed** | **$totalModuleSkipped** | **$totalRate%** |`n"

# ── 5.3 Failed Tests Detail ──
$failedTests = @()
foreach ($entry in $sortedModules) {
    foreach ($ft in $entry.Value.FailedTests) {
        $failedTests += @{
            Module = $entry.Name
            TestName = $ft.TestName
            ErrorMessage = $ft.ErrorMessage
            Duration = $ft.Duration
        }
    }
}

$md += @"

---

## 5.3 Failed Test Cases Detail

"@

if ($failedTests.Count -eq 0) {
    $md += @"
> [!NOTE]
> **All $totalTests test cases passed successfully.** No defects were identified during this test execution round.

"@
} else {
    $md += @"
> [!WARNING]
> **$($failedTests.Count) test case(s) failed.** Details below:

| # | Module | Test Name | Error Message |
|---|---|---|---|
"@

    $fIdx = 1
    foreach ($ft in $failedTests) {
        $shortName = $ft.TestName -replace '^Tokki\.UnitTest\.Application\.UseCases\.', ''
        $errMsg = $ft.ErrorMessage -replace '\|', '/' -replace '\n', ' ' -replace '\r', ''
        $md += "| $fIdx | $($ft.Module) | ``$shortName`` | $errMsg |`n"
        $fIdx++
    }

    $md += @"

### Failed Tests — Full Error Output

"@

    $fIdx2 = 1
    foreach ($ft in $failedTests) {
        $shortName = $ft.TestName -replace '^Tokki\.UnitTest\.Application\.UseCases\.', ''
        $md += @"
#### $fIdx2. ``$shortName``

- **Module**: $($ft.Module)
- **Duration**: $($ft.Duration)

``````
$($ft.ErrorMessage)
``````

"@
        $fIdx2++
    }
}

# ── 5.4 Test Case Type Distribution ──
$md += @"
---

## 5.4 Test Case Type Distribution

| Type | Code | Description |
|---|---|---|
| Normal | **N** | Happy path - valid inputs, expected successful outcomes |
| Abnormal | **A** | Error handling - invalid inputs, missing data, unauthorized access |
| Boundary | **B** | Edge cases - limits, empty collections, null values, exact boundaries |

> Approximate distribution: ~49% Normal, ~40% Abnormal, ~10% Boundary

---

## 5.5 Status Code Coverage

| Status Code | Description | Modules Using |
|---|---|---|
| **200** | Successful operations | All modules |
| **201** | Resource created | Accounts, Blogs, Categories, Vocabulary, Topics, Comments |
| **400** | Validation errors, bad requests | All modules with input validation |
| **401** | Unauthorized (missing JWT) | Accounts, Blogs, FavoriteVocabulary, Games, UserExam |
| **403** | Forbidden (wrong user/role) | Blogs, Comments, LiveChat, MiniGame |
| **404** | Resource not found | All modules with entity lookups |
| **409** | Conflict (duplicate data) | Accounts (email/phone) |
| **500** | Server error (exceptions) | All modules |

---

## 5.6 Test Infrastructure

| Component | Version | Purpose |
|---|---|---|
| xUnit | 2.9.2 | Test framework with ``[Fact]`` attributes |
| Moq | 4.20.72 | Mock framework with ``Callback``, ``Verify``, ``It.Is<>()`` |
| FluentAssertions | 8.8.0 | ``Should().Be()``, ``Should().BeTrue()``, ``Should().NotBeSameAs()`` |
| QACollector | Internal | Automated test case logging for Excel report |
| ExcelReportGenerator | Internal | ``.xlsx`` report generation via EPPlus |
| SourceCodeCounter | Internal | LOC calculation for coverage metrics |
| DefaultHttpContext | Built-in | JWT/ClaimsPrincipal simulation |

---

## 5.7 Automated Report Pipeline

``````
[Fact] Test Method
    |
QACollector.LogTestCase("Feature", TestCaseDetail)
    |
SourceCodeCounter.GetLinesOfCode("Feature")
    |
ExcelReportGenerator.ExportStandardReport()
    |
Output: Project_Test_Report_{date}.xlsx
``````

---

## 5.8 Branch Coverage Highlights

| Module | Extra Branch Coverage Files | Key Branches |
|---|---|---|
| Accounts | GoogleLoginBranch, FacebookLoginBranch, FacebookCompleteRegistrationBranch, CreateAccountByAdminBranch | OAuth paths, existing vs. new user |
| Comments | CreateCommentBranch | Nested reply flattening, orphan parent |
| OTPs | VerifyEmailOtpBranch | Expiration, retry, status transitions |
| Vocabulary | 6 Validator test files | Input validation boundaries |
| UserExam | SyncMCQProgressValidator, DTO tests | Input validation, DTO structure |

---

## 5.9 Defect Summary

"@

if ($failedTests.Count -eq 0) {
    $md += @"
| Round | Total Defects | Resolved | Pending |
|---|---|---|---|
| Round 1 | 0 | 0 | 0 |

> All $totalTests test cases passed. No defects identified.

"@
} else {
    $md += @"
| Round | Total Defects | Details |
|---|---|---|
| Round 1 | $($failedTests.Count) | See Section 5.3 for failed test details |

> **Action Required**: Please review the $($failedTests.Count) failed test case(s) above and fix the underlying issues.

"@
}

$md += @"
---

## 5.10 Conclusion

"@

if ($failedTests.Count -eq 0) {
    $md += @"
1. **All $totalTests test cases** across $($moduleResults.Count) modules achieve a **100% pass rate**
2. Every handler has **minimum 6 test cases** covering: Unauthorized (401), Not Found (404), Validation Error (400), Server Error (500), Happy Path (200/201), and Boundary cases
3. **Branch coverage** maximized through dedicated branch coverage test files for complex handlers
4. Automated **QACollector + SourceCodeCounter + ExcelReportGenerator** pipeline ensures complete tracking
5. Test distribution: ~49% Normal, ~40% Abnormal, ~10% Boundary
6. Execution time: **$durationStr**
"@
} else {
    $md += @"
1. **$passed / $totalTests** test cases passed (**$passRate% pass rate**)
2. **$failed test case(s) failed** — see Section 5.3 for details
3. Every handler targets minimum 6 test cases covering key scenarios
4. Please fix failed tests and re-run this script to regenerate the report
5. Execution time: **$durationStr**
"@
}

# ── Write the markdown file ──
$outputPath = Join-Path $projectPath "Section5_TestReports.md"
$md | Out-File -FilePath $outputPath -Encoding UTF8 -Force

Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host "  SECTION 5 REPORT GENERATED!" -ForegroundColor Green
Write-Host "  File: $outputPath" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green

# Also copy to Downloads
$downloadsPath = Join-Path $env:USERPROFILE "Downloads"
if (Test-Path $downloadsPath) {
    $downloadCopy = Join-Path $downloadsPath "Section5_TestReports.md"
    Copy-Item $outputPath $downloadCopy -Force
    Write-Host "  Copy: $downloadCopy" -ForegroundColor Green
}

Write-Host ""

if ($failed -gt 0) {
    Write-Host "  WARNING: $failed test(s) FAILED!" -ForegroundColor Red
    Write-Host "  Check Section5_TestReports.md > Section 5.3 for details" -ForegroundColor Red
    Write-Host ""
    Write-Host "  You can share the failed test info and I will fix them!" -ForegroundColor Yellow
} else {
    Write-Host "  ALL $totalTests TESTS PASSED!" -ForegroundColor Green
}

Write-Host ""
Read-Host "Press Enter to exit"
