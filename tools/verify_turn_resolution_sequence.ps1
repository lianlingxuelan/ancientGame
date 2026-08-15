param(
    [string]$BattleControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

# 只验证战斗行动是否按表现批次串行推进，不启动 Unity，也不修改工程文件。
if (-not (Test-Path -LiteralPath $BattleControllerPath)) {
    Write-Error "Battle controller not found: $BattleControllerPath"
    exit 1
}

$source = Get-Content -LiteralPath $BattleControllerPath -Raw -Encoding UTF8
$requiredFragments = @(
    "ResolveFollowUpActionsRoutine",
    "StartCoroutine(ResolveFollowUpActionsRoutine(playerMessage))",
    "yield return WaitForPresentationQueueToFinish();",
    "StopFollowUpResolutionRoutine",
    "resolvingEnemyTurn ||"
)

$missing = @()
foreach ($fragment in $requiredFragments) {
    if (-not $source.Contains($fragment)) {
        $missing += $fragment
    }
}

if ($missing.Count -gt 0) {
    Write-Error ("Turn resolution sequence validation failed. Missing: " + ($missing -join ", "))
    exit 1
}

$completePlayerAction = [regex]::Match($source, "private void CompletePlayerAction\(string playerMessage\)[\s\S]*?\n\s*}\n\s*/// <summary>").Value
if ([string]::IsNullOrEmpty($completePlayerAction) -or $completePlayerAction.Contains("while (!battleEnded")) {
    Write-Error "CompletePlayerAction still resolves follow-up actions synchronously."
    exit 1
}

Write-Host "Turn resolution sequence static validation passed."
