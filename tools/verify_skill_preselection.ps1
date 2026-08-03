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
    "private void UseOrQueueSkill",
    "private bool TryExecuteQueuedSkillForCurrentActor",
    "private bool CanPreselectSkill",
    "queuedSkills.Clear();",
    "queuedSkills.Remove(target);",
    'UseOrQueueSkill(PoetryStrikeCost, "poetry_strike"',
    'UseOrQueueSkill(DreamAreaCost, "dream_area"',
    'UseOrQueueSkill(HealingVerseCost, "healing_verse"',
    "TryExecuteQueuedSkillForCurrentActor(out queuedMessage)"
)

foreach ($fragment in $requiredFragments) {
    if (-not $content.Contains($fragment)) {
        throw "Missing required skill-preselection contract: $fragment"
    }
}

Write-Output "Skill preselection static validation passed."
