param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$managerPath = Join-Path $projectRoot 'ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\Data\CharacterDevelopmentManager.cs'
$routerPath = Join-Path $projectRoot 'ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\HomePageRouter.cs'

function Assert-Contains {
    param([string]$Content, [string]$Expected, [string]$Message)

    if (-not $Content.Contains($Expected)) {
        throw $Message + ": " + $Expected
    }
}

if (-not (Test-Path -LiteralPath $managerPath)) {
    throw "Character development manager not found: $managerPath"
}

$manager = Get-Content -LiteralPath $managerPath -Raw
$router = Get-Content -LiteralPath $routerPath -Raw

Assert-Contains $manager 'public const string LiQingzhaoId' 'Missing stable Li Qingzhao character id'
Assert-Contains $manager 'public CharacterDevelopmentSnapshot GetSnapshot(string characterId)' 'Missing read-only character snapshot contract'
Assert-Contains $manager 'public RewardItem[] GetNextLevelCosts(string characterId)' 'Missing next-level cost preview contract'
Assert-Contains $manager 'public CharacterLevelUpResult TryLevelUp(string characterId)' 'Missing character level-up contract'
Assert-Contains $manager 'PlayerResourceManager.Instance.TrySpend(costs)' 'Level-up must use the atomic wallet spending API'
Assert-Contains $manager 'PlayerPrefs.SetInt(BuildLevelKey(characterId), nextLevel)' 'Successful level-up must persist the next level'
Assert-Contains $manager 'public sealed class CharacterDevelopmentSnapshot' 'Missing immutable-style UI snapshot type'
Assert-Contains $manager 'public sealed class CharacterLevelUpResult' 'Missing level-up result type'
Assert-Contains $manager 'if (snapshot.level >= snapshot.maxLevel)' 'Max-level must reject before resource spending'

Assert-Contains $router 'ConfigureStoryDetailForTraining();' 'Training screen must configure a dedicated level-up action'
Assert-Contains $router 'private void TryLevelUpLiQingzhao()' 'Missing training action handler'
Assert-Contains $router 'CharacterDevelopmentManager.Instance.TryLevelUp(CharacterDevelopmentManager.LiQingzhaoId)' 'Training action must call the character development manager'
Assert-Contains $router 'CharacterDevelopmentManager.Instance.GetSnapshot(CharacterDevelopmentManager.LiQingzhaoId)' 'Character detail must read the central snapshot'
Assert-Contains $router 'private string BuildTrainingInfoText()' 'Training screen must render level and cost preview'

Write-Output 'Character leveling loop static validation passed.'
