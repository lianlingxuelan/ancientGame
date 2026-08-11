$ErrorActionPreference = 'Stop'

$battleFile = Join-Path $PSScriptRoot '..\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs'
$battleSource = Get-Content -Raw -LiteralPath $battleFile

function Require-SourceText([string]$text, [string]$expected, [string]$message)
{
    if (-not $text.Contains($expected))
    {
        throw "[FAIL] $message`nMissing: $expected"
    }
}

Require-SourceText $battleSource 'using Shouyou.Data;' 'Battle controller must reference development data.'
Require-SourceText $battleSource 'IsLiQingzhaoCharacterId(characterId)' 'Formation path must identify Li Qingzhao only.'
Require-SourceText $battleSource 'CreateLiQingzhaoUnitFromDevelopment' 'Li Qingzhao needs an isolated creation path.'
Require-SourceText $battleSource 'CharacterDevelopmentManager.Instance.GetSnapshot(CharacterDevelopmentManager.LiQingzhaoId)' 'Battle stats must read the development snapshot.'
Require-SourceText $battleSource 'snapshot != null ? snapshot.health' 'Health must prefer the development snapshot.'
Require-SourceText $battleSource 'snapshot != null ? snapshot.attack' 'Attack must prefer the development snapshot.'
Require-SourceText $battleSource 'private int CalculateDamage(BattleUnitState attacker, BattleUnitState target)' 'Damage formula entry must remain unchanged.'

if ($battleSource.Contains('PlayerPrefs.GetInt(') -or $battleSource.Contains('PlayerPrefs.SetInt('))
{
    throw '[FAIL] BattleDemoController must not access PlayerPrefs directly.'
}

Write-Output '[PASS] Li Qingzhao battle stats use the restricted development entry point.'
