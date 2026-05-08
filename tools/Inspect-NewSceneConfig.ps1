param(
    [string]$RepoRoot = "."
)

$ErrorActionPreference = "Stop"

function Resolve-AbsPath([string]$base, [string]$relativePath)
{
    return [System.IO.Path]::GetFullPath((Join-Path $base $relativePath))
}

function ConvertFrom-JsonWithComments([string]$rawJson)
{
    # ScopeData json 常含 /* ... */ 注释，先去注释再解析。
    $clean = [System.Text.RegularExpressions.Regex]::Replace(
        $rawJson,
        "/\*.*?\*/",
        "",
        [System.Text.RegularExpressions.RegexOptions]::Singleline)
    return $clean | ConvertFrom-Json
}

$jsonPath = Resolve-AbsPath $RepoRoot "rpg_3d_2604140/editor/data/GameEntry/ScopeData/GameDataScene/new_scene.json"
$sceneInfoPath = Resolve-AbsPath $RepoRoot "rpg_3d_2604140/scene/new_scene/scene_info.xml"
$luaPath = Resolve-AbsPath $RepoRoot "rpg_3d_2604140/scene/new_scene/config.lua"
$sceneItemsPath = Resolve-AbsPath $RepoRoot "rpg_3d_2604140/scene/new_scene/map.scene_items"

if (-not (Test-Path $jsonPath)) { throw "missing file: $jsonPath" }
if (-not (Test-Path $sceneInfoPath)) { throw "missing file: $sceneInfoPath" }
if (-not (Test-Path $luaPath)) { throw "missing file: $luaPath" }
if (-not (Test-Path $sceneItemsPath)) { throw "missing file: $sceneItemsPath" }

$sceneJson = ConvertFrom-JsonWithComments (Get-Content $jsonPath -Raw)
$sceneInfoXml = [xml](Get-Content $sceneInfoPath -Raw)
$sceneItems = Get-Content $sceneItemsPath -Raw | ConvertFrom-Json
$luaContent = Get-Content $luaPath -Raw

$root = $sceneJson.Root
$sizeX = [int]$root.Size.X
$sizeY = [int]$root.Size.Y
$mapSizeTokens = ($sceneInfoXml.Scene.MapSize -split "\s+") | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

if ($mapSizeTokens.Count -ne 2)
{
    throw "scene_info.xml MapSize format invalid: $($sceneInfoXml.Scene.MapSize)"
}

$mapX = [int]$mapSizeTokens[0]
$mapY = [int]$mapSizeTokens[1]
$sameSize = ($sizeX -eq $mapX -and $sizeY -eq $mapY)

$terrainCount = 0
$xValues = @()
$yValues = @()
if ($sceneItems.TerrainInfo -is [System.Collections.IEnumerable])
{
    foreach ($item in $sceneItems.TerrainInfo)
    {
        if ($null -eq $item.pos) { continue }
        $xValues += [double]$item.pos.x
        $yValues += [double]$item.pos.y
        $terrainCount++
    }
}

$minX = if ($xValues.Count -gt 0) { ($xValues | Measure-Object -Minimum).Minimum } else { 0 }
$maxX = if ($xValues.Count -gt 0) { ($xValues | Measure-Object -Maximum).Maximum } else { 0 }
$minY = if ($yValues.Count -gt 0) { ($yValues | Measure-Object -Minimum).Minimum } else { 0 }
$maxY = if ($yValues.Count -gt 0) { ($yValues | Measure-Object -Maximum).Maximum } else { 0 }

Write-Output "=== new_scene 可改字段与当前值 ==="
Write-Output "new_scene.json: $jsonPath"
Write-Output "  Root.Name=$($root.Name)"
Write-Output "  Root.Path=$($root.Path)"
Write-Output "  Root.HostedSceneTag=$($root.HostedSceneTag)"
Write-Output "  Root.DefaultCamera=$($root.DefaultCamera)"
Write-Output "  Root.Size=($sizeX,$sizeY)"
Write-Output ""
Write-Output "scene_info.xml: $sceneInfoPath"
Write-Output "  Scene.MapSize=($mapX,$mapY)"
Write-Output "  Root.Size 与 MapSize 一致: $sameSize"
Write-Output ""
Write-Output "config.lua: $luaPath"
if ($luaContent -match "scene_name\s*=\s*'([^']*)'")
{
    Write-Output "  scene_name=$($Matches[1])"
}
if ($luaContent -match "camera_name\s*=\s*'([^']*)'")
{
    Write-Output "  camera_name=$($Matches[1])"
}
Write-Output ""
Write-Output "map.scene_items: $sceneItemsPath"
Write-Output "  TerrainInfo.Count=$terrainCount"
Write-Output "  TerrainInfo.pos.x range=[$minX,$maxX]"
Write-Output "  TerrainInfo.pos.y range=[$minY,$maxY]"
Write-Output ""
Write-Output "提示: map.acmap/HeightData.dat/Collision.dat 属于二进制资源，不建议手改。"
