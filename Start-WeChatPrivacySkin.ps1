$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe = Join-Path $root "bin\Release\net10.0-windows\win-x64\publish\WeChatPrivacySkin.exe"

if (-not (Test-Path -LiteralPath $exe)) {
    & (Join-Path $root "Build-Release.ps1")
}

Start-Process -FilePath $exe
