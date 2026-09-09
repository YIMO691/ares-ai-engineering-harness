param([Parameter(Mandatory)][string]$Source,[Parameter(Mandatory)][string]$Destination,[string]$Git='git')
$ErrorActionPreference='Stop'
$sourceRoot=[IO.Path]::GetFullPath($Source)
$target=[IO.Path]::GetFullPath($Destination)
if(-not $target.StartsWith('D:\AgentWorkspace\',[StringComparison]::OrdinalIgnoreCase)){throw 'Demo destination must be inside D:/AgentWorkspace'}
if(Test-Path -LiteralPath $target){throw 'Destination exists'}
foreach($path in @($sourceRoot,$target)){
 $a=$path
 while($a){if((Test-Path -LiteralPath $a)-and((Get-Item -LiteralPath $a -Force).Attributes-band[IO.FileAttributes]::ReparsePoint)){throw 'Linked path denied'};$a=[IO.Path]::GetDirectoryName($a)}
}
& $Git clone --no-hardlinks -- $sourceRoot $target
if($LASTEXITCODE-ne 0){throw 'Demo clone failed'}
Write-Host ('Created isolated demo: '+$target)
