param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$version=[regex]::Match((Get-Content -LiteralPath (Join-Path $root 'src/AppUpdates.cs') -Raw -Encoding UTF8),'DisplayVersion = "([0-9.]+)"').Groups[1].Value
if(-not $version){throw 'Missing release version.'}
$qa=Join-Path $root ('qa-'+[Guid]::NewGuid().ToString('N'))
& (Join-Path $root 'src/verify.ps1') -ResultDirectory $qa
$exe=Join-Path $root ('src/Workday-'+$version+'.exe')
$artifacts=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $artifacts){throw 'Output directory already exists.'}
New-Item -ItemType Directory -Path $artifacts | Out-Null
$exeName='Workday-'+$version+'.exe'
Copy-Item -LiteralPath $exe -Destination (Join-Path $artifacts $exeName)
$hash=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
$notes=Get-Content -LiteralPath (Join-Path $root 'RELEASE-NOTES.md') -Raw -Encoding UTF8
$tag='workday-v'+$version
$manifest=[ordered]@{appId='workday320';version=$version+'.0';url="https://github.com/isilria/temporary-worker-pay-calculator/releases/tag/$tag";notes=$notes.Trim();downloadUrl="https://github.com/isilria/temporary-worker-pay-calculator/releases/download/$tag/$exeName";sha256=$hash}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $artifacts 'latest.json') -Encoding UTF8
$sourceStage=Join-Path $qa 'source'
New-Item -ItemType Directory -Path (Join-Path $sourceStage 'src') -Force | Out-Null
$sourceRoot=Join-Path $root 'src'
foreach($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object {$_.Extension -ne '.exe'}){
 $relative=$file.FullName.Substring($sourceRoot.Length+1)
 $destination=Join-Path (Join-Path $sourceStage 'src') $relative
 New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
 Copy-Item -LiteralPath $file.FullName -Destination $destination
}
foreach($name in @('README.md','RELEASE.md','RELEASE-NOTES.md','package.ps1')){Copy-Item -LiteralPath (Join-Path $root $name) -Destination $sourceStage}
Compress-Archive -Path (Join-Path $sourceStage '*') -DestinationPath (Join-Path $artifacts ('Workday-'+$version+'-source.zip')) -CompressionLevel Optimal
$records=@('Workday '+$version,'Windows .NET Framework / AnyCPU',('EXE SHA256 '+$hash),'')
foreach($report in Get-ChildItem -LiteralPath $qa -Filter '*tests.txt' -File -Recurse){$records+=Get-Content -LiteralPath $report.FullName -Encoding UTF8}
$records | Set-Content -LiteralPath (Join-Path $artifacts ('validation-'+$version+'.txt')) -Encoding UTF8
Get-ChildItem -LiteralPath $artifacts -File | Sort-Object Name | ForEach-Object {((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())+'  '+$_.Name} | Set-Content -LiteralPath (Join-Path $artifacts 'SHA256SUMS.txt') -Encoding UTF8
Write-Output ('Release artifacts: '+$artifacts)
