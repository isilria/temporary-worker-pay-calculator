$ErrorActionPreference='Stop'
$root=$PSScriptRoot
& (Join-Path $root 'src/build.ps1')
$exe=Join-Path $root 'src/build/ShortTermPayroll_v1.0.exe'
$version=[Version](Get-Item -LiteralPath $exe).VersionInfo.FileVersion
$display=$version.ToString(2)
$tag='v'+$version.ToString(3)
$validation=Join-Path $root ('validation_'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $validation | Out-Null
foreach($flag in @('--test','--xlsx-test','--update-test')){
 $p=Start-Process -FilePath $exe -ArgumentList @($flag,('"'+$validation+'"')) -WindowStyle Hidden -PassThru -Wait
 if($p.ExitCode -ne 0){throw "검사 실패: $flag. $validation 확인"}
}
$artifacts=Join-Path $root 'artifacts'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
$stage=Join-Path $artifacts ('stage_'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
$exeName='ShortTermPayroll-'+$display+'.exe'
Copy-Item -LiteralPath $exe -Destination (Join-Path $artifacts $exeName)
Copy-Item -LiteralPath $exe -Destination (Join-Path $stage ('대체근로자_임금계산기_'+$display+'.exe'))
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination (Join-Path $stage '사용안내.txt')
$hash=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
$results=@('정식 '+$display+' / '+(Get-Date -Format 'yyyy-MM-dd'), 'Windows .NET Framework / AnyCPU', '기존 1.1 기능 유지 및 업데이트 배포 연결')
foreach($file in Get-ChildItem -LiteralPath $validation -Recurse -Filter '*test*.txt'){$results+=Get-Content -LiteralPath $file.FullName}
$results+='EXE SHA256 '+$hash
$results | Set-Content -LiteralPath (Join-Path $artifacts '검증결과.txt') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $artifacts '검증결과.txt') -Destination $stage
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath (Join-Path $artifacts ($display+'_실행파일.zip')) -Force
$sourceStage=Join-Path $artifacts ('source_'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $sourceStage 'src') -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $root 'src') | Where-Object {$_.Name -ne 'build'} | Copy-Item -Destination (Join-Path $sourceStage 'src') -Recurse
foreach($file in @('README.md','RELEASE.md','package.ps1','.gitignore')){Copy-Item -LiteralPath (Join-Path $root $file) -Destination $sourceStage}
Compress-Archive -Path (Join-Path $sourceStage '*') -DestinationPath (Join-Path $artifacts ($display+'_소스백업.zip')) -Force
$manifest=[ordered]@{appId='shortpay';version=$version.ToString(4);url="https://github.com/isilria/temporary-worker-pay-calculator/releases/tag/$tag";notes="정식 $display. 당직 편성·수당·엑셀 출력 개선 및 업데이트 확인·검증 다운로드 지원.";downloadUrl="https://github.com/isilria/temporary-worker-pay-calculator/releases/download/$tag/$exeName";sha256=$hash}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $artifacts 'latest.json') -Encoding UTF8
Get-ChildItem -LiteralPath $artifacts -File | Where-Object {$_.Extension -in @('.exe','.zip')} | ForEach-Object {((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())+'  '+$_.Name} | Set-Content -LiteralPath (Join-Path $artifacts 'SHA256SUMS.txt') -Encoding UTF8
Write-Output "배포 준비 완료: $artifacts"
