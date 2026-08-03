param(
    [string]$BattleControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

# 只检查战斗表现队列的结构契约，不修改任何游戏文件。
$controllerExists = Test-Path -LiteralPath $BattleControllerPath
if ($controllerExists -eq $false) {
    Write-Error "Battle controller not found: $BattleControllerPath"
    exit 1
}

$source = Get-Content -LiteralPath $BattleControllerPath -Raw -Encoding UTF8
$requiredFragments = @(
    "BattlePresentationEvent",
    "presentationEvents",
    "QueuePresentationEvent",
    "PlayPresentationQueue",
    "IsBattleInputLocked",
    "PresentationEventType.Attack",
    "PresentationEventType.Damage",
    "PresentationEventType.Heal",
    "PresentationEventType.Defeat"
)

$missing = @()
foreach ($fragment in $requiredFragments) {
    if (-not $source.Contains($fragment)) {
        $missing += $fragment
    }
}

if ($missing.Count -gt 0) {
    Write-Error ("Battle presentation queue static validation failed. Missing: " + ($missing -join ", "))
    exit 1
}

Write-Host "Battle presentation queue static validation passed."
