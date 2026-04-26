# FGUI 脚本跨项目迁移说明（中文）

本文档用于把当前 FGUI 导出/校验脚本迁移到其他星火项目时，快速定位需要改的路径与验证步骤。

## 1）当前预期目录结构

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

## 2）`ValidateAndBuild-Fgui.bat` 当前行为

当前脚本执行顺序为：

1. 导出 scatter（`Export-FguiScatter.ps1`）
2. 校验导出命名（`Validate-FguiExportNames.ps1`）
3. 校验 scatter manifest（`Check-FguiScatterManifest.ps1`）
4. 校验 movieclip 运行时资源（`Check-FguiMovieClipRuntime.ps1`）

注意：该脚本现在**不再执行** `dotnet build`。

## 2.1）`GameEntry.csproj` 迁移必改项（重要）

跨项目迁移 FGUI in SCE 时，目标项目的 `src/GameEntry.csproj` 需要检查以下两项：

1. 客户端配置必须定义 `CLIENT` 常量（保证 `#if CLIENT` 代码生效）。
2. 服务端配置必须排除客户端与 FGUI 源码（避免服务端编译到客户端类型）。

建议加入以下片段（按你的项目实际结构调整）：

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

## 3）最小迁移改动：每个 BAT 只改一行

若目标项目目录名不是 `rpg_3d_2604140`，请在每个 BAT 里修改：

```bat
set PROJECT_ROOT=%SCRIPT_DIR%..\rpg_3d_2604140
```

例如改成：

```bat
set PROJECT_ROOT=%SCRIPT_DIR%..\MyGameProject
```

需要改的 BAT：

- `Export-FguiScatter.bat`
- `ValidateAndBuild-Fgui.bat`
- `Check-FguiMigrationRefs.bat`
- `Check-FguiExportNames.bat`
- `Check-FguiMovieClipRuntime.bat`

## 4）什么时候需要改 PS1 默认值

一般不需要改（因为 BAT 已传 `-ProjectRoot`）。

只有以下情况才需要改 PS1：

- 你直接运行 PS1（不经过 BAT）；
- 新项目目录结构和当前约定不一致。

含有 `ProjectRoot` 默认值的 PS1：

- `Export-FguiScatter.ps1`
- `Check-FguiMigrationRefs.ps1`
- `Validate-FguiExportNames.ps1`
- `Check-FguiScatterManifest.ps1`
- `Check-FguiMovieClipRuntime.ps1`

当前默认值：

```powershell
[string]$ProjectRoot = (Join-Path $PSScriptRoot "..\rpg_3d_2604140")
```

## 5）各脚本路径依赖说明

### Export-FguiScatter.ps1

- 输入目录：`<ProjectRoot>/ui/image/fgui/scatter`
- 输出目录：`<ProjectRoot>/ui/image/fgui/scatter`
- 产物：
  - `<ProjectRoot>/ui/image/fgui/scatter/manifest.json`
  - `<ProjectRoot>/ui/image/fgui/scatter/movieclip-manifest.json`
- 同步到运行时：
  - `<ProjectRoot>/AppBundle/ui/image/fgui/scatter`
- 导出器工程（相对 `tools`）：
  - `tools/FguiScatterExporter/FguiScatterExporter.csproj`

### Check-FguiMigrationRefs.ps1

- 用途：检查迁移包关键引用是否完整（尤其是 `ItemBagUiClientSys` 的依赖闭包）
- 关键检查：
  - `src/FGUI/SCE/FguiMgr.cs`
  - `src/Systems/FGUI/Client/FGUIBootstrapClientSys.cs`
  - `src/Systems/FGUI/Client/Logic/FguiExampleRunnerClientSys.cs`
  - 若存在 `ItemBagUiClientSys.cs`，则必须同时存在：
    - `src/Systems/Item/Shared/ItemNetContract.cs`
    - `src/Systems/Item/Client/ItemClient.cs`
    - `src/Systems/Item/Client/ItemNotificationUiClientSys.cs`
    - `src/Systems/Item/Client/ItemDetailPopupUiClientSys.cs`
    - `src/Systems/Item/Client/EquipItemDetailPopupUiClientSys.cs`

### Validate-FguiExportNames.ps1

- 默认校验目录：
  - `<ProjectRoot>/../UIProject/assets`

如果 UIProject 不在此处，请用 `-AssetsDir` 显式传参。

### Check-FguiScatterManifest.ps1

- 读取：
  - `<ProjectRoot>/ui/image/fgui/scatter/manifest.json`
  - `<ProjectRoot>/ui/image/fgui/scatter/movieclip-manifest.json`
- 校验图片文件路径：
  - `<ProjectRoot>/ui/...`

### Check-FguiMovieClipRuntime.ps1

- 运行时根目录默认：
  - `<ProjectRoot>/AppBundle`
- Manifest 相对路径默认：
  - `ui/image/fgui/scatter/movieclip-manifest.json`

## 6）常见迁移场景

### 场景 A：目录结构相同，仅项目目录名不同

只改 BAT 的 `PROJECT_ROOT` 一行即可。

### 场景 B：`UIProject` 不在 `<ProjectRoot>/../UIProject`

两种方案：

- 修改 `Validate-FguiExportNames.ps1` 默认 `AssetsDir`；
- 或运行时显式传参：

```powershell
pwsh -File tools/Validate-FguiExportNames.ps1 -ProjectRoot "<ProjectRoot>" -AssetsDir "<实际Assets路径>"
```

### 场景 C：`AppBundle` 路径不同

修改：

- `Export-FguiScatter.ps1` 的同步目标路径；
- `Check-FguiMovieClipRuntime.ps1` 的 `RuntimeRoot` 默认值；

或运行时传 `-RuntimeRoot`。

## 7）建议验证命令

在仓库根目录执行：

```bat
:: 先检查 csproj（CLIENT/Server 排除）是否已按上节处理
tools\Check-FguiMigrationRefs.bat
tools\Check-FguiExportNames.bat
tools\Check-FguiMovieClipRuntime.bat
```

完整流水线（含导出）：

```bat
tools\ValidateAndBuild-Fgui.bat
```

## 8）交接给其他项目 AI 的清单

1. 确认 `<RepoRoot>`、`<ProjectRoot>`、`<UIProject/assets>`、`<AppBundle>` 实际路径。
2. 先修改 `src/GameEntry.csproj`（补齐 `CLIENT` 常量、服务端排除客户端源码）。
3. 按需修改 BAT 的 `PROJECT_ROOT`。
4. 先运行 `tools\Check-FguiMigrationRefs.bat`（避免拷贝不完整导致编译报缺类型）。
5. 运行 `tools\Export-FguiScatter.bat`。
6. 运行 `tools\Check-FguiExportNames.bat`。
7. 运行 `tools\Check-FguiMovieClipRuntime.bat`。
8. 若报路径错误，优先通过 `-ProjectRoot/-AssetsDir/-RuntimeRoot` 显式传参验证，再决定是否改脚本默认值。
