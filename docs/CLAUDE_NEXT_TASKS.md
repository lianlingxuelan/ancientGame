# Claude Code Next Tasks

Updated: 2026-08-14 21:40 Asia/Shanghai

This file is a handoff board, not the execution log. Please write review results to `docs/AI_TASK_LOG.md`.

## ✅ DONE — Claude Review: Todo44-FE-R1-CODE 第一章主线进度概览 PASS (2026-08-14)

Scope: 任意第一章关卡详情底部只读展示 1–6 关的通关、剧情阅读与下一目标;不改主线规则。未改后端/数据库/Scene_Boot/资源/支付/奖励发放/解锁规则/伤害公式。
See `docs/AI_TASK_LOG.md` → Todo44-FE-R1-REVIEW.

- BuildChapterProgressOverview 纯读:仅 GetHighestClearedStageId/GetStageStateLabel/IsStoryRead + MainlineStageCatalog.Get,无写关/发奖/后端/PlayerPrefs ✓
- 终关用 >= MaxMainlineStageId(常量 6)判定"第一章已完成",无第七关;关卡一览循环覆盖 1–6 关 ✓
- 集成在 ShowMainlineStageDetail 正文末尾追加,storyPlaybackState.Reset 与按钮配置未旁路 ✓
- verify_chapter_progress_overview 7 片段命中、3 禁止片段不存在;相关 6 脚本全部 PASS;git diff --check 干净;HomePageRouter.cs 保留 UTF-8 BOM ✓
- P2:禁止片段拼写与真实写 API 不符,属精确拼写守卫非语义守卫——已由 Todo44-FE-R2-FIX 修复,关闭 ✓
- R2-FIX:守卫升级为方法体级(大括号深度截取 BuildChapterProgressOverview 体),禁止项改真实写 API(MarkStoryRead/CompleteMainlineStage/GrantRewards/ShouyouBackendBootstrap/PlayerPrefs),6 脚本全 PASS,生产 C# 未动 ✓
- 仍待人工:Unity Play Mode 打开未通关/已通关/第六关详情,确认总进度与存档一致,打开关闭不改变资源与记录 ✓

---

## ✅ DONE — Claude Review: Todo42-FE-R1-CODE 第一章剧情播放 UI 接入 PASS (2026-08-14)

Scope: HomePageRouter 唯一持有 MainlineStoryPlaybackState,绑定"开始阅读/回看剧情"入口;删除旧行号/阅读计时器/直接 MarkStoryRead 写路径。未改后端/数据库/Scene_Boot/资源/支付/解锁规则/战斗数值。
See `docs/AI_TASK_LOG.md` → Todo42-FE-R1-REVIEW.

- 唯一 storyPlaybackState 实例,无旧字段/计时器/跳过延时常量/直接 MarkStoryRead ✓
- StartStoryReading 缺失目录安全回退 + TryStart 后渲染首句;AdvanceStoryReading 委托 TryAdvance ✓
- Update 仅详情打开且未完成时按 unscaledDeltaTime 累计;关闭/切换关卡 Reset,已读保留 ✓
- 读完与跳过共用幂等 CompletePlayback 写已读,无双写;新增 verify_mainline_story_playback_ui_integration.ps1;5 脚本静态校验全部 PASS ✓
- HomePageRouter.cs 保留 UTF-8 BOM;git diff --check 干净;Scene_Boot 历史搅动须提交排除 ✓

仍待人工:Unity Play Mode 验证第一关"开始阅读"逐句推进、3 秒后跳过、关闭/切换不串台词、完成显示重读/回看入口。

---

## ✅ DONE — Claude Review: Todo43-FE-R1-CODE 剧情完成后的主线行动引导 PASS (2026-08-14)

Scope: 剧情完成提示按未通关/已通关非终关/终关三种状态生成下一步文案,只读不写。未改后端/数据库/Scene_Boot/资源/支付/解锁规则/发奖逻辑/伤害公式。
See `docs/AI_TASK_LOG.md` → Todo43-FE-R1-REVIEW.

- BuildStoryCompletionGuidance 纯读:不发奖、不写通关、不同步后端、不写 PlayerPrefs ✓
- 未通关仅预览 GetRewards("战斗胜利后可获得"),不提前发奖 ✓
- 非终关经 IsStageUnlocked(nextStageId) 门控提示下一关;终关用 >= MaxMainlineStageId 判定,无第七关 ✓
- 剧情已读与战斗通关独立,按钮配置仍走 ConfigureStoryDetailForMainlineStage;verify_story_completion_guidance.ps1 全片段命中,5 脚本 PASS ✓
- HomePageRouter.cs 保留 UTF-8 BOM;git diff --check 干净;Scene_Boot 历史搅动须提交排除 ✓

仍待人工:Unity Play Mode 验证未通关读/跳剧情不产生资源与通关记录变更;通关后重读显示下一关引导;第六关完成显示第一章完成不越界。

---

## ✅ DONE — Claude Review: Todo41-FE-R1-CODE 第一章剧情播放状态 PASS (2026-08-13)

Scope: 新增不依赖页面的 MainlineStoryPlaybackState,支持逐句推进、3 秒跳过门槛、正常读完/跳过统一经 LevelProgressManager.MarkStoryRead 写已读,不直接访问 PlayerPrefs。未改后端/数据库/Scene_Boot/资源/充值/战斗数值,未触碰 HomePageRouter 与 MainlineStoryCatalog(避免与 Todo39 冲突)。
See `docs/AI_TASK_LOG.md` → Todo41-FE-R1-REVIEW.

- TryStart 经 TryGet 初始化 6 关,非法/空剧情 Reset+false ✓
- TryAdvance 末句走 CompletePlayback 收口,完成后不可再推进 ✓
- IsSkipAvailable 需满 3 秒;跳过与读完统一写已读,幂等保护 ✓
- 无 PlayerPrefs 直写;新增 verify_mainline_story_playback_state.ps1;20 项静态校验全部 PASS ✓
- P2:MainlineStoryPlaybackState.cs 无 UTF-8 BOM(Codex 生成)——已补上,关闭 ✓

仍待人工:Todo39 审查完成后把播放状态绑定到"开始阅读/回看剧情"按钮,Unity Play Mode 验证逐句推进、3 秒跳过、末句完成与已读写入。

---

## ✅ DONE — Claude Review: Todo39-FE-R1-CODE 关卡详情与养成引导收口 PASS (2026-08-13)

Scope: 锁关详情入口"未解锁"→"解锁条件"并指向前一关;奖励预览统一读 MainlineStageCatalog.GetRewards 与结算同源;等级不足只提示养成不拦截挑战;已通关明确重复挑战不推进主线。未改关卡开放规则/战斗数值/后端/数据库/Scene_Boot/资源/充值。
See `docs/AI_TASK_LOG.md` → Todo39-FE-R1-REVIEW.

- 锁关按钮 → ShowLockedStageHint,正文 BuildLockedStageRequirementText(指向前一关)✓
- 奖励预览 BuildBattleRewardText(GetRewards) 空数组兜底 rewardPreview ✓
- BuildMainlineStageGuidance 读李清照等级,仅提示不拦截 ✓
- 新增 verify_mainline_stage_guidance.ps1;19 项静态校验全部 PASS ✓

仍待人工:Unity Play Mode 验证锁关"解锁条件"指引、低等级养成建议随升级消失、奖励预览与结算一致。

---

## ✅ DONE — Claude Review: Todo40-FE-R1-CODE 剧情目录安全读取 PASS (2026-08-13)

Scope: MainlineStoryCatalog 新增 LineCount/GetLine/TryGet/GetStageIds 边界安全 API,保留 Get 首关回退;未触碰 HomePageRouter 以避免与 Todo39 冲突。未改文案/后端/数据库/Scene_Boot/资源/充值。
See `docs/AI_TASK_LOG.md` → Todo40-FE-R1-REVIEW.

- LineCount null→0、GetLine 越界/null→空文本、TryGet 未命中→false+null、GetStageIds 返回副本 ✓
- Get 重构为 TryGet+回退,旧调用方零改动 ✓
- 新增 verify_mainline_story_catalog.ps1;19 项静态校验全部 PASS ✓
- P2:MainlineStoryCatalog.cs 无 UTF-8 BOM(历史遗留)——已补上,关闭 ✓

仍待人工:剧情回看页接入后逐句播放边界冒烟。

---

## ✅ DONE — Claude Review: Todo38-FE-R1-CODE 第一章主线成长闭环 PASS (2026-08-12)

Scope: 新档从第一关开始,胜利后逐关解锁;结算保留资源奖励入口,末关停止提供伪"下一关"。未改伤害公式/后端/数据库/Scene_Boot/资源目录/充值入口。
See `docs/AI_TASK_LOG.md` → Todo38-FE-R1-REVIEW.

- LevelProgressManager.CompleteStage 拦截越关写入(DemoInitialUnlockedStageId 2→1,新档起点第一关)✓
- HomePageRouter 结算三态文案 + 末关"本章完成"按钮禁用 ✓
- 重复挑战保留奖励发放但不推进主线进度 ✓
- 锁关 UI/数据双层防护(解锁前禁入 + 进度管理器拦截)✓
- verify_immediate_defeat_removal 修正定位覆盖 BindRuntimeReferences 防复建 guard ✓
- 新增 verify_mainline_progression_rules.ps1;10 项静态校验全部 PASS ✓

P2 观察点:① 工作区 BattleDemoController.cs 实有 +7 行 referencesBound guard(属 Todo37 范畴)——已于 CODE 记录补充说明,关闭;② LevelProgressManager.cs 无 UTF-8 BOM——已补上,关闭;③ IsStageUnlocked 注释过时——已更新,关闭。
仍待人工:Unity Play Mode 以新档验证 逐关解锁/重复挑战奖励/末关无下一关/锁关拦截。

---

## ✅ DONE — Todo28-FE-R1-CODE 第一章主流程闭环 (2026-08-09)

Scope: 通关结算奖励真实入账到最小本地资源钱包（PlayerPrefs 持久化），让 选关→战斗→结算拿奖励→解锁下一关→剧情 的循环对玩家真正成立。未改伤害/行动值/AP/CD/后端/数据库。
See `docs/AI_TASK_LOG.md` → Todo28-FE-R1-CODE.

- 新增 PlayerResourceManager（GetCount / GrantRewards，键前缀 Shouyou.Player.Resource.*）✓
- ShowBattleVictoryDetail 结算时实际入账 + "当前持有"余额展示 ✓
- 清理 HomePageRouter 三处历史 `???` 乱码注释 ✓
- 新增 verify_mainline_reward_grant.ps1；含新增在内 9 项静态校验全部 PASS ✓

**下一优先级（候选）：资源/背包系统扩展（结算→养成消费闭环，含体力系统）。**
仍待人工：Unity Play Mode 验证 Todo27 新表现 + Todo28 结算入账/余额累加/重启保留。

---

## DONE - Claude Review: Todo31-FE-R1-CODE action-order preview PASS (2026-08-10)

Scope: add read-only action-order preview to the battle top hint. Do not change action-value sorting, speed calculation, damage, AP/CD, backend, database, assets, Scene_Boot, or battle settlement.

Files:
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- `tools/verify_battle_action_preview.ps1`

Please verify:
1. `BuildActionOrderPreview()` is read-only: it must not assign `actionCursor`, `currentActor`, or sort/mutate `actionOrder`.
2. Preview starts at the existing action cursor, skips null/defeated units, and caps at four upcoming units.
3. The unit label clearly distinguishes ally and enemy without changing who acts.
4. `BuildBattleRoundTip()` retains current actor and selected target context, then adds the order preview on a second line.
5. `SetBattleMessage()` cannot overwrite the richer round-tip display with an old one-line string.
6. Run `tools/verify_battle_action_preview.ps1`, `tools/verify_immediate_defeat_removal.ps1`, `tools/verify_battle_presentation_polish.ps1`, `tools/verify_battle_presentation_queue.ps1`, `tools/verify_battle_loop.ps1`, `tools/verify_skill_preselection.ps1`, `tools/verify_dual_mode_skill_input.ps1`, `tools/verify_battle_target_feedback.ps1`, and `tools/verify_formation_battle_linkage.ps1`.
7. Unity smoke: select portraits, perform a normal action, preselect a skill and allow enemy actions. Preview should refresh with the actual action queue but never alter the actor.

Known boundary: this is a text preview, not a visual action timeline. No animation or turn-authority redesign is introduced.

---

## DONE - Claude Review: Todo30-FE-R1-CODE no implicit revive PASS (2026-08-10)

Scope: fix only the implicit reappearance of retired battle units. No damage formula, action-value, AP/CD, backend, database, assets, or Scene_Boot change.

Files:
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- `tools/verify_immediate_defeat_removal.ps1`

Please verify:
1. `LoadBackendBattleConfig()` never calls `ResetDemoBattle()` after an asynchronous config/icon request completes.
2. `ResetAllUnitViewRemovalState()` is called only from the explicit new-battle entry (`ResetDemoBattle`); `RefreshView` does not set `view.isRemoved = false`.
3. A retired unit remains excluded from target/action logic and keeps its hidden slot while the same battle continues.
4. Starting or retrying a new battle still restores all slots normally.
5. No resurrection feature is introduced accidentally: a future revive must be a dedicated skill/resolution path, never an async-load or render-refresh side effect.
6. Run `tools/verify_immediate_defeat_removal.ps1`, `tools/verify_battle_presentation_polish.ps1`, `tools/verify_battle_presentation_queue.ps1`, `tools/verify_battle_loop.ps1`, `tools/verify_skill_preselection.ps1`, `tools/verify_dual_mode_skill_input.ps1`, `tools/verify_battle_target_feedback.ps1`, and `tools/verify_formation_battle_linkage.ps1`.
7. Unity smoke: defeat a unit, wait for backend configuration/icon completion and advance later actions; it must not return, be selected, or receive damage. Retry/new battle is the only current restoration route.

Known boundary: no revive skill or revive UI is implemented in this slice.

---

## ✅ DONE — Claude Review: Todo26-FE-R1 战斗表现事件队列 (2026-08-09)

Scope: 攻击/受击/治疗飘字/阵亡改为可顺序播放的前端表现事件队列，保留头像施法动画接入点。
Reviewed and passed (1 round, 0 P1, 3 个 P2 非阻塞观察点)。See `docs/AI_TASK_LOG.md` → Todo26-FE-R1-REV + Todo26-FE-R1-PASS.

- FIFO 表现队列 + 协程顺序播放 ✓
- 播放期输入锁（六入口 + 按钮变灰）✓
- OnDisable/Reset/Retreat 清理无残留 ✓
- PortraitAttackEffectRequested 扩展点保留 ✓
- 伤害/行动值/AP/CD/后端/DB 均未修改 ✓
- 4 项静态校验 PASS ✓

P2 观察点（供"战斗表现完善"阶段参考）：
1. 飘字时长从 0.8s 缩至约 0.42s（与受击脉冲绑定），可读性略降。
2. 胜利结算后"重新开始"按钮在表现播放期可点但被锁静默忽略（约 1 秒），建议按钮同步变灰。
3. 历史遗留 `???` 乱码注释（a2032421，非本轮引入），建议后续统一清理。

**下一优先级：第一章主流程闭环（战斗表现完善已交付）。**
仍待人工：Unity Play Mode 验证 Todo26 多事件连播/退出残留/胜负结算时序 + Todo27 新表现（施法高亮/受击三段时序/阵亡淡出/飘字时长）。

---

## ✅ DONE — Todo27-FE-R1-CODE 战斗表现完善 (2026-08-09)

Scope: 在 Todo26 表现事件队列之上完善表现层。未改伤害公式/行动值/AP/冷却/后端/数据库。
See `docs/AI_TASK_LOG.md` → Todo27-FE-R1-CODE.

- 攻击者头像施法高亮（sin 上抬 + 放大 + 青色渐变）✓
- 受击白闪 → 颜色脉冲 → 飘字上浮淡出三段时序，飘字可见时长恢复 0.8s ✓
- 阵亡淡出置灰（与 defeated 灰态一致）✓
- startBattleButton 表现锁定时同步变灰（修复 Todo26 P2-2）✓
- 清理 4 处历史遗留 `???` 乱码注释（修复 Todo26 P2-3）✓
- 新增 verify_battle_presentation_polish.ps1；含新增在内 5 项静态校验全部 PASS ✓

**下一优先级：第一章主流程闭环（已交付 Todo28，见上方区块）。**
仍待人工：Unity Play Mode 验证 Todo27 新表现（多事件连播无残留、施法/受击/阵亡手感、播放中退出重置）。

---

## ✅ DONE — BackendFix-R1 (2026-07-31)

- `/api/v1/health` endpoint added → HTTP 200, `{ ok: true, service: "ShouyouServer" }`
- `buildAssetResponse()` URL 改为 `/api/v1/assets?iconKey=xxx` 格式 → 四个技能图标均 HTTP 200 + image/png
- Unity `GetHealth()` 改为调用真正的 `/api/v1/health`
- 未删 shouyou.db
- 记录: `docs/AI_TASK_LOG.md` → BackendFix-R1

---

## ✅ DONE — Backend-Check-R1: health endpoint and UTF-8 JSON headers

Priority: P1

Please check the backend side:

1. Add or confirm `GET /api/v1/health`, returning JSON like `{ "ok": true, "service": "ShouyouServer" }`.
2. Ensure every JSON response sets `Content-Type: application/json; charset=utf-8`.
3. Keep `GET /api/v1/battle/demo-config` field names stable. Unity now consumes: `stageId`, `maxActionPoint`, `allies`, `enemies`, `skills`.
4. Keep `GET /api/v1/assets?category=battle_skill` field names stable. Unity now consumes the top-level `icons` array.
5. Do not delete the old `shouyou.db`. If old DB formation data conflicts with `demo-config`, the Unity battle page treats `demo-config` as the authority for battle startup. DB cleanup should be a separate approved task.

Acceptance checks:

1. Local backend is reachable on port 5188.
2. `/api/v1/battle/demo-config` returns Li Qingzhao in slot1 and Wanhe in slot2.
3. `/api/v1/assets?category=battle_skill` returns real icon URLs under top-level `icons`.
4. Chinese JSON displays correctly in Node and Unity.

## ✅ DONE — Frontend-Review-R1: Unity battle API integration (2026-07-31 12:15)

Priority: P1 → reviewed and passed. See `docs/AI_TASK_LOG.md` → Todo14-FE-R1-REV + Todo14-FE-R1-PASS.

- ShouyouApiModels DTOs match backend ✓
- UTF-8 decoding before JsonUtility ✓
- BattleDemoController loads from demo-config ✓
- Wanhe slot2 not overridden ✓
- Skill buttons fallback ✓
- No CS0103, no DB deletion ✓

## ✅ DONE — Claude Review Request - Todo14-FE-R2-FIX (2026-07-31 12:15)

Scope: Unity frontend battle demo compile fix → reviewed and passed. See `docs/AI_TASK_LOG.md` → Todo14-FE-R1-REV (R2 verdict PASS, 0 critical, 0 warning).

## ✅ DONE — Claude Review Request - Todo15-FE-R1-CODE (2026-07-31 12:30)

Scope: battle skill icon rendering → reviewed and passed. See `docs/AI_TASK_LOG.md` → Todo15-FE-R1-REV + Todo15-FE-R1-PASS.

- 4 skill icons rendered as child SkillIcon nodes ✓
- iconKey frontend mapping correct ✓
- Labels visible, buttons clickable ✓
- shouyou.db untouched ✓

## ✅ DONE — Backend Fix Request - BattleSkillAssetRoute-R1 (2026-07-31)

Priority: P1

Codex verified that `GET /api/v1/assets?category=battle_skill` returns valid icon metadata, but every returned URL like `/assets/icons/skill_basic_attack.png` returns 404. The active server only serves binary icon files through `GET /api/v1/assets?iconKey={key}`.

Please make the URL in `buildAssetResponse()` match a working binary route. Recommended response URL:

`http://127.0.0.1:5188/api/v1/assets?iconKey=skill_basic_attack`

Requirements:

1. Keep the list endpoint shape unchanged: top-level `icons`, each with `iconKey`, `displayName`, `url`, `category`, `version`, `width`, `height`, `_placeholder`.
2. Returned file URL for every non-placeholder battle skill must respond HTTP 200 with `image/png`.
3. Do not delete, recreate, or modify `ShouyouServer/data/shouyou.db`.
4. Append a REVIEW or CODE record to `docs/AI_TASK_LOG.md` with exact endpoint test results.

Frontend status: `BattleDemoController` already maps the four skills to `skill_basic_attack`, `skill_poetry_attack`, `skill_group_damage`, and `skill_heal`; no Unity API contract change is required.

## ✅ DONE — Claude Review Request - Todo16-FE-R1-CODE (2026-07-31 12:45)

Scope: Unity frontend battle core loop → reviewed and passed. See `docs/AI_TASK_LOG.md` → Todo16-FE-R1-REV + Todo16-FE-R1-PASS.

- actionValue turn order, green-highlight active actor, skill AP/cooldown ✓
- Enemy auto turns, safety counter, victory/defeat/retreat routes ✓
- No DB write, shouyou.db untouched ✓
- 1 round, 0 P1 defects

## ✅ DONE — Claude Review Request - Todo17-FE-R1-CODE (2026-07-31 13:00)

Scope: Chapter One configurable mainline loop → reviewed and passed. See `docs/AI_TASK_LOG.md` → Todo17-FE-R1-REV + Todo17-FE-R1-PASS.

- Story reading + 3s skip guard + story-read vs battle-clear separation ✓
- Battle page shows stage title/ID, settlement shows reward preview ✓
- No backend/DB/raw asset changes ✓
- 1 round, 0 P1 defects

## ✅ DONE — Todo19-FE-R1-CODE: Schema First 字段补全（第一批·已审批）

Priority: P1 | 批准范围仅限第一批，第二/三批暂不执行

参考文档：`C:\Users\Administrator\Desktop\神识碎片、梦境等级、玄幻角色解锁、梦境 CG、雅集挚友剧情.txt`

### 核心原则

1. **只完整定义两个对象的终态字段**：战斗单位（BattleUnitState / BattleUnitDto）、结算奖励（RewardItem）
2. 神识碎片、梦境等级、玄幻角色解锁、CG、雅集挚友属于"玩家梦域进度"领域，**不借这次结算改动塞进来**，留在第二/三批
3. 所有新增字段默认值必须保持现有战斗与伤害公式行为不变

### 改动范围（4 个文件，不动后端、不新建 Controller、不删库）

#### 文件 1：BattleDemoController.cs
路径：`Assets/_Project/Scripts/UI/BattleDemoController.cs`

**BattleUnitState 新增字段（不改现有字段名，旧构造函数保持兼容）：**

```
// ---- 本轮启用（参与排序，不参与伤害公式）----
speed          // 默认 100。仅用于 BuildActionOrder：同 actionValue 时 speed 高者先动
               // 接口没给速度时默认 100 → 当前回合顺序完全不变

// ---- 本轮仅存储，不参与伤害/命中/克制/UI ----
critRate       // float，默认 0
critDamage     // float，默认 1.5f
hitRate        // float，默认 1.0f
dodgeRate      // float，默认 0
element        // string，默认 null
starLevel      // int，默认 1
breakLevel     // int，默认 0
buffIds        // string[]，默认 new string[0]
```

`BuildActionOrder` 改动：仅在 `actionValue` 相等时，以 `speed` 降序作为第二排序条件。其余排序逻辑不动。

`CreateUnitFromDto`：所有新字段带 null/0 fallback。后端不返回这些字段时 `JsonUtility.FromJson` 自动填默认值。

#### 文件 2：ShouyouApiModels.cs
路径：`Assets/_Project/Scripts/Network/ShouyouApiModels.cs`

**BattleUnitDto 同步新增对应可选字段。**

**新增 RewardItem 类：**

```csharp
[Serializable]
public class RewardItem
{
    public string id;
    public string category;    // "coin"/"material"/"fragment"/"skill_book"/"shenshi"
    public string name;
    public int amount;
    public string iconPath;    // null = 文字占位
    public int quality;        // 1-5，默认 1
    // 以下本轮占位，方案二启用
    public bool isBound;
    public string expireTime;  // null = 永久
    public string description;
}
```

#### 文件 3：MainlineStageCatalog.cs
路径：`Assets/_Project/Scripts/Data/MainlineStageCatalog.cs`

- `MainlineStageInfo` 新增 `public RewardItem[] rewards`
- 因引用 `RewardItem`（位于 `Shouyou.Network` 命名空间），文件顶部需加 `using Shouyou.Network;`
- 此为 Demo 阶段可接受的轻度耦合，等后端接管奖励表后再迁移为独立领域模型
- 当前每关 `rewards` 填 1-2 条占位条目（铜钱 + 玉）

#### 文件 4：HomePageRouter.cs
路径：`Assets/_Project/Scripts/UI/HomePageRouter.cs`

**`ShowBattleVictoryDetail` 结算展示规则：**
```
if (completedStage.rewards != null && completedStage.rewards.Length > 0)
    逐条遍历 rewards，拼 "name × amount"
else
    回退现有 rewardPreview 字符串
```
旧关卡配置（rewards 为空）永远不会空白。

### 本轮禁区

- ❌ critRate/critDamage/hitRate/dodgeRate/element/buffIds 参与伤害公式
- ❌ 新字段参与 UI 渲染（speed 除外）
- ❌ 新建 Controller 文件
- ❌ 改动后端任何代码
- ❌ 删除或重建 `ShouyouServer/data/shouyou.db`

### 第二批（暂不执行，独立任务）

- 神识碎片 DTO + `ShenshiFragmentController` 空壳
- 梦境等级 DTO + `DreamRealmController` 空壳
- 玄幻角色解锁条件：`CharacterUnlockDto.unlockCondition` 预留

### 第三批（暂不执行，独立任务）

- 梦境 CG 触发引擎 + 全屏播放
- 雅集挚友好感度 + 对话树
- 元素克制计算 + Buff 状态机
- 暴击/命中/闪避公式接入

### Claude 审查清单

1. `BattleUnitState` 新字段不破坏现有构造函数和 `CreateUnitFromDto`
2. `speed` 仅作为 actionValue 同值时的第二排序键，不影响当前回合顺序
3. `critRate` 等存储字段不参与 `ApplyDamage` / `PerformPlayerAttack` 伤害计算
4. `RewardItem` 反序列化缺失字段不抛异常
5. 结算文案：`rewards` 有有效条目则展示，为空则回退 `rewardPreview`
6. `MainlineStageCatalog.cs` 有 `using Shouyou.Network;`
7. `shouyou.db` 未被修改

---

## ✅ DONE — Claude Review Request - Todo18-FE-R1-CODE (2026-07-31 14:40 review)

Scope: mainline chapter remote-config override with local fallback.

Please verify:
1. Unity compiles with no errors in `MainlineStageCatalog.cs` and `ShouyouBackendBootstrap.cs`.
2. With backend 5188 running, Console logs `主线关卡配置已套用后端章节数据：chapter-1` after startup.
3. `/api/v1/chapters` title and `recommendedLevel` are shown by the mainline stage detail; local objective, reward preview, and recommended power remain present.
4. Stop the backend and enter Play Mode again: mainline page, story detail, and battle entry still work with local fallback and no null reference.
5. Confirm no write, delete, or regeneration happened to `ShouyouServer/data/shouyou.db`.

Known boundary:
- This task intentionally keeps UI unlock gating in `LevelProgressManager` / backend stage progress. `StageDto.defaultUnlocked` is stored in the mapped catalogue but is not a second unlock authority.

---

## ✅ DONE — Claude Review Request - Todo19-FE-R1-CODE (2026-07-31 23:35)

Scope: first batch only — battle-unit terminal schema and mainline settlement reward rendering → reviewed and passed. See `docs/AI_TASK_LOG.md` → Todo19-FE-R1-REV (verdict PASS, 1 P2, 0 P1) + Todo19-FE-R1-PASS.

All 7 review items passed. Single P2: RewardItem.quality field initializer ineffective for JsonUtility (non-blocking, only affects future backend reward table integration).

No Dream progress, CG trigger, friendship, elemental-calculation, Buff-state, critical-hit, or hit/dodge gameplay logic was introduced. shouyou.db untouched.

---

## ✅ DONE — Todo20-FE-R1-CODE: Chapter One battle-loop hardening (2026-08-01)

Scope: Chapter One battle-loop hardening only. No backend, database, damage-formula, asset, or Dream-system change.

Review verdict: **PASS** — 1 round, 0 P1, 0 P2. All 7 checklist items verified.

- verify_battle_loop.ps1 static check: PASS ✅
- Startup interception (ValidateBattleStartup + GetBattleUnavailableReason): ✅
- Defeated unit selection feedback (SelectAlly/SelectEnemy): ✅
- Anti-duplicate settlement (TryFinishBattle/RetreatBattle/TryGetBattleActionContext): ✅
- Damage formulas unchanged ✅
- shouyou.db untouched ✅
- HomePageRouter.currentBattleAlreadySettled lock intact ✅

Records: `docs/AI_TASK_LOG.md` → Todo20-FE-R1-REV + Todo20-FE-R1-PASS

**Pending Unity Play Mode regression** (cannot run from this terminal):
庭院 → 第一章 → 编队 → 战斗 → 普攻/词意连击/如梦令/疗愈 → 胜利/失败/撤退

---

## ✅ DONE — Todo22-FE-R1-CODE: Action queue + skill pre-selection (2026-08-02)

Review verdict: **PASS** — 1 round, 0 P1, 0 P2.

- Skill pre-selection: QueuedSkillState + QueueSkill + CanQueueSkill + TryExecuteQueuedSkillForCurrentActor
- Auto-execute on next ally turn; target fallback on defeat; clear on reset
- BattlePortraitEffectRequest event data class (future animation hook)
- verify_skill_preselection.ps1 / verify_battle_loop.ps1 / verify_portrait_attack_effect_hook.ps1 all PASS
- Damage formulas unchanged; shouyou.db untouched
- Records: AI_TASK_LOG.md → Todo22-FE-R1-REV + Todo22-FE-R1-PASS

---

## ⏳ Claude Review Request — Todo21-FE-R1-CODE (2026-08-02)

Scope: reserve the portrait-attack-effect presentation hook only. No actual animation, backend, database, asset, damage-formula, AP/CD, or settlement change.

Files:
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- `tools/verify_portrait_attack_effect_hook.ps1`
- `docs/superpowers/plans/2026-08-02-portrait-attack-effect-hook.md`

Please verify:
1. `BattlePortraitEffectRequest` exposes only presentation context: attacker/target side, slot, name, skill id and area flag.
2. `PortraitAttackEffectRequested` is emitted before existing `ApplyDamage` calls for basic attack, poetry strike, dream-area attack and enemy basic attack.
3. Healing does not incorrectly emit an attack effect.
4. The event may have no listener without throwing or changing existing battle results.
5. `CalculateDamage`, skill AP/CD, stage settlement, `ShouyouServer`, and `shouyou.db` are unchanged.
6. Run: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_portrait_attack_effect_hook.ps1`.
7. Unity Play Mode regression: battle remains playable through basic attack, skills, enemy turn, victory, defeat and retreat.

Known boundary: this task intentionally does not show visual feedback until a later portrait-animation presenter subscribes to the event.

---

## ⏳ Claude Review Request — Todo22-FE-R1-CODE (2026-08-02)

Scope: action-value queue keeps deciding the actor; three big skills are now preselected and execute only at that actor's next action. No backend, database, damage-formula, asset, or settlement-schema change.

Files:
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- `tools/verify_skill_preselection.ps1`
- `docs/superpowers/plans/2026-08-02-action-queue-skill-preselection.md`

Please verify:
1. `CastPoetryStrike` / `CastDreamAreaAttack` / `CastHealingVerse` only enqueue; AP, cooldown, damage, heal and portrait effect do not happen until `TryExecuteQueuedSkillForCurrentActor`.
2. `CompletePlayerAction` processes enemies and queued ally skills until it reaches an ally with no queued instruction; it must not loop indefinitely.
3. A queued single-target skill uses its stored slot when alive and falls back to another alive enemy when its original target has retired.
4. `ResetDemoBattle` and `ApplyDamage` clear obsolete queued instructions; a defeated unit cannot later auto-cast.
5. `CalculateDamage`, `CalculateSkillDamage`, `CalculateAreaSkillDamage`, `CalculateHealAmount`, backend contracts, stage settlement, and `ShouyouServer/data/shouyou.db` are unchanged.
6. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_skill_preselection.ps1` and `tools/verify_battle_loop.ps1`.
7. Unity Play Mode: queue each big skill, advance through a full round, then check normal attack, auto battle, enemy turns, victory, defeat and retreat.

Known boundary: this is a first interaction slice. It supports one queued big skill per character, does not yet draw an action timeline or target-preview arrow, and keeps basic attack immediate.

---

## ⏳ Claude Review Request — Todo23-FE-R1-CODE (2026-08-02)

Scope: battle target-selection readability only. Current action authority remains the action-value queue; this task does not change damage, AP/CD, queued skills, backend, assets, settlement, or `shouyou.db`.

Files:
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- `tools/verify_battle_target_feedback.ps1`
- `docs/superpowers/plans/2026-08-02-battle-target-feedback.md`

Please verify:
1. `BuildBattleRoundTip()` displays current actor plus the currently selected, alive enemy target; selecting a target never changes `currentActor`.
2. `SelectEnemy()` still determines the future single-target attack target, while `SelectAlly()` remains read-only for turn ownership.
3. `RefreshView(...)` gets an explicit `isEnemy` context and gives current actor, selected enemy, selected ally, and defeated unit distinct visual states without touching HP, damage or action calculations.
4. Selecting a defeated enemy keeps the existing early-return feedback; a retired selected target is still safely handled by the existing target fallback.
5. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_battle_target_feedback.ps1`, `tools/verify_battle_loop.ps1`, and `tools/verify_skill_preselection.ps1`.
6. Unity Play Mode: click different enemy and ally portraits, use normal attack and one queued skill, then complete victory/defeat/retreat smoke checks.
7. Confirm `ShouyouServer/data/shouyou.db` is unchanged.

Known boundary: no animated target arrow or action timeline is added in this slice; it is intentionally limited to readable text, color and outline feedback.

---

## ⏳ Claude Review Request — Todo24-FE-R1-CODE (2026-08-02)

Scope: editable six-slot formation to saved backend cache to battle entrance identity linkage, frontend only. No backend, database, damage formula, AP/CD, Dream Domain, or settlement-schema change.

Files:
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/FormationDemoController.cs`
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/Network/ShouyouBackendBootstrap.cs`
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/Editor/HomeUILayoutBuilder.cs`
- `tools/verify_formation_battle_linkage.ps1`
- `docs/superpowers/plans/2026-08-02-formation-to-battle-linkage.md`

Please verify:
1. Select a slot then a candidate places that candidate there; choosing a candidate without a selected slot only guides and does not mutate formation.
2. Selecting a duplicate character swaps its old position with the selected slot; clearing then saving remains empty after reopening formation (no client-side auto-fill).
3. Successful save reloads Bootstrap formation cache; failed save keeps the local draft. Run with backend 5188 available.
4. Saved formation controls ally identity at battle entrance. Demo config is only the temporary stat/portrait template and remains enemy source; damage, action values, AP/CD and queued skills are unchanged.
5. Empty formation slots become non-actionable empty ally units. Battle prompt and settlement formation summary both use the saved formation cache.
6. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_formation_battle_linkage.ps1`, `tools/verify_battle_loop.ps1`, `tools/verify_skill_preselection.ps1`, and `tools/verify_battle_target_feedback.ps1`.
7. Unity smoke flow: run `Shouyou > UI > Clean And Rebuild Prototype`; enter formation, assign 李清照/婉禾, save, enter battle, then move one character, save/reenter, clear a slot, save/reenter. Verify the battle names match each saved formation state.
8. Confirm `ShouyouServer/data/shouyou.db` is unchanged.

Known boundary: first chapter exposes two candidate buttons only; full roster scrolling, final per-character stat/portrait tables, and formation persistence redesign are intentionally outside this slice.

---

## Claude Review Request - Todo25-FE-R1-CODE (2026-08-02)

Scope: action-value queue remains the only source of action authority. This slice only adds dual-mode input for the three big skills: current actor executes immediately, selected non-current ally preselects for their next action. No backend, database, damage formula, AP/CD rule, asset, stage settlement, or Dream Domain change.

Files:
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- `tools/verify_dual_mode_skill_input.ps1`
- `tools/verify_skill_preselection.ps1`
- `docs/superpowers/plans/2026-08-02-dual-mode-skill-input.md`

Please verify:
1. When the selected skill owner is `currentActor`, all three big skills execute immediately, consume AP/register cooldown through the existing resolution path, and then complete that player action.
2. When the selected skill owner is a living ally other than `currentActor`, `QueueSkill` only records the instruction; it does not consume AP, change cooldown, call `CompletePlayerAction`, or advance `actionCursor`.
3. A queued same skill is cancelable on a second click. A different queued skill for the same owner remains protected from accidental overwrite.
4. `GetSelectedSkillOwner` never changes `currentActor`; clicking a portrait only chooses an immediate/next-turn skill owner.
5. Button state is readable and consistent: immediate, preselect next turn, queued/cancel, existing queued instruction, CD, insufficient AP, and unavailable state.
6. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_dual_mode_skill_input.ps1`, `tools/verify_skill_preselection.ps1`, `tools/verify_battle_loop.ps1`, `tools/verify_battle_target_feedback.ps1`, and `tools/verify_formation_battle_linkage.ps1`.
7. Unity Play Mode smoke: current actor immediate cast; another selected ally preselect; current actor finishes normally; queued ally automatically casts next action; cancellation, cooldown/AP insufficient, victory, defeat and retreat.

Known boundary: no action timeline, animated casting, hit sequencing, or portrait animation presenter is added here. Those belong to the next battle-presentation slice.

---

## Claude Review Request - Todo26-FE-R1-CODE (2026-08-03)

Scope: front-end presentation sequencing only. Battle values, damage formulas, action value, action points, cooldowns, backend, database, settlement rules and assets are out of scope and must remain unchanged.

Files:
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- `tools/verify_battle_presentation_queue.ps1`
- `docs/superpowers/plans/2026-08-03-battle-presentation-event-queue.md`

Please verify:
1. `BattlePresentationEvent` only carries presentation information and the FIFO queue is consumed by one coroutine; it must not calculate or mutate combat values.
2. Basic attacks, enemy basic attacks, poetry strike, dream-area attack and healing all enqueue the correct visual sequence. Healing must not emit `PortraitAttackEffectRequested`.
3. `PortraitAttackEffectRequested` fires when the queued Attack event is played, not before it. No listener must be safe.
4. Queue playback locks player input, while `PerformAutoAttacks` keeps its existing multi-attack behavior by using the internal resolution entry point.
5. `ResetDemoBattle`, `RetreatBattle` and `OnDisable` clear the queue; confirm no coroutine or old floating text/highlight leaks into the next battle/page.
6. `CalculateDamage`, `CalculateSkillDamage`, `CalculateAreaSkillDamage`, `CalculateHealAmount`, action-value ordering, AP/CD, stage settlement, `ShouyouServer`, and `shouyou.db` are unchanged.
7. Run: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_battle_presentation_queue.ps1`, `tools/verify_battle_loop.ps1`, `tools/verify_skill_preselection.ps1`, and `tools/verify_dual_mode_skill_input.ps1`.
8. Unity Play Mode smoke: basic attack, each big skill, enemy action, auto battle, victory, defeat, retreat and reset. Verify attack pulse -> floating text -> defeat label -> next actor is readable, and no input can double-fire while the sequence is playing.

Known boundary: current battle values are still resolved by the existing synchronous battle flow. This slice serializes presentation only; a future battle timeline should move state commits to each visual event if frame-accurate HP transitions are required.

---

## DONE - Claude Review: Todo29-FE-R1-CODE immediate defeat removal PASS (2026-08-10)

Scope: frontend defeated-unit presentation only. Preserve the final damage float, then immediately hide the defeated unit slot. No damage formula, action-value, AP/CD, backend, database, asset, or Scene_Boot change.

Files:
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- `tools/verify_immediate_defeat_removal.ps1`
- `tools/verify_battle_presentation_polish.ps1`

Please verify:
1. `ApplyDamage(..., out damageApplied)` exits for null or defeated targets, and every caller queues presentation only when `damageApplied` is true.
2. The four direct damage paths queue damage before defeat: basic attack, queued poetry strike, area strike, and enemy action.
3. `canPlayWhenTargetIsDefeated` preserves only the final damage float; stale damage events must not target a defeated unit.
4. `HideDefeatedUnitView` clears float/defeat text and deactivates the unit slot. `BattleUnitView.isRemoved` must reset when a later battle starts.
5. The old `PlayDefeatFade` and `DefeatFadeSeconds` paths are absent; no gray/fade/retreat label remains in this flow.
6. Run `tools/verify_immediate_defeat_removal.ps1`, `tools/verify_battle_presentation_polish.ps1`, `tools/verify_battle_presentation_queue.ps1`, `tools/verify_battle_loop.ps1`, `tools/verify_skill_preselection.ps1`, `tools/verify_dual_mode_skill_input.ps1`, `tools/verify_battle_target_feedback.ps1`, and `tools/verify_formation_battle_linkage.ps1`.
7. Unity smoke: defeat any unit. Expected order is final damage float, immediate slot disappearance, no later damage/float against that unit, and a full unit reset on a new battle.
8. Confirm `ShouyouServer/data/shouyou.db` and `Scene_Boot.unity` are unchanged.

Known boundary: battle values still resolve synchronously. This slice only corrects visual retirement and stale-target presentation.

---

## DONE - Claude Review: Todo32-FE-R1-CODE resource wallet PASS (2026-08-11)

Scope: resource-wallet data layer only. Add safe affordability and spending APIs on top of the existing PlayerPrefs-backed reward wallet. No page/UI creation, backend, database, battle formula, AP/CD, asset, or Scene_Boot change.

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/Data/PlayerResourceManager.cs`
- `tools/verify_player_resource_spending.ps1`

Please verify:

1. `CanAfford(id, amount)` returns false for empty id, non-positive amount, or insufficient balance; it performs no write.
2. `TrySpend(id, amount)` rejects all invalid/insufficient cases without calling `SetInt` or `Save`.
3. A successful `TrySpend` reduces only the requested resource by exactly `amount`, never below zero, then persists once.
4. `GrantRewards` keeps its original reward-grant behavior and existing PlayerPrefs key format.
5. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_player_resource_spending.ps1` and `tools/verify_mainline_reward_grant.ps1`.
6. Confirm no changes under `ShouyouServer`, `ShouyouServer/data/shouyou.db`, `Scene_Boot.unity`, battle scripts, or asset directories.

Known boundary: no current screen consumes resources yet. The next main-flow slice can bind this existing API to a concrete stamina or character-development action after product values are defined.

---

## DONE - Claude Review: Todo33-FE-R1-CODE atomic batch spend PASS (2026-08-11)

Scope: resource-wallet data layer only. Add an atomic multi-resource spending overload for future development costs. No page/UI creation, backend, database, battle formula, AP/CD, asset, or Scene_Boot change.

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/Data/PlayerResourceManager.cs`
- `tools/verify_player_resource_batch_spending.ps1`

Please verify:

1. `TrySpend(RewardItem[] costs)` rejects null/empty arrays and every invalid item before any PlayerPrefs write.
2. Duplicate resource ids are aggregated before the affordability pass, so an account cannot overspend by splitting one cost over several entries.
3. All `CanAfford` checks complete before the first `PlayerPrefs.SetInt`; any insufficient resource leaves every balance unchanged.
4. Success subtracts exactly the aggregated amount of each requested resource, never below zero, and calls `PlayerPrefs.Save()` once.
5. Existing `TrySpend(string, int)` and `GrantRewards` semantics remain unchanged.
6. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_player_resource_batch_spending.ps1`, `tools/verify_player_resource_spending.ps1`, and `tools/verify_mainline_reward_grant.ps1`.
7. Confirm no changes under `ShouyouServer`, `ShouyouServer/data/shouyou.db`, `Scene_Boot.unity`, battle scripts, or asset directories.

Known boundary: no UI or product cost table invokes this API yet. The next product decision must define one real consumer (for example a character upgrade or stamina recovery) rather than inventing temporary costs.

---

## DONE - Claude Review: Todo34-FE-R1-CODE training balance display PASS (2026-08-11)

Scope: read-only training-resource display only. No product costs, spending action, stat growth, backend, database, battle, asset, Scene_Boot, or commercial entry change.

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/Data/MainlineStageCatalog.cs`
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/HomePageRouter.cs`
- `tools/verify_training_resource_balance.ps1`

Please verify:

1. `GetKnownRewardTypes()` traverses `DefaultStages` in stable order, deduplicates by reward id, skips invalid entries, and returns copied `RewardItem` objects.
2. `ShowTrainingInfo()` appends current balances through `BuildTrainingResourceBalanceText()` without defining costs or calling `TrySpend`.
3. Each displayed material balance comes from `PlayerResourceManager.Instance.GetCount(reward.id)` and opening the entry produces no `PlayerPrefs.SetInt` or `PlayerPrefs.Save` call.
4. `CloneRewards()` remains behaviorally compatible after reusing `CloneReward()`.
5. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_training_resource_balance.ps1`, `tools/verify_player_resource_batch_spending.ps1`, `tools/verify_player_resource_spending.ps1`, and `tools/verify_mainline_reward_grant.ps1`.
6. Unity Play Mode smoke: obtain at least one mainline reward, open 角色 -> 养成, confirm the shown count matches the reward wallet; reopen without spending and confirm balances remain unchanged.
7. Confirm no changes under `ShouyouServer`, `ShouyouServer/data/shouyou.db`, `Scene_Boot.unity`, battle scripts, resource directories, or payment/recharge code.

Known boundary: this remains a compact text-based entry over the generic detail panel. A dedicated character-development screen, level costs and actual material spending should be a later, separately reviewed gameplay slice.

---

## DONE - Claude Review: Todo35-FE-R1-CODE character leveling loop PASS (2026-08-11)

Scope: first playable Li Qingzhao level-up loop only. No backend/database/Scene_Boot/assets/battle-formula/payment changes.

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/Data/CharacterDevelopmentManager.cs`
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/HomePageRouter.cs`
- `tools/verify_character_leveling_loop.ps1`

Please verify:

1. `GetSnapshot` defaults to level 1, clamps persisted levels into the legal range, and derives health/attack/defense from one canonical formula.
2. `GetNextLevelCosts` returns no cost at max level and returns independent reward objects otherwise.
3. `TryLevelUp` validates unsupported/max-level states before spending; a failed batch spend cannot change the level.
4. Successful upgrade spends exactly the displayed cost, persists only the next level, and returns an understandable result snapshot/message.
5. `ShowCharacterDetail` and `ShowTrainingInfo` both read the same development manager state; training button rebinding does not leave generic story actions active.
6. Run `tools/verify_character_leveling_loop.ps1`, `tools/verify_training_resource_balance.ps1`, `tools/verify_player_resource_batch_spending.ps1`, `tools/verify_player_resource_spending.ps1`, and `tools/verify_mainline_reward_grant.ps1`.
7. Unity Play Mode smoke: gain mainline rewards, enter 角色 -> 养成, level up once, reopen character detail and confirm stats/level refresh; then test insufficient material with no resource or level change.
8. Confirm no changes under `ShouyouServer`, `ShouyouServer/data/shouyou.db`, `Scene_Boot.unity`, battle scripts, resource directories, or payment/recharge code.

Known boundary: level-derived stats are intentionally display-only in this slice; battle stat integration, skill/gear/breakthrough and server-side transactional persistence are separate future tasks.

---

## DONE - Claude Review: Todo36-FE-R1-CODE character battle stat sync PASS (2026-08-11)

Scope: connect Li Qingzhao's existing development snapshot to the initial unit values of a newly started battle only. No backend, database, Scene_Boot, assets, recharge/payment, enemy values, or damage-formula changes.

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- `tools/verify_character_battle_stat_sync.ps1`

Please verify:

1. Only Li Qingzhao takes the isolated development path; both existing `li-qingzhao` formation id and `li_qingzhao` development id are accepted.
2. Her `maxHp` and `attack` come from `CharacterDevelopmentManager.GetSnapshot`, with safe template/default fallback when the snapshot is unavailable.
3. Her action value, speed, portrait and stored terminal fields still use DTO/default values; no new combat formula, crit, hit, dodge, element or buff behavior is introduced.
4. Other allies and enemies still use their pre-existing creation paths and numeric values.
5. `BattleDemoController` has no direct PlayerPrefs access.
6. `CalculateDamage` and enemy creation remain behaviorally unchanged.
7. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_character_battle_stat_sync.ps1`, `verify_character_leveling_loop.ps1`, `verify_formation_battle_linkage.ps1`, and `verify_battle_loop.ps1`.
8. Unity Play Mode smoke: level Li Qingzhao once, start a fresh battle, and compare her HP/attack against Lv.1; confirm other units and the ongoing-battle state are unaffected.

Known boundary: values refresh only when a new battle is constructed. Mid-battle live stat mutation, skill/gear/breakthrough, and backend transactional persistence remain separate tasks.

Review verdict: **PASS** — 1 round, 0 P1 (2026-08-11).

- Scope confirmed: only `BattleDemoController.cs` (+46/-4, `git diff --check` clean, UTF-8 BOM intact) and the new `tools/verify_character_battle_stat_sync.ps1`.
- `IsLiQingzhaoCharacterId` accepts both `li-qingzhao` and `li_qingzhao`; branch sits before the template-null check, so the development path always wins for Li Qingzhao.
- `CreateLiQingzhaoUnitFromDevelopment` reads `CharacterDevelopmentManager.GetSnapshot`, prefers `snapshot.health`/`snapshot.attack`, falls back to template/default; action value/speed/portrait/terminal fields keep DTO/default values (no new combat formula).
- 15-arg `BattleUnitState` constructor and all `BattleUnitDto` fields match the call site; other allies/enemies untouched; no direct PlayerPrefs in `BattleDemoController`; `CalculateDamage` unchanged.
- `verify_character_battle_stat_sync` + 7 regression scripts all exit 0.
- P2 notes: Li Qingzhao Lv.1 attack is now 180 (dev base) vs the old hardcoded 220 (intended snapshot-first behavior, watch for balance); defense not yet wired into battle units; working-tree `Scene_Boot.unity` churn (15945×2 lines) is pre-existing and must be excluded at commit.
- Records: `docs/AI_TASK_LOG.md` → Todo36-FE-R1-REVIEW.
- Still needs Unity Play Mode smoke: level up once, start fresh battle, confirm HP/attack reflect the new level while other units and the ongoing battle are unaffected.

---

## TODO - Claude Review: Todo37-FE-R1-CODE 阵亡单位战斗内回场回归修复

Scope: only preserve the existing `BattleUnitView.isRemoved` state during one live battle. No backend/database/Scene_Boot/assets/recharge or damage-formula changes.

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- `tools/verify_immediate_defeat_removal.ps1`

Please verify:

1. `BindRuntimeReferences()` returns immediately after its first successful binding, before any `BuildView(...)`, so a live battle cannot recreate `BattleUnitView` instances.
2. This guard does not prevent first-time binding from `Awake()` and does not change the explicit fresh-battle reset path in `ResetDemoBattle()`.
3. After `HideDefeatedUnitView()` sets `isRemoved`, pressing main battle, any skill, or automatic battle cannot re-enable the same slot.
4. A defeated unit remains invalid as actor and target; no damage, action, or selection can use it after removal.
5. New battles still restore all slots through the existing `ResetAllUnitViewRemovalState()` path.
6. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_immediate_defeat_removal.ps1`, `verify_battle_loop.ps1`, `verify_battle_presentation_queue.ps1`, `verify_battle_presentation_polish.ps1`, `verify_skill_preselection.ps1`, `verify_dual_mode_skill_input.ps1`, `verify_battle_target_feedback.ps1`, and `verify_formation_battle_linkage.ps1`.
7. Confirm no changes under `ShouyouServer`, `ShouyouServer/data/shouyou.db`, `Scene_Boot.unity`, asset directories, or payment/recharge code.

Manual Unity smoke requested:

1. Defeat one unit and wait for its final damage float to finish.
2. Continue by pressing a normal attack, skill, and auto battle once each.
3. Confirm the defeated slot remains hidden, cannot be selected, cannot act, and cannot receive damage.
4. Return/re-enter a fresh battle and confirm that all normal unit slots are restored for the new battle.

---

## TODO - Claude Review: Todo38-FE-R1-CODE 第一章主线成长闭环

Scope: 新档逐关解锁与结算收口。仅检查以下文件；不得修改后端、数据库、Scene_Boot、资源或支付代码。

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/Data/LevelProgressManager.cs`
- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/HomePageRouter.cs`
- `tools/verify_mainline_progression_rules.ps1`
- `tools/verify_immediate_defeat_removal.ps1`（仅校验脚本定位修正）

Please verify:

1. 新档只从第 1 关开放；已有本地/后端同步进度不会被本改动回退。
2. `CompleteStage` 会拒绝未解锁关卡，同时仍允许当前已解锁关卡首次完成。
3. 首次胜利准确解锁后一关；重复挑战不推进最高进度。
4. 重复挑战资源奖励的既有发放逻辑仍保留，并且结算文案明确“主线进度不变”。
5. 第 6 关首次完成显示本章完成提示，下一关按钮不可交互；继续操作不会回到同一关造成伪循环。
6. 运行 `tools/verify_mainline_progression_rules.ps1`、`verify_mainline_reward_grant.ps1`、`verify_character_leveling_loop.ps1`、`verify_character_battle_stat_sync.ps1`、`verify_player_resource_spending.ps1`、`verify_player_resource_batch_spending.ps1`、`verify_training_resource_balance.ps1`、`verify_formation_battle_linkage.ps1`、`verify_battle_loop.ps1` 与 `verify_immediate_defeat_removal.ps1`。
7. Unity Play Mode 冒烟：以独立测试存档验证“关卡 1 -> 关卡 2 解锁 -> 奖励入账 -> 养成入口可消耗资源 -> 新开战斗使用更新属性”。
8. 确认没有改动 `ShouyouServer`、`ShouyouServer/data/shouyou.db`、`Scene_Boot.unity`、资源目录、支付/充值代码或伤害公式。

---

## TODO - Claude Review: Todo39-FE-R1-CODE 第一章关卡详情与养成引导收口

Scope: 仅收口主线关卡详情的玩家指引。不得修改后端、数据库、Scene_Boot、资源、支付、关卡开放规则或伤害公式。

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/HomePageRouter.cs`
- `tools/verify_mainline_stage_guidance.ps1`

Please verify:

1. 未解锁关卡的详情入口显示“解锁条件”，点击后能明确说明前一关名称；仍不得进入剧情或战斗。
2. `BuildLockedStageRequirementText` 对第 2 至第 6 关解析 `stage.id - 1`，第 1 关不会产生无效前置引用。
3. 奖励预览优先使用 `MainlineStageCatalog.GetRewards(stage.id)`；奖励目录为空或无效时保留 `rewardPreview` 兜底。
4. 李清照等级低于 `recommendLevel` 时显示养成建议；达到推荐等级后不再显示；该提示不改变挑战可用性或数值。
5. 已通关关卡明确标明重复挑战奖励与主线进度的关系，未通关关卡仍保留阅读/战斗入口。
6. 运行 `tools/verify_mainline_stage_guidance.ps1`、`verify_mainline_progression_rules.ps1`、`verify_mainline_reward_grant.ps1`、`verify_character_leveling_loop.ps1`、`verify_character_battle_stat_sync.ps1`、`verify_player_resource_spending.ps1`、`verify_player_resource_batch_spending.ps1`、`verify_training_resource_balance.ps1`、`verify_formation_battle_linkage.ps1`、`verify_battle_loop.ps1`、`verify_immediate_defeat_removal.ps1`。
7. Unity Play Mode 冒烟：依次打开关卡 1、锁定的关卡 2、已通关关卡，检查长文本不遮挡按钮且按钮语义正确。
8. 确认没有改动 `ShouyouServer`、`ShouyouServer/data/shouyou.db`、`Scene_Boot.unity`、资源目录、支付/充值代码、关卡开放规则或伤害公式。

---

## TODO - Claude Review: Todo40-FE-R1-CODE 第一章剧情目录安全读取

Scope: only improve the local first-chapter story catalog read API. Do not modify UI routing, backend, database, Scene_Boot, assets, payment/recharge, stage unlock rules, or damage formulas.

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/Data/MainlineStoryCatalog.cs`
- `tools/verify_mainline_story_catalog.ps1`
- `docs/superpowers/plans/2026-08-13-mainline-story-catalog-api.md`

Please verify:

1. `MainlineStorySequence.LineCount` returns the actual number of lines and tolerates a null line array.
2. `GetLine(index)` returns the configured text for valid indices and an empty string for negative, overflow, null-array, or null-line cases; it must not throw.
3. `TryGet(stageId, out sequence)` returns true only for configured stages and returns false with `sequence == null` for invalid IDs.
4. Legacy `Get(stageId)` still returns the matching sequence for valid IDs and the first sequence for invalid IDs, preserving existing UI callers.
5. `GetStageIds()` returns a fresh ordered value array; caller mutation must not alter the internal catalog.
6. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_mainline_story_catalog.ps1`, plus the existing mainline progression, stage guidance, reward, character-leveling, battle-stat, resource, formation, battle-loop, and immediate-defeat validation scripts.
7. Confirm no edits under `ShouyouServer`, `ShouyouServer/data/shouyou.db`, `Scene_Boot.unity`, asset directories, payment/recharge code, damage formulas, or `HomePageRouter.cs`.

Manual Unity smoke is optional for this data-only task. If performed, open the current first-chapter story route and confirm its existing line playback is unchanged.

---

## TODO - Claude Review: Todo41-FE-R1-CODE 第一章剧情播放状态

Scope: only add a UI-independent first-chapter playback state. Do not modify HomePageRouter, backend, database, Scene_Boot, assets, payment/recharge, stage unlock rules, or battle formulas.

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/Data/MainlineStoryPlaybackState.cs`
- `tools/verify_mainline_story_playback_state.ps1`
- `docs/superpowers/plans/2026-08-13-mainline-story-playback-state.md`

Please verify:

1. `TryStart(stageId)` only accepts configured non-empty story sequences; invalid IDs reset state and return false without any persistence write.
2. `CurrentLine`, `CurrentLineIndex`, and `LineCount` remain safe before start, after reset, and after completion.
3. `TryAdvance()` moves exactly one line when a next line exists; advancing the final line completes the playback and cannot double-write completion.
4. `AdvanceTime()` ignores negative/zero time and completed/unstarted states; `TrySkip()` stays unavailable until exactly 3 seconds of positive elapsed time.
5. Final-line completion and accepted skip both call the existing `LevelProgressManager.MarkStoryRead`; the new class must not access `PlayerPrefs` directly.
6. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_mainline_story_playback_state.ps1`, plus story catalog, mainline progression, stage guidance, reward, character leveling, resource, formation, battle loop, and immediate-defeat regression scripts.
7. Confirm no changes under `ShouyouServer`, `ShouyouServer/data/shouyou.db`, `Scene_Boot.unity`, asset directories, payment/recharge code, damage formulas, or `HomePageRouter.cs`.

Known boundary: this task deliberately does not create or alter a story UI. After Todo39's HomePageRouter review is complete, a later task may bind the existing “开始阅读/回看剧情” entry to this state and verify the player-facing text flow in Unity Play Mode.

---

## TODO - Claude Review: Todo42-FE-R1-CODE 第一章剧情播放 UI 接入

Scope: bind the existing story detail buttons to the already reviewed `MainlineStoryPlaybackState`. Do not modify backend, database, Scene_Boot, assets, payment/recharge, stage unlock rules, or battle formulas.

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/HomePageRouter.cs`
- `tools/verify_mainline_story_playback_ui_integration.ps1`
- `docs/superpowers/plans/2026-08-14-mainline-story-playback-ui-integration.md`

Please verify:

1. `HomePageRouter` has one `MainlineStoryPlaybackState` instance and no legacy line index, elapsed timer, skip-delay constant, or direct `MarkStoryRead(currentMainlineStageId)` write path.
2. `StartStoryReading` handles a missing catalog entry safely and otherwise calls `TryStart` before rendering the first line.
3. `Update` advances the playback timer only while the story detail panel is open and playback is unfinished.
4. `AdvanceStoryReading` delegates to `TryAdvance`; final-line completion and accepted skip render the completion state without duplicate persistence writes.
5. Closing the detail dialog and selecting another stage call `Reset`, while completed read-state remains persisted in `LevelProgressManager`.
6. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_mainline_story_playback_ui_integration.ps1`, `verify_mainline_story_playback_state.ps1`, `verify_mainline_story_catalog.ps1`, and `verify_mainline_stage_guidance.ps1`.
7. Unity Play Mode smoke: stage 1 -> 开始阅读 -> 下一句到结尾; repeat and wait 3 seconds -> 跳过剧情; close/reopen or switch stage -> no old line appears; completed stage shows 重读剧情 and 回看剧情.
8. Confirm no edits under `ShouyouServer`, `ShouyouServer/data/shouyou.db`, `Scene_Boot.unity`, asset directories, payment/recharge code, stage unlock logic, or damage formulas.

---

## TODO - Claude Review: Todo43-FE-R1-CODE 剧情完成后的主线行动引导

Scope: only improve player-facing text after story completion. Do not modify backend, database, Scene_Boot, assets, payment/recharge, stage unlock rules, reward grant behavior, or damage formulas.

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/HomePageRouter.cs`
- `tools/verify_story_completion_guidance.ps1`
- `docs/superpowers/plans/2026-08-14-story-completion-guidance.md`

Please verify:

1. `BuildStoryCompletionGuidance` is read-only: it must not call reward grant, stage completion, backend sync, or write PlayerPrefs.
2. An unread/uncleared stage completion message says rewards are granted only after battle victory and only previews `MainlineStageCatalog.GetRewards`.
3. A cleared non-final stage points to the actual next stage only when `LevelProgressManager.IsStageUnlocked(nextStageId)` is true.
4. The final stage completion branch does not calculate or display a nonexistent stage seven.
5. `CompleteStoryReading` still keeps story-read and battle-clear as independent progress; button configuration remains routed through `ConfigureStoryDetailForMainlineStage`.
6. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_story_completion_guidance.ps1`, plus `verify_mainline_story_playback_ui_integration.ps1`, `verify_mainline_story_playback_state.ps1`, `verify_mainline_story_catalog.ps1`, and `verify_mainline_stage_guidance.ps1`.
7. Unity Play Mode smoke: read or skip an uncleared stage and confirm no resource/clear-record mutation; after battle victory re-read it and confirm the next-stage guidance; repeat for stage six if reachable.
8. Confirm no edits under `ShouyouServer`, `ShouyouServer/data/shouyou.db`, `Scene_Boot.unity`, asset directories, payment/recharge code, stage unlock logic, reward grant code, or damage formulas.

---

## TODO - Claude Review: Todo44-FE-R1-CODE 第一章主线进度概览

Scope: only add read-only chapter progress text to the existing mainline stage detail. Do not modify backend, database, Scene_Boot, assets, payment/recharge, stage unlock rules, reward grant behavior, or damage formulas.

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/HomePageRouter.cs`
- `tools/verify_chapter_progress_overview.ps1`
- `docs/superpowers/plans/2026-08-14-chapter-progress-overview.md`

Please verify:

1. `BuildChapterProgressOverview` reads progress only and does not call stage completion, story persistence, reward grants, backend sync, or PlayerPrefs.
2. The overview is appended only to the existing `ShowMainlineStageDetail` text path and keeps existing detail buttons/configuration unchanged.
3. It uses `GetHighestClearedStageId`, `GetStageStateLabel`, `IsStoryRead`, and `MainlineStageCatalog.Get` to render all six first-chapter stages.
4. Before the final stage is cleared, the next target is the actual next stage; once stage six is cleared, it shows chapter completion and never queries stage seven.
5. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_chapter_progress_overview.ps1`, plus `verify_story_completion_guidance.ps1`, `verify_mainline_story_playback_ui_integration.ps1`, `verify_mainline_story_playback_state.ps1`, `verify_mainline_story_catalog.ps1`, and `verify_mainline_stage_guidance.ps1`.
6. Unity Play Mode smoke: open an uncleared stage, a cleared non-final stage, and stage six if reachable; confirm the overview state is correct and the body does not obscure the existing story-detail buttons.
7. Confirm no edits under `ShouyouServer`, `ShouyouServer/data/shouyou.db`, `Scene_Boot.unity`, asset directories, payment/recharge code, stage unlock logic, reward grant code, or damage formulas.

---

## TODO - Claude Review: Todo44-FE-R2-FIX 章节进度概览测试守卫

Scope: only repair the Todo44 static verification guard. No production C#, backend, database, Scene_Boot, assets, payment/recharge, stage unlock, reward, or damage changes.

Files:

- `tools/verify_chapter_progress_overview.ps1`

Please verify:

1. The script isolates the `BuildChapterProgressOverview` method body by balanced braces before checking forbidden write paths.
2. The forbidden set uses real project APIs: `MarkStoryRead`, `CompleteMainlineStage`, `GrantRewards`, `ShouyouBackendBootstrap`, and `PlayerPrefs`.
3. It does not falsely fail because unrelated HomePageRouter methods legitimately persist story or battle progress.
4. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_chapter_progress_overview.ps1`, `verify_story_completion_guidance.ps1`, and `verify_mainline_story_playback_ui_integration.ps1`.
5. Confirm no production C# file, backend, database, Scene_Boot, resource, payment, unlock, reward, or damage file changed in this fix.

---

## TODO - Claude Review: Todo45-FE-R1-CODE 第一章关卡详情行动入口收口

Scope: clarify the existing first-chapter stage-detail actions only. Do not modify backend, database, Scene_Boot, assets, payment/recharge, stage unlock rules, reward grants, or damage formulas.

Files:

- `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/HomePageRouter.cs`
- `tools/verify_mainline_stage_action_entries.ps1`

Please verify:

1. `ConfigureStoryDetailForMainlineStage` presents a coherent state matrix: locked stages expose only the unlock explanation; unlocked uncleared stages expose read, formation, and first challenge; cleared stages expose replay and challenge again.
2. `OpenFormationFromMainlineStageDetail` only closes the existing detail dialog and opens the existing formation tab. It must not create/save a formation, persist progress, grant rewards, or write player data.
3. Reading mode still configures its own “跳过剧情” action via `ConfigureStoryDetailForReading`; repurposing the idle detail button must not break story playback.
4. `BuildMainlineStageGuidance` is display-only and gives the player the same story/formation/challenge order shown by the buttons.
5. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_mainline_stage_action_entries.ps1`, `verify_mainline_stage_guidance.ps1`, `verify_chapter_progress_overview.ps1`, and `verify_mainline_story_playback_ui_integration.ps1`.
6. Unity Play Mode smoke: inspect a locked stage, an unlocked uncleared stage, and a cleared stage; use “调整编队” and return; start reading and confirm “跳过剧情” still works; verify no resource/progress mutation from opening the detail or switching to formation.
7. Confirm no changes under `ShouyouServer`, `ShouyouServer/data/shouyou.db`, `Scene_Boot.unity`, asset directories, payment/recharge code, stage unlock rules, reward grant code, or damage formulas.

---

## TODO - Claude Review: Todo46-FE-R1-CODE 第一章主线挑战准备闭环

### 审查范围

本轮只调整主线关卡从“详情”进入“战斗”的前端流程：先展示出战准备，再由玩家确认后进入战斗。

- 生产代码：`ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/HomePageRouter.cs`
- 静态验证：`tools/verify_mainline_battle_preparation.ps1`

未改动后端、数据库、`Scene_Boot.unity`、资源、支付、奖励发放、关卡解锁或伤害公式。

### 请重点验证

1. `EnterBattlePrototype()` 不再直接调用 `ShowBattle()`，而是先进入 `ShowBattlePreparation()`。
2. 出战准备能展示当前关卡、推荐战力、当前编队战力和编队摘要；这些都是只读展示，不写入存档或后端。
3. 准备页的四种动作正确：返回关卡详情、调整编队、确认挑战、取消出战。
4. 只有 `StartBattleFromPreparation()` 才调用 `ShowBattle()`；确认前会再次校验关卡是否解锁、是否有可战斗编队。
5. 缺少编队时不会进入战斗，沿用现有 `HasBattleReadyFormation()` 的反馈逻辑。
6. 静态脚本和既有主线/编队回归脚本均应通过；请在 Unity Play Mode 手动验证：开始挑战 -> 出战准备 -> 返回/调整编队/取消/确认挑战。
7. 确认没有越界改动：后端、数据库、`Scene_Boot.unity`、资产、支付、奖励、解锁规则、伤害公式均保持不变。

---

## TODO - Claude Review: Todo47-FE-R1-CODE 自动战斗表现节奏串行化

### 审查范围

本轮只调整自动战斗的前端表现节奏：每次自动行动完成其攻击、受击、飘字和退场表现后，才允许下一段自动行动开始。

- 生产代码：`ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- 静态验证：`tools/verify_auto_battle_presentation_sequence.ps1`

未改动后端、数据库、`Scene_Boot.unity`、资源、支付、奖励、关卡解锁、编队持久化或伤害公式。

### 请重点验证

1. `PerformAutoAttacks()` 只启动 `PerformAutoAttacksRoutine()`，不再包含同步 `while` 连续解析多次攻击。
2. `PerformAutoAttacksRoutine()` 每次 `PerformPlayerAttackInternal()` 后都会通过 `WaitForPresentationQueueToFinish()` 等待表现队列清空。
3. 自动战斗期间输入被锁定；`ResetDemoBattle`、`RetreatBattle` 与 `OnDisable` 均能停止自动协程，不能留下跨页面攻击。
4. `WaitForPresentationQueueToFinish()` 只读取表现状态；不得修改伤害、行动值、技能冷却、奖励、关卡或编队数据。
5. 运行 `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_auto_battle_presentation_sequence.ps1`，以及 `verify_battle_presentation_queue.ps1`、`verify_battle_presentation_polish.ps1`、`verify_battle_loop.ps1`、`verify_battle_action_preview.ps1`、`verify_formation_battle_linkage.ps1` 和 `verify_mainline_battle_preparation.ps1`。
6. Unity Play Mode：点击自动战斗，检查两段以上我方连续行动在视觉上严格串行；自动过程中撤退、重开或离开页面后，不出现旧协程残留攻击。
7. 确认没有越界改动：后端、数据库、`Scene_Boot.unity`、资产、支付、奖励、解锁规则、编队数据及伤害公式均保持不变。

---

## TODO - Claude Review: Todo48-FE-R1-CODE 回合战斗行动逐段结算

### 审查范围

本轮只调整玩家出手后的后续行动推进节奏：敌方出手和已预选我方技能都必须等待各自攻击表现结束后，才推进下一位行动者。

- 生产代码：`ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- 静态验证：`tools/verify_turn_resolution_sequence.ps1`

未改动后端、数据库、`Scene_Boot.unity`、资源、支付、奖励、解锁规则、编队或伤害公式。

### 请重点验证

1. `CompletePlayerAction()` 只创建 `ResolveFollowUpActionsRoutine()`，不再同步 while 循环结算多个敌方/预选行动。
2. 协程在玩家本次攻击表现结束后才移动至下一行动者；每次敌方攻击或预选技能后均等待 `WaitForPresentationQueueToFinish()`。
3. `resolvingEnemyTurn` 在后续结算期间锁定输入，正常结束、重置、撤退和离开页面时均会恢复或清理。
4. `FinishFollowUpResolution`、`StopFollowUpResolutionRoutine` 不改变伤害、行动点、冷却、奖励或关卡状态。
5. 运行 `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_turn_resolution_sequence.ps1`，并回归 Todo47 的全部战斗验证脚本。
6. Unity Play Mode：玩家出手后连续观察至少两名敌人的出手；预选一个非当前我方角色技能，确认其到行动位时单独展示；表现期间不允许重复点击技能或开始战斗。
7. 确认没有越界改动：后端、数据库、`Scene_Boot.unity`、资产、支付、奖励、解锁、编队数据及伤害公式均保持不变。

---

## TODO - Claude Review: Todo49-FE-R1-CODE 自动战斗完整行动链接力

### 审查范围

本轮只修复自动战斗的行动衔接：本次攻击与其敌我后续行动链结束后，如果重新轮到我方，则自动继续下一次攻击。

- 生产代码：`ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- 静态验证：`tools/verify_auto_battle_turn_handoff.ps1`

未改动后端、数据库、`Scene_Boot.unity`、资源、支付、奖励、解锁规则、编队或伤害公式。

### 请重点验证

1. `PerformAutoAttacksRoutine()` 在每次 `PerformPlayerAttackInternal()` 后等待表现队列和 `WaitForFollowUpResolutionToFinish()`。
2. 自动战斗不会因敌方行动开始就提前停止；再次轮到我方时自动继续，直至胜负、撤退、重置或安全退出。
3. `WaitForFollowUpResolutionToFinish()` 只观测协程状态，不改变伤害、行动值、技能冷却、奖励或关卡状态。
4. 运行 `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_auto_battle_turn_handoff.ps1`，并回归 Todo48、Todo47 战斗验证。
5. Unity Play Mode：观察两次以上我方自动行动及中间敌方/预选行动；自动中撤退、重置、切页后没有残留攻击或输入锁。
6. 确认没有越界改动：后端、数据库、`Scene_Boot.unity`、资产、支付、奖励、解锁、编队数据及伤害公式均保持不变。

---

## TODO - Claude Review: Todo50-FE-R1-CODE 战斗行动链过程反馈同步

### 审查范围

本轮仅补齐已存在行动链的过程反馈：开始结算时立即反映输入锁定；每个敌方或预选我方行动完成后更新战斗提示。

- 生产代码：`ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- 静态验证：`tools/verify_battle_action_feedback_sync.ps1`

未改动后端、数据库、`Scene_Boot.unity`、资源、支付、奖励、解锁规则、编队或伤害公式。

### 请重点验证

1. `ShowResolvingActionLog()` 仅更新展示文本并刷新界面，不写入数值、状态、奖励或存档。
2. `CompletePlayerAction()` 在设置 `resolvingEnemyTurn` 后立即调用过程反馈；敌方攻击和预选技能各结算一次后也会刷新提示。
3. 行动链期间由既有 `IsBattleInputLocked()` 使技能、自动和开始入口不可重复触发；结束、撤退、重置与切页后锁定可正常清理。
4. 运行 `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_battle_action_feedback_sync.ps1`，并回归 Todo49、Todo48、Todo47 的战斗验证脚本。
5. Unity Play Mode：观察我方行动、至少一名敌方行动和一项预选技能行动，确认文本逐段变化、操作入口不可连点，胜负结算仍能到达。
6. 确认没有越界改动：后端、数据库、`Scene_Boot.unity`、资产、支付、奖励、解锁、编队数据及伤害公式均保持不变。
