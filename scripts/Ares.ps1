param(
 [Parameter(Mandatory)][string]$SettingsFile,
 [Parameter(Mandatory)][ValidateSet('projects','project','create','document','align','lens','list','status','agree','begin','submit','verify','feedback','reopen','accept','recover')][string]$Operation,
 [string]$RequestFile,
 [switch]$Build,
 [switch]$BuildLens
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$config=Get-Content -LiteralPath $SettingsFile -Raw | ConvertFrom-Json
$settings=$config.Workbench
function OutputPath([string]$value) {
 if(-not [IO.Path]::IsPathFullyQualified($value)){throw 'Absolute output path required'}
 $full=[IO.Path]::GetFullPath($value)
 if(-not $full.StartsWith('D:\AgentWorkspace\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must remain under D:/AgentWorkspace'}
 $a=$full
 while($a){if((Test-Path -LiteralPath $a)-and((Get-Item -LiteralPath $a -Force).Attributes-band[IO.FileAttributes]::ReparsePoint)){throw 'Reparse output denied'};$a=[IO.Path]::GetDirectoryName($a)}
 [IO.Directory]::CreateDirectory($full)|Out-Null
 return $full
}
$scratch=OutputPath $settings.ScratchRoot
foreach($name in @('TEMP','TMP','TMPDIR','APPDATA','LOCALAPPDATA','DOTNET_CLI_HOME','NUGET_HTTP_CACHE_PATH','NUGET_PLUGINS_CACHE_PATH')) {
 [Environment]::SetEnvironmentVariable($name,(OutputPath (Join-Path $scratch ('cli-'+$name.ToLowerInvariant()))),'Process')
}
$env:DOTNET_ROOT=Split-Path -Parent $settings.DotnetExecutable
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='false'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_NOLOGO='1'
$env:NUGET_PACKAGES=if($settings.NugetPackages){OutputPath $settings.NugetPackages}else{OutputPath (Join-Path $scratch 'nuget')}
$artifacts=OutputPath (Join-Path $scratch 'cli-build')
$dll=Join-Path $artifacts 'bin/Ares.Workbench.Cli/debug/Ares.Workbench.Cli.dll'
if($Build) {
 & $settings.DotnetExecutable build (Join-Path $repo 'src/Ares.Workbench.Cli/Ares.Workbench.Cli.csproj') "-p:AresArtifactsRoot=$artifacts" --verbosity minimal
 if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}
}
if($BuildLens) {
 $lensRoot=[IO.Path]::GetFullPath($settings.ChangeLensRoot)
 $lensArtifacts=OutputPath (Join-Path $scratch 'lens-build')
 & $settings.DotnetExecutable build (Join-Path $lensRoot 'worker/ChangeLens.Analyzer/ChangeLens.Analyzer.csproj') "-p:AresArtifactsRoot=$lensArtifacts" --configuration Release --verbosity minimal
 if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}
 $builtWorker=Join-Path $lensArtifacts 'bin/ChangeLens.Analyzer/release/ChangeLens.Analyzer.dll'
 if([IO.Path]::GetFullPath($settings.ChangeLensWorker)-ne[IO.Path]::GetFullPath($builtWorker)){throw "Set ChangeLensWorker to $builtWorker in local settings"}
}
if(-not(Test-Path -LiteralPath $dll)){throw 'CLI is not built. Repeat with -Build once.'}
$arguments=@($dll,'direct',(Resolve-Path -LiteralPath $SettingsFile).Path,$Operation)
if($RequestFile){$arguments+=(Resolve-Path -LiteralPath $RequestFile).Path}
& $settings.DotnetExecutable @arguments
exit $LASTEXITCODE
