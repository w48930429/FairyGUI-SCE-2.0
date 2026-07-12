[CmdletBinding()]
param(
    [Parameter()]
    [string]$ProjectPath = ".",

    [Parameter()]
    [string]$ChangePath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$verifyScript = Join-Path -Path $PSScriptRoot -ChildPath "verify-change.ps1"
if (-not (Test-Path -LiteralPath $verifyScript)) {
    throw "verify-change.ps1 not found under scripts/."
}

$verifyParameters = @{
    ProjectPath = $ProjectPath
}

if (-not [string]::IsNullOrWhiteSpace($ChangePath)) {
    $verifyParameters.ChangePath = $ChangePath
}

if ($env:SKIP_LINT_IN_PRECOMMIT -eq "1") {
    $verifyParameters.SkipLint = $true
}

if ($env:SKIP_INDEX_IN_PRECOMMIT -eq "1") {
    $verifyParameters.SkipIndex = $true
}

if ($env:SKIP_OSPEC_VERIFY_IN_PRECOMMIT -eq "1") {
    $verifyParameters.SkipOspecVerify = $true
}

if ($env:SKIP_DOTNET_IN_PRECOMMIT -eq "1") {
    $verifyParameters.SkipDotnet = $true
}

& $verifyScript @verifyParameters
