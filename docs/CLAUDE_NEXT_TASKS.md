# Claude Code Next Tasks

Updated: 2026-08-09 11:40 Asia/Shanghai

This file is a handoff board, not the execution log. Please write review results to `docs/AI_TASK_LOG.md`.

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

**下一优先级：战斗表现完善（先）→ 第一章主流程闭环（后）。**
仍待人工：Unity Play Mode 验证 Todo26 多事件连播/退出残留/胜负结算时序。

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
