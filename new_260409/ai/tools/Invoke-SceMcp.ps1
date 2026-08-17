#requires -Version 5.1
<#
.SYNOPSIS
  Trigger/Data MCP 的兼容入口：探测 HTTP 端口；未监听则按工程 docs\.editor-root 启动 TriggerMcpHost.exe；轮询 /health 至可调用后，执行**一次** JSON-RPC tools/call。

.DESCRIPTION
  - 端口上已有 MCP 且地图就绪 → 不启第二个 Host，直接执行 tools/call。
  - 连接失败（无服务）→ 若已解析到工程根（见 -ProjectRoot）则启动宿主 exe（读 docs\.editor-root、--map 工程根、--mcp-port / --mcp-mode），再轮询直至就绪。**启动前**若检测到已有 **TriggerMcpHost.exe** 且命令行 **--map** / **--mcp-port** 与本次一致，则**拒绝再启**并抛错。
  - 请求 JSON 仅支持单次 { "tool", "arguments" }，可选 **baseUrl**；**不支持** `calls` 批量（每次脚本运行只对应一个工具调用）。
  - 完整编辑器工具（如 `editor_get_context`、`debug_*`、`runtime_*`、`resource_catalog_*`、`uiauthoring_*`、`ui_*`）请使用 `Invoke-SceEditorMcp.ps1`。本脚本保留 TriggerMcpHost fallback，但不会把它伪装成完整编辑器。
  - -HealthOnly：仅保证端点就绪（或起 Host 后就绪），不调用工具。
  - 由本脚本**新启动**的 TriggerMcpHost 默认带 **--exit-when-parent-pid**：从当前 Shell 沿 **Win32_Process** 父链解析「要监视的进程」：**优先**取父进程为 **explorer.exe** 的**本链上那一环**（即资源管理器之下的顶层 GUI 进程，多为 IDE 主进程）；若无法判定（未见 explorer 父链），则取链上**第一个非 PowerShell**（非 powershell.exe / pwsh.exe）的进程。该进程退出后宿主退出；**-KeepHostAfterLauncher** 可关闭。

.PARAMETER ProjectRoot
  含 project.sce 与 docs\.editor-root 的 SCE 工程根。**可选**：未指定且本脚本位于 **<工程根>\ai\tools\**（推荐）或旧版 **<工程根>\.cursor\tools\** 时，自动使用 **ai**（或 **.cursor**）的父目录作为工程根；其它路径下若需自动起 Host 须显式传入。

.PARAMETER Port
  未在 JSON 中指定 baseUrl 时使用的 MCP 端口，默认 8765。

.PARAMETER RequestJsonPath
  请求 JSON 路径。须含 **tool**；可选 **baseUrl**、**arguments**。

.PARAMETER Tool
  不用 JSON 文件时，指定单个工具名（与 -ArgumentsJson 配合）。

.PARAMETER ArgumentsJson
  单个工具时的参数 JSON 对象字符串。

.PARAMETER HealthOnly
  仅探测/启动并等到就绪，输出最后一帧 /health，不执行 tools/call。

.PARAMETER McpMode
  启动宿主时传给 TriggerMcpHost 的 --mcp-mode，默认 http。

.PARAMETER HostExtraArgs
  启动宿主时追加到命令行的额外参数（如 --headless、--log-file=...）。

.PARAMETER KeepHostAfterLauncher
  **未指定**时：脚本从 **$PID** 起沿父链解析监视目标（见上文 **--exit-when-parent-pid** 说明）。**该进程退出后**宿主退出，**与单次脚本是否跑完无关**。若无法解析则不打该参数并给出警告。不需要此行为时请加 **-KeepHostAfterLauncher**。

.EXAMPLE
  # 脚本在 <工程根>\ai\tools\ 下时可省略 -ProjectRoot
  .\Invoke-SceMcp.ps1 -RequestJsonPath ".\trigger-mcp-call.example.json"

.EXAMPLE
  .\Invoke-SceMcp.ps1 -ProjectRoot "D:\Maps\MyMap" -RequestJsonPath ".\trigger-mcp-call.example.json"

.EXAMPLE
  .\Invoke-SceMcp.ps1 -Port 8765 -Tool "trigger_list_files" -ArgumentsJson '{"env":"Common"}'

.EXAMPLE
  .\Invoke-SceMcp.ps1 -HealthOnly
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $ProjectRoot,

    [Parameter(Mandatory = $false)]
    [int] $Port = 8765,

    [Parameter(Mandatory = $false)]
    [string] $RequestJsonPath,

    [Parameter(Mandatory = $false)]
    [string] $Tool,

    [Parameter(Mandatory = $false)]
    [string] $ArgumentsJson = "{}",

    [Parameter(Mandatory = $false)]
    [switch] $HealthOnly,

    [Parameter(Mandatory = $false)]
    [string] $McpMode = "http",

    [Parameter(Mandatory = $false)]
    [string[]] $HostExtraArgs = @(),

    [Parameter(Mandatory = $false)]
    [switch] $KeepHostAfterLauncher,

    [Parameter(Mandatory = $false)]
    [int] $HealthPollIntervalSec = 2,

    [Parameter(Mandatory = $false)]
    [int] $HealthTimeoutSec = 600
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-DefaultProjectRootFromScriptLocation {
    $scriptDir = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptDir)) { return $null }
    $toolsLeaf = Split-Path $scriptDir -Leaf
    $parentDir = Split-Path $scriptDir -Parent
    if ([string]::IsNullOrWhiteSpace($parentDir)) { return $null }
    $parentLeaf = Split-Path $parentDir -Leaf
    if (-not ($toolsLeaf -ieq 'tools')) { return $null }
    # 推荐：<MapRoot>\ai\tools\
    if ($parentLeaf -ieq 'ai') {
        $mapRoot = Split-Path $parentDir -Parent
        if ([string]::IsNullOrWhiteSpace($mapRoot)) { return $null }
        return [System.IO.Path]::GetFullPath($mapRoot)
    }
    # 兼容旧版：<MapRoot>\.cursor\tools\
    if ($parentLeaf -ieq '.cursor') {
        $mapRoot = Split-Path $parentDir -Parent
        if ([string]::IsNullOrWhiteSpace($mapRoot)) { return $null }
        return [System.IO.Path]::GetFullPath($mapRoot)
    }
    return $null
}

function Get-DefaultBaseUrl([int] $p) {
    return "http://127.0.0.1:$p/"
}

function Invoke-McpJsonRpc {
    param(
        [string] $BaseUrl,
        [object] $Payload
    )
    if (-not $BaseUrl.EndsWith("/")) { $BaseUrl = $BaseUrl + "/" }
    $uri = $BaseUrl.TrimEnd("/") + "/"
    $json = $Payload | ConvertTo-Json -Depth 30 -Compress
    return Invoke-RestMethod -Uri $uri -Method Post -ContentType "application/json; charset=utf-8" -Body $json
}

function ConvertTo-ArgumentsTable([object] $Obj) {
    if ($null -eq $Obj) { return @{} }
    if ($Obj -is [hashtable]) { return $Obj }
    if ($Obj -is [System.Collections.IDictionary]) {
        $out = @{}
        foreach ($k in $Obj.Keys) { $out[$k] = $Obj[$k] }
        return $out
    }
    $h = @{}
    foreach ($p in $Obj.PSObject.Properties) {
        $h[$p.Name] = $p.Value
    }
    return $h
}

function Test-HealthReady {
    param([object] $Health)
    if ($null -eq $Health) { return $false }
    $svc = $Health.service
    $mapReady = $Health.mapReady
    $phase = $Health.mapLoadPhase
    if ($svc -eq "sce-trigger-mcp-host") {
        return ($true -eq $mapReady) -or ($phase -eq "ready")
    }
    return ($true -eq $Health.ok)
}

function Test-IsFullEditorOnlyMcpToolName {
    param([string] $Name)
    if ([string]::IsNullOrWhiteSpace($Name)) { return $false }
    return (
        $Name -eq "editor_get_context" -or
        $Name.StartsWith("debug_", [StringComparison]::OrdinalIgnoreCase) -or
        $Name.StartsWith("runtime_", [StringComparison]::OrdinalIgnoreCase) -or
        $Name.StartsWith("resource_catalog_", [StringComparison]::OrdinalIgnoreCase) -or
        $Name.StartsWith("uiauthoring_", [StringComparison]::OrdinalIgnoreCase) -or
        $Name.StartsWith("ui_", [StringComparison]::OrdinalIgnoreCase)
    )
}

function Assert-RpcOk {
    param([object] $Rpc)
    if ($null -eq $Rpc) { throw "Empty JSON-RPC response." }
    $errProp = $Rpc.PSObject.Properties['error']
    if ($null -ne $errProp -and $null -ne $errProp.Value) {
        $errObj = $errProp.Value
        $msg = $errObj.message
        if ($null -eq $msg) { $msg = $errObj | ConvertTo-Json -Compress }
        throw "JSON-RPC error: $msg"
    }
}

function Get-Health {
    param([string] $Uri)
    try {
        return Invoke-RestMethod -Uri $Uri -Headers @{ Accept = "application/json" } -Method Get -TimeoutSec 10
    } catch {
        return $null
    }
}

# Get-CimInstance Win32_Process 可能返回 0/1/多行；对集合取 .Name 在 StrictMode 下会报「找不到 Name」。
function ConvertTo-SingleWin32ProcessRow {
    <#
    .SYNOPSIS
      将 Win32_Process 的 CIM 输出规范为单行 PSCustomObject 或 $null。
    #>
    param([object] $CimOutput)

    $items = @($CimOutput)
    if ($items.Count -eq 0) {
        return $null
    }
    $row = $items[0]
    if ($null -eq $row) {
        return $null
    }
    try {
        return [pscustomobject]@{
            Name              = [string]$row.Name
            ProcessId         = [int]$row.ProcessId
            ParentProcessId   = [int]$row.ParentProcessId
        }
    } catch {
        return $null
    }
}

function Get-McpHostWatchProcessRowFromCim {
    <#
    .SYNOPSIS
      指定 ProcessId 对应的一条 Win32_Process，或 $null。
    #>
    param([int] $ProcessId)

    if ($ProcessId -le 0) {
        return $null
    }
    try {
        $cimOut = Get-CimInstance -ClassName Win32_Process -Filter "ProcessId=$ProcessId" -ErrorAction Stop
        return ConvertTo-SingleWin32ProcessRow -CimOutput $cimOut
    } catch {
        return $null
    }
}

function Get-ExplorerProcessIdList {
    try {
        $cimOut = Get-CimInstance -ClassName Win32_Process -Filter "Name='explorer.exe'" -ErrorAction SilentlyContinue
        $list = New-Object System.Collections.Generic.List[int]
        foreach ($row in @($cimOut)) {
            if ($null -ne $row) { $list.Add([int]$row.ProcessId) | Out-Null }
        }
        return , $list.ToArray()
    } catch {
        return @()
    }
}

function Test-IsPowerShellHostProcessName {
    param([string] $Name)
    if ([string]::IsNullOrWhiteSpace($Name)) { return $false }
    $leaf = [System.IO.Path]::GetFileName($Name.Trim()).ToLowerInvariant()
    return ($leaf -eq 'powershell.exe' -or $leaf -eq 'pwsh.exe')
}

function Get-McpHostWatchProcessId {
    <#
    .SYNOPSIS
      从当前 Shell 沿父链解析供 TriggerMcpHost --exit-when-parent-pid 使用的 PID。
    .NOTES
      优先：本链上**父进程为 explorer.exe** 的那一进程（资源管理器之下的顶层一环）。
      否则：从起始 PID 沿父链**第一个非 PowerShell**（powershell.exe / pwsh.exe）进程。
    .PARAMETER ResolveProcessRow
      可选。调试/测试注入：{ param([int] $ProcessId) < PSCustomObject Name,ProcessId,ParentProcessId 或 $null > }
    #>
    param(
        [Parameter(Mandatory = $true)]
        [int] $StartProcessId,

        [Parameter(Mandatory = $false)]
        [scriptblock] $ResolveProcessRow = $null
    )

    if ($null -eq $ResolveProcessRow) {
        $ResolveProcessRow = { param([int] $p) Get-McpHostWatchProcessRowFromCim -ProcessId $p }
    }

    $explorerPids = @(Get-ExplorerProcessIdList)
    $firstNonPsPid = -1
    $currentPid = $StartProcessId
    $guard = 0
    while ($currentPid -gt 0 -and $guard -lt 256) {
        $guard++
        $proc = & $ResolveProcessRow $currentPid
        if ($null -eq $proc) {
            break
        }
        if ($firstNonPsPid -lt 0) {
            if (-not (Test-IsPowerShellHostProcessName -Name $proc.Name)) {
                $firstNonPsPid = [int]$proc.ProcessId
            }
        }
        $ppid = [int]$proc.ParentProcessId
        if ($explorerPids.Count -gt 0 -and ($explorerPids -contains $ppid)) {
            return [int]$proc.ProcessId
        }
        if ($ppid -le 0 -or $ppid -eq $currentPid) {
            break
        }
        $currentPid = $ppid
    }
    if ($firstNonPsPid -gt 0) {
        return $firstNonPsPid
    }
    return -1
}

function Resolve-TriggerMcpHostExe {
    param([string] $ExeDirectory)
    $probe = Join-Path $ExeDirectory "TriggerMcpHost.exe"
    if (Test-Path -LiteralPath $probe) {
        return (Get-Item -LiteralPath $probe).FullName
    }
    $hit = Get-ChildItem -LiteralPath $ExeDirectory -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -ieq "TriggerMcpHost.exe" } |
        Select-Object -First 1
    if ($null -eq $hit) {
        throw "TriggerMcpHost.exe not found in: $ExeDirectory"
    }
    return $hit.FullName
}

function Get-MapRootFromTriggerMcpHostCommandLine {
    param([string] $CommandLine)
    if ([string]::IsNullOrWhiteSpace($CommandLine)) { return $null }
    $m = [regex]::Match($CommandLine, '--map\s+(?:"([^"]+)"|(\S+))')
    if (-not $m.Success) { return $null }
    $raw = if ($m.Groups[1].Success) { $m.Groups[1].Value } else { $m.Groups[2].Value }
    if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
    try {
        return [System.IO.Path]::GetFullPath($raw)
    } catch {
        return $null
    }
}

function Get-McpPortFromTriggerMcpHostCommandLine {
    param([string] $CommandLine)
    if ([string]::IsNullOrWhiteSpace($CommandLine)) { return -1 }
    $m1 = [regex]::Match($CommandLine, '--mcp-port=(\d+)')
    if ($m1.Success) { return [int]$m1.Groups[1].Value }
    $m2 = [regex]::Match($CommandLine, '--mcp-port\s+(\d+)\b')
    if ($m2.Success) { return [int]$m2.Groups[1].Value }
    return -1
}

function Get-ExistingTriggerMcpHostPidForMapAndPort {
    <#
    .SYNOPSIS
      若已有 TriggerMcpHost.exe 且命令行中 --map 与 --mcp-port 与本次一致，返回其 PID；否则返回 -1。
    #>
    param(
        [string] $MapRoot,
        [int] $McpPort
    )
    if ([string]::IsNullOrWhiteSpace($MapRoot) -or $McpPort -le 0) {
        return -1
    }
    $wantMap = [System.IO.Path]::GetFullPath($MapRoot)
    try {
        $cimOut = Get-CimInstance -ClassName Win32_Process -Filter "Name='TriggerMcpHost.exe'" -ErrorAction SilentlyContinue
    } catch {
        return -1
    }
    foreach ($row in @($cimOut)) {
        if ($null -eq $row) { continue }
        $cmd = [string]$row.CommandLine
        $pidPort = Get-McpPortFromTriggerMcpHostCommandLine -CommandLine $cmd
        if ($pidPort -ne $McpPort) { continue }
        $pidMap = Get-MapRootFromTriggerMcpHostCommandLine -CommandLine $cmd
        if ($null -eq $pidMap) { continue }
        if ([string]::Equals($pidMap, $wantMap, [StringComparison]::OrdinalIgnoreCase)) {
            return [int]$row.ProcessId
        }
    }
    return -1
}

function Start-TriggerMcpHostProcess {
    param(
        [string] $MapRoot,
        [int] $McpPort,
        [string] $McpModeValue,
        [string[]] $ExtraArgs,
        [int] $ExitWhenParentPid = -1
    )
    $mapRoot = [System.IO.Path]::GetFullPath($MapRoot)
    if (-not (Test-Path -LiteralPath $mapRoot)) {
        throw "ProjectRoot does not exist: $mapRoot"
    }
    $projectSce = Join-Path $mapRoot "project.sce"
    if (-not (Test-Path -LiteralPath $projectSce)) {
        Write-Warning "[Invoke-SceMcp] project.sce not found under ProjectRoot; confirm this is the SCE map root."
    }
    $rootFile = Join-Path (Join-Path $mapRoot "docs") ".editor-root"
    if (-not (Test-Path -LiteralPath $rootFile)) {
        throw "File not found (required to locate TriggerMcpHost): $rootFile"
    }
    $firstLine = (Get-Content -LiteralPath $rootFile -TotalCount 1 -Encoding UTF8).Trim()
    if ([string]::IsNullOrWhiteSpace($firstLine)) {
        throw "First line of docs\.editor-root is empty."
    }
    $exeDir = $firstLine.Trim('"')
    if (-not (Test-Path -LiteralPath $exeDir)) {
        throw "Cannot resolve directory from docs\.editor-root: $exeDir"
    }
    $exeDirFull = [System.IO.Path]::GetFullPath($exeDir)
    $exePath = Resolve-TriggerMcpHostExe -ExeDirectory $exeDirFull

    $argList = [System.Collections.Generic.List[string]]::new()
    $argList.Add("--map")
    $argList.Add($mapRoot)
    $argList.Add("--mcp-port=$McpPort")
    $argList.Add("--mcp-mode=$McpModeValue")
    if ($ExitWhenParentPid -gt 0) {
        $argList.Add("--exit-when-parent-pid=$ExitWhenParentPid")
    }
    foreach ($a in $ExtraArgs) {
        if (-not [string]::IsNullOrWhiteSpace($a)) { $argList.Add($a) }
    }

    Write-Host "[Invoke-SceMcp] Starting: $exePath $($argList -join ' ')"
    Start-Process -FilePath $exePath -ArgumentList $argList -WorkingDirectory $exeDirFull -WindowStyle Hidden
}

# --- 解析为单次 tools/call ---
$baseUrl = Get-DefaultBaseUrl -p $Port
[string] $invokeTool = $null
$invokeArguments = $null

if ($RequestJsonPath) {
    if (-not (Test-Path -LiteralPath $RequestJsonPath)) {
        throw "RequestJsonPath not found: $RequestJsonPath"
    }
    $doc = Get-Content -LiteralPath $RequestJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $pBase = $doc.PSObject.Properties['baseUrl']
    if ($null -ne $pBase -and $pBase.Value) { $baseUrl = [string]$pBase.Value }

    $pCalls = $doc.PSObject.Properties['calls']
    if ($null -ne $pCalls -and $null -ne $pCalls.Value -and @($pCalls.Value).Count -gt 0) {
        throw "Request JSON 'calls' (batch) is no longer supported. Use one 'tool' + 'arguments' per JSON file and run the script once per MCP tool call."
    }
    $pTool = $doc.PSObject.Properties['tool']
    if ($null -ne $pTool -and $pTool.Value) {
        $invokeTool = [string]$pTool.Value
        $pArgs = $doc.PSObject.Properties['arguments']
        $invokeArguments = if ($null -ne $pArgs -and $null -ne $pArgs.Value) { $pArgs.Value } else { @{} }
    } else {
        if (-not $HealthOnly) {
            throw "Request JSON must contain 'tool' (or use -HealthOnly)."
        }
    }
} else {
    if ($HealthOnly) {
        # 无 JSON、无 Tool：仅健康检查 / 起 Host
    } elseif ([string]::IsNullOrWhiteSpace($Tool)) {
        throw "Specify -RequestJsonPath, or -Tool, or -HealthOnly."
    } else {
        try {
            $invokeArguments = $ArgumentsJson | ConvertFrom-Json
        } catch {
            throw "ArgumentsJson is not valid JSON: $($_.Exception.Message)"
        }
        $invokeTool = $Tool
    }
}

if (-not $HealthOnly -and [string]::IsNullOrWhiteSpace($invokeTool)) {
    throw "No tool to invoke. Use -HealthOnly for probe-only, or add 'tool' in JSON / use -Tool."
}

if (-not $HealthOnly -and (Test-IsFullEditorOnlyMcpToolName -Name $invokeTool)) {
    throw "Tool '$invokeTool' requires the full SCE Editor MCP. Use ai/tools/Invoke-SceEditorMcp.ps1 against an editor started with --mcp-port; this Trigger/Data helper refuses to start or use TriggerMcpHost for full-editor-only tools."
}

[string] $mapRootForHost = $ProjectRoot
if ([string]::IsNullOrWhiteSpace($mapRootForHost)) {
    $mapRootForHost = Get-DefaultProjectRootFromScriptLocation
}

$healthUri = ($baseUrl.TrimEnd("/")) + "/health"
$launchPort = $Port
if ($baseUrl -match ':\d+') {
    try { $launchPort = ([Uri]($baseUrl.TrimEnd("/"))).Port } catch { }
}
$started = Get-Date

# --- 探测：无 HTTP 健康响应则视为需起 Host ---
$health = Get-Health -Uri $healthUri
$needStart = ($null -eq $health)

if ($needStart -and -not [string]::IsNullOrWhiteSpace($mapRootForHost)) {
    $existingHostPid = Get-ExistingTriggerMcpHostPidForMapAndPort -MapRoot $mapRootForHost -McpPort $launchPort
    if ($existingHostPid -gt 0) {
        throw "TriggerMcpHost already running (PID $existingHostPid) for this map and MCP port $launchPort; refusing to start another. If /health at $healthUri is still unreachable, wait for startup, fix the port/baseUrl, or exit the existing host."
    }
    $watchPid = -1
    if (-not $KeepHostAfterLauncher) {
        $watchPid = Get-McpHostWatchProcessId -StartProcessId $PID
        if ($watchPid -le 0) {
            Write-Warning "[Invoke-SceMcp] Could not resolve watch process from `$PID parent chain (explorer-direct-child or first non-PowerShell); TriggerMcpHost will not auto-exit when launcher closes."
        } else {
            Write-Host "[Invoke-SceMcp] Host will exit when watch PID $watchPid (explorer-top-level on chain, else first non-PowerShell ancestor) exits."
        }
    }
    Start-TriggerMcpHostProcess -MapRoot $mapRootForHost -McpPort $launchPort -McpModeValue $McpMode -ExtraArgs $HostExtraArgs -ExitWhenParentPid $watchPid
    Start-Sleep -Seconds 2
} elseif ($needStart -and [string]::IsNullOrWhiteSpace($mapRootForHost)) {
    throw "MCP not reachable at $healthUri and SCE map root not resolved (pass -ProjectRoot, or run this script from <MapRoot>\ai\tools\ or legacy <MapRoot>\.cursor\tools\); start TriggerMcpHost manually."
} else {
    Write-Host "[Invoke-SceMcp] MCP already responding at $healthUri (will wait for mapReady if needed)."
}

# --- 轮询直至可调用 ---
while ($true) {
    $health = Get-Health -Uri $healthUri
    if ($null -eq $health) {
        if (((Get-Date) - $started).TotalSeconds -gt $HealthTimeoutSec) {
            throw "Timeout waiting for MCP HTTP at $healthUri"
        }
        Start-Sleep -Seconds $HealthPollIntervalSec
        continue
    }
    if (-not ($health.ok)) {
        $err = $health.mapLoadError
        throw "Health reports ok=false (mapLoadError: $err)"
    }
    if (Test-HealthReady -Health $health) { break }
    if (((Get-Date) - $started).TotalSeconds -gt $HealthTimeoutSec) {
        throw "Timeout waiting for mapReady / healthy MCP (last mapLoadPhase=$($health.mapLoadPhase))"
    }
    Write-Host "[Invoke-SceMcp] Waiting for map (phase=$($health.mapLoadPhase), mapReady=$($health.mapReady))..."
    Start-Sleep -Seconds $HealthPollIntervalSec
}

if ($HealthOnly) {
    Write-Host "[Invoke-SceMcp] Health OK. Last snapshot:"
    $health | ConvertTo-Json -Depth 10
    exit 0
}

# --- 单次 tools/call（多步请多次运行脚本，勿在同一 JSON 内批量） ---
$argTable = ConvertTo-ArgumentsTable -Obj $invokeArguments
$body = [ordered]@{
    jsonrpc = "2.0"
    id      = 1
    method  = "tools/call"
    params  = [ordered]@{
        name      = $invokeTool
        arguments = $argTable
    }
}
Write-Host "[Invoke-SceMcp] tools/call: $invokeTool"
$rpcResult = Invoke-McpJsonRpc -BaseUrl $baseUrl -Payload $body
Assert-RpcOk -Rpc $rpcResult

[ordered]@{
    mcpBaseUrl = $baseUrl.TrimEnd("/")
    tool       = $invokeTool
    result     = $rpcResult.result
} | ConvertTo-Json -Depth 40

