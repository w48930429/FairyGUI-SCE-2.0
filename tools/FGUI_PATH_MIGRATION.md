# FGUI Script Path Migration Guide

This guide explains how to move the current FGUI export/validate scripts to another Spark project.

## 1) Current Expected Layout

```text
<RepoRoot>/
  tools/
    Export-FguiScatter.bat
    ValidateAndBuild-Fgui.bat
    Check-FguiMigrationRefs.bat
    Check-FguiExportNames.bat
    Check-FguiMovieClipRuntime.bat
    Check-FguiMigrationRefs.ps1
    Export-FguiScatter.ps1
    Validate-FguiExportNames.ps1
    Check-FguiScatterManifest.ps1
    Check-FguiMovieClipRuntime.ps1
    FguiScatterExporter/
      FguiScatterExporter.csproj
  rpg_3d_2604140/
    ui/image/fgui/scatter/...
    AppBundle/ui/image/fgui/scatter/...
  UIProject/
    assets/...
```

## 2) What `ValidateAndBuild-Fgui.bat` Does (Current)

`ValidateAndBuild-Fgui.bat` now runs:

1. Export scatter (`Export-FguiScatter.ps1`)
2. Validate export names (`Validate-FguiExportNames.ps1`)
3. Validate scatter manifest (`Check-FguiScatterManifest.ps1`)
4. Validate movieclip runtime (`Check-FguiMovieClipRuntime.ps1`)

It does **not** run `dotnet build` anymore.

## 2.1) Required `GameEntry.csproj` updates (Important)

When migrating FGUI in SCE to another project, ensure target `src/GameEntry.csproj` has:

1. `CLIENT` define constants for client configurations (`#if CLIENT` code must compile).
2. Server configurations excluding client and FGUI source files.

Recommended snippet (adjust if your layout differs):

```xml
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Client-Debug|AnyCPU'">
  <DefineConstants>$(DefineConstants);CLIENT;DEBUG</DefineConstants>
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Client-Release|AnyCPU'">
  <DefineConstants>$(DefineConstants);CLIENT;RELESE</DefineConstants>
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Client-Resource|AnyCPU'">
  <DefineConstants>$(DefineConstants);CLIENT;RESOURCE</DefineConstants>
</PropertyGroup>

<ItemGroup Condition="'$(Configuration)' == 'Server-Debug' or '$(Configuration)' == 'Server-Release'">
  <Compile Remove="FGUI/**/*.cs" />
  <Compile Remove="Systems/**/Client/**/*.cs" />
</ItemGroup>
```

## 3) Minimal Migration: Only One Line to Edit in Each BAT

If target project folder name is not `rpg_3d_2604140`, update this line in each BAT:

```bat
set PROJECT_ROOT=%SCRIPT_DIR%..\rpg_3d_2604140
```

Change it to your actual project directory, for example:

```bat
set PROJECT_ROOT=%SCRIPT_DIR%..\MyGameProject
```

Affected BAT files:

- `Export-FguiScatter.bat`
- `ValidateAndBuild-Fgui.bat`
- `Check-FguiMigrationRefs.bat`
- `Check-FguiExportNames.bat`
- `Check-FguiMovieClipRuntime.bat`

## 4) When You Must Edit PS1 Defaults

Normally you do not need to edit PS1 if BAT passes `-ProjectRoot`.

Edit PS1 only if:

- You will run PS1 directly without BAT, or
- Your folder structure differs from the expected pattern.

Default `ProjectRoot` appears in:

- `Export-FguiScatter.ps1`
- `Check-FguiMigrationRefs.ps1`
- `Validate-FguiExportNames.ps1`
- `Check-FguiScatterManifest.ps1`
- `Check-FguiMovieClipRuntime.ps1`

Current default:

```powershell
[string]$ProjectRoot = (Join-Path $PSScriptRoot "..\rpg_3d_2604140")
```

## 5) Paths Used by Each Script

### Export-FguiScatter.ps1

- Input: `<ProjectRoot>/ui/image/fgui/scatter`
- Output: `<ProjectRoot>/ui/image/fgui/scatter`
- Manifest:
  - `<ProjectRoot>/ui/image/fgui/scatter/manifest.json`
  - `<ProjectRoot>/ui/image/fgui/scatter/movieclip-manifest.json`
- Sync target:
  - `<ProjectRoot>/AppBundle/ui/image/fgui/scatter`
- Exporter project (relative to `tools`):
  - `tools/FguiScatterExporter/FguiScatterExporter.csproj`

### Check-FguiMigrationRefs.ps1

- Purpose: verify migration reference closure, especially when reusing `ItemBagUiClientSys`.
- Key checks:
  - `src/FGUI/SCE/FguiMgr.cs`
  - `src/Systems/FGUI/Client/FGUIBootstrapClientSys.cs`
  - `src/Systems/FGUI/Client/Logic/FguiExampleRunnerClientSys.cs`
  - If `ItemBagUiClientSys.cs` exists, these must also exist:
    - `src/Systems/Item/Shared/ItemNetContract.cs`
    - `src/Systems/Item/Client/ItemClient.cs`
    - `src/Systems/Item/Client/ItemNotificationUiClientSys.cs`
    - `src/Systems/Item/Client/ItemDetailPopupUiClientSys.cs`
    - `src/Systems/Item/Client/EquipItemDetailPopupUiClientSys.cs`

### Validate-FguiExportNames.ps1

- Assets dir default:
  - `<ProjectRoot>/../UIProject/assets`

If your UIProject is elsewhere, pass `-AssetsDir` explicitly.

### Check-FguiScatterManifest.ps1

- Reads:
  - `<ProjectRoot>/ui/image/fgui/scatter/manifest.json`
  - `<ProjectRoot>/ui/image/fgui/scatter/movieclip-manifest.json`
- Verifies image files under:
  - `<ProjectRoot>/ui/...`

### Check-FguiMovieClipRuntime.ps1

- Runtime root default:
  - `<ProjectRoot>/AppBundle`
- Manifest relative path default:
  - `ui/image/fgui/scatter/movieclip-manifest.json`

## 6) Common Migration Scenarios

### Scenario A: Same structure, different project folder name

Only update `PROJECT_ROOT` in BAT files.

### Scenario B: `UIProject` not at `<ProjectRoot>/../UIProject`

Either:

- Edit `Validate-FguiExportNames.ps1` default `AssetsDir`, or
- Run with explicit param:

```powershell
pwsh -File tools/Validate-FguiExportNames.ps1 -ProjectRoot "<ProjectRoot>" -AssetsDir "<ActualAssetsDir>"
```

### Scenario C: `AppBundle` path differs

Update `Export-FguiScatter.ps1` (`SyncAppBundle` target) and `Check-FguiMovieClipRuntime.ps1` (`RuntimeRoot` default), or pass `-RuntimeRoot`.

## 7) Recommended Verification Commands

From repo root:

```bat
:: Ensure csproj has CLIENT defines and server-side source exclusion first
tools\Check-FguiMigrationRefs.bat
tools\Check-FguiExportNames.bat
tools\Check-FguiMovieClipRuntime.bat
```

Optional full pipeline:

```bat
tools\ValidateAndBuild-Fgui.bat
```

## 8) Handoff Checklist for Another AI

1. Confirm actual `<RepoRoot>`, `<ProjectRoot>`, `<UIProject/assets>`, `<AppBundle>`.
2. Update `src/GameEntry.csproj` first (CLIENT defines + server exclusion of client/FGUI sources).
3. Update BAT `PROJECT_ROOT` lines if needed.
4. Run `tools\Check-FguiMigrationRefs.bat` first (prevents missing-type compile failures from partial copy).
5. Run `tools\Export-FguiScatter.bat`.
6. Run `tools\Check-FguiExportNames.bat`.
7. Run `tools\Check-FguiMovieClipRuntime.bat`.
8. If failures are path-related, pass explicit params to PS1 or update PS1 defaults.
