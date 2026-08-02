param(
    [string]$ControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ControllerPath)) {
    throw "BattleDemoController not found: $ControllerPath"
}

$content = Get-Content -LiteralPath $ControllerPath -Raw -Encoding UTF8
$requiredFragments = @(
    "private readonly Dictionary<BattleUnitState, QueuedSkillState> queuedSkills",
    "private void QueueSkill",
    "private bool TryExecuteQueuedSkillForCurrentActor",
    "private bool CanQueueSkill",
    "queuedSkills.Clear();",
    "queuedSkills.Remove(target);",
    'QueueSkill(PoetryStrikeCost, "poetry_strike"',
    'QueueSkill(DreamAreaCost, "dream_area"',
    'QueueSkill(HealingVerseCost, "healing_verse"',
    "TryExecuteQueuedSkillForCurrentActor(out queuedMessage)"
)

foreach ($fragment in $requiredFragments) {
    if (-not $content.Contains($fragment)) {
        throw "Missing required skill-preselection contract: $fragment"
    }
}

Write-Output "Skill preselection static validation passed."
