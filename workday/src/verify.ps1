param([switch]$Live,[string]$BaselineExecutable="",[switch]$SkipBuild,[string]$ResultDirectory="")
$ErrorActionPreference='Stop'
if(-not $SkipBuild){& (Join-Path $PSScriptRoot 'build.ps1');if($LASTEXITCODE -ne 0){throw 'Build failed'}}
if($BaselineExecutable){$env:WORKDAY_BASELINE_EXE=(Resolve-Path -LiteralPath $BaselineExecutable).Path}
$verificationRoot=if($ResultDirectory){[IO.Path]::GetFullPath($ResultDirectory)}else{Join-Path (Split-Path $PSScriptRoot -Parent) ('qa-'+[Guid]::NewGuid().ToString('N'))}
$releaseSource=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'AppUpdates.cs') -Raw -Encoding UTF8
$version=[regex]::Match($releaseSource,'DisplayVersion = "([0-9.]+)"').Groups[1].Value
$workdayExe=Join-Path $PSScriptRoot ('Workday-'+$version+'.exe')
foreach($mode in @('policy1114','flat','nonconsent',$(if($Live){'holiday-live'}else{'holiday'}),'update')){
 $folder=Join-Path $verificationRoot $mode
 $process=Start-Process -FilePath $workdayExe -ArgumentList @(('--'+$mode+'-test'),('"'+$folder+'"')) -WindowStyle Hidden -PassThru
 if(-not $process.WaitForExit(45000)){Stop-Process -InputObject $process -Force;throw ('Timed out: '+$mode)}
 if($process.ExitCode -ne 0){throw ('Failed: '+$mode+'; '+$verificationRoot)}
}
Write-Output ('Verification records: '+$verificationRoot)