param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot "..\rpg_3d_2604140"),
    [string]$RuntimeRoot,
    [string]$MovieClipManifestRelative = "ui/image/fgui/scatter/movieclip-manifest.json"
)

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
if (-not $RuntimeRoot) {
    $RuntimeRoot = Join-Path $ProjectRoot "AppBundle"
}

$ErrorActionPreference = "Stop"

function Resolve-RuntimePath([string]$runtimeRoot, [string]$relativePath) {
    $normalized = $relativePath.Replace('/', [IO.Path]::DirectorySeparatorChar).Replace('\', [IO.Path]::DirectorySeparatorChar)
    return Join-Path $runtimeRoot $normalized
}

if (-not (Test-Path -LiteralPath $RuntimeRoot)) {
    Write-Error "RuntimeRoot not found: $RuntimeRoot"
    exit 1
}

$manifestPath = Resolve-RuntimePath $RuntimeRoot $MovieClipManifestRelative
if (-not (Test-Path -LiteralPath $manifestPath)) {
    Write-Error "MovieClip manifest not found in runtime root: $manifestPath"
    exit 1
}

$manifestJson = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($null -eq $manifestJson.entries) {
    Write-Error "MovieClip manifest entries is null: $manifestPath"
    exit 1
}

if ($manifestJson.entries.Count -le 0) {
    Write-Warning "MovieClip manifest has no entries (treated as warning): $manifestPath"
    Write-Host ("[PASS] runtime movieclip manifest ok: {0}" -f $manifestPath)
    Write-Host "[PASS] movieclip entries=0 clips=0"
    exit 0
}

$missing = New-Object System.Collections.Generic.List[string]
$invalid = New-Object System.Collections.Generic.List[string]
$clips = New-Object System.Collections.Generic.HashSet[string]

foreach ($entry in $manifestJson.entries) {
    if ([string]::IsNullOrWhiteSpace($entry.packageId) -or
        [string]::IsNullOrWhiteSpace($entry.clipItemId) -or
        [string]::IsNullOrWhiteSpace($entry.imagePath) -or
        $null -eq $entry.frameIndex) {
        $invalid.Add("invalid entry packageId='$($entry.packageId)' clipItemId='$($entry.clipItemId)' frameIndex='$($entry.frameIndex)' imagePath='$($entry.imagePath)'")
        continue
    }

    $null = $clips.Add("$($entry.packageId)::$($entry.clipItemId)")

    $runtimeImagePath = Resolve-RuntimePath $RuntimeRoot ("ui/" + $entry.imagePath.TrimStart('/', '\'))
    if (-not (Test-Path -LiteralPath $runtimeImagePath)) {
        $missing.Add("missing frame file packageId=$($entry.packageId) clipItemId=$($entry.clipItemId) frame=$($entry.frameIndex) path=$runtimeImagePath")
    }
}

if ($invalid.Count -gt 0) {
    $invalid | ForEach-Object { Write-Error $_ }
    exit 1
}

if ($missing.Count -gt 0) {
    $missing | Select-Object -First 20 | ForEach-Object { Write-Error $_ }
    if ($missing.Count -gt 20) {
        Write-Error ("... truncated {0} more missing frame files" -f ($missing.Count - 20))
    }
    exit 1
}

Write-Host ("[PASS] runtime movieclip manifest ok: {0}" -f $manifestPath)
Write-Host ("[PASS] movieclip entries={0} clips={1}" -f $manifestJson.entries.Count, $clips.Count)
exit 0

