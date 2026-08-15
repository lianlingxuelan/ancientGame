$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$routerPath = Join-Path $projectRoot 'ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\HomePageRouter.cs'
$source = Get-Content -Raw -LiteralPath $routerPath

function Assert-Contains([string]$needle, [string]$message) {
    if (-not $source.Contains($needle)) {
        throw $message
    }
}

# 主线挑战必须先进入只读的出战准备步骤，而不是直接跳入战斗页。
Assert-Contains 'ShowBattlePreparation();' 'EnterBattlePrototype must open the battle preparation flow.'
Assert-Contains 'private void ShowBattlePreparation()' 'Battle preparation method is missing.'
Assert-Contains 'private void StartBattleFromPreparation()' 'Battle confirmation method is missing.'
Assert-Contains 'private string BuildBattlePreparationText()' 'Battle preparation text builder is missing.'
Assert-Contains 'OpenFormationFromMainlineStageDetail' 'Battle preparation must keep a formation action.'
Assert-Contains 'StartBattleFromPreparation' 'Battle preparation must provide an explicit confirmation action.'
Assert-Contains 'ReturnToCurrentMainlineStageDetail' 'Battle preparation must provide a return action.'

Write-Host 'Mainline battle preparation static validation passed.'
