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

function To-Text {
    param([int[]]$CodePoints)

    return -join ($CodePoints | ForEach-Object { [char]$_ })
}

if (-not (Test-Path -LiteralPath $routerPath)) {
    throw 'HomePageRouter source file is missing.'
}

$routerSource = Get-Content -LiteralPath $routerPath -Raw -Encoding UTF8
$formationLabel = To-Text @(0x8C03, 0x6574, 0x7F16, 0x961F)
$startChallengeLabel = To-Text @(0x5F00, 0x59CB, 0x6311, 0x6218)
$replayChallengeLabel = To-Text @(0x518D, 0x6B21, 0x6311, 0x6218)
$unavailableLabel = To-Text @(0x6682, 0x672A, 0x5F00, 0x653E)
$startReadLabel = To-Text @(0x5F00, 0x59CB, 0x9605, 0x8BFB)
$replayStoryLabel = To-Text @(0x56DE, 0x770B, 0x5267, 0x60C5)

# The stage detail must expose a direct formation route before battle.
Assert-Contains $routerSource 'OpenFormationFromMainlineStageDetail' 'Stage detail needs a formation entry method'
Assert-Contains $routerSource ('"' + $formationLabel + '"') 'Stage detail needs a clear formation button label'
Assert-Contains $routerSource 'ShowMainlineFormationTab();' 'Formation entry must route to the existing formation tab'

# Locked stages must not present combat as an available action.
Assert-Contains $routerSource ('unlocked ? (cleared ? "' + $replayChallengeLabel + '" : "' + $startChallengeLabel + '") : "' + $unavailableLabel + '"') 'Battle label must distinguish locked, first-clear, and replay states'

# The player-facing guide must describe the story-formation-battle order.
Assert-Contains $routerSource ($startReadLabel + ' / ' + $formationLabel + ' / ' + $startChallengeLabel) 'Uncleared stage guidance must show the main action order'
Assert-Contains $routerSource ($replayStoryLabel + ' / ' + $formationLabel + ' / ' + $replayChallengeLabel) 'Cleared stage guidance must show replay actions'

Write-Output 'Mainline stage action entries static validation passed.'
