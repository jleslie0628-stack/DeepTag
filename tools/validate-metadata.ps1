param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$testDir = Join-Path $ProjectRoot "TestFiles"
$exif = Join-Path $ProjectRoot "tools\exiftool\exiftool.exe"
$buildDir = Join-Path $ProjectRoot "src\MetadataEditor\bin\Release\net8.0-windows"

if (-not (Test-Path $exif)) {
    throw "ExifTool not found at $exif. Run tools/download-exiftool.ps1 first."
}

function Assert-True($condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Write-Host "=== Metadata Editor validation ==="

# 1) Plain text filesystem dates
$txt = Join-Path $testDir "sample.txt"
$newCreated = Get-Date "2020-02-02 12:00:00"
$newModified = Get-Date "2020-02-03 13:00:00"
$newAccessed = Get-Date "2020-02-04 14:00:00"
[IO.File]::SetCreationTime($txt, $newCreated)
[IO.File]::SetLastWriteTime($txt, $newModified)
[IO.File]::SetLastAccessTime($txt, $newAccessed)
$info = Get-Item $txt
Assert-True ($info.CreationTime.ToString("yyyy-MM-dd HH:mm") -eq $newCreated.ToString("yyyy-MM-dd HH:mm")) "Text creation time"
Assert-True ($info.LastWriteTime.ToString("yyyy-MM-dd HH:mm") -eq $newModified.ToString("yyyy-MM-dd HH:mm")) "Text modified time"
Write-Host "PASS: Plain text filesystem dates"

# 2) PNG has no editable media metadata (matches app MediaIndicatorTags logic)
$png = Join-Path $testDir "sample.png"
$pngJson = & $exif -json -G1 -a $png | ConvertFrom-Json
$mediaTags = @('DateTimeOriginal','Make','Model','LensModel','ISO','FNumber','ExposureTime','GPSLatitude','GPSLongitude','CreateDate','MediaCreateDate')
$pngMedia = $pngJson[0].PSObject.Properties | Where-Object {
    $short = if ($_.Name -like '*:*') { $_.Name.Split(':')[-1] } else { $_.Name }
    $mediaTags -contains $short
}
Assert-True ($pngMedia.Count -eq 0) "PNG should not expose editable media metadata"
Write-Host "PASS: PNG has no embedded media metadata"

# 3) JPEG EXIF read/write
$jpg = Join-Path $testDir "sample.jpg"
& $exif -overwrite_original -Make="Canon" -Model="EOS R5" -DateTimeOriginal="2023:01:15 10:30:00" $jpg | Out-Null
$before = & $exif -json -G1 -a $jpg | ConvertFrom-Json
Assert-True ($before[0].'IFD0:Make' -eq 'Canon') "JPEG initial Make"
& $exif -overwrite_original -Make="Sony" -Model="ILCE-7M4" -DateTimeOriginal="2024:06:15 14:30:00" $jpg | Out-Null
$after = & $exif -json -G1 -a $jpg | ConvertFrom-Json
Assert-True ($after[0].'IFD0:Make' -eq 'Sony') "JPEG updated Make"
Assert-True ($after[0].'IFD0:Model' -eq 'ILCE-7M4') "JPEG updated Model"
Assert-True ($after[0].'ExifIFD:DateTimeOriginal' -like '2024:06:15 14:30:00*') "JPEG updated DateTimeOriginal"
Write-Host "PASS: JPEG EXIF metadata read/write"

# 4) Bundled exiftool copied to build output
$builtExif = Join-Path $buildDir "tools\exiftool\exiftool.exe"
Assert-True (Test-Path $builtExif) "Built output includes bundled exiftool.exe"
Write-Host "PASS: Build output bundles ExifTool"

# 5) MP4 if present
$mp4 = Join-Path $testDir "sample.mp4"
if (Test-Path $mp4) {
    & $exif -overwrite_original -CreateDate="2021:11:11 11:11:11" $mp4 | Out-Null
    $mp4After = & $exif -json -G1 -a $mp4 | ConvertFrom-Json
    $createDate = $mp4After[0].'QuickTime:CreateDate'
    if (-not $createDate) {
        $createDate = ($mp4After[0].PSObject.Properties | Where-Object { $_.Name -like '*:CreateDate' -and $_.Name -notlike 'System:*' } | Select-Object -First 1).Value
    }
    Assert-True ($createDate -like '2021:11:11 11:11:11*') "MP4 updated CreateDate"
    Write-Host "PASS: MP4 metadata read/write"
} else {
    Write-Host "SKIP: MP4 test (ffmpeg not installed)"
}

Write-Host "=== All executed checks passed ==="
