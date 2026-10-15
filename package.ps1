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
$parallelChecks=@()
$parallelChecks+=Start-Process -FilePath $exe -ArgumentList @('--basis-test',('"'+(Join-Path $validation 'basis')+'"')) -WindowStyle Hidden -PassThru
$parallelChecks+=Start-Process -FilePath (Join-Path $root 'src/build/FlatUiTests.exe') -ArgumentList ('"'+(Join-Path $validation 'layout')+'"') -WindowStyle Hidden -PassThru
$parallelChecks+=Start-Process -FilePath (Join-Path $root 'src/build/ReopenTests.exe') -ArgumentList @(('"'+(Join-Path $validation 'reopen')+'"'),('"'+(Join-Path $root 'src/test-fixtures/legacy-wage.xlsx')+'"')) -WindowStyle Hidden -PassThru
Invoke-Check $exe @('--logic-test',(Join-Path $validation 'logic'))
Invoke-Check $exe @('--xlsx-test',(Join-Path $validation 'excel'))
Invoke-Check $exe @('--update-test',(Join-Path $validation 'update'))
foreach($checkProcess in $parallelChecks){$checkProcess.WaitForExit();$checkProcess.Refresh();if($checkProcess.ExitCode -ne 0){throw ('화면·복원 검사 실패: '+$validation)}}
$logs=@('basis/work-plan-tests.txt','logic/logic-audit-tests.txt','logic/schedule/schedule-tests.txt','excel/excel-tests.txt','layout/flat-ui-tests.txt','reopen/reopen-tests.txt','reopen/session-tests.txt','update/update-tests.txt')
$counts=@(122,40,145,164,60,419,25,20)
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
Copy-Item -LiteralPath $exe -Destination (Join-Path $stage ('대체근로자_임금계산기_'+$version.ToString(3)+'.exe'))
$hash=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
$engineHash=(Get-FileHash -LiteralPath (Join-Path $root 'src/Engine.cs') -Algorithm SHA256).Hash.ToLowerInvariant()
$verification=@(
    ('대체근로자 임금계산기 정식 '+$version.ToString(3)+' / '+(Get-Date -Format 'yyyy-MM-dd')),
    ('파일 버전 '+$version.ToString(4)+' / Windows .NET Framework / AnyCPU'),
    '배포 소스로 995개 검사 통과: 교육자료·금액·설정창122, 계산·저장·금액·출력40, 근무편성·수당·저장145, 엑셀·신청서164, 화면60, 복원419, 종료25, 업데이트20. 교육자료122·계산40·편성145·엑셀164·업데이트20은 실제 배포EXE로 실행했습니다.',
    '달력 왼쪽 세 탭을 계약근무일(연한 유칼립투스) / 근무상황부(설정 테마) / 통상근로자 근무일(회색)로 배치했습니다. 날짜 칸·범례도 같은 역할 색상이며6개 테마에서 실제 렌더링을 검사했습니다.',
    '가상 자료와 격리된 설정을 사용했습니다. 화면은 작업 영역 밖에서 활성화하지 않고 검사했습니다.',
    '신청서 문구는 직종명 → 원근로자 → 원근로자명 순서이며 같은 연도의 종료일은 월·일만 표시합니다.',
    '새 산정표의 입력 보존, 일반·당직 복원, 변경 재저장, 원본 보존, 손상·수식·계산 결과 변조 거부를 확인했습니다.',
    '교육자료 예시: 약정40시간÷같은 기간 통상8일=5시간. 첫 주 실제 지급도5시간입니다. 기존 날짜별 예시:96시간÷15일=6.4시간씩3회, 합계19.2시간, 유급 공휴일3회도 각6.4시간, 실지급액1,468,600원. 고정 주24시간 약정의 비례계산은 별개입니다.',
    '계약·주휴 설정창의 근거 메모·하단 확인 체크와 확인 서명에 따른 출력 제한을 제거했습니다. 실제 입력 오류 차단은 유지합니다.',
    'PDF 생성·외관 및 실제 프린터 인쇄는 이번 배포에서 재검증하지 않았습니다.',
    ('교육자료 반영 Engine.cs SHA256 '+$engineHash),
    '',
    '실제 검사 기록:'
)+$testRecords+@('',('EXE SHA256 '+$hash))
$verification | Set-Content -LiteralPath (Join-Path $artifacts 'validation-1.4.txt') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $artifacts 'validation-1.4.txt') -Destination (Join-Path $root 'src/검증결과.txt')
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination (Join-Path $artifacts '사용안내.md')
Copy-Item -LiteralPath (Join-Path $root 'CHANGELOG.md') -Destination (Join-Path $artifacts '변경내용.md')
Copy-Item -LiteralPath (Join-Path $root 'src/달력탭_변경안내.md') -Destination $artifacts
Copy-Item -LiteralPath (Join-Path $validation 'basis/달력탭_1.png') -Destination (Join-Path $artifacts '화면_계약근무일.png')
Copy-Item -LiteralPath (Join-Path $validation 'basis/달력탭_테마연동.png') -Destination (Join-Path $artifacts '화면_근무상황부_테마.png')
Copy-Item -LiteralPath (Join-Path $root 'src/주휴수당_재검토_결과.md') -Destination $artifacts
Copy-Item -LiteralPath (Join-Path $validation 'basis/education-임금산정표.xlsx') -Destination (Join-Path $artifacts '교육자료예시_주휴5시간.xlsx')
Copy-Item -LiteralPath (Join-Path $validation 'logic/임금산정표.xlsx') -Destination (Join-Path $artifacts '복원예시_주휴19.2시간.xlsx')
Copy-Item -LiteralPath (Join-Path $validation 'excel/신청서_표기수정_예시.xlsx') -Destination (Join-Path $artifacts '신청서_표기수정_예시.xlsx')
$manifest=[ordered]@{appId='shortpay';version=$version.ToString(4);url="https://github.com/isilria/temporary-worker-pay-calculator/releases/tag/$tag";notes='달력 세 탭 적용: 계약근무일 유칼립투스색, 근무상황부 설정 테마 연동, 통상근로자 근무일 회색. 주휴 계산·저장 복원은 유지합니다.';downloadUrl="https://github.com/isilria/temporary-worker-pay-calculator/releases/download/$tag/$exeName";sha256=$hash}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $artifacts 'latest.json') -Encoding UTF8
foreach($name in @('사용안내.md','변경내용.md','주휴수당_재검토_결과.md','교육자료예시_주휴5시간.xlsx','복원예시_주휴19.2시간.xlsx','신청서_표기수정_예시.xlsx')){Copy-Item -LiteralPath (Join-Path $artifacts $name) -Destination $stage}
Copy-Item -LiteralPath (Join-Path $artifacts 'validation-1.4.txt') -Destination (Join-Path $stage '검증결과.txt')
foreach($name in @('달력탭_변경안내.md','화면_계약근무일.png','화면_근무상황부_테마.png')){Copy-Item -LiteralPath (Join-Path $artifacts $name) -Destination $stage}
Get-ChildItem -LiteralPath (Join-Path $root 'src') | Where-Object {$_.Name -ne 'build'} | Copy-Item -Destination (Join-Path $sourceStage 'src') -Recurse
foreach($name in @('README.md','RELEASE.md','CHANGELOG.md','package.ps1','.gitignore')){Copy-Item -LiteralPath (Join-Path $root $name) -Destination $sourceStage}
Copy-Item -LiteralPath (Join-Path $artifacts 'latest.json') -Destination $sourceStage
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath (Join-Path $artifacts ('ShortTermPayroll-'+$display+'-windows.zip')) -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $sourceStage '*') -DestinationPath (Join-Path $artifacts ('ShortTermPayroll-'+$display+'-source.zip')) -CompressionLevel Optimal
Get-ChildItem -LiteralPath $artifacts -File | Where-Object {$_.Name -ne 'SHA256SUMS.txt'} | Sort-Object Name | ForEach-Object {((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())+'  '+$_.Name} | Set-Content -LiteralPath (Join-Path $artifacts 'SHA256SUMS.txt') -Encoding UTF8
Write-Output "995개 검사 통과. 배포 파일: $artifacts"
Write-Output "검사 원본: $validation"
