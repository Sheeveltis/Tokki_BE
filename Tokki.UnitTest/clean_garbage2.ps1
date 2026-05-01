$RootPath = "D:\fpt\ky 9\doAn\Tokki_BE\Tokki.UnitTest"
$wordPattern = "(?:\w+ly|\w+ing|\w+tions?|\w+ments?|test|tests|string|array|check|checks|valiant|marvellous|polite|gentle|bold|magic|careful|ingenious|cheerful|delicate|empty|zero|search|term|pagination|sorting|filtered|null|success|properties|fail|failed|valid)"

$totalFiles = 0

$files = Get-ChildItem -Path $RootPath -Recurse -Include "*.cs" | Where-Object { $_.FullName -notmatch "obj|bin" }

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content
    
    # We replace any long chain (4+) of these words that ends right before a quote.
    # Note: \s+ is included in the match, so the preceding text will be kept.
    $regex = '(?:\s+' + $wordPattern + '){4,}(?=")'
    
    $content = [regex]::Replace($content, $regex, "", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    
    # Clean trailing spaces before quotes: `"Valid "` -> `"Valid"`
    $content = [regex]::Replace($content, '(\w)\s+"', '$1"')

    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $totalFiles++
        Write-Host "Fixed: $($file.Name)"
    }
}

Write-Host "`nDone! Fixed $totalFiles files."
