param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot "..\rpg_3d_2604140"),
    [string]$Manifest,
    [string]$MovieClipManifest
)

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
if (-not $Manifest) {
    $Manifest = Join-Path $ProjectRoot "ui\image\fgui\scatter\manifest.json"
}
if (-not $MovieClipManifest) {
    $MovieClipManifest = Join-Path $ProjectRoot "ui\image\fgui\scatter\movieclip-manifest.json"
}

$ErrorActionPreference = "Stop"

function Resolve-ImagePath([string]$imagePath) {
    $projectRoot = $ProjectRoot
    $normalized = $imagePath.Replace('/', [IO.Path]::DirectorySeparatorChar).Replace('\', [IO.Path]::DirectorySeparatorChar)
    return Join-Path $projectRoot ("ui\" + $normalized.TrimStart('\', '/'))
}

if (-not (Test-Path -LiteralPath $Manifest)) {
    Write-Error "Scatter manifest not found: $Manifest"
    exit 1
}

if (-not (Test-Path -LiteralPath $MovieClipManifest)) {
    Write-Error "MovieClip scatter manifest not found: $MovieClipManifest"
    exit 1
}

$manifestJson = Get-Content -LiteralPath $Manifest -Raw | ConvertFrom-Json
$movieClipManifestJson = Get-Content -LiteralPath $MovieClipManifest -Raw | ConvertFrom-Json

if ($null -eq $manifestJson.entries) {
    Write-Error "Scatter manifest entries is null: $Manifest"
    exit 1
}

if ($null -eq $movieClipManifestJson.entries) {
    Write-Error "MovieClip scatter manifest has null entries: $MovieClipManifest"
    exit 1
}

$errors = New-Object System.Collections.Generic.List[string]
$hasNormalEntries = $manifestJson.entries.Count -gt 0
if (-not $hasNormalEntries) {
    Write-Warning "Scatter manifest has no entries (treated as warning): $Manifest"
}

if ($hasNormalEntries) {
    foreach ($entry in $manifestJson.entries) {
        if ([string]::IsNullOrWhiteSpace($entry.packageId) -or
            [string]::IsNullOrWhiteSpace($entry.itemId) -or
            [string]::IsNullOrWhiteSpace($entry.imagePath)) {
            $errors.Add("normal manifest invalid entry: packageId='$($entry.packageId)' itemId='$($entry.itemId)' imagePath='$($entry.imagePath)'")
            continue
        }

        $file = Resolve-ImagePath $entry.imagePath
        if (-not (Test-Path -LiteralPath $file)) {
            $errors.Add("normal manifest missing file: $($entry.imagePath)")
        }
    }
}

$clipGroups = @{}
foreach ($entry in $movieClipManifestJson.entries) {
    if ([string]::IsNullOrWhiteSpace($entry.packageId) -or
        [string]::IsNullOrWhiteSpace($entry.clipItemId) -or
        [string]::IsNullOrWhiteSpace($entry.imagePath) -or
        $null -eq $entry.frameIndex) {
        $errors.Add("movieclip manifest invalid entry: packageId='$($entry.packageId)' clipItemId='$($entry.clipItemId)' frameIndex='$($entry.frameIndex)' imagePath='$($entry.imagePath)'")
        continue
    }

    $file = Resolve-ImagePath $entry.imagePath
    if (-not (Test-Path -LiteralPath $file)) {
        $errors.Add("movieclip manifest missing file: clip=$($entry.clipName) frame=$($entry.frameIndex) imagePath=$($entry.imagePath)")
    }

    $clipKey = "$($entry.packageId)::$($entry.clipItemId)"
    if (-not $clipGroups.ContainsKey($clipKey)) {
        $clipGroups[$clipKey] = New-Object System.Collections.Generic.List[int]
    }
    $clipGroups[$clipKey].Add([int]$entry.frameIndex)
}

foreach ($clipKey in $clipGroups.Keys) {
    $indices = $clipGroups[$clipKey] | Sort-Object
    if ($indices.Count -eq 0) {
        continue
    }

    if ($indices[0] -ne 0) {
        $errors.Add("movieclip frame sequence invalid: clip=$clipKey firstIndex=$($indices[0]) expected=0")
        continue
    }

    for ($i = 1; $i -lt $indices.Count; $i++) {
        if ($indices[$i] -ne ($indices[$i - 1] + 1)) {
            $errors.Add("movieclip frame sequence invalid: clip=$clipKey prev=$($indices[$i - 1]) current=$($indices[$i])")
            break
        }
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host ("[PASS] scatter manifest valid. entries={0}" -f $manifestJson.entries.Count)
Write-Host ("[PASS] movieclip scatter manifest valid. entries={0} clips={1}" -f $movieClipManifestJson.entries.Count, $clipGroups.Keys.Count)
exit 0

