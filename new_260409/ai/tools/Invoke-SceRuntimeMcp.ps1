#requires -Version 5.1
<#
.SYNOPSIS
  Calls the shared SCE Runtime MCP fallback client.

.DESCRIPTION
  Use this only when the AI environment cannot see the editor MCP tool
  `runtime_call_tool`. This project-side file is a thin shim; the versioned
  protocol implementation remains in WasiCoreSDK/tools/runtime-mcp.

.EXAMPLE
  .\Invoke-SceRuntimeMcp.ps1 -Ping -Wait

.EXAMPLE
  .\Invoke-SceRuntimeMcp.ps1 -Tool debug.capture_screenshot -ArgumentsJson '{"path":"RuntimeMcpScreenshots/ui.png","overwrite":true}' -Wait

.EXAMPLE
  .\Invoke-SceRuntimeMcp.ps1 -ClientVersion
#>
[CmdletBinding(DefaultParameterSetName = "Tool")]
param(
    [Parameter(Mandatory = $false)]
    [string] $HostName = "127.0.0.1",

    [Parameter(Mandatory = $false)]
    [ValidateRange(1, 65535)]
    [int] $Port = 18765,

    [Parameter(Mandatory = $true, ParameterSetName = "Tool")]
    [string] $Tool,

    [Parameter(Mandatory = $false, ParameterSetName = "Tool")]
    [string] $ArgumentsJson = "{}",

    [Parameter(Mandatory = $true, ParameterSetName = "RequestFile")]
    [string] $RequestJsonPath,

    [Parameter(Mandatory = $true, ParameterSetName = "Ping")]
    [switch] $Ping,

    [Parameter(Mandatory = $true, ParameterSetName = "ListTools")]
    [switch] $ListTools,

    [Parameter(Mandatory = $true, ParameterSetName = "ClientVersion")]
    [switch] $ClientVersion,

    [Parameter(Mandatory = $false)]
    [ValidateRange(500, 30000)]
    [int] $TimeoutMs = 5000,

    [Parameter(Mandatory = $false)]
    [switch] $Wait,

    [Parameter(Mandatory = $false)]
    [ValidateRange(1, 300)]
    [int] $WaitTimeoutSec = 30,

    [Parameter(Mandatory = $false)]
    [ValidateRange(100, 10000)]
    [int] $WaitPollIntervalMs = 500,

    [Parameter(Mandatory = $false)]
    [switch] $Pretty
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Find-SceRuntimeMcpClient {
    $sdkLocalCandidate = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\..\tools\runtime-mcp\Invoke-SceRuntimeMcpClient.ps1"))
    if (Test-Path -LiteralPath $sdkLocalCandidate -PathType Leaf) {
        return $sdkLocalCandidate
    }

    if (-not [string]::IsNullOrWhiteSpace($env:WASI_CORE_SDK_PATH)) {
        $environmentCandidate = Join-Path $env:WASI_CORE_SDK_PATH "tools\runtime-mcp\Invoke-SceRuntimeMcpClient.ps1"
        if (Test-Path -LiteralPath $environmentCandidate -PathType Leaf) {
            return [System.IO.Path]::GetFullPath($environmentCandidate)
        }
    }

    $toolsParent = Split-Path -Parent $PSScriptRoot
    $projectRoot = Split-Path -Parent $toolsParent
    $propsPath = Join-Path $projectRoot "src\WasiCoreSDK.props"
    if (Test-Path -LiteralPath $propsPath -PathType Leaf) {
        try {
            [xml]$props = Get-Content -LiteralPath $propsPath -Raw -Encoding UTF8
            $sdkPathNode = @($props.Project.PropertyGroup.WasiCoreSDKPath)[0]
            if ($null -ne $sdkPathNode -and -not [string]::IsNullOrWhiteSpace([string]$sdkPathNode.InnerText)) {
                $sharedCandidate = Join-Path ([string]$sdkPathNode.InnerText) "tools\runtime-mcp\Invoke-SceRuntimeMcpClient.ps1"
                if (Test-Path -LiteralPath $sharedCandidate -PathType Leaf) {
                    return [System.IO.Path]::GetFullPath($sharedCandidate)
                }
            }
        } catch {
            throw "Failed to resolve the shared Runtime MCP client from '$propsPath'. $($_.Exception.Message)"
        }
    }

    throw "Shared Runtime MCP client not found. Refresh AI context with the current WasiCoreSDK, or verify src/WasiCoreSDK.props."
}

try {
    $clientPath = Find-SceRuntimeMcpClient
} catch {
    [ordered]@{
        success = $false
        error_code = "runtime_mcp_client_not_found"
        stage = "client_resolution"
        message = $_.Exception.Message
    } | ConvertTo-Json -Compress
    exit 1
}

try {
    & $clientPath @PSBoundParameters
    if (Test-Path -LiteralPath variable:LASTEXITCODE) {
        exit ([int]$LASTEXITCODE)
    }
    exit 0
} catch {
    $stage = [string]$_.Exception.Data["SceRuntimeMcpStage"]
    if ([string]::IsNullOrWhiteSpace($stage)) {
        $stage = "client_invocation"
    }
    [ordered]@{
        success = $false
        error_code = "runtime_mcp_client_invocation_failed"
        stage = $stage
        message = $_.Exception.Message
    } | ConvertTo-Json -Compress
    exit 1
}
