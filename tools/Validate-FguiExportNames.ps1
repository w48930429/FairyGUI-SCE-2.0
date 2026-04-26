param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot "..\rpg_3d_2604140"),
    [string]$AssetsDir
)

$ErrorActionPreference = "Stop"
$ProjectRoot = (Resolve-Path $ProjectRoot).Path
if (-not $AssetsDir) {
    $AssetsDir = Join-Path $ProjectRoot "..\UIProject\assets"
}

$identifierPattern = '^[A-Za-z_][A-Za-z0-9_]*$'
$keywords = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
@(
    'abstract','as','base','bool','break','byte','case','catch','char','checked','class','const','continue',
    'decimal','default','delegate','do','double','else','enum','event','explicit','extern','false','finally',
    'fixed','float','for','foreach','goto','if','implicit','in','int','interface','internal','is','lock','long',
    'namespace','new','null','object','operator','out','override','params','private','protected','public',
    'readonly','ref','return','sbyte','sealed','short','sizeof','stackalloc','static','string','struct','switch',
    'this','throw','true','try','typeof','uint','ulong','unchecked','unsafe','ushort','using','virtual','void',
    'volatile','while'
) | ForEach-Object { [void]$keywords.Add($_) }

function Get-SuggestedName {
    param([string]$Name)
    $clean = [regex]::Replace($Name, '[^A-Za-z0-9_]', '_')
    if ([string]::IsNullOrWhiteSpace($clean)) {
        $clean = 'Field'
    }
    if ($clean -match '^[0-9]') {
        $clean = '_' + $clean
    }
    if ($keywords.Contains($clean)) {
        $clean = $clean + 'Field'
    }
    return $clean
}

function Is-ValidIdentifier {
    param([string]$Name)
    if ([string]::IsNullOrWhiteSpace($Name)) {
        return $false
    }
    if ($Name -notmatch $identifierPattern) {
        return $false
    }
    return -not $keywords.Contains($Name)
}

function Resolve-LineNumber {
    param(
        [string[]]$Lines,
        [string]$Token
    )
    for ($i = 0; $i -lt $Lines.Length; $i++) {
        if ($Lines[$i].Contains($Token)) {
            return ($i + 1)
        }
    }
    return 1
}

if (-not (Test-Path $AssetsDir)) {
    Write-Error "FGUI assets directory not found: $AssetsDir"
    exit 2
}

$files = Get-ChildItem -Path $AssetsDir -Recurse -Filter *.xml -File |
    Where-Object { $_.Name -ne 'package.xml' } |
    Sort-Object FullName

if ($files.Count -eq 0) {
    Write-Error "No FGUI XML files found under: $AssetsDir"
    exit 2
}

$issues = New-Object System.Collections.Generic.List[object]

foreach ($file in $files) {
    [xml]$doc = Get-Content -Path $file.FullName
    $lines = Get-Content -Path $file.FullName

    $controllerNodes = $doc.SelectNodes('//controller[@name]')
    foreach ($node in $controllerNodes) {
        $name = $node.Attributes['name'].Value
        if (Is-ValidIdentifier -Name $name) {
            continue
        }

        $token = ('controller name="{0}"' -f $name)
        $line = Resolve-LineNumber -Lines $lines -Token $token
        $issues.Add([pscustomobject]@{
                File = $file.FullName
                Line = $line
                Kind = 'ControllerName'
                Name = $name
                Suggested = (Get-SuggestedName -Name $name)
            })
    }

    $namedNodes = $doc.SelectNodes('//*[@name]')
    foreach ($node in $namedNodes) {
        if ($node.Name -eq 'controller') {
            continue
        }

        $name = $node.Attributes['name'].Value
        if (Is-ValidIdentifier -Name $name) {
            continue
        }

        $token = ('name="{0}"' -f $name)
        $line = Resolve-LineNumber -Lines $lines -Token $token
        $issues.Add([pscustomobject]@{
                File = $file.FullName
                Line = $line
                Kind = ('NodeName<' + $node.Name + '>')
                Name = $name
                Suggested = (Get-SuggestedName -Name $name)
            })
    }
}

if ($issues.Count -gt 0) {
    Write-Host ("[FAIL] FGUI export name validation failed. issueCount={0}" -f $issues.Count)
    foreach ($issue in $issues) {
        Write-Host ("- {0}:{1} [{2}] name='{3}' suggested='{4}'" -f
            $issue.File, $issue.Line, $issue.Kind, $issue.Name, $issue.Suggested)
    }
    exit 1
}

Write-Host ("[PASS] FGUI export name validation passed. files={0}" -f $files.Count)
exit 0

