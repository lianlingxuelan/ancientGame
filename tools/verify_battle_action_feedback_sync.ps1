param(
    [string]$BattleControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

# 只验证行动链过程中的反馈刷新契约，不启动 Unity，也不修改工程文件。
if (-not (Test-Path -LiteralPath $BattleControllerPath)) {
    Write-Error "Battle controller not found: $BattleControllerPath"
    exit 1
}

$source = Get-Content -LiteralPath $BattleControllerPath -Raw -Encoding UTF8
$requiredFragments = @(
    "ShowResolvingActionLog",
    "行动表现中，等待下一位行动者。",
    "ShowResolvingActionLog(playerMessage);",
    "ShowResolvingActionLog(actionLog);"
)

$missing = @()
foreach ($fragment in $requiredFragments) {
    if (-not $source.Contains($fragment)) {
        $missing += $fragment
    }
}

if ($missing.Count -gt 0) {
    Write-Error ("Battle action feedback sync validation failed. Missing: " + ($missing -join ", "))
    exit 1
}

Write-Host "Battle action feedback sync static validation passed."
