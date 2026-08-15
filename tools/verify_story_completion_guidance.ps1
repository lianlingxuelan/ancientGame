$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$routerPath = Join-Path $repoRoot 'ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\HomePageRouter.cs'
$source = Get-Content -Raw $routerPath

$requiredFragments = @(
    'private string BuildStoryCompletionGuidance()',
    'LevelProgressManager.Instance.IsStageCleared(currentMainlineStageId)',
    'MainlineStageCatalog.GetRewards(currentMainlineStageId)',
    'BuildBattleRewardText(stageRewards, stage.rewardPreview)',
    'LevelProgressManager.Instance.GetNextStageId(currentMainlineStageId)',
    'LevelProgressManager.Instance.IsStageUnlocked(nextStageId)',
    'BuildStoryCompletionGuidance()'
)

foreach ($fragment in $requiredFragments)
{
    if (-not $source.Contains($fragment))
    {
        throw "[FAIL] Missing story completion guidance fragment: $fragment"
    }
}

Write-Host '[PASS] Story completion guidance exposes battle, reward, and next-stage decisions.'
