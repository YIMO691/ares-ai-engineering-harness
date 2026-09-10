param([string]$Root = (Join-Path $PSScriptRoot '..'))
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath $Root).Path
$issues = [System.Collections.Generic.List[string]]::new()
$files = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Filter '*.md' |
    Where-Object { $_.FullName -notmatch '[\\/]\.git[\\/]' })
$linkCount = 0

foreach ($file in $files) {
    $content = Get-Content -Raw -LiteralPath $file.FullName
    if ([string]::IsNullOrWhiteSpace($content)) {
        $issues.Add("Empty Markdown: $($file.FullName)")
        continue
    }
    # Check relative file targets in prose; code examples are not filesystem declarations.
    $fence = $null
    $headings = 0
    foreach ($line in ($content -split "`n")) {
        if ($line -match '^\s*(`{3,}|~{3,})(.*)$') {
            $marker = $Matches[1]
            if ($null -eq $fence) { $fence = $marker }
            elseif ($marker[0] -eq $fence[0] -and $marker.Length -ge $fence.Length -and [string]::IsNullOrWhiteSpace($Matches[2])) { $fence = $null }
            continue
        }
        if ($null -ne $fence) { continue }
        if ($line -match '^# ') { $headings++ }
        foreach ($match in [regex]::Matches($line, '\[[^\]]*\]\(([^)]+)\)')) {
            $link = $match.Groups[1].Value.Trim()
            if ($link -match '^(https?://|mailto:|#)') { continue }
            $target = (($link -split '#', 2)[0]).Trim('<', '>')
            if (-not $target) { continue }
            $linkCount++
            $candidate = [IO.Path]::GetFullPath((Join-Path $file.DirectoryName $target))
            if (-not (Test-Path -LiteralPath $candidate)) { $issues.Add("Broken relative link: $($file.FullName) -> $link") }
        }
    }
    if ($headings -ne 1) { $issues.Add("Expected one H1: $($file.FullName)") }
    if ($null -ne $fence) { $issues.Add("Unclosed code fence: $($file.FullName)") }
}

foreach ($entry in @('README.md','UPSTREAM-AGENTS.md','CONTRIBUTING.md','templates/SPEC.md','docs/WORKFLOW.md',
    'docs/ENGINEERING_RULES.md','docs/AI_COLLABORATION.md','docs/SOURCES.md','templates/README.md','templates/PROJECT-RULES.md',
    'templates/CLIENT.md','templates/SERVER.md','templates/VERIFICATION.md',
    'ARES_PROFILE.md','AI-PLAYBOOK.md')) {
    if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $entry))) { $issues.Add("Missing current entry: $entry") }
}
if ($issues.Count -gt 0) {
    $issues | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}
Write-Output "Validated $($files.Count) Markdown files and $linkCount relative file links."
Write-Output 'Documentation checks only; no business acceptance or release verdict is inferred.'
