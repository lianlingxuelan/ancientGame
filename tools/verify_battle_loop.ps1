param(
    [string]$BattleControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

# Validate key Chapter One battle-loop guards without modifying game files.
if (-not (Test-Path -LiteralPath $BattleControllerPath)) {
    Write-Error "Battle controller not found: $BattleControllerPath"
    exit 1
}

$source = Get-Content -LiteralPath $BattleControllerPath -Raw -Encoding UTF8
if ([string]::IsNullOrWhiteSpace($source)) {
    Write-Error "Battle controller source is empty."
    exit 1
}

$requiredFragments = @(
    "ValidateBattleStartup",
    "GetBattleUnavailableReason",
    "if (battleEnded)",
    "if (selectedUnit.defeated)",
    "if (selectedEnemy.defeated)"
)

$missing = @()
foreach ($fragment in $requiredFragments) {
    if (-not $source.Contains($fragment)) {
        $missing += $fragment
    }
}

if ($missing.Count -gt 0) {
    Write-Error ("Battle loop static validation failed. Missing: " + ($missing -join ", "))
    exit 1
}

Write-Host "Battle loop static validation passed."
