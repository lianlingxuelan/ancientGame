param(
    [string]$ControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

# 静态契约检查：确认战斗控制器已预留“头像攻击特效”数据和通知入口。
if (-not (Test-Path $ControllerPath))
{
    throw "BattleDemoController not found: $ControllerPath"
}

$source = Get-Content -Raw -Encoding UTF8 $ControllerPath
$requiredFragments = @(
    "BattlePortraitEffectRequest",
    "PortraitAttackEffectRequested",
    "RequestPortraitAttackEffect",
    "attackerSlotIndex",
    "attackerIsAlly",
    "skillId"
)

$missing = @($requiredFragments | Where-Object { -not $source.Contains($_) })
if ($missing.Count -gt 0)
{
    throw "Portrait attack effect hook is incomplete. Missing: $($missing -join ', ')"
}

Write-Host "Portrait attack effect hook static validation passed."
