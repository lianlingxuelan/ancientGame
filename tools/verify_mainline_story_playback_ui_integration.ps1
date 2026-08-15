$ErrorActionPreference = 'Stop'

# 静态检查：剧情详情页必须只通过 MainlineStoryPlaybackState 管理逐句阅读与已读存档。

$requiredFragments = @(
    'private MainlineStoryPlaybackState storyPlaybackState = new MainlineStoryPlaybackState();',
    'storyPlaybackState.AdvanceTime(Time.unscaledDeltaTime);',
    'storyPlaybackState.TryStart(currentMainlineStageId)',
    'storyPlaybackState.TryAdvance()',
    'storyPlaybackState.TrySkip()',
    'storyPlaybackState.CurrentLine',
    'storyPlaybackState.CurrentLineIndex',
    'storyPlaybackState.LineCount',
    'storyPlaybackState.Reset();'
)

$legacyFragments = @(
    'storyReadingActive',
    'currentStoryLineIndex',
    'storyReadingStartedAt',
    'StorySkipDelaySeconds',
    'LevelProgressManager.Instance.MarkStoryRead(currentMainlineStageId);'
)

foreach ($fragment in $requiredFragments) {
    if (-not (Select-String -LiteralPath 'F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\HomePageRouter.cs' -SimpleMatch -Pattern $fragment -Quiet)) {
        throw "缺少剧情播放状态接入片段: $fragment"
    }
}

foreach ($fragment in $legacyFragments) {
    if (Select-String -LiteralPath 'F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\UI\HomePageRouter.cs' -SimpleMatch -Pattern $fragment -Quiet) {
        throw "仍保留旧剧情状态或重复存档路径: $fragment"
    }
}

Write-Host 'PASS: Mainline story playback UI integration static checks passed.'
