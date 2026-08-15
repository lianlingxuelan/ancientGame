param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$routerPath = Join-Path $projectRoot 'ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\HomePageRouter.cs'

function Assert-Contains {
    param([string]$Content, [string]$Expected, [string]$Message)

    if (-not $Content.Contains($Expected)) {
        throw ("{0}: {1}" -f $Message, $Expected)
    }
}

if (-not (Test-Path -LiteralPath $routerPath)) {
    throw 'HomePageRouter source file is missing.'
}

$routerSource = Get-Content -LiteralPath $routerPath -Raw -Encoding UTF8

# Lock requirements must resolve a concrete previous stage.
Assert-Contains $routerSource 'BuildLockedStageRequirementText' 'Locked-stage requirement helper is required'
Assert-Contains $routerSource 'MainlineStageCatalog.Get(stage.id - 1)' 'Locked-stage requirement must resolve the previous stage'

# Stage details must provide both reward and development guidance.
Assert-Contains $routerSource 'BuildMainlineStageGuidance' 'Stage guidance helper is required'
Assert-Contains $routerSource 'CharacterDevelopmentManager.Instance.GetSnapshot' 'Stage guidance must read the current character level'
Assert-Contains $routerSource 'BuildMainlineStageRewardPreview' 'Stage reward preview helper is required'
Assert-Contains $routerSource 'MainlineStageCatalog.GetRewards(stage.id)' 'Reward preview must use the stage reward catalog'

# Locked stages still retain a clickable explanation route.
Assert-Contains $routerSource 'new UnityEngine.Events.UnityAction(ShowLockedStageHint)' 'Locked story button must route to the unlock-condition hint'

Write-Output 'Mainline stage guidance static validation passed.'
