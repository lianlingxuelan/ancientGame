param(
    [string]$BattleControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

# 只验证自动战斗的表现节奏契约，不启动 Unity，也不修改工程文件。
if (-not (Test-Path -LiteralPath $BattleControllerPath)) {
    Write-Error "Battle controller not found: $BattleControllerPath"
    exit 1
}

$source = Get-Content -LiteralPath $BattleControllerPath -Raw -Encoding UTF8
$requiredFragments = @(
    "PerformAutoAttacksRoutine",
    "StartCoroutine(PerformAutoAttacksRoutine())",
    "isAutoBattleRunning",
    "WaitForPresentationQueueToFinish",
    "StopAutoBattleRoutine"
)

$missing = @()
foreach ($fragment in $requiredFragments) {
    if (-not $source.Contains($fragment)) {
        $missing += $fragment
    }
}

if ($missing.Count -gt 0) {
    Write-Error ("Auto battle presentation sequence validation failed. Missing: " + ($missing -join ", "))
    exit 1
}

$autoSection = [regex]::Match($source, "public void PerformAutoAttacks\(\)[\s\S]*?\n\s*}\n\s*/// <summary>").Value
if ($autoSection.Contains("while (!battleEnded")) {
    Write-Error "Auto battle still resolves multiple actions synchronously in PerformAutoAttacks."
    exit 1
}

Write-Host "Auto battle presentation sequence static validation passed."
