[CmdletBinding()]
param(
    [Parameter()]
    [string]$ProjectPath = ".",

    [Parameter()]
    [string]$ChangePath = "",

    [Parameter()]
    [switch]$SkipLint,

    [Parameter()]
    [switch]$SkipIndex,

    [Parameter()]
    [switch]$SkipOspecVerify,

    [Parameter()]
    [switch]$SkipDotnet,

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

function Resolve-TargetChange {
    param([string]$ProjectRoot)

    if (-not [string]::IsNullOrWhiteSpace($ChangePath)) {
        $candidatePath = if ([System.IO.Path]::IsPathRooted($ChangePath)) {
            $ChangePath
        }
        else {
            Join-Path -Path $ProjectRoot -ChildPath $ChangePath
        }

        if (-not (Test-Path -LiteralPath $candidatePath)) {
            throw "Change path does not exist: $ChangePath"
        }

        return [System.IO.Path]::GetRelativePath($ProjectRoot, (Resolve-Path -LiteralPath $candidatePath).Path).Replace("\", "/")
    }

    $activeRoot = Join-Path -Path $ProjectRoot -ChildPath "changes/active"
    if (-not (Test-Path -LiteralPath $activeRoot)) {
        throw "Directory not found: changes/active"
    }

    $activeChanges = @(Get-ChildItem -LiteralPath $activeRoot -Directory | Sort-Object -Property Name)
    if ($activeChanges.Count -eq 0) {
        throw "No active changes found. Pass -ChangePath explicitly."
    }

    if ($activeChanges.Count -gt 1) {
        $names = $activeChanges.Name -join ", "
        throw "Multiple active changes found: $names. Pass -ChangePath explicitly."
    }

    return "changes/active/$($activeChanges[0].Name)"
}

function Invoke-LintIfConfigured {
    param([string]$ProjectRoot)

    $packageJsonPath = Join-Path -Path $ProjectRoot -ChildPath "package.json"
    if (-not (Test-Path -LiteralPath $packageJsonPath)) {
        Write-Host "Lint skipped: package.json not found."
        return
    }

    Assert-CommandAvailable -CommandName "npm"

    $package = Get-Content -LiteralPath $packageJsonPath -Raw | ConvertFrom-Json
    if (-not $package.scripts -or -not $package.scripts.lint) {
        Write-Host "Lint skipped: no npm lint script."
        return
    }

    Invoke-Checked -Executable "npm" -Arguments @("run", "lint") -Label "npm run lint"
}

Assert-CommandAvailable -CommandName "ospec"
Assert-CommandAvailable -CommandName "dotnet"

$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
$targetChange = Resolve-TargetChange -ProjectRoot $projectRoot
$buildIndexScript = Join-Path -Path $projectRoot -ChildPath "build-index-auto.js"

Push-Location -LiteralPath $projectRoot
try {
    if (-not $SkipDotnet) {
        Invoke-Checked -Executable "dotnet" -Arguments @("test", "tests/AutoArmy.Shared.Tests/AutoArmy.Shared.Tests.csproj") -Label "dotnet test"
        Invoke-Checked -Executable "dotnet" -Arguments @("build", "src/GameEntry.csproj", "-c", "Server-Debug") -Label "dotnet build Server-Debug"
        Invoke-Checked -Executable "dotnet" -Arguments @("build", "src/GameEntry.csproj", "-c", "Client-Debug") -Label "dotnet build Client-Debug"
    }

    if (-not $SkipLint) {
        Invoke-LintIfConfigured -ProjectRoot $projectRoot
    }

    if (-not $SkipIndex) {
        if (Test-Path -LiteralPath $buildIndexScript) {
            Assert-CommandAvailable -CommandName "node"
            Invoke-Checked -Executable "node" -Arguments @("build-index-auto.js") -Label "node build-index-auto.js"
        }
        else {
            Write-Host "Index generation skipped: build-index-auto.js not found."
        }
    }

    if (-not $SkipOspecVerify) {
        Invoke-Checked -Executable "ospec" -Arguments @("verify", $targetChange) -Label "ospec verify"
    }

    Invoke-Checked -Executable "ospec" -Arguments @("changes", "status", ".") -Label "ospec changes status"
    Write-Host "Verification completed for: $targetChange"
}
finally {
    Pop-Location
}
