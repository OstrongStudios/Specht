<#
.SYNOPSIS
  Generate Specht icon assets (ICO + MSIX PNG variants) from a single SVG source.
  Mirrors Spieglein's scripts/generate-msix-assets.ps1 approach.

.PARAMETER Source
  Path to source SVG. Default: src/Specht.App/Assets/source/specht.svg

.PARAMETER OutDir
  Output directory. Default: src/Specht.App/Assets/
#>

param(
    [string]$Source = "src/Specht.App/Assets/source/specht.svg",
    [string]$OutDir = "src/Specht.App/Assets"
)

$ErrorActionPreference = 'Stop'

$magick = (Get-ChildItem 'C:\Program Files\ImageMagick*' -Directory -ErrorAction SilentlyContinue |
           Select-Object -First 1).FullName
if (-not $magick) { throw "ImageMagick not found. Install via 'winget install ImageMagick.ImageMagick'." }
$magick = Join-Path $magick 'magick.exe'

$repoRoot = Split-Path -Parent $PSScriptRoot
$src = Join-Path $repoRoot $Source
$out = Join-Path $repoRoot $OutDir

if (-not (Test-Path $src)) { throw "Source SVG not found: $src" }
New-Item -ItemType Directory -Force $out | Out-Null

function Render([int]$size, [string]$name) {
    $path = Join-Path $out $name
    & $magick -background none -density 384 $src -resize "${size}x${size}" $path 2>&1 | Out-Null
    Write-Host "  $name ($size px)"
}

Write-Host "Source: $src"
Write-Host "Output: $out"
Write-Host ""

Write-Host "MSIX PNG assets:"
Render 71  'SmallTile.png'
Render 142 'SmallTile.scale-200.png'
Render 150 'Square150x150Logo.png'
Render 300 'Square150x150Logo.scale-200.png'
Render 310 'LargeTile.png'
Render 620 'LargeTile.scale-200.png'
Render 44  'Square44x44Logo.png'
Render 88  'Square44x44Logo.scale-200.png'
Render 16  'Square44x44Logo.targetsize-16.png'
Render 24  'Square44x44Logo.targetsize-24.png'
Render 32  'Square44x44Logo.targetsize-32.png'
Render 48  'Square44x44Logo.targetsize-48.png'
Render 256 'Square44x44Logo.targetsize-256.png'
Render 16  'Square44x44Logo.targetsize-16_altform-unplated.png'
Render 24  'Square44x44Logo.targetsize-24_altform-unplated.png'
Render 32  'Square44x44Logo.targetsize-32_altform-unplated.png'
Render 48  'Square44x44Logo.targetsize-48_altform-unplated.png'
Render 256 'Square44x44Logo.targetsize-256_altform-unplated.png'
Render 50  'StoreLogo.png'
Render 100 'StoreLogo.scale-200.png'
Render 620 'SplashScreen.png'
Render 1240 'SplashScreen.scale-200.png'
Render 310 'Wide310x150Logo.png'
Render 620 'Wide310x150Logo.scale-200.png'

Write-Host ""
Write-Host "Multi-resolution ICO:"
$icoSizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$tmpPngs = @()
foreach ($size in $icoSizes) {
    $tmp = Join-Path $env:TEMP "specht-ico-$size.png"
    & $magick -background none -density 384 $src -resize "${size}x${size}" $tmp 2>&1 | Out-Null
    $tmpPngs += $tmp
}
$icoPath = Join-Path $out 'AppIcon.ico'
& $magick $tmpPngs $icoPath 2>&1 | Out-Null
foreach ($t in $tmpPngs) { Remove-Item $t -ErrorAction SilentlyContinue }
Write-Host "  AppIcon.ico (16-256 px, multi-res)"

Write-Host ""
Write-Host "Done. Total files in $out :"
(Get-ChildItem $out -File | Measure-Object).Count
