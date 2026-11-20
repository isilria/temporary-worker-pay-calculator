$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source = Join-Path $PSScriptRoot 'SchoolWorkdayCalculator.cs'
$epplus = Join-Path $PSScriptRoot 'EPPlus.dll'
$template = Join-Path $PSScriptRoot 'workday_template.xlsx'
$icon = Join-Path $PSScriptRoot 'assets\workday_320_helper_icon.ico'
$releaseSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'AppUpdates.cs') -Raw -Encoding UTF8
$version = [regex]::Match($releaseSource, 'DisplayVersion = "([0-9.]+)"').Groups[1].Value
if (-not $version) { throw 'Missing release version.' }
$output = Join-Path $PSScriptRoot ('Workday-' + $version + '.exe')
$sources = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' -File | ForEach-Object FullName)

& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /win32icon:$icon /out:$output `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    /r:System.Web.Extensions.dll /r:Microsoft.CSharp.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll `
    /r:$epplus `
    /resource:"$epplus,SchoolWorkdayCalculator.EPPlus.dll" `
    /resource:"$template,SchoolWorkdayCalculator.Template.xlsx" `
    /resource:"$icon,SchoolWorkdayCalculator.AppIcon.ico" `
    /resource:"$(Join-Path $PSScriptRoot holidays.json),holidays.json" `
    @sources

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Build complete: $output"
