param(
    [string]$BattleControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

# Static contract check for the battle presentation polish task.
# Does not modify any game file.

$controllerExists = Test-Path -LiteralPath $BattleControllerPath
if ($controllerExists -eq $false) {
    Write-Error "Battle controller not found: $BattleControllerPath"
    exit 1
}

$source = Get-Content -LiteralPath $BattleControllerPath -Raw -Encoding UTF8

# New presentation symbols that must exist.
$requiredFragments = @(
    "PlayAttackerCast",
    "PlayImpactWhiteFlash",
    "PlayFloatingTextRise",
    "HideDefeatedUnitView",
    "ImpactWhiteFlashSeconds",
    "HitColorPulseSeconds",
    "FloatingTextRiseSeconds"
)

# Old presentation constants that must be fully removed.
$removedFragments = @(
    "DefeatPresentationSeconds",
    "HitPresentationSeconds",
    "PlayDefeatFade",
    "DefeatFadeSeconds"
)

# Garbled comments (/// ???) must be gone.
$garbledCommentPattern = '/// \?+'

$problems = @()
foreach ($fragment in $requiredFragments) {
    if (-not $source.Contains($fragment)) {
        $problems += "MISSING: $fragment"
    }
}

foreach ($fragment in $removedFragments) {
    if ($source.Contains($fragment)) {
        $problems += "SHOULD_BE_REMOVED: $fragment"
    }
}

$garbledCount = ([regex]::Matches($source, $garbledCommentPattern)).Count
if ($garbledCount -gt 0) {
    $problems += "GARBLED_COMMENTS_REMAIN: $garbledCount"
}

# The start/restart battle button must gray out while the presentation queue is locked.
if (-not $source.Contains("SetButtonInteractable(startBattleButton, !IsBattleInputLocked()")) {
    $problems += "MISSING: startBattleButton should gray out while input is locked"
}

if ($problems.Count -gt 0) {
    Write-Error ("Battle presentation polish static validation failed:`n" + ($problems -join "`n"))
    exit 1
}

Write-Host "Battle presentation polish static validation passed."
