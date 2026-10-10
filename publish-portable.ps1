$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$out = Join-Path $root "dist\NetworkActivityTray"

if (Test-Path $out) {
    Remove-Item $out -Recurse -Force
}

Write-Host "Publishing folder-based self-contained x64 build..."

dotnet publish `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:DebugType=None `
    -o $out

if (Test-Path ".\PENetwork_W10_GUI_Colored.icl") {
    Copy-Item ".\PENetwork_W10_GUI_Colored.icl" $out -Force
}

New-Item -ItemType File -Path (Join-Path $out "portable.txt") -Force | Out-Null

$zip = Join-Path $root "dist\NetworkActivityTray-0.5.0-win-x64-portable.zip"
if (Test-Path $zip) {
    Remove-Item $zip -Force
}

Compress-Archive -Path $out -DestinationPath $zip

$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $zip -Leaf)" | Set-Content "$zip.sha256"

Write-Host ""
Write-Host "Portable package:"
Write-Host $zip
Write-Host ""
Write-Host "SHA-256:"
Write-Host $hash
