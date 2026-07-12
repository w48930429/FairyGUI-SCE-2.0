[CmdletBinding()]
param(
    [Parameter()]
    [string]$ProjectPath = ".",

    [Parameter()]
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

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

Assert-CommandAvailable -CommandName "git"

$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
$gitDirectory = Join-Path -Path $projectRoot -ChildPath ".git"
$hookScript = Join-Path -Path $projectRoot -ChildPath ".githooks/pre-commit"

if (-not (Test-Path -LiteralPath $gitDirectory)) {
    throw "Not a git repository root: $projectRoot"
}

if (-not (Test-Path -LiteralPath $hookScript)) {
    throw "Hook script not found: .githooks/pre-commit"
}

Push-Location -LiteralPath $projectRoot
try {
    Invoke-Checked -Executable "git" -Arguments @("config", "core.hooksPath", ".githooks") -Label "git config core.hooksPath"
    Write-Host "Git hook path set to .githooks"
}
finally {
    Pop-Location
}
