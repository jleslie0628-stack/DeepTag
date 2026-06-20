# Run this script once to download ExifTool into tools/exiftool/
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$toolsDir = Join-Path $projectRoot "tools\exiftool"
$tempZip = Join-Path $env:TEMP "exiftool-13.59_64.zip"
$tempExtract = Join-Path $env:TEMP "exiftool-extract"
New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
if (Test-Path $tempExtract) { Remove-Item $tempExtract -Recurse -Force }
Invoke-WebRequest -Uri "https://downloads.sourceforge.net/project/exiftool/exiftool-13.59_64.zip" -OutFile $tempZip -UseBasicParsing
Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force
$sourceDir = Get-ChildItem $tempExtract -Directory | Select-Object -First 1
Copy-Item -Path (Join-Path $sourceDir.FullName "*") -Destination $toolsDir -Recurse -Force
$kExe = Join-Path $toolsDir "exiftool(-k).exe"
$exe = Join-Path $toolsDir "exiftool.exe"
if ((Test-Path $kExe) -and -not (Test-Path $exe)) { Copy-Item $kExe $exe }
Write-Host "ExifTool installed to $toolsDir"
