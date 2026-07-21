param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "NamePlateStudio.csproj"
$publishDir = Join-Path $root "artifacts\publish\NamePlateStudio\$Runtime"
$distDir = Join-Path $root "dist"
$nsiPath = Join-Path $PSScriptRoot "NamePlateStudio.nsi"
$iconScript = Join-Path $PSScriptRoot "Create-Icons.ps1"
$iconFile = Join-Path $root "Assets\NamePlateStudioClick.ico"
$uniconFile = Join-Path $root "Assets\NamePlateStudio.ico"
$outFile = Join-Path $distDir "NamePlateStudio-Setup-$Version-$Runtime.exe"

$makeNsisCommand = Get-Command makensis -ErrorAction SilentlyContinue
$makeNsis = if ($makeNsisCommand) { $makeNsisCommand.Source } else { $null }
if (-not $makeNsis) {
    $defaultNsis = @(
        "C:\Program Files (x86)\NSIS\makensis.exe",
        "C:\Program Files\NSIS\makensis.exe"
    )

    $makeNsis = $defaultNsis | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $makeNsis) {
    throw "NSIS makensis.exe를 찾을 수 없습니다. NSIS를 설치한 뒤 다시 실행해주세요."
}

if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir, $distDir | Out-Null

& $iconScript

$publishArgs = @(
    "publish",
    $project,
    "-c", $Configuration,
    "-r", $Runtime,
    "-o", $publishDir,
    "-p:DebugType=None",
    "-p:DebugSymbols=false"
)

if ($FrameworkDependent) {
    $publishArgs += "--self-contained"
    $publishArgs += "false"
} else {
    $publishArgs += "--self-contained"
    $publishArgs += "true"
}

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish에 실패했습니다. 종료 코드: $LASTEXITCODE"
}

if (Test-Path -LiteralPath $outFile) {
    Remove-Item -LiteralPath $outFile -Force
}

& $makeNsis `
    "/DAPP_VERSION=$Version" `
    "/DPUBLISH_DIR=$publishDir" `
    "/DICON_FILE=$iconFile" `
    "/DUNICON_FILE=$uniconFile" `
    "/DOUT_FILE=$outFile" `
    $nsiPath

if ($LASTEXITCODE -ne 0) {
    throw "NSIS 설치 파일 생성에 실패했습니다. 종료 코드: $LASTEXITCODE"
}

if (-not (Test-Path $outFile)) {
    throw "NSIS 설치 파일 생성에 실패했습니다: $outFile"
}

Write-Host "Installer created: $outFile"
