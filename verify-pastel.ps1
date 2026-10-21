param([string]$ValidationDirectory='')
$ErrorActionPreference='Stop'
if(-not $ValidationDirectory){$ValidationDirectory=Join-Path $PSScriptRoot 'qa-final'}
New-Item -ItemType Directory -Path $ValidationDirectory -Force | Out-Null
foreach($payrollBuild in @('build.ps1','build-ui.ps1','build-reopen.ps1')){& (Join-Path $PSScriptRoot ('src/'+$payrollBuild))}
$payrollExecutable=Join-Path $PSScriptRoot 'src/build/ShortTermPayroll.exe'
function Run-PayrollCheck([string]$Executable,[string[]]$Arguments){
 $quoted=$Arguments | ForEach-Object {'"'+$_+'"'}
 $process=Start-Process -FilePath $Executable -ArgumentList $quoted -WindowStyle Hidden -PassThru -Wait
 if($process.ExitCode -ne 0){throw ('Verification failed: '+($Arguments -join ' '))}
}
foreach($payrollCheck in @('basis','logic','xlsx','update','holiday-data')){
 $payrollMode='--'+$payrollCheck+'-test'; Run-PayrollCheck -Executable $payrollExecutable -Arguments @($payrollMode,(Join-Path $ValidationDirectory $payrollCheck))
}
Run-PayrollCheck (Join-Path $PSScriptRoot 'src/build/FlatUiTests.exe') @((Join-Path $ValidationDirectory 'layout'))
Run-PayrollCheck (Join-Path $PSScriptRoot 'src/build/ReopenTests.exe') @((Join-Path $ValidationDirectory 'reopen'),(Join-Path $PSScriptRoot 'src/test-fixtures/legacy-wage.xlsx'))
$payrollLogs=@('basis/work-plan-tests.txt','logic/logic-audit-tests.txt','logic/schedule/schedule-tests.txt','xlsx/excel-tests.txt','layout/flat-ui-tests.txt','reopen/reopen-tests.txt','reopen/session-tests.txt','update/update-tests.txt','holiday-data/holiday-data-tests.txt')
$payrollTotal=0
foreach($payrollLog in $payrollLogs){$payrollRecord=Get-Content -LiteralPath (Join-Path $ValidationDirectory $payrollLog) -Raw -Encoding UTF8;if($payrollRecord -notmatch '^PASS (\d+) '){throw ('Invalid test record: '+$payrollLog)};$payrollTotal+=[int]$Matches[1];$payrollRecord.Trim()}
Write-Output ('TOTAL '+$payrollTotal+' passed')
