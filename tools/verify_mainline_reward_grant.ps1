param(
    [string]$RouterPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\HomePageRouter.cs",
    [string]$ResourceManagerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\Data\PlayerResourceManager.cs"
)

# Static contract check for the chapter-one mainline loop closure.
# Reward items must be actually granted on settlement, not only rendered as text.
# Does not modify any game file.

$routerExists = Test-Path -LiteralPath $RouterPath
$managerExists = Test-Path -LiteralPath $ResourceManagerPath
if ($routerExists -eq $false) {
    Write-Error "HomePageRouter not found: $RouterPath"
    exit 1
}
if ($managerExists -eq $false) {
    Write-Error "PlayerResourceManager not found: $ResourceManagerPath"
    exit 1
}

$routerSource = Get-Content -LiteralPath $RouterPath -Raw -Encoding UTF8
$managerSource = Get-Content -LiteralPath $ResourceManagerPath -Raw -Encoding UTF8

# PlayerResourceManager contract: class, persistence key prefix, read/write API, namespaces.
$managerFragments = @(
    "class PlayerResourceManager",
    "namespace Shouyou.Data",
    "using Shouyou.Network;",
    "ResourceKeyPrefix",
    "public int GetCount(",
    "public void GrantRewards(",
    "PlayerPrefs.GetInt(",
    "PlayerPrefs.Save()"
)

# HomePageRouter integration: grant before render, balance text builder, using list, no garbled comments.
$routerFragments = @(
    "using System.Collections.Generic;",
    "PlayerResourceManager.Instance.GrantRewards(",
    "BuildResourceBalanceText",
    "PlayerResourceManager.Instance.GetCount("
)

$problems = @()
foreach ($fragment in $managerFragments) {
    if (-not $managerSource.Contains($fragment)) {
        $problems += "MISSING in PlayerResourceManager: $fragment"
    }
}

foreach ($fragment in $routerFragments) {
    if (-not $routerSource.Contains($fragment)) {
        $problems += "MISSING in HomePageRouter: $fragment"
    }
}

# Garbled comments (/// ???) must be gone from both touched C# files.
$garbledCommentPattern = '/// \?+'
$routerGarbledCount = ([regex]::Matches($routerSource, $garbledCommentPattern)).Count
if ($routerGarbledCount -gt 0) {
    $problems += "GARBLED_COMMENTS_REMAIN in HomePageRouter: $routerGarbledCount"
}

if ($problems.Count -gt 0) {
    Write-Error ("Mainline reward grant static validation failed:`n" + ($problems -join "`n"))
    exit 1
}

Write-Host "Mainline reward grant static validation passed."
