$ErrorActionPreference = 'Stop'

# Static guard: the chapter overview must remain read-only.
$repoRoot = Split-Path -Parent $PSScriptRoot
$routerPath = Join-Path $repoRoot 'ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/HomePageRouter.cs'
$source = Get-Content -Raw -Encoding UTF8 $routerPath

$requiredSnippets = @(
    'private string BuildChapterProgressOverview()',
    'GetHighestClearedStageId()',
    'GetStageStateLabel(stageId)',
    'IsStoryRead(stageId)',
    'MainlineStageCatalog.Get(stageId)',
    'LevelProgressManager.MaxMainlineStageId',
    'BuildChapterProgressOverview();'
)

$missing = @()
foreach ($snippet in $requiredSnippets) {
    if (-not $source.Contains($snippet)) {
        $missing += $snippet
    }
}

if ($missing.Count -gt 0) {
    throw ('Chapter progress overview is missing: ' + ($missing -join '; '))
}

# Inspect only the target method body to avoid false positives from other flows.
$methodMarker = 'private string BuildChapterProgressOverview()'
$methodStart = $source.IndexOf($methodMarker)
if ($methodStart -lt 0) {
    throw 'Chapter progress overview method cannot be located.'
}

$bodyStart = $source.IndexOf('{', $methodStart)
if ($bodyStart -lt 0) {
    throw 'Chapter progress overview method has no body.'
}

$depth = 0
$bodyEnd = -1
for ($index = $bodyStart; $index -lt $source.Length; $index++) {
    if ($source[$index] -eq '{') {
        $depth++
    }
    elseif ($source[$index] -eq '}') {
        $depth--
        if ($depth -eq 0) {
            $bodyEnd = $index
            break
        }
    }
}

if ($bodyEnd -lt 0) {
    throw 'Chapter progress overview method body is not balanced.'
}

$overviewBody = $source.Substring($bodyStart, $bodyEnd - $bodyStart + 1)
$forbiddenSnippets = @(
    'MarkStoryRead(',             # Actual story persistence API.
    'CompleteMainlineStage(',     # Actual stage-clear persistence API.
    'GrantRewards(',              # Actual reward grant API.
    'ShouyouBackendBootstrap.',   # Backend synchronization is forbidden here.
    'PlayerPrefs.'                # Direct local persistence is forbidden here.
)

foreach ($snippet in $forbiddenSnippets) {
    if ($overviewBody.Contains($snippet)) {
        throw ('Chapter progress overview must not mutate progress or rewards: ' + $snippet)
    }
}

Write-Host 'PASS: chapter progress overview contract is present and read-only.'
