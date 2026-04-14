# ============================================================
# run-tests.ps1  –  Chạy toàn bộ unit test & xuất file Excel
# Cách dùng: Chuột phải file này → "Run with PowerShell"
#            Hoặc: powershell -ExecutionPolicy Bypass -File run-tests.ps1
# ============================================================

$projectPath = "$PSScriptRoot"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TOKKI UNIT TEST RUNNER" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Build trước
Write-Host "[1/2] Building project..." -ForegroundColor Yellow
dotnet build $projectPath --configuration Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED. Fix errors before running tests." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host " Build OK" -ForegroundColor Green
Write-Host ""

# Chạy toàn bộ test suite
Write-Host "[2/2] Running all tests..." -ForegroundColor Yellow
Write-Host "      (Excel files will be saved to Downloads folder)" -ForegroundColor Gray
Write-Host ""

dotnet test $projectPath `
    --configuration Release `
    --no-build `
    --logger "console;verbosity=normal" `
    --nologo

Write-Host ""
if ($LASTEXITCODE -eq 0) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  ALL TESTS PASSED" -ForegroundColor Green
    Write-Host "  Check Downloads folder for Excel" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
} else {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  SOME TESTS FAILED" -ForegroundColor Red
    Write-Host "  Check output above for details" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
}

Write-Host ""
# Mở thư mục Downloads để tiện kiểm tra
$downloads = [System.IO.Path]::Combine($env:USERPROFILE, "Downloads")
Write-Host "Opening Downloads folder: $downloads" -ForegroundColor Cyan
Start-Process explorer.exe $downloads

Read-Host "Press Enter to exit"
