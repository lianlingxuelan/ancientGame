param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$playbackPath = Join-Path $projectRoot 'ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\Data\MainlineStoryPlaybackState.cs'
$progressPath = Join-Path $projectRoot 'ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\Data\LevelProgressManager.cs'

function Assert-Contains {
    param([string]$Content, [string]$Expected, [string]$Message)

    if (-not $Content.Contains($Expected)) {
        throw ("{0}: {1}" -f $Message, $Expected)
    }
}

if (-not (Test-Path -LiteralPath $playbackPath)) {
    throw 'Mainline story playback state source file is missing.'
}

$playbackSource = Get-Content -LiteralPath $playbackPath -Raw -Encoding UTF8
$progressSource = Get-Content -LiteralPath $progressPath -Raw -Encoding UTF8

# 剧情页需要一个不依赖 UI 的播放状态，以便后续复用到首次阅读与回看入口。
Assert-Contains $playbackSource 'public sealed class MainlineStoryPlaybackState' 'Playback state type is required'
Assert-Contains $playbackSource 'public const float SKIP_UNLOCK_SECONDS = 3f;' 'Skip unlock delay must remain three seconds'
Assert-Contains $playbackSource 'public bool TryStart(int stageId)' 'Playback state must safely start a configured stage'
Assert-Contains $playbackSource 'public bool TryAdvance()' 'Playback state must support line-by-line progression'
Assert-Contains $playbackSource 'public void AdvanceTime(float deltaSeconds)' 'Playback state must track reading time separately from UI'
Assert-Contains $playbackSource 'public bool TrySkip()' 'Playback state must support a gated skip action'
Assert-Contains $playbackSource 'public bool IsSkipAvailable' 'Playback state must expose skip availability to the UI'
Assert-Contains $playbackSource 'public bool IsCompleted' 'Playback state must expose a completed state'

# 读完和跳过都必须复用既有进度管理器，不能直接在新模块里写 PlayerPrefs。
Assert-Contains $playbackSource 'LevelProgressManager.Instance.MarkStoryRead' 'Playback completion must mark the stage as read through progress manager'
if ($playbackSource.Contains('PlayerPrefs.')) {
    throw 'Playback state must not access PlayerPrefs directly.'
}

# 既有记录 API 是此模块的唯一持久化依赖，避免读取进度和通关进度混写。
Assert-Contains $progressSource 'public void MarkStoryRead(int stageId)' 'Progress manager story-read API is required'

Write-Output 'Mainline story playback state static validation passed.'
