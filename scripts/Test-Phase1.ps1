param(
    [Parameter(Mandatory=$true)][string]$Dotnet,
    [Parameter(Mandatory=$true)][string]$DemoRoot,
    [Parameter(Mandatory=$true)][string]$ScratchRoot,
    [Parameter(Mandatory=$true)][string]$EvidenceRoot
)
$ErrorActionPreference = 'Stop'
function New-TaskDirectory([string]$Value) {
    if (-not [IO.Path]::IsPathRooted($Value)) { throw 'Absolute task output path required.' }
    $resolved = [IO.Path]::GetFullPath($Value)
    if (-not $resolved.StartsWith('D:\AgentWorkspace\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Task outputs must be inside D:\AgentWorkspace.'
    }
    $ancestor = $resolved
    while ($ancestor) {
        if (Test-Path -LiteralPath $ancestor) {
            if ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Linked output path denied.' }
        }
        $ancestor = [IO.Path]::GetDirectoryName($ancestor)
    }
    [IO.Directory]::CreateDirectory($resolved) | Out-Null
    return $resolved
}
$taskScratch = New-TaskDirectory $ScratchRoot
$taskEvidence = New-TaskDirectory (Join-Path $EvidenceRoot ([Guid]::NewGuid().ToString('N')))
$env:ARES_DOTNET = (Resolve-Path -LiteralPath $Dotnet).Path
$env:DOTNET_ROOT = Split-Path -Parent $env:ARES_DOTNET
$env:ARES_GIT = (@(Get-Command git -CommandType Application)[0]).Source
$env:ARES_DEMO_SOURCE = (Resolve-Path -LiteralPath $DemoRoot).Path
$env:ARES_EVIDENCE = $taskEvidence
$locations = @{
    TEMP='temp'; TMP='temp'; DOTNET_CLI_HOME='dotnet-home'; NUGET_PACKAGES='nuget'
    NUGET_HTTP_CACHE_PATH='nuget-http'; NUGET_PLUGINS_CACHE_PATH='nuget-plugins'
    APPDATA='appdata'; LOCALAPPDATA='localappdata'
}
foreach ($name in $locations.Keys) {
    [Environment]::SetEnvironmentVariable($name, (New-TaskDirectory (Join-Path $taskScratch $locations[$name])), 'Process')
}
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$artifacts = New-TaskDirectory (Join-Path $taskScratch 'build')
$env:ARES_TEST_HELPER = Join-Path $artifacts 'bin\Ares.Workbench.FakeProcess\debug\Ares.Workbench.FakeProcess.dll'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $repoRoot
try {
    & $env:ARES_DOTNET build Ares.Workbench.sln "-p:AresArtifactsRoot=$artifacts" --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'Solution build failed.' }
    & $env:ARES_DOTNET test Ares.Workbench.sln --no-build --no-restore "-p:AresArtifactsRoot=$artifacts" --verbosity minimal --logger trx --results-directory $taskEvidence
    if ($LASTEXITCODE -ne 0) { throw 'Solution tests failed.' }
    Write-Output "Evidence: $taskEvidence"
} finally {
    Pop-Location
}
