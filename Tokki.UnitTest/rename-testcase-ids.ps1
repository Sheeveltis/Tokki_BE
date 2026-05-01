$root = "D:\fpt\ky 9\doAn\Tokki_BE\Tokki.UnitTest"
$files = Get-ChildItem -Path $root -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*" }

$totalFilesUpdated = 0
$totalIdsChanged = 0

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)

    # Find all TestCaseID = "TC-..." or TestCaseID = "..." assignments
    $tcPattern = 'TestCaseID\s*=\s*"([^"]+)"'
    $tcMatches = [regex]::Matches($content, $tcPattern)

    if ($tcMatches.Count -eq 0) { continue }

    # Build mapping: for each TestCaseID, find its FunctionGroup
    $groupCounters = @{}
    $replacements = [System.Collections.Specialized.OrderedDictionary]::new()

    foreach ($match in $tcMatches) {
        $oldId = $match.Groups[1].Value

        # Skip if already processed (duplicate ID in same file)
        if ($replacements.Contains($oldId)) { continue }

        $pos = $match.Index

        # Search nearby (before and after) for FunctionGroup
        $searchStart = [Math]::Max(0, $pos - 1000)
        $searchEnd = [Math]::Min($content.Length, $pos + 1000)
        $nearbyText = $content.Substring($searchStart, $searchEnd - $searchStart)

        $fgMatch = [regex]::Match($nearbyText, 'FunctionGroup\s*=\s*"([^"]+)"')

        if ($fgMatch.Success) {
            $fg = $fgMatch.Groups[1].Value
        } else {
            # Fallback: derive from filename
            $fg = [System.IO.Path]::GetFileNameWithoutExtension($file.Name) -replace 'Tests$', '' -replace 'HandlerTests$', ''
        }

        # Increment counter for this FunctionGroup
        if (-not $groupCounters.ContainsKey($fg)) {
            $groupCounters[$fg] = 0
        }
        $groupCounters[$fg]++
        $num = $groupCounters[$fg].ToString("D2")

        # New ID: FunctionGroup with spaces -> underscores, remove special chars
        $cleanFg = ($fg -replace '\s+', '_') -replace '[^a-zA-Z0-9_]', ''
        $newId = $cleanFg + "_" + $num

        $replacements[$oldId] = $newId
    }

    # Apply replacements (longer IDs first to avoid partial matches)
    $newContent = $content
    $sortedKeys = @($replacements.Keys) | Sort-Object -Property Length -Descending
    foreach ($oldId in $sortedKeys) {
        $newId = $replacements[$oldId]
        $newContent = $newContent.Replace($oldId, $newId)
    }

    if ($newContent -ne $content) {
        [System.IO.File]::WriteAllText($file.FullName, $newContent, (New-Object System.Text.UTF8Encoding $true))
        $totalFilesUpdated++
        $totalIdsChanged += $replacements.Count
        Write-Host "Updated: $($file.Name) ($($replacements.Count) IDs)"
    }
}

Write-Host "`nTotal: $totalFilesUpdated files updated, $totalIdsChanged IDs changed"
