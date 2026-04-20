$RootPath = "D:\fpt\ky 9\doAn\Tokki_BE\Tokki.UnitTest"

$words = @(
    "smoothly","elegantly","gracefully","neatly","seamlessly","intelligently","dependably",
    "magically","fluently","beautifully","brilliantly","smartly","cleanly","organically",
    "natively","securely","cleverly","safely","rationally","majestically","effortlessly",
    "comfortably","competently","playfully","wisely","correctly","stably","solidly","reliably",
    "intuitively","confidently","dynamically","creatively","properly","expertly","deftly",
    "optimally","bravely","flawlessly","impressively","comprehensively","sensibly","logically",
    "peacefully","magnetically","thoughtfully","functionally","compactly","eloquently",
    "magnificently","effectively","powerfully","nicely","wonderfully","robustly","fluidly",
    "skillfully","instinctively","boldly","accurately","appropriately","successfully","easily",
    "excellently","automatically","naturally","flexibly","skilfully","valiantly","quietly",
    "marvellously","politely","gently","carefully","ingeniously","cheerfully","delicately",
    "test","tests","testing","validation","validations","string","array","check","checks","checking",
    "safely","mapping","empty","zero","search","term","pagination","sorting","appropriately","filtered","null","success","properties","fail","failed"
)

$wordRegex = "(?:" + ($words -join "|") + ")"
# Pattern: space followed by the word, repeated 3 or more times. We use a group that catches at least 3 of these words.
$garbagePattern = "(\s+" + $wordRegex + "){3,}\s*"

$files = Get-ChildItem -Path $RootPath -Recurse -Include "*.cs"

$totalReplacements = 0

foreach ($file in $files) {
    if ($file.FullName -match "obj|bin") { continue }
    
    $content = Get-Content $file.FullName -Raw
    $original = $content
    
    # We replace any long chain of these words that ends right before a quote.
    # For properties: Name = "Some description garbage garbage garbage"
    $content = [regex]::Replace($content,
        '(' + $wordRegex + '(?:\s+' + $wordRegex + '){3,})(?=")',
        "",
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

    if ($content -ne $original) {
        # Check if there are trailing spaces left before the quote
        $content = [regex]::Replace($content, '\s+"', '"')
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $totalReplacements++
        Write-Host "Fixed: $($file.Name)"
    }
}

Write-Host "Done! Fixed $totalReplacements files."
