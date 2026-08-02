param(
    [string]$ControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ControllerPath)) {
    throw "BattleDemoController not found: $ControllerPath"
}

$content = Get-Content -LiteralPath $ControllerPath -Raw -Encoding UTF8
$requiredFragments = @(
    "private string BuildBattleRoundTip()",
    "private string GetSelectedEnemyName()",
    "private Color GetSlotBackgroundColor(BattleUnitState unit, bool selected, bool acting, bool isEnemy)",
    "RefreshView(allyViews[i], allyUnits[i], i == selectedAllyIndex, allyUnits[i] == currentActor, false);",
    "RefreshView(enemyViews[i], enemyUnits[i], i == selectedEnemyIndex, enemyUnits[i] == currentActor, true);",
    "view.slotImage.color = GetSlotBackgroundColor(unit, selected, acting, isEnemy);"
)

foreach ($fragment in $requiredFragments) {
    if (-not $content.Contains($fragment)) {
        throw "Missing required battle target-feedback contract: $fragment"
    }
}

Write-Output "Battle target-feedback static validation passed."
