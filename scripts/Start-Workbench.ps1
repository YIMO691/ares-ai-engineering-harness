param([int]$Port=5271,[switch]$NoBuild,[string]$SettingsFile=(Join-Path $PSScriptRoot 'workbench.local.json'))
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$config=Get-Content -LiteralPath $SettingsFile -Raw | ConvertFrom-Json
$settings=$config.Workbench
function OutputPath([string]$Value) {
 if(-not [IO.Path]::IsPathFullyQualified($Value)){throw 'Absolute output path required'}
 $full=[IO.Path]::GetFullPath($Value)
 if(-not $full.StartsWith('D:\AgentWorkspace\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must remain under D:/AgentWorkspace'}
 $a=$full
 while($a){if((Test-Path -LiteralPath $a)-and((Get-Item -LiteralPath $a -Force).Attributes-band[IO.FileAttributes]::ReparsePoint)){throw 'Linked output path denied'};$a=[IO.Path]::GetDirectoryName($a)}
 return $full
}
$scratch=OutputPath $settings.ScratchRoot
if($Port-lt 1024-or$Port-gt 65535){throw 'Invalid localhost port'}
foreach($name in @('TEMP','TMP','TMPDIR','APPDATA','LOCALAPPDATA','DOTNET_CLI_HOME','NUGET_HTTP_CACHE_PATH','NUGET_PLUGINS_CACHE_PATH')){
 $path=OutputPath (Join-Path $scratch ('host-'+$name.ToLowerInvariant()))
 [IO.Directory]::CreateDirectory($path)|Out-Null
 [Environment]::SetEnvironmentVariable($name,$path,'Process')
}
$dotnet=$settings.DotnetExecutable
$env:DOTNET_ROOT=Split-Path -Parent $dotnet
$env:DOTNET_NOLOGO='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='false'
$env:NUGET_PACKAGES=if($settings.NugetPackages){OutputPath $settings.NugetPackages}else{OutputPath (Join-Path $scratch 'nuget')}
$env:ARES_SETTINGS_FILE=(Resolve-Path -LiteralPath $SettingsFile).Path
$env:ASPNETCORE_ENVIRONMENT='Production'
Write-Host ("Ares Workbench: http://127.0.0.1:"+$Port)
Write-Host 'Keep this window open. Stop with Ctrl+C.'
$argsList=@('run','--project',(Join-Path $repo 'src\Ares.Workbench.Web\Ares.Workbench.Web.csproj'),('-p:AresArtifactsRoot='+ (Join-Path $scratch 'host-build')),'--no-launch-profile')
if($NoBuild){$argsList+='--no-build'}
$argsList+=@('--','--urls',("http://127.0.0.1:"+$Port))
& $dotnet @argsList
exit $LASTEXITCODE
