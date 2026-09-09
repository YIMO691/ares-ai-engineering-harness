param([Parameter(Mandatory)][string]$ScratchRoot,[Parameter(Mandatory)][string]$EvidenceRoot,[string]$Dotnet=(Get-Command dotnet -CommandType Application).Source,[string]$Git=(Get-Command git -CommandType Application).Source)
$ErrorActionPreference='Stop'
function OutputPath([string]$value){
 if(-not [IO.Path]::IsPathFullyQualified($value)){throw 'Absolute output required'}
 $full=[IO.Path]::GetFullPath($value)
 if(-not $full.StartsWith('D:\AgentWorkspace\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must be under D:/AgentWorkspace'}
 $a=$full
 while($a){if((Test-Path -LiteralPath $a)-and((Get-Item -LiteralPath $a -Force).Attributes-band[IO.FileAttributes]::ReparsePoint)){throw 'Linked output'};$a=[IO.Path]::GetDirectoryName($a)}
 [IO.Directory]::CreateDirectory($full)|Out-Null
 return $full
}
$scratch=OutputPath $ScratchRoot
$evidence=OutputPath $EvidenceRoot
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
foreach($name in @('TEMP','TMP','TMPDIR','APPDATA','LOCALAPPDATA','DOTNET_CLI_HOME','NUGET_HTTP_CACHE_PATH','NUGET_PLUGINS_CACHE_PATH')){
 [Environment]::SetEnvironmentVariable($name,(OutputPath (Join-Path $scratch $name.ToLowerInvariant())),'Process')
}
$env:DOTNET_ROOT=Split-Path -Parent $Dotnet
$env:DOTNET_NOLOGO='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='false'
if(-not $env:NUGET_PACKAGES){$env:NUGET_PACKAGES=OutputPath (Join-Path $scratch 'nuget')}
$env:ARES_DOTNET=$Dotnet
$env:ARES_GIT=$Git
$env:ARES_EVIDENCE=$evidence
$build=OutputPath (Join-Path $scratch 'build')
$env:ARES_TEST_HELPER=Join-Path $build 'bin\Ares.Workbench.FakeProcess\debug\Ares.Workbench.FakeProcess.dll'
$demo=OutputPath (Join-Path $scratch ('demo-'+[Guid]::NewGuid().ToString('N')))
foreach($file in Get-ChildItem -LiteralPath (Join-Path $repo 'examples\LabelFormatter') -Recurse -File){
 $relative=[IO.Path]::GetRelativePath((Join-Path $repo 'examples\LabelFormatter'),$file.FullName)
 $dest=[IO.Path]::GetFullPath((Join-Path $demo $relative))
 if(-not $dest.StartsWith($demo+[IO.Path]::DirectorySeparatorChar)){throw 'Escaped fixture'}
 $null=OutputPath (Split-Path -Parent $dest)
 [IO.File]::Copy($file.FullName,$dest,$false)
}
& $Git -C $demo init -b main
if($LASTEXITCODE-ne 0){throw 'Fixture init failed'}
& $Git -C $demo add .
if($LASTEXITCODE-ne 0){throw 'Fixture stage failed'}
& $Git -C $demo -c user.name='Ares Test' -c user.email='test@example.invalid' commit -m 'test fixture baseline'
if($LASTEXITCODE-ne 0){throw 'Fixture commit failed'}
$env:ARES_DEMO_SOURCE=$demo
& $Dotnet build (Join-Path $repo 'Ares.Workbench.sln') "-p:AresArtifactsRoot=$build" --verbosity minimal
if($LASTEXITCODE-ne 0){throw 'Build failed'}
& $Dotnet test (Join-Path $repo 'Ares.Workbench.sln') --no-build --no-restore "-p:AresArtifactsRoot=$build" --logger trx --results-directory $evidence
if($LASTEXITCODE-ne 0){throw 'Tests failed'}
