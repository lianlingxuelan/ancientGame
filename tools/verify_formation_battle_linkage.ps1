param(
    [string]$FormationPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\FormationDemoController.cs",
    [string]$BattlePath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\BattleDemoController.cs",
    [string]$BuilderPath = "F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\Editor\HomeUILayoutBuilder.cs"
)

$ErrorActionPreference = "Stop"

foreach ($path in @($FormationPath, $BattlePath, $BuilderPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required source file not found: $path"
    }
}

$formation = Get-Content -LiteralPath $FormationPath -Raw -Encoding UTF8
$battle = Get-Content -LiteralPath $BattlePath -Raw -Encoding UTF8
$builder = Get-Content -LiteralPath $BuilderPath -Raw -Encoding UTF8

$formationContracts = @(
    "private int selectedSlotIndex = -1;",
    "public void SelectCandidateOne()",
    "public void SelectCandidateTwo()",
    "public void ClearSelectedSlot()",
    "public void BeginFormationEditing()",
    "private void AssignCandidateToSelectedSlot(string characterId)",
    "private bool TryMoveExistingCharacter(string characterId, int targetSlotIndex)",
    "selectedCharacterIds[selectedSlotIndex] = characterId;"
)

foreach ($fragment in $formationContracts) {
    if (-not $formation.Contains($fragment)) {
        throw "Missing formation linkage contract: $fragment"
    }
}

if ($formation.Contains("EnsureDemoCompanionSlot")) {
    throw "Formation linkage validation failed: legacy auto-fill must not overwrite an intentionally empty slot."
}

$battleContracts = @(
    "private BattleUnitState CreateAllyUnit(int index)",
    "string formationCharacterId = ShouyouBackendBootstrap.GetBattleFormationSlotId(index);",
    "return CreateAllyUnitFromFormation(formationCharacterId, index, dto);",
    "private BattleUnitState CreateAllyUnitFromFormation(string characterId, int index, BattleUnitDto template)"
)

foreach ($fragment in $battleContracts) {
    if (-not $battle.Contains($fragment)) {
        throw "Missing battle formation-source contract: $fragment"
    }
}

$builderContracts = @(
    "FormationCandidatePanel",
    "FormationCandidate_1",
    "FormationCandidate_2",
    "ClearFormationSlotButton"
)

foreach ($fragment in $builderContracts) {
    if (-not $builder.Contains($fragment)) {
        throw "Missing formation UI contract: $fragment"
    }
}

Write-Output "Formation-to-battle linkage static validation passed."
