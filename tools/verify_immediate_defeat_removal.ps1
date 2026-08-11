param(
    [string]$BattleControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

# Static validation only. Does not modify game files.
if (-not (Test-Path -LiteralPath $BattleControllerPath)) {
    Write-Error "Battle controller not found: $BattleControllerPath"
    exit 1
}

$source = Get-Content -LiteralPath $BattleControllerPath -Raw -Encoding UTF8
$requiredFragments = @(
    "HideDefeatedUnitView",
    "QueueDefeatPresentation",
    "isRemoved",
    "damageApplied",
    "CreateDamage(target, damage",
    "canPlayWhenTargetIsDefeated",
    "if (target == null || target.defeated)",
    "view.button.gameObject.SetActive(false)"
)

$missing = @()
foreach ($fragment in $requiredFragments) {
    if (-not $source.Contains($fragment)) {
        $missing += $fragment
    }
}

if ($missing.Count -gt 0) {
    Write-Error ("Immediate defeat removal validation failed. Missing: " + ($missing -join ", "))
    exit 1
}

if ($source.Contains("PlayDefeatFade")) {
    Write-Error "Immediate defeat removal validation failed. Old fade presentation still exists."
    exit 1
}

$applyDamageMatch = [regex]::Match($source, 'private bool ApplyDamage\([\s\S]*?\n        }\n\n        /// <summary>')
if ($applyDamageMatch.Success -and $applyDamageMatch.Value.Contains("CreateDefeat")) {
    Write-Error "Immediate defeat removal validation failed. ApplyDamage must not enqueue defeat before the damage float."
    exit 1
}

$damageCallIndex = $source.IndexOf("ShowDamageText(target, damage, targetDefeated);")
$defeatCallIndex = $source.IndexOf("QueueDefeatPresentation(target, targetDefeated);")
if ($damageCallIndex -lt 0 -or $defeatCallIndex -lt 0 -or $defeatCallIndex -lt $damageCallIndex) {
    Write-Error "Immediate defeat removal validation failed. Damage float must be queued before the defeat removal."
    exit 1
}

# 战斗配置和图标是异步加载的。加载完成只能刷新显示，不能偷偷重建整场战斗，
# 否则已经阵亡的单位会被新创建的满血单位替换，形成“无技能自动复活”的假象。
$loadConfigStart = $source.IndexOf("private IEnumerator LoadBackendBattleConfig()")
# 异步配置完成不能重置活跃战斗，否则阵亡单位会被重新创建。
$loadConfigStart = $source.IndexOf("private IEnumerator LoadBackendBattleConfig()")
$loadConfigStart = $source.IndexOf("private IEnumerator LoadBackendBattleConfig()")
$loadConfigEnd = $source.IndexOf("private IEnumerator DownloadSkillIcons")
if ($loadConfigStart -lt 0 -or $loadConfigEnd -le $loadConfigStart) {
    Write-Error "Immediate defeat removal validation failed. Cannot locate LoadBackendBattleConfig."
    exit 1
}

$loadConfigBody = $source.Substring($loadConfigStart, $loadConfigEnd - $loadConfigStart)
if ($loadConfigBody.Contains("ResetDemoBattle();")) {
    Write-Error "Immediate defeat removal validation failed. Async config loading must not reset a live battle."
    exit 1
}

if (-not $source.Contains("ResetAllUnitViewRemovalState();")) {
    Write-Error "Immediate defeat removal validation failed. New battles must explicitly restore removed slots."
    exit 1
}

$refreshViewStart = $source.IndexOf("private void RefreshView")
$refreshViewEnd = $source.IndexOf("private string GetUnitDisplayText")
if ($refreshViewStart -lt 0 -or $refreshViewEnd -le $refreshViewStart) {
    Write-Error "Immediate defeat removal validation failed. Cannot locate RefreshView."
    exit 1
}

$refreshViewBody = $source.Substring($refreshViewStart, $refreshViewEnd - $refreshViewStart)
if ($refreshViewBody.Contains("view.isRemoved = false")) {
    Write-Error "Immediate defeat removal validation failed. RefreshView must not revive a removed slot."
    exit 1
}

Write-Host "Immediate defeat removal static validation passed."
