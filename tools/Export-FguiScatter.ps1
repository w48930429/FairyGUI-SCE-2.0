param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot "..\rpg_3d_2604140"),
    [string]$InputDir,
    [string]$OutputDir,
    [string]$Manifest,
    [string]$MovieClipManifest,
    [bool]$SyncAppBundle = $true
)

$ErrorActionPreference = "Stop"
$ProjectRoot = (Resolve-Path $ProjectRoot).Path
if (-not $InputDir) { $InputDir = Join-Path $ProjectRoot "ui\image\fgui\scatter" }
if (-not $OutputDir) { $OutputDir = Join-Path $ProjectRoot "ui\image\fgui\scatter" }
if (-not $Manifest) { $Manifest = Join-Path $ProjectRoot "ui\image\fgui\scatter\manifest.json" }
if (-not $MovieClipManifest) { $MovieClipManifest = Join-Path $ProjectRoot "ui\image\fgui\scatter\movieclip-manifest.json" }

$project = Join-Path $PSScriptRoot "FguiScatterExporter\FguiScatterExporter.csproj"
if (-not (Test-Path $project)) {
    Write-Error "Exporter project not found: $project"
    exit 2
}

if (-not (Test-Path $InputDir)) {
    Write-Error "InputDir not found: $InputDir"
    exit 2
}

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

Write-Host "[FGUI][SCATTER] projectRoot=$ProjectRoot"
Write-Host "[FGUI][SCATTER] input=$InputDir"
Write-Host "[FGUI][SCATTER] output=$OutputDir"
Write-Host "[FGUI][SCATTER] manifest=$Manifest"
Write-Host "[FGUI][SCATTER] movieclipManifest=$MovieClipManifest"

dotnet run --project $project -- --input-dir $InputDir --output-dir $OutputDir --manifest $Manifest --movieclip-manifest $MovieClipManifest
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    Write-Error "Scatter export failed. exitCode=$exitCode"
    exit $exitCode
}

if (-not (Test-Path $MovieClipManifest)) {
    Write-Error "MovieClip scatter manifest not found after export: $MovieClipManifest"
    exit 3
}

$manifestBytes = [IO.Path]::ChangeExtension($Manifest, ".bytes")
$movieClipManifestBytes = [IO.Path]::ChangeExtension($MovieClipManifest, ".bytes")
$manifestContent = Get-Content -LiteralPath $Manifest -Raw
$movieClipManifestContent = Get-Content -LiteralPath $MovieClipManifest -Raw
Set-Content -LiteralPath $manifestBytes -Value $manifestContent -Encoding utf8NoBOM
Set-Content -LiteralPath $movieClipManifestBytes -Value $movieClipManifestContent -Encoding utf8NoBOM
Write-Host "[FGUI][SCATTER] wrote manifest bytes: $manifestBytes"
Write-Host "[FGUI][SCATTER] wrote movieclip manifest bytes: $movieClipManifestBytes"

if ($SyncAppBundle) {
    $appBundleScatter = Join-Path $ProjectRoot "AppBundle\ui\image\fgui\scatter"
    if (-not (Test-Path $appBundleScatter)) {
        New-Item -ItemType Directory -Path $appBundleScatter -Force | Out-Null
    }

    Write-Host "[FGUI][SCATTER] sync AppBundle: $appBundleScatter"
    Copy-Item -Path (Join-Path $OutputDir "*") -Destination $appBundleScatter -Recurse -Force
}

Write-Host "[FGUI][SCATTER] export finished"
exit 0
