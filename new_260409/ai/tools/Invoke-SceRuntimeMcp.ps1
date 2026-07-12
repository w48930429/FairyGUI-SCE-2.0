#requires -Version 5.1
<#
.SYNOPSIS
  Calls the SCE client Runtime MCP TCP bridge once.

.DESCRIPTION
  This is the fallback path when the AI tool environment cannot see the
  editor MCP bridge tool `runtime_call_tool`. The client Runtime MCP bridge is
  not a standard MCP tools/list endpoint. It accepts one JSON request followed
  by the SCE delimiter "|*|`n".

.EXAMPLE
  .\Invoke-SceRuntimeMcp.ps1 -Ping

.EXAMPLE
  .\Invoke-SceRuntimeMcp.ps1 -ListTools

.EXAMPLE
  .\Invoke-SceRuntimeMcp.ps1 -Tool debug.capture_screenshot -ArgumentsJson '{"path":"RuntimeMcpScreenshots/ui.png","overwrite":true,"maxWidth":1280,"maxHeight":720}'

.EXAMPLE
  .\Invoke-SceRuntimeMcp.ps1 -Tool card_ui.snapshot -ArgumentsJson '{}' -Wait
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

$Delimiter = "|*|`n"

function ConvertFrom-JsonObject {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Json,

        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    try {
        $value = $Json | ConvertFrom-Json
    } catch {
        throw "$Name must be valid JSON. $($_.Exception.Message)"
    }

    if ($null -eq $value) {
        return [pscustomobject]@{}
    }

    return $value
}

function ConvertTo-Hashtable {
    param([object] $Value)

    if ($null -eq $Value) {
        return @{}
    }

    if ($Value -is [hashtable]) {
        return $Value
    }

    if ($Value -is [System.Collections.IDictionary]) {
        $table = @{}
        foreach ($key in $Value.Keys) {
            $table[$key] = ConvertTo-Hashtable -Value $Value[$key]
        }
        return $table
    }

    if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [string])) {
        $items = New-Object System.Collections.Generic.List[object]
        foreach ($item in $Value) {
            $items.Add((ConvertTo-Hashtable -Value $item))
        }
        return $items.ToArray()
    }

    $properties = $Value.PSObject.Properties
    if ($properties.Count -gt 0 -and $Value.GetType().Namespace -ne "System") {
        $table = @{}
        foreach ($property in $properties) {
            $table[$property.Name] = ConvertTo-Hashtable -Value $property.Value
        }
        return $table
    }

    return $Value
}

function Test-RuntimeMcpPort {
    param(
        [string] $HostName,
        [int] $Port,
        [int] $ConnectTimeoutMs
    )

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $task = $client.ConnectAsync($HostName, $Port)
        return $task.Wait($ConnectTimeoutMs) -and $client.Connected
    } catch {
        return $false
    } finally {
        $client.Dispose()
    }
}

function Wait-RuntimeMcpPort {
    param(
        [string] $HostName,
        [int] $Port,
        [int] $TimeoutSec,
        [int] $PollIntervalMs
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSec)
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-RuntimeMcpPort -HostName $HostName -Port $Port -ConnectTimeoutMs ([Math]::Min($PollIntervalMs, 1000))) {
            return
        }
        Start-Sleep -Milliseconds $PollIntervalMs
    }

    throw "Client Runtime MCP is not reachable on ${HostName}:${Port} after ${TimeoutSec}s."
}

function New-RuntimeRequest {
    param(
        [string] $ToolName,
        [object] $Arguments
    )

    return [ordered]@{
        id     = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
        method = "runtime.call_tool"
        params = [ordered]@{
            name      = $ToolName
            arguments = $Arguments
        }
    }
}

function Resolve-RequestPayload {
    if ($PSCmdlet.ParameterSetName -eq "Ping") {
        return New-RuntimeRequest -ToolName "debug.ping" -Arguments @{}
    }

    if ($PSCmdlet.ParameterSetName -eq "ListTools") {
        return New-RuntimeRequest -ToolName "debug.list_tools" -Arguments @{}
    }

    if ($PSCmdlet.ParameterSetName -eq "Tool") {
        $arguments = ConvertFrom-JsonObject -Json $ArgumentsJson -Name "ArgumentsJson"
        return New-RuntimeRequest -ToolName $Tool -Arguments (ConvertTo-Hashtable -Value $arguments)
    }

    $resolvedPath = [System.IO.Path]::GetFullPath($RequestJsonPath)
    if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
        throw "RequestJsonPath does not exist: $resolvedPath"
    }

    $requestObject = ConvertFrom-JsonObject -Json (Get-Content -LiteralPath $resolvedPath -Raw -Encoding UTF8) -Name "RequestJsonPath"
    $request = ConvertTo-Hashtable -Value $requestObject

    if ($request.ContainsKey("method")) {
        return $request
    }

    if (-not $request.ContainsKey("name")) {
        throw "RequestJsonPath must contain either a full runtime request with 'method', or a compact object with 'name' and optional 'arguments'."
    }

    $arguments = @{}
    if ($request.ContainsKey("arguments")) {
        $arguments = $request["arguments"]
    }

    return New-RuntimeRequest -ToolName ([string]$request["name"]) -Arguments $arguments
}

function Invoke-RuntimeMcpTcp {
    param(
        [string] $HostName,
        [int] $Port,
        [object] $Payload,
        [int] $TimeoutMs
    )

    $json = $Payload | ConvertTo-Json -Depth 50 -Compress
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $connectTask = $client.ConnectAsync($HostName, $Port)
        if (-not $connectTask.Wait($TimeoutMs)) {
            throw "Timed out connecting to ${HostName}:${Port} after ${TimeoutMs}ms."
        }

        $stream = $client.GetStream()
        $stream.ReadTimeout = $TimeoutMs
        $stream.WriteTimeout = $TimeoutMs

        $requestBytes = [System.Text.Encoding]::UTF8.GetBytes($json + $Delimiter)
        $stream.Write($requestBytes, 0, $requestBytes.Length)
        $stream.Flush()

        $responseBytes = New-Object System.Collections.Generic.List[byte]
        $buffer = New-Object byte[] 4096
        while ($true) {
            $read = $stream.Read($buffer, 0, $buffer.Length)
            if ($read -le 0) {
                throw "Client Runtime MCP connection closed before a response was received."
            }

            for ($i = 0; $i -lt $read; $i++) {
                $responseBytes.Add($buffer[$i])
            }

            $text = [System.Text.Encoding]::UTF8.GetString($responseBytes.ToArray())
            $index = $text.IndexOf($Delimiter, [StringComparison]::Ordinal)
            if ($index -ge 0) {
                return $text.Substring(0, $index)
            }
        }
    } finally {
        $client.Dispose()
    }
}

if ($Wait) {
    Wait-RuntimeMcpPort -HostName $HostName -Port $Port -TimeoutSec $WaitTimeoutSec -PollIntervalMs $WaitPollIntervalMs
}

$payload = Resolve-RequestPayload
$responseText = Invoke-RuntimeMcpTcp -HostName $HostName -Port $Port -Payload $payload -TimeoutMs $TimeoutMs

if ($Pretty) {
    try {
        $responseText | ConvertFrom-Json | ConvertTo-Json -Depth 50
    } catch {
        $responseText
    }
} else {
    $responseText
}

try {
    $response = $responseText | ConvertFrom-Json
    if ($null -ne $response -and $response.PSObject.Properties["success"] -and $response.success -eq $false) {
        exit 2
    }
} catch {
    # Keep raw output usable even if the response is not JSON.
}
