# ============================================================
# extract-failures.ps1
# Chay test va chi xuat danh sach FAILED tests
# Output nho gon, de copy paste
# ============================================================

$projectPath = "$PSScriptRoot"
$trxDir = Join-Path $projectPath "TestResults"

if (Test-Path $trxDir) { Remove-Item -Path $trxDir -Recurse -Force }

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RUNNING TESTS..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Build
dotnet build $projectPath --configuration Release --nologo -v quiet 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED!" -ForegroundColor Red
    Read-Host "Press Enter"
    exit 1
}

# Run tests with TRX
dotnet test $projectPath `
    --configuration Release `
    --no-build `
    --logger "trx;LogFileName=Result.trx" `
    --results-directory $trxDir `
    --nologo `
    -v quiet 2>&1 | Out-Null

# Parse TRX
$trxFile = Get-ChildItem -Path $trxDir -Filter "*.trx" -Recurse | Select-Object -First 1
if (-not $trxFile) {
    Write-Host "No TRX file found!" -ForegroundColor Red
    exit 1
}

[xml]$trx = Get-Content $trxFile.FullName
$results = $trx.TestRun.Results.UnitTestResult

$total = $results.Count
$passed = ($results | Where-Object { $_.outcome -eq "Passed" }).Count
$failedList = $results | Where-Object { $_.outcome -eq "Failed" }
$failedCount = $failedList.Count

# Build output
$output = @()
$output += "TOKKI TEST RESULTS"
$output += "=================="
$output += "Total: $total | Passed: $passed | Failed: $failedCount"
$output += "Pass Rate: $([math]::Round(($passed/$total)*100, 1))%"
$output += ""

if ($failedCount -gt 0) {
    $output += "FAILED TESTS ($failedCount):"
    $output += "=========================="
    
    $idx = 1
    foreach ($f in $failedList) {
        $name = $f.testName -replace '^Tokki\.UnitTest\.Application\.UseCases\.', ''
        $name = $name -replace '^Tokki\.UnitTest\.', ''
        
        $err = ""
        if ($f.Output -and $f.Output.ErrorInfo) {
            $err = $f.Output.ErrorInfo.Message
            # Trim to first 2 lines max
            $lines = $err -split "`n"
            if ($lines.Count -gt 2) {
                $err = ($lines[0..1] -join " ").Trim()
            } else {
                $err = $err.Trim() -replace "`r", "" -replace "`n", " "
            }
            if ($err.Length -gt 150) { $err = $err.Substring(0, 150) + "..." }
        }
        
        $output += "$idx. $name"
        $output += "   ERR: $err"
        $output += ""
        $idx++
    }

    # Group by module
    $output += ""
    $output += "SUMMARY BY MODULE:"
    $output += "==================="
    
    $moduleGroups = @{}
    foreach ($f in $failedList) {
        $mod = "Other"
        if ($f.testName -match 'UseCases\.([^\.]+)\.') { $mod = $Matches[1] }
        if (-not $moduleGroups.ContainsKey($mod)) { $moduleGroups[$mod] = 0 }
        $moduleGroups[$mod]++
    }
    
    foreach ($m in ($moduleGroups.GetEnumerator() | Sort-Object Name)) {
        $output += "  $($m.Name): $($m.Value) failed"
    }
} else {
    $output += "ALL TESTS PASSED!"
}

# Write to file
$outputPath = Join-Path $projectPath "failed_tests.txt"
$output | Out-File -FilePath $outputPath -Encoding UTF8 -Force

# Also show in console
$output | ForEach-Object { Write-Host $_ }

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Output saved to: $outputPath" -ForegroundColor Green
Write-Host "  Copy noi dung file nay gui cho AI" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Green
Read-Host "Press Enter to exit"
