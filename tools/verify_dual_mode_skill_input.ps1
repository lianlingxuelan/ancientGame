param(
    [string]$ControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ControllerPath)) {
    throw "BattleDemoController not found: $ControllerPath"
}

$content = Get-Content -LiteralPath $ControllerPath -Raw -Encoding UTF8
$requiredFragments = @(
    "private void UseOrQueueSkill",
    "private bool CanExecuteSkillImmediately",
    "private bool CanPreselectSkill",
    "private BattleUnitState GetSelectedSkillOwner",
    "GetSkillInputState",
    "SkillInputState.Queued",
    "SkillInputState.Immediate",
    "ExecuteSkillNow",
    "QueueSkill(skillOwner"
)

foreach ($fragment in $requiredFragments) {
    if (-not $content.Contains($fragment)) {
        throw "Missing dual-mode skill-input contract: $fragment"
    }
}

Write-Output "Dual-mode skill-input static validation passed."
