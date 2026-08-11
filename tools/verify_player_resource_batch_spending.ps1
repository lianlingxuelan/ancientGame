param(
    [string]$ResourceManagerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\Data\PlayerResourceManager.cs"
)

$ErrorActionPreference = 'Stop'

# Todo33：养成一次可能同时消耗铜钱与材料。
# 任一项余额不足时必须整体失败，避免出现“只扣了一半材料”的不可恢复状态。
if (-not (Test-Path -LiteralPath $ResourceManagerPath))
{
    throw "PlayerResourceManager not found: $ResourceManagerPath"
}

$source = Get-Content -LiteralPath $ResourceManagerPath -Raw -Encoding UTF8

$requiredContracts = @(
    'public bool TrySpend(RewardItem[] costs)',
    'Dictionary<string, int> totalCosts',
    'if (costs == null || costs.Length == 0)',
    'if (cost == null || string.IsNullOrEmpty(cost.id) || cost.amount <= 0)',
    'if (!CanAfford(pair.Key, pair.Value))',
    'PlayerPrefs.SetInt(BuildKey(pair.Key), GetCount(pair.Key) - pair.Value)',
    'PlayerPrefs.Save()'
)

foreach ($contract in $requiredContracts)
{
    if ($source.IndexOf($contract, [System.StringComparison]::Ordinal) -lt 0)
    {
        throw "Missing batch resource-spending contract: $contract"
    }
}

$methodStart = $source.IndexOf('public bool TrySpend(RewardItem[] costs)', [System.StringComparison]::Ordinal)
$methodEnd = $source.IndexOf('/// <summary>', $methodStart + 1, [System.StringComparison]::Ordinal)
if ($methodStart -lt 0 -or $methodEnd -lt 0)
{
    throw 'Cannot isolate TrySpend batch method for atomicity validation.'
}

$methodBody = $source.Substring($methodStart, $methodEnd - $methodStart)
$firstWrite = $methodBody.IndexOf('PlayerPrefs.SetInt', [System.StringComparison]::Ordinal)
$affordabilityCheck = $methodBody.IndexOf('if (!CanAfford(pair.Key, pair.Value))', [System.StringComparison]::Ordinal)
if ($firstWrite -lt 0 -or $affordabilityCheck -lt 0 -or $affordabilityCheck -gt $firstWrite)
{
    throw 'Batch spending must finish every affordability check before its first PlayerPrefs write.'
}

Write-Host 'Player resource batch spending static validation passed.'
