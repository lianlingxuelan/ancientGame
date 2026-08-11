param(
    [string]$ResourceManagerPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\Data\PlayerResourceManager.cs"
)

$ErrorActionPreference = 'Stop'

# Todo32：资源钱包必须支持“先检查、再扣除”的最小消费闭环。
# 该静态校验约束接口和防御条件，避免未来养成/体力入口直接写 PlayerPrefs。
if (-not (Test-Path -LiteralPath $ResourceManagerPath))
{
    throw "PlayerResourceManager not found: $ResourceManagerPath"
}

$source = Get-Content -LiteralPath $ResourceManagerPath -Raw -Encoding UTF8

$requiredContracts = @(
    'public bool CanAfford(string id, int amount)',
    'public bool TrySpend(string id, int amount)',
    'if (string.IsNullOrEmpty(id) || amount <= 0)',
    'if (!CanAfford(id, amount))',
    'PlayerPrefs.SetInt(BuildKey(id), current - amount)',
    'PlayerPrefs.Save()'
)

foreach ($contract in $requiredContracts)
{
    if ($source.IndexOf($contract, [System.StringComparison]::Ordinal) -lt 0)
    {
        throw "Missing player-resource spending contract: $contract"
    }
}

if ($source -match 'current\s*-\s*amount\s*<\s*0')
{
    throw 'Resource spending must reject insufficient balance before writing a negative value.'
}

Write-Host 'Player resource spending static validation passed.'
