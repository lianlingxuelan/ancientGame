param(
    [string]$ControllerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ControllerPath)) {
    throw "BattleDemoController not found: $ControllerPath"
}

$content = Get-Content -LiteralPath $ControllerPath -Raw -Encoding UTF8
$requiredFragments = @(
    "private string BuildActionOrderPreview()",
    "private string GetActionPreviewUnitName(BattleUnitState unit)",
    "BuildActionOrderPreview()",
    "ActionPreviewCount"
)

foreach ($fragment in $requiredFragments) {
    if (-not $content.Contains($fragment)) {
        throw "Missing action preview contract: $fragment"
    }
}

$previewStart = $content.IndexOf("private string BuildActionOrderPreview()")
$previewEnd = $content.IndexOf("private string GetActionPreviewUnitName", $previewStart)
if ($previewStart -lt 0 -or $previewEnd -le $previewStart) {
    throw "Cannot locate action preview implementation."
}

$previewBody = $content.Substring($previewStart, $previewEnd - $previewStart)
if ($previewBody.Contains("actionCursor =") -or $previewBody.Contains("currentActor =") -or $previewBody.Contains("actionOrder.Sort")) {
    throw "Action preview must be read-only and must not mutate turn authority."
}

Write-Output "Battle action-preview static validation passed."
