# Builds the one-click install kit:
#   dist\LolGuard-Install\      LolGuard.exe + Install.bat
#   dist\LolGuard-Install.zip   the same two files, ready to copy to the laptop
$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$dotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$publish = Join-Path $root 'publish'
$kit = Join-Path $root 'dist\LolGuard-Install'
$zip = Join-Path $root 'dist\LolGuard-Install.zip'

& $dotnet publish (Join-Path $root 'LolGuard.csproj') -c Release -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

Remove-Item $kit, $zip -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $kit | Out-Null
Copy-Item (Join-Path $publish 'LolGuard.exe') $kit

# cmd.exe needs CRLF line endings in batch files, or labels and goto can fail.
$bat = Get-Content -Raw (Join-Path $root 'kit\Install.bat')
[IO.File]::WriteAllText((Join-Path $kit 'Install.bat'), ($bat -replace '\r?\n', "`r`n"))

Compress-Archive -Path "$kit\*" -DestinationPath $zip
Get-ChildItem $kit, $zip | Select-Object FullName, Length | Format-Table -AutoSize | Out-String
