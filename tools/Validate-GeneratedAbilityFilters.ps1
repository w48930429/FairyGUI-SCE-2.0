param(
    [string]$AbilityDir = (Join-Path $PSScriptRoot "..\\src\\DataGenerated\\GameEntry\\ScopeData\\GameDataAbilityExecute")
)

$ErrorActionPreference = "Stop"

function Get-LineNumber {
    param(
        [string]$Text,
        [int]$Index
    )
    if ($Index -lt 0) {
        return 1
    }
    return ([regex]::Matches($Text.Substring(0, $Index), "`n")).Count + 1
}

function Remove-CommentsAndSeparators {
    param([string]$Text)
    $noBlock = [regex]::Replace($Text, "/\*.*?\*/", "", [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $noLine = [regex]::Replace($noBlock, "//.*?$", "", [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $noComma = $noLine -replace ",", ""
    return $noComma.Trim()
}

if (-not (Test-Path $AbilityDir)) {
    Write-Error "Ability directory not found: $AbilityDir"
    exit 2
}

$files = Get-ChildItem -Path $AbilityDir -Filter *.cs -File | Sort-Object Name
if ($files.Count -eq 0) {
    Write-Error "No generated ability files found under: $AbilityDir"
    exit 2
}

$issues = New-Object System.Collections.Generic.List[object]

foreach ($file in $files) {
    $raw = Get-Content -Raw -Path $file.FullName

    $commentMatches = [regex]::Matches($raw, "/\*\s*""[^""]+""\s*\*/")
    foreach ($m in $commentMatches) {
        $line = Get-LineNumber -Text $raw -Index $m.Index
        $issues.Add([pscustomobject]@{
                File = $file.FullName
                Line = $line
                Rule = "COMMENT_ENUM_IN_GENERATED_CODE"
                Detail = "Found commented enum token in generated ability filter: $($m.Value)"
            })
    }

    $isAttack = [regex]::IsMatch($raw, "IsAttack\s*=\s*true")
    $isUnitTarget = [regex]::IsMatch($raw, "TargetType\s*=\s*.*AbilityTargetType\.Unit")
    if (-not ($isAttack -and $isUnitTarget)) {
        continue
    }

    $requiredMatches = [regex]::Matches($raw, "Required\s*=\s*\[(?<body>.*?)\]", [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($requiredMatches.Count -eq 0) {
        $issues.Add([pscustomobject]@{
                File = $file.FullName
                Line = 1
                Rule = "MISSING_REQUIRED_FILTER_FOR_ATTACK_UNIT_ABILITY"
                Detail = "Attack+Unit ability has no Required filter."
            })
        continue
    }

    $hasValidRequired = $false
    foreach ($match in $requiredMatches) {
        $body = $match.Groups["body"].Value
        $clean = Remove-CommentsAndSeparators -Text $body
        $relationshipTokens = [regex]::Matches($clean, "UnitRelationship\.(Self|Player|Alliance|Neutral|Enemy|Visible)") |
            ForEach-Object { $_.Groups[1].Value } |
            Select-Object -Unique
        if ($relationshipTokens.Count -gt 1) {
            $line = Get-LineNumber -Text $raw -Index $match.Index
            $issues.Add([pscustomobject]@{
                    File = $file.FullName
                    Line = $line
                    Rule = "MULTI_RELATIONSHIP_REQUIRED_IN_SINGLE_FILTER"
                    Detail = "Single Required filter contains multiple relationship tags (AND semantics): $($relationshipTokens -join ', ')"
                })
        }

        if (-not [string]::IsNullOrWhiteSpace($clean)) {
            $hasValidRequired = $true
            break
        }
    }

    if (-not $hasValidRequired) {
        $line = Get-LineNumber -Text $raw -Index $requiredMatches[0].Index
        $issues.Add([pscustomobject]@{
                File = $file.FullName
                Line = $line
                Rule = "EMPTY_REQUIRED_FILTER_FOR_ATTACK_UNIT_ABILITY"
                Detail = "Attack+Unit ability Required filter is empty after removing comments."
            })
    }
}

if ($issues.Count -gt 0) {
    Write-Host "[FAIL] Generated ability filter validation failed. issueCount=$($issues.Count)"
    foreach ($issue in $issues) {
        Write-Host ("- {0}:{1} [{2}] {3}" -f $issue.File, $issue.Line, $issue.Rule, $issue.Detail)
    }
    exit 1
}

Write-Host "[PASS] Generated ability filter validation passed. files=$($files.Count)"
exit 0
