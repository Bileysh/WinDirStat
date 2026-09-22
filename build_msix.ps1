[CmdletBinding()]
param (
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x64", "x86", "arm64")]
    [string]$Platform = "x64",

    [string]$OutputDir = "$PWD\artifacts",

    [string]$ProjectPath = "src\Volumetric.App\Volumetric.App.csproj",

    [string]$CertificatePath,

    [string]$CertificatePassword,

    [string]$PackageVersion,

    [switch]$SkipClean
)

$ErrorActionPreference = "Stop"
$sw = [System.Diagnostics.Stopwatch]::StartNew()

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "OK  $msg" -ForegroundColor Green }
function Write-Err($msg)  { Write-Host "ERR $msg" -ForegroundColor Red }

$IsCI = [bool]$env:GITHUB_ACTIONS

Write-Step "Volumetric MSIX build ($Configuration | $Platform)"
Write-Host "  Project     : $ProjectPath"
Write-Host "  OutputDir   : $OutputDir"
Write-Host "  Running in CI: $IsCI"

if (-not (Test-Path $ProjectPath)) {
    Write-Err "Project file not found: $ProjectPath (run this script from the repo root)"
    exit 1
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

if (-not $SkipClean) {
    Write-Step "Cleaning previous outputs"
    dotnet clean $ProjectPath -c $Configuration -p:Platform=$Platform | Out-Null
}


$publishArgs = @(
    "publish", $ProjectPath,
    "-c", $Configuration,
    "-p:Platform=$Platform",
    "-p:RuntimeIdentifier=win-$Platform",
    "-p:GenerateAppxPackageOnBuild=true",
    "-p:AppxPackageDir=$OutputDir\",
    "-p:AppxBundle=Never",
    "-p:PublishTrimmed=false",
    "-p:UapAppxPackageBuildMode=SideloadOnly"
)

if ($PackageVersion) {
    if ($PackageVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        Write-Err "PackageVersion must be in x.y.z.w form, got: $PackageVersion"
        exit 1
    }
    Write-Host "  Overriding package version -> $PackageVersion"
    $publishArgs += "-p:AppxPackageVersion=$PackageVersion"
}

if ($CertificatePath) {
    if (-not (Test-Path $CertificatePath)) {
        Write-Err "CertificatePath not found: $CertificatePath"
        exit 1
    }
    Write-Host "  Signing with: $CertificatePath"
    $publishArgs += "-p:PackageCertificateKeyFile=$CertificatePath"
    if ($CertificatePassword) {
        $publishArgs += "-p:PackageCertificatePassword=$CertificatePassword"
    }
}

Write-Step "Running dotnet publish"
& dotnet @publishArgs
$publishExit = $LASTEXITCODE

if ($publishExit -ne 0) {
    Write-Err "dotnet publish failed with exit code $publishExit"
    exit 1
}

$package = Get-ChildItem -Path $OutputDir -Recurse -Include *.msix, *.msixbundle |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $package) {
    Write-Err "No .msix/.msixbundle produced under $OutputDir"
    exit 2
}

Write-Ok "Package produced: $($package.FullName)"
Write-Host "  Size: $([math]::Round($package.Length / 1MB, 2)) MB"

if ($IsCI) {
    "package-path=$($package.FullName)"  >> $env:GITHUB_OUTPUT
    "package-name=$($package.Name)"      >> $env:GITHUB_OUTPUT
    "output-dir=$OutputDir"              >> $env:GITHUB_OUTPUT
}

$sw.Stop()
Write-Ok "Build complete in $([math]::Round($sw.Elapsed.TotalSeconds, 1))s"
exit 0
