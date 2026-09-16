param(
 [Parameter(Mandatory)][string]$ScratchRoot,
 [Parameter(Mandatory)][string]$EvidenceRoot,
 [string]$Dotnet=(@(Get-Command dotnet -CommandType Application)[0]).Source,
 [string]$Git=(@(Get-Command git -CommandType Application)[0]).Source,
 [string]$Python=(@(Get-Command python -CommandType Application)[0]).Source,
 [switch]$InstallTestDependencies
)
$ErrorActionPreference='Stop'
# Test-Workbench validates absolute paths and rejects linked output ancestors before writing.
& (Join-Path $PSScriptRoot 'Test-Workbench.ps1') -ScratchRoot $ScratchRoot -EvidenceRoot $EvidenceRoot -Dotnet $Dotnet -Git $Git
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
& (Join-Path $repo 'workflow/scripts/validate-workflow.ps1')
$env:PYTHONDONTWRITEBYTECODE='1'
$env:PYTHONIOENCODING='utf-8'
& $Python -B -m unittest discover -s (Join-Path $repo 'tools/ange-eval/tests') -v
if($LASTEXITCODE-ne 0){throw 'ANGE evaluation checks failed'}
$lensRoot=Join-Path $repo 'tools/change-lens'
$env:PYTHONDONTWRITEBYTECODE='1'
$env:PYTHONIOENCODING='utf-8'
$env:DOTNET_ROLL_FORWARD='Major'
$env:PATH=(Split-Path -Parent $Dotnet)+[IO.Path]::PathSeparator+(Split-Path -Parent $Git)+[IO.Path]::PathSeparator+$env:PATH
$env:GIT_CONFIG_GLOBAL=Join-Path $ScratchRoot 'gitconfig'
$env:XDG_CONFIG_HOME=Join-Path $ScratchRoot 'xdg'
$dependencies=Join-Path $ScratchRoot 'python-libs'
$env:PYTHONPATH=$dependencies
if($InstallTestDependencies) {
 & $Python -B -m pip install --target $dependencies --cache-dir (Join-Path $ScratchRoot 'pip-cache') 'jsonschema>=4.23,<5' 'PyYAML>=6,<7'
 if($LASTEXITCODE-ne 0){throw 'Python test dependencies failed'}
}
$lensBuild=Join-Path $ScratchRoot 'lens-build'
& $Dotnet build (Join-Path $lensRoot 'worker/ChangeLens.Analyzer/ChangeLens.Analyzer.csproj') "-p:AresArtifactsRoot=$lensBuild" -c Release --verbosity minimal
if($LASTEXITCODE-ne 0){throw 'Change Lens build failed'}
$env:CHANGE_LENS_WORKER=Join-Path $lensBuild 'bin/ChangeLens.Analyzer/release/ChangeLens.Analyzer.dll'
Push-Location $lensRoot
try {
 & $Python -B -m unittest discover -s tests -v
 if($LASTEXITCODE-ne 0){throw 'Change Lens checks failed'}
} finally {Pop-Location}
