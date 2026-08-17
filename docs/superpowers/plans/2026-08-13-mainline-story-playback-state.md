# 第一章剧情播放状态 Implementation Plan

**Goal:** 为第一章关卡剧情提供独立的逐句阅读、三秒跳过和已读写入状态对象。

**Architecture:** 新增纯 C# 的 `MainlineStoryPlaybackState`，只读取 `MainlineStoryCatalog`，并通过 `LevelProgressManager` 写既有已读状态；不接入页面、不改场景或后端。

**Tech Stack:** Unity 2020 C#、本地 `PlayerPrefs` 由既有进度管理器封装、PowerShell 静态验证。

## 约束

- 不改 `HomePageRouter.cs`，避免与 Todo39 审查改动冲突。
- 不改后端、数据库、`Scene_Boot.unity`、资源目录、支付/充值或战斗数值。
- 仅使用 `LevelProgressManager.MarkStoryRead` 记录阅读完成，不直接访问 `PlayerPrefs`。

### Task 1：测试先行

- [x] 新增 `tools/verify_mainline_story_playback_state.ps1`，约束播放状态 API、三秒跳过门槛和既有已读记录依赖。
- [x] 在生产文件不存在时运行验证，确认以缺少播放状态文件失败。

### Task 2：最小播放状态

- [x] 新增 `MainlineStoryPlaybackState.cs`，提供安全开始、逐句推进、阅读计时、跳过、完成和重置接口。
- [ ] 运行新验证与主线回归验证，确认所有检查通过。
- [ ] 按协作协议追加日志和 Claude 审查入口。
