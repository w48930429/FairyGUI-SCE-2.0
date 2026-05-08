param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot "..\rpg_3d_2604140"),
    [switch]$RequireItemBagEntry = $true
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ProjectRoot)) {
    Write-Error "ProjectRoot not found: $ProjectRoot"
    exit 1
}

$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$srcRoot = Join-Path $ProjectRoot "src"
if (-not (Test-Path -LiteralPath $srcRoot)) {
    Write-Error "src directory not found: $srcRoot"
    exit 1
}

function Get-SourcePath([string]$relativePath) {
    $normalized = $relativePath.Replace('/', [IO.Path]::DirectorySeparatorChar).Replace('\', [IO.Path]::DirectorySeparatorChar)
    return Join-Path $srcRoot $normalized
}

function Ensure-FilesExist([string[]]$relativePaths, [string]$context) {
    $missing = New-Object System.Collections.Generic.List[string]
    foreach ($rel in $relativePaths) {
        $full = Get-SourcePath $rel
        if (-not (Test-Path -LiteralPath $full)) {
            $missing.Add($rel)
        }
    }

    if ($missing.Count -gt 0) {
        Write-Error ("{0} missing files: {1}" -f $context, ($missing -join ", "))
        return $false
    }

    return $true
}

$coreRefs = @(
    "FGUI/SCE/FguiMgr.cs",
    "Systems/FGUI/Client/FGUIBootstrapClientSys.cs",
    "Systems/FGUI/Client/Logic/FguiExampleRunnerClientSys.cs"
)

$allOk = $true
if (-not (Ensure-FilesExist -relativePaths $coreRefs -context "FGUI core/entry")) {
    $allOk = $false
}

$itemBagRel = "Systems/Item/Client/ItemBagUiClientSys.cs"
$itemBagPath = Get-SourcePath $itemBagRel
$hasItemBagEntry = Test-Path -LiteralPath $itemBagPath

if ($RequireItemBagEntry -and -not $hasItemBagEntry) {
    Write-Error "ItemBag bottom entry not found: $itemBagRel"
    $allOk = $false
}

if ($hasItemBagEntry) {
    $itemBagRefs = @(
        "Systems/Item/Shared/ItemNetContract.cs",
        "Systems/Item/Client/ItemClient.cs",
        "Systems/Item/Client/ItemNotificationUiClientSys.cs",
        "Systems/Item/Client/ItemDetailPopupUiClientSys.cs",
        "Systems/Item/Client/EquipItemDetailPopupUiClientSys.cs"
    )

    if (-not (Ensure-FilesExist -relativePaths $itemBagRefs -context "ItemBag entry dependencies")) {
        $allOk = $false
    }
}
else {
    Write-Warning "ItemBagUiClientSys.cs is absent. If you use a custom bottom entry, this can be ignored."
}

if (-not $allOk) {
    exit 1
}

Write-Host "[PASS] FGUI migration reference check passed."
Write-Host ("[PASS] ProjectRoot={0}" -f $ProjectRoot)
Write-Host ("[PASS] ItemBagEntryPresent={0}" -f $hasItemBagEntry)
exit 0

