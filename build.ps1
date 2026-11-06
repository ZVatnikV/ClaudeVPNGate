$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$proj = ".\src\ClaudeVPNGate\ClaudeVPNGate.csproj"
dotnet build $proj -c Release --nologo
$rids = @("win-x64","linux-x64","osx-arm64","osx-x64")
foreach ($rid in $rids) {
  Write-Host "==> $rid"
  dotnet publish $proj -c Release -r $rid --self-contained true -o ".\dist\$rid" -p:DebugType=None -p:DebugSymbols=false --nologo
}
$iscc = "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
New-Item -ItemType Directory -Force -Path .\dist\installer | Out-Null
& $iscc .\installer\ClaudeVPNGate.iss
function Pack-Linux($rid, $name) {
  $stage = ".\dist\stage-$name"
  if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
  New-Item -ItemType Directory -Force -Path "$stage\app" | Out-Null
  Copy-Item ".\dist\$rid\*" "$stage\app" -Recurse
  Copy-Item .\packaging\install-linux.sh "$stage\install-linux.sh"
  tar -czf ".\dist\installer\$name.tar.gz" -C $stage .
}
function Pack-Mac($rid, $name) {
  $stage = ".\dist\stage-$name"
  $mac = Join-Path $stage "Claude VPN Gate.app\Contents\MacOS"
  if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
  New-Item -ItemType Directory -Force -Path $mac | Out-Null
  Copy-Item ".\dist\$rid\*" $mac -Recurse
  Copy-Item .\packaging\Info.plist (Join-Path $stage "Claude VPN Gate.app\Contents\Info.plist")
  Copy-Item .\packaging\install-macos.sh "$stage\install-macos.sh"
  tar -czf ".\dist\installer\$name.tar.gz" -C $stage .
}
Pack-Linux "linux-x64" "ClaudeVPNGate-linux-x64"
Pack-Mac "osx-arm64" "ClaudeVPNGate-macos-arm64"
Pack-Mac "osx-x64" "ClaudeVPNGate-macos-x64"
Get-ChildItem .\dist\installer | Format-Table Name, Length
