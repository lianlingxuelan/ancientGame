param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$progressPath = Join-Path $projectRoot 'ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\Data\LevelProgressManager.cs'
$routerPath = Join-Path $projectRoot 'ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\HomePageRouter.cs'

function Assert-Contains {
    param([string]$Content, [string]$Expected, [string]$Message)

    if (-not $Content.Contains($Expected)) {
        throw ("{0}: {1}" -f $Message, $Expected)
    }
}

if (-not (Test-Path -LiteralPath $progressPath) -or -not (Test-Path -LiteralPath $routerPath)) {
    throw 'Mainline progression source file is missing.'
}

$progressSource = Get-Content -LiteralPath $progressPath -Raw -Encoding UTF8
$routerSource = Get-Content -LiteralPath $routerPath -Raw -Encoding UTF8

# 第一章应从第一关开始，不能把第二关作为默认测试捷径开放。
Assert-Contains $progressSource 'private const int DemoInitialUnlockedStageId = 1;' 'Chapter one must initially unlock only stage one'

# 进度管理器本身必须阻止越过前置关卡的调用，而不只依赖 UI 按钮。
Assert-Contains $progressSource 'if (!IsStageUnlocked(safeStageId))' 'CompleteStage must reject locked stages'

# 结算必须根据是否存在后续关卡调整按钮和文案，最后一关不应出现伪下一关。
Assert-Contains $routerSource 'bool hasNextStage = currentMainlineStageId < LevelProgressManager.MaxMainlineStageId;' 'Settlement must detect the final stage'
Assert-Contains $routerSource 'ConfigureStoryDetailForBattleVictory(hasNextStage);' 'Settlement button layout must receive final-stage state'
Assert-Contains $routerSource 'private void ConfigureStoryDetailForBattleVictory(bool hasNextStage)' 'Victory layout must support a disabled final-stage next button'
Assert-Contains $routerSource '"\u672c\u7ae0\u5b8c\u6210"' 'Final stage must use a chapter-complete label'

Write-Output 'Mainline progression rules static validation passed.'
