param(
    [string]$CatalogPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\Data\MainlineStageCatalog.cs",
    [string]$RouterPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\HomePageRouter.cs"
)

$ErrorActionPreference = 'Stop'

foreach ($path in @($CatalogPath, $RouterPath))
{
    if (-not (Test-Path -LiteralPath $path))
    {
        throw "Required source file not found: $path"
    }
}

$catalogSource = Get-Content -LiteralPath $CatalogPath -Raw -Encoding UTF8
$routerSource = Get-Content -LiteralPath $RouterPath -Raw -Encoding UTF8

$catalogContracts = @(
    'public static RewardItem[] GetKnownRewardTypes()',
    'for (int i = 0; i < DefaultStages.Length; i++)',
    'DefaultRewardsByStageId.TryGetValue(DefaultStages[i].id, out rewards)',
    'if (item == null || string.IsNullOrEmpty(item.id) || knownRewards.ContainsKey(item.id))',
    'knownRewards.Add(item.id, CloneReward(item))'
)

foreach ($contract in $catalogContracts)
{
    if ($catalogSource.IndexOf($contract, [System.StringComparison]::Ordinal) -lt 0)
    {
        throw "Missing known-reward catalog contract: $contract"
    }
}

$routerContracts = @(
    'BuildTrainingResourceBalanceText()',
    'MainlineStageCatalog.GetKnownRewardTypes()',
    'PlayerResourceManager.Instance.GetCount(reward.id)'
)

foreach ($contract in $routerContracts)
{
    if ($routerSource.IndexOf($contract, [System.StringComparison]::Ordinal) -lt 0)
    {
        throw "Missing training resource-balance contract: $contract"
    }
}

$trainingStart = $routerSource.IndexOf('public void ShowTrainingInfo()', [System.StringComparison]::Ordinal)
$trainingEnd = $routerSource.IndexOf('public void ShowBondInfo()', $trainingStart, [System.StringComparison]::Ordinal)
if ($trainingStart -lt 0 -or $trainingEnd -lt 0)
{
    throw 'Cannot isolate ShowTrainingInfo for read-only validation.'
}

$trainingBody = $routerSource.Substring($trainingStart, $trainingEnd - $trainingStart)
if ($trainingBody.IndexOf('BuildTrainingResourceBalanceText()', [System.StringComparison]::Ordinal) -lt 0)
{
    throw 'ShowTrainingInfo must append the current resource balance.'
}

if ($trainingBody.IndexOf('TrySpend(', [System.StringComparison]::Ordinal) -ge 0)
{
    throw 'ShowTrainingInfo must stay read-only and cannot spend resources.'
}

Write-Host 'Training resource balance static validation passed.'
