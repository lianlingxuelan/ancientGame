param(
    [string]$BattleControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

# 只验证自动战斗是否会等待完整后续行动链后再继续，不启动 Unity，也不修改工程文件。
if (-not (Test-Path -LiteralPath $BattleControllerPath)) {
    Write-Error "Battle controller not found: $BattleControllerPath"
    exit 1
}

$source = Get-Content -LiteralPath $BattleControllerPath -Raw -Encoding UTF8
$requiredFragments = @(
    "WaitForFollowUpResolutionToFinish",
    "followUpResolutionCoroutine != null",
    "yield return WaitForFollowUpResolutionToFinish();",
    "while (!battleEnded && safety-- > 0)"
)

$missing = @()
foreach ($fragment in $requiredFragments) {
    if (-not $source.Contains($fragment)) {
        $missing += $fragment
    }
}

if ($missing.Count -gt 0) {
    Write-Error ("Auto battle turn handoff validation failed. Missing: " + ($missing -join ", "))
    exit 1
}

$autoRoutine = [regex]::Match($source, "private IEnumerator PerformAutoAttacksRoutine\(\)[\s\S]*?\n\s*}\n\s*/// <summary>").Value
if ([string]::IsNullOrEmpty($autoRoutine) -or -not $autoRoutine.Contains("PerformPlayerAttackInternal();")) {
    Write-Error "Auto battle routine no longer invokes the existing player action implementation."
    exit 1
}

Write-Host "Auto battle turn handoff static validation passed."
