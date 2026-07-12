[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ChangeName,

    [Parameter()]
    [string]$ProjectPath = ".",

    [Parameter()]
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-KebabCase {
    param([string]$Value)
    return $Value -match "^[a-z0-9]+(?:-[a-z0-9]+)*$"
}

function Assert-CommandAvailable {
    param([string]$CommandName)
    if (-not (Get-Command -Name $CommandName -ErrorAction SilentlyContinue)) {
        throw "Missing required command: $CommandName"
    }
}

function Invoke-Checked {
    param(
        [string]$Executable,
        [string[]]$Arguments,
        [string]$Label
    )

    if ($DryRun) {
        Write-Host "[DryRun] $Executable $($Arguments -join ' ')"
        return
    }

    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Label failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-KebabCase -Value $ChangeName)) {
    throw "Change name must be kebab-case, for example: auto-army-progression-ui-loop"
}

Assert-CommandAvailable -CommandName "ospec"

$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
$changeRelativePath = "changes/active/$ChangeName"

Push-Location -LiteralPath $projectRoot
try {
    if (Test-Path -LiteralPath $changeRelativePath) {
        Write-Host "Change already exists: $changeRelativePath"
    }
    else {
        Invoke-Checked -Executable "ospec" -Arguments @("new", $ChangeName, ".") -Label "ospec new"
    }

    Invoke-Checked -Executable "ospec" -Arguments @("progress", $changeRelativePath) -Label "ospec progress"
    Write-Host "Change ready: $changeRelativePath"
}
finally {
    Pop-Location
}
