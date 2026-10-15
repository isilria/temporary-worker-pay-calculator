param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$validation=Join-Path $root ('validation_'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $validation | Out-Null
function Invoke-Check([string]$Executable,[string[]]$CheckArguments) {
    $quotedArguments=$CheckArguments | ForEach-Object {'"'+$_+'"'}
    $checkProcess=Start-Process -FilePath $Executable -ArgumentList $quotedArguments -WindowStyle Hidden -PassThru -Wait
    if($checkProcess.ExitCode -ne 0){throw "검사 실패: $Executable. $validation 확인"}
}
& (Join-Path $root 'src/build.ps1')
& (Join-Path $root 'src/build-logic.ps1')
& (Join-Path $root 'src/build-ui.ps1')
& (Join-Path $root 'src/build-reopen.ps1')
$exe=Join-Path $root 'src/build/ShortTermPayroll.exe'
$logic=Join-Path $root 'src/build/LogicAudit.exe'
Invoke-Check $logic @((Join-Path $validation 'logic'))
Invoke-Check $logic @('--excel-only',(Join-Path $validation 'excel'))
Invoke-Check (Join-Path $root 'src/build/FlatUiTests.exe') @((Join-Path $validation 'layout'))
Invoke-Check (Join-Path $root 'src/build/ReopenTests.exe') @((Join-Path $validation 'reopen'),(Join-Path $root 'src/test-fixtures/legacy-wage.xlsx'))
Invoke-Check $exe @('--update-test',(Join-Path $validation 'update'))
$logs=@('logic/logic-audit-tests.txt','logic/schedule/schedule-tests.txt','excel/excel-tests.txt','layout/flat-ui-tests.txt','reopen/reopen-tests.txt','reopen/session-tests.txt','update/update-tests.txt')
$counts=@(40,145,164,60,371,25,16)
$testRecords=@()
for($index=0;$index -lt $logs.Length;$index++) {
    $record=Get-Content -LiteralPath (Join-Path $validation $logs[$index]) -Raw
    if(-not $record.StartsWith('PASS '+$counts[$index]+' ')){throw ('검사 결과 불일치: '+$logs[$index])}
    $testRecords+=$record.Trim()
}
$version=[Version](Get-Item -LiteralPath $exe).VersionInfo.FileVersion
$display=$version.ToString(2)
$tag='v'+$version.ToString(3)
if(-not $OutputDirectory){$OutputDirectory=Join-Path $root ('artifacts/'+$tag)}
$artifacts=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $artifacts){throw '배포 폴더가 이미 있습니다. 새 빈 경로를 지정하세요.'}
New-Item -ItemType Directory -Path $artifacts | Out-Null
$stage=Join-Path $validation 'windows-package'
$sourceStage=Join-Path $validation 'source-package'
New-Item -ItemType Directory -Path $stage,(Join-Path $sourceStage 'src') | Out-Null
$exeName='ShortTermPayroll-'+$display+'.exe'
Copy-Item -LiteralPath $exe -Destination (Join-Path $artifacts $exeName)
Copy-Item -LiteralPath $exe -Destination (Join-Path $stage ('대체근로자_임금계산기_'+$display+'.exe'))
$hash=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
$engineHash=(Get-FileHash -LiteralPath (Join-Path $root 'src/Engine.cs') -Algorithm SHA256).Hash.ToLowerInvariant()
$verification=@(
    ('대체근로자 임금계산기 정식 '+$display+' / '+(Get-Date -Format 'yyyy-MM-dd')),
    ('파일 버전 '+$version.ToString(4)+' / Windows .NET Framework / AnyCPU'),
    '배포 소스로 821개 검사 통과: 계산·저장·금액·출력 40, 근무편성·수당·저장 145, 엑셀·신청서 문구 164, 화면 배치 60, 엑셀 복원 371, 종료·재실행·기관 설정 25, 업데이트 16.',
    '가상 자료와 격리된 설정을 사용했습니다. 화면은 작업 영역 밖에서 활성화하지 않고 검사했습니다.',
    '신청서 문구는 직종명 → 원근로자 → 원근로자명 순서이며 같은 연도의 종료일은 월·일만 표시합니다.',
    '새 산정표의 입력 보존, 일반·당직 복원, 변경 재저장, 원본 보존, 손상·수식·계산 결과 변조 거부를 확인했습니다.',
    '주휴 예시: 실제 근무 12일·96시간, 유급휴일 3일·18시간, 주휴 4.8+6+6=16.8시간, 실지급액 1,429,260원.',
    'PDF 생성·외관 및 실제 프린터 인쇄는 이번 배포에서 재검증하지 않았습니다.',
    ('주휴 재검토판과 동일한 Engine.cs SHA256 '+$engineHash),
    '',
    '실제 검사 기록:'
)+$testRecords+@('',('EXE SHA256 '+$hash))
$verification | Set-Content -LiteralPath (Join-Path $artifacts 'validation-1.4.txt') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $artifacts 'validation-1.4.txt') -Destination (Join-Path $root 'src/검증결과.txt')
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination (Join-Path $artifacts '사용안내.md')
Copy-Item -LiteralPath (Join-Path $root 'CHANGELOG.md') -Destination (Join-Path $artifacts '변경내용.md')
Copy-Item -LiteralPath (Join-Path $root 'src/주휴수당_재검토_결과.md') -Destination $artifacts
Copy-Item -LiteralPath (Join-Path $validation 'logic/임금산정표.xlsx') -Destination (Join-Path $artifacts '복원예시_주휴16.8시간.xlsx')
Copy-Item -LiteralPath (Join-Path $validation 'excel/신청서_표기수정_예시.xlsx') -Destination (Join-Path $artifacts '신청서_표기수정_예시.xlsx')
$manifest=[ordered]@{appId='shortpay';version=$version.ToString(4);url="https://github.com/isilria/temporary-worker-pay-calculator/releases/tag/$tag";notes='주휴 계산 개선, 영역별 컬러 테마, 종료 시 계산 입력 초기화, 임금산정표 작업 복원 및 신청서 표기 개선.';downloadUrl="https://github.com/isilria/temporary-worker-pay-calculator/releases/download/$tag/$exeName";sha256=$hash}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $artifacts 'latest.json') -Encoding UTF8
foreach($name in @('사용안내.md','변경내용.md','주휴수당_재검토_결과.md','복원예시_주휴16.8시간.xlsx','신청서_표기수정_예시.xlsx')){Copy-Item -LiteralPath (Join-Path $artifacts $name) -Destination $stage}
Copy-Item -LiteralPath (Join-Path $artifacts 'validation-1.4.txt') -Destination (Join-Path $stage '검증결과.txt')
Get-ChildItem -LiteralPath (Join-Path $root 'src') | Where-Object {$_.Name -ne 'build'} | Copy-Item -Destination (Join-Path $sourceStage 'src') -Recurse
foreach($name in @('README.md','RELEASE.md','CHANGELOG.md','package.ps1','.gitignore')){Copy-Item -LiteralPath (Join-Path $root $name) -Destination $sourceStage}
Copy-Item -LiteralPath (Join-Path $artifacts 'latest.json') -Destination $sourceStage
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath (Join-Path $artifacts ('ShortTermPayroll-'+$display+'-windows.zip')) -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $sourceStage '*') -DestinationPath (Join-Path $artifacts ('ShortTermPayroll-'+$display+'-source.zip')) -CompressionLevel Optimal
Get-ChildItem -LiteralPath $artifacts -File | Where-Object {$_.Name -ne 'SHA256SUMS.txt'} | Sort-Object Name | ForEach-Object {((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())+'  '+$_.Name} | Set-Content -LiteralPath (Join-Path $artifacts 'SHA256SUMS.txt') -Encoding UTF8
Write-Output "821개 검사 통과. 배포 파일: $artifacts"
Write-Output "검사 원본: $validation"
