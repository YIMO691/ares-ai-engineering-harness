$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$markdownFiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Filter '*.md' |
    Where-Object { $_.FullName -notmatch '[\\/]\.git[\\/]' }

$issues = [System.Collections.Generic.List[string]]::new()

foreach ($file in $markdownFiles) {
    $content = Get-Content -Raw -LiteralPath $file.FullName

    if ([string]::IsNullOrWhiteSpace($content)) {
        $issues.Add("Empty Markdown file: $($file.FullName)")
        continue
    }

    if ($content -notmatch '(?m)^# ') {
        $issues.Add("Missing H1 heading: $($file.FullName)")
    }

    $fenceCount = ([regex]::Matches($content, '(?m)^```')).Count
    if ($fenceCount % 2 -ne 0) {
        $issues.Add("Unmatched code fence: $($file.FullName)")
    }

    foreach ($match in [regex]::Matches($content, '\[[^\]]*\]\(([^)]+)\)')) {
        $link = $match.Groups[1].Value.Trim()
        if ($link -match '^(https?://|mailto:|#)') {
            continue
        }

        $linkPath = (($link -split '#', 2)[0]).Trim('<', '>')
        if (-not $linkPath) {
            continue
        }

        $candidate = [System.IO.Path]::GetFullPath((Join-Path $file.DirectoryName $linkPath))
        if (-not (Test-Path -LiteralPath $candidate)) {
            $issues.Add("Broken relative link: $($file.FullName) -> $link")
        }
    }
}

$requiredFiles = @(
    'README.md',
    'SOP.md',
    'TASK-LEVELS.md',
    'START-HERE.md',
    'UPSTREAM-AGENTS.md',
    'AI-PLAYBOOK.md',
    'templates/DELIVERY-CHECKLIST.md',
    'templates/DELIVERY.md',
    'templates/ALIGNMENT-GATE.md',
    'templates/FORMAL-FEATURE/README.md',
    'examples/L3-complex-feature/DELIVERY.md',
    'prompts/NEW-TASK.md',
    'prompts/CONTINUE-TASK.md',
    'prompts/ALIGN-GATE.md',
    '.github/PULL_REQUEST_TEMPLATE.md'
)

foreach ($relativePath in $requiredFiles) {
    $candidate = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $candidate)) {
        $issues.Add("Missing required file: $relativePath")
    }
}

$agentsPath = Join-Path $repoRoot 'UPSTREAM-AGENTS.md'
if ((Test-Path -LiteralPath $agentsPath) -and (Get-Item -LiteralPath $agentsPath).Length -ge 32768) {
    $issues.Add('UPSTREAM-AGENTS.md exceeds the default 32 KiB project instruction limit.')
}

$sop = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'SOP.md')
if ($sop.IndexOf('## 8. Align') -lt 0 -or $sop.IndexOf('## 9. Done') -lt 0 -or
    $sop.IndexOf('## 8. Align') -gt $sop.IndexOf('## 9. Done')) {
    $issues.Add('SOP must place Align before Done.')
}

$alignPrompt = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'prompts/ALIGN-GATE.md')
if ($alignPrompt -notmatch '无法访问必要实现或测试证据') {
    $issues.Add('Align prompt must fail when executable evidence is unavailable.')
}

$formalTemplates = @(
    'templates/PRD.md',
    'templates/SDD.md',
    'templates/TEST-PLAN.md',
    'templates/DELIVERY.md'
)

foreach ($relativePath in $formalTemplates) {
    $content = Get-Content -Raw -LiteralPath (Join-Path $repoRoot $relativePath)
    if ($content -notmatch '\[必填\]') {
        $issues.Add("Formal template must mark required sections: $relativePath")
    }
}

$testPlan = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'templates/TEST-PLAN.md')
if ($testPlan -notmatch '实际执行结果统一写入 `DELIVERY\.md`') {
    $issues.Add('TEST-PLAN must route actual execution results to DELIVERY.md.')
}

$delivery = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'templates/DELIVERY.md')
if ($delivery -notmatch '# 8\. Align Gate \[必填\]' -or
    $delivery -notmatch '# 9\. 最终交付决定 \[必填\]') {
    $issues.Add('DELIVERY must contain the required Align Gate and final delivery decision.')
}

$taskLevels = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'TASK-LEVELS.md')
if ($taskLevels -notmatch '`DELIVERY\.md`：最终实现、测试结果、偏移、Align 和交付决定的唯一汇总') {
    $issues.Add('L3 minimum artifacts must include DELIVERY.md.')
}

if ($issues.Count -gt 0) {
    $issues | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output "Validated $($markdownFiles.Count) Markdown files."
Write-Output 'Required workflow files, relative links, instruction size, and Align invariants passed.'
