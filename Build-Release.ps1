$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnet = Join-Path $root ".dotnet\dotnet.exe"
$installer = Join-Path $root "dotnet-install.ps1"

if (-not (Test-Path -LiteralPath $dotnet)) {
    if (-not (Test-Path -LiteralPath $installer)) {
        Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installer
    }

    powershell -NoProfile -ExecutionPolicy Bypass -File $installer -Channel "10.0" -Quality "GA" -InstallDir (Join-Path $root ".dotnet")
}

& $dotnet publish -c Release -r win-x64 --self-contained false
exit $LASTEXITCODE
