param(
    [string]$RootPath = "D:\fpt\ky 9\doAn\Tokki_BE\Tokki.UnitTest"
)

# This regex matches long sequences of adverbs (3+) that are hallucinated garbage text
$garbagePattern = '(?:(?:smoothly|elegantly|gracefully|neatly|seamlessly|intelligently|dependably|magically|fluently|beautifully|brilliantly|smartly|cleanly|organically|natively|securely|cleverly|safely|rationally|majestically|effortlessly|comfortably|competently|playfully|wisely|correctly|stably|solidly|reliably|intuitively|confidently|dynamically|creatively|properly|expertly|deftly|optimally|bravely|flawlessly|impressively|comprehensively|sensibly|logically|peacefully|magnetically|thoughtfully|functionally|compactly|eloquently|magnificently|effectively|powerfully|nicely|wonderfully|robustly|fluidly|skillfully|instinctively|boldly|accurately|appropriately|successfully|easily|excellently|automatically|naturally|flexibly)\s+){3,}(?:smoothly|elegantly|gracefully|neatly|seamlessly|intelligently|dependably|magically|fluently|beautifully|brilliantly|smartly|cleanly|organically|natively|securely|cleverly|safely|rationally|majestically|effortlessly|comfortably|competently|playfully|wisely|correctly|stably|solidly|reliably|intuitively|confidently|dynamically|creatively|properly|expertly|deftly|optimally|bravely|flawlessly|impressively|comprehensively|sensibly|logically|peacefully|magnetically|thoughtfully|functionally|compactly|eloquently|magnificently|effectively|powerfully|nicely|wonderfully|robustly|fluidly|skillfully|instinctively|boldly|accurately|appropriately|successfully|easily|excellently|automatically|naturally|flexibly)'

$files = Get-ChildItem -Path $RootPath -Recurse -Include "*.cs" | Where-Object {
    (Get-Content $_.FullName -Raw) -match $garbagePattern
}

Write-Host "Found $($files.Count) files with garbage text"
$totalReplacements = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    # Extract test case info from file name
    $handlerName = $file.BaseName -replace 'Tests$', '' -replace 'HandlerTests$', 'Handler'
    
    # Replace garbage in Description fields
    $content = [regex]::Replace($content, 
        '(Description\s*=\s*"[^"]{0,60}?)(' + $garbagePattern + ')(")',
        { param($m) $m.Groups[1].Value + $m.Groups[3].Value },
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    
    # Replace garbage in ExpectedResult fields
    $content = [regex]::Replace($content,
        '(ExpectedResult\s*=\s*"[^"]{0,60}?)(' + $garbagePattern + ')(")',
        { param($m) $m.Groups[1].Value + $m.Groups[3].Value },
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    
    # Replace garbage in AppliedConditions strings
    $content = [regex]::Replace($content,
        '("[^"]{0,60}?)(' + $garbagePattern + ')("\s*})',
        { param($m) $m.Groups[1].Value + $m.Groups[3].Value },
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $totalReplacements++
        Write-Host "  Fixed: $($file.Name)"
    }
}

Write-Host "`nDone! Fixed $totalReplacements files."
