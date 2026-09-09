param([Parameter(Mandatory)][string]$User,[Parameter(Mandatory)][string]$ScratchRoot,[Parameter(Mandatory)][string]$EvidenceRoot,[Parameter(Mandatory)][string]$CodexHome,[Parameter(Mandatory)][string]$CodexExecutable)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($ScratchRoot)
$evidence=[IO.Path]::GetFullPath($EvidenceRoot)
$codexHome=[IO.Path]::GetFullPath($CodexHome)
foreach($target in @($root,$evidence,$codexHome)){
 if(-not $target.StartsWith('D:\AgentWorkspace\Ares\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid output'}
 $a=$target
 while($a){if((Test-Path -LiteralPath $a)-and((Get-Item -LiteralPath $a -Force).Attributes-band[IO.FileAttributes]::ReparsePoint)){throw 'Linked path denied'};$a=[IO.Path]::GetDirectoryName($a)}
}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
if(-not([Security.Principal.WindowsPrincipal]::new($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Sandbox provisioning needs administrator; model host must remain non-administrator.'}
$account=[Security.Principal.NTAccount]::new($User)
$null=$account.Translate([Security.Principal.SecurityIdentifier])
foreach($name in @('TEMP','TMP','TMPDIR','APPDATA','LOCALAPPDATA')){
 $path=[IO.Path]::GetFullPath((Join-Path $root ('setup-'+$name.ToLowerInvariant())))
 [IO.Directory]::CreateDirectory($path)|Out-Null
 [Environment]::SetEnvironmentVariable($name,$path,'Process')
}
[IO.Directory]::CreateDirectory($codexHome)|Out-Null
$env:CODEX_HOME=$codexHome
& $CodexExecutable sandbox setup --elevated --user $User --codex-home $codexHome 2>&1 | Out-File -LiteralPath (Join-Path $evidence 'codex-sandbox-setup.txt') -Encoding utf8
if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}
$bin=[IO.Path]::GetFullPath((Join-Path $codexHome '.sandbox-bin'))
$before=Get-Acl -LiteralPath $bin
$beforeDacl=$before.GetSecurityDescriptorSddlForm([Security.AccessControl.AccessControlSections]::Access)
& "$env:SystemRoot\System32\icacls.exe" $bin /setowner $User | Out-Null
if($LASTEXITCODE-ne 0){throw 'Owner repair failed'}
$after=Get-Acl -LiteralPath $bin
if($beforeDacl-ne $after.GetSecurityDescriptorSddlForm([Security.AccessControl.AccessControlSections]::Access)){throw 'Unexpected DACL change'}
@{target=$bin;before_owner=$before.Owner;after_owner=$after.Owner;dacl_unchanged=$true;recursive=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $evidence 'sandbox-owner.json') -Encoding utf8
exit 0
