param(
    [string]$BuildNumber,
    [switch]$InstallWinFsp
)

$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot
$artifacts = Join-Path $root "artifacts"
$publish = Join-Path $artifacts "publish"
$winFspMsi = Join-Path $artifacts "winfsp.msi"
$winFspUrl = "https://github.com/winfsp/winfsp/releases/download/v2.2B5/winfsp-2.2.26286.msi"
$iscc = Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"

if (-not (Test-Path $iscc)) {
    $iscc = Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"
}

if (-not (Test-Path $winFspMsi)) {
    New-Item -ItemType Directory -Force $artifacts | Out-Null
    Invoke-WebRequest $winFspUrl -OutFile $winFspMsi
}

if ($InstallWinFsp -and -not (Test-Path "HKLM:\SOFTWARE\WOW6432Node\WinFsp")) {
    Start-Process msiexec.exe -ArgumentList "/i `"$winFspMsi`" /qn /norestart" -Wait
}

$publishArguments = @()

if ($BuildNumber) {
    $publishArguments += "-p:BuildNumber=$BuildNumber"
}

Remove-Item $publish -Recurse -Force -ErrorAction SilentlyContinue

foreach ($project in "src\WinCasc\WinCasc.csproj", "src\WinCasc.Shell\WinCasc.Shell.csproj") {
    dotnet publish (Join-Path $root $project) -c Release -r win-x64 --self-contained -o $publish @publishArguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

& $iscc (Join-Path $PSScriptRoot "WinCasc.iss")
exit $LASTEXITCODE
