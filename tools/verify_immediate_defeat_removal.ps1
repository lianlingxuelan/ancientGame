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

$bindStart = $source.IndexOf("private void BindRuntimeReferences()")
$bindEnd = $source.IndexOf("private void SelectAlly(", $bindStart)
if ($bindStart -lt 0 -or $bindEnd -le $bindStart) {
    Write-Error "Immediate defeat removal validation failed. Cannot locate BindRuntimeReferences."
    exit 1
}

$bindBody = $source.Substring($bindStart, $bindEnd - $bindStart)
$guardIndex = $bindBody.IndexOf("if (referencesBound)")
$guardReturnIndex = if ($guardIndex -ge 0) { $bindBody.IndexOf("return;", $guardIndex) } else { -1 }
$firstBuildViewIndex = $bindBody.IndexOf("BuildView(")
if ($guardIndex -lt 0 -or $guardReturnIndex -lt $guardIndex -or $firstBuildViewIndex -lt 0 -or $guardIndex -gt $firstBuildViewIndex) {
    Write-Error "Immediate defeat removal validation failed. BindRuntimeReferences must return before rebuilding unit views."
    exit 1
}

Write-Host "Immediate defeat removal static validation passed."
