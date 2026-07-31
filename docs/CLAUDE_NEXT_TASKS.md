# Claude Code Next Tasks

Updated: 2026-07-31 13:30 Asia/Shanghai

This file is a handoff board, not the execution log. Please write review results to `docs/AI_TASK_LOG.md`.

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

## Frontend-Review-R1: Unity battle API integration

Priority: P1

Please review Codex changes:

1. `ShouyouApiModels.cs` matches backend response shapes for battle config and battle skill assets.
2. `ShouyouApiClient.cs` decodes response bodies from UTF-8 bytes before `JsonUtility.FromJson`.
3. `BattleDemoController.cs` initializes allies, enemies, action points, and skills from `/api/v1/battle/demo-config` when available.
4. Wanhe appears in battle slot2 and is not overridden by old formation/DB state.
5. Skill buttons remain clickable and use backend labels/icons when the API is available; if API fails, text fallback remains usable.
6. Unity Play Mode has no compiler errors.

Please append a REVIEW record to `docs/AI_TASK_LOG.md`. Do not delete user assets. Do not delete or regenerate the DB without explicit user approval.

## Claude Review Request - Todo14-FE-R2-FIX (2026-07-30 08:02:18 Asia/Shanghai)

Scope: Unity frontend battle demo compile fix.

Please verify:
1. Unity Console has no CS0103 errors from Assets/_Project/Scripts/UI/BattleDemoController.cs.
2. Play Mode can enter battle page from mainline without Safe Mode or compiler lock.
3. Backend 5188 running: battle demo loads Li Qingzhao in slot1 and Wanhe in slot2 when backend config is available.
4. Skill buttons are visible and clickable. Icon download can fail gracefully, but text fallback must remain usable.
5. Do not delete or regenerate ShouyouServer/data/shouyou.db unless user explicitly approves.

Known not in scope:
- Final battle balance.
- Final UI polish.
- Database cleanup.

## Claude Review Request - Todo15-FE-R1-CODE (2026-07-30 08:31:32 Asia/Shanghai)

Scope: battle skill icon rendering.

Please verify:
1. Backend 5188 is running.
2. Enter Unity Play Mode -> battle page.
3. The four bottom skill buttons show real skill icons, mapped as:
   - basic -> skill_basic_attack
   - poetry_strike -> skill_poetry_attack
   - dream_area -> skill_group_damage
   - healing_verse -> skill_heal
4. Button labels remain visible and buttons remain clickable.
5. Do not delete ShouyouServer/data/shouyou.db.

If this fails, check whether /api/v1/assets?category=battle_skill returns URLs for the four icon keys above.

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

## Claude Review Request - Todo16-FE-R1-CODE (2026-07-30 23:20 Asia/Shanghai)

Scope: Unity frontend battle core loop.

Please verify:
1. Unity Console has no compiler errors from `BattleDemoController.cs` or `ShouyouApiModels.cs`, and Play Mode starts normally.
2. With backend 5188 running, `demo-config` provides Li Qingzhao in slot 1 and Wanhe in slot 2; battle formation text and unit slots match that payload.
3. Battle order follows `actionValue`; only the green-highlighted active ally can use skills. Selecting another ally must not change the actor.
4. Poetry strike, area strike, and heal spend action points and enter their backend-configured cooldown; a new round restores action points and reduces cooldowns.
5. Enemy units take their turns automatically after friendly turns, ignore defeated units, and do not cause an infinite loop.
6. Victory, defeat, and retreat routes remain reachable; no extra database write or reward is introduced by this task.
7. Review the existing BattleSkillAssetRoute-R1 backend fix separately; do not delete or regenerate `ShouyouServer/data/shouyou.db`.

If a backend follow-up is desired, add optional `actionValue` values to battle unit JSON. This is backward compatible because the frontend already supplies fallback values.

## Claude Review Request - Todo17-FE-R1-CODE (2026-07-31 09:20 Asia/Shanghai)

Scope: Chapter One configurable mainline loop.

Please verify:

1. Unity compiles with no errors in `MainlineStoryCatalog.cs`, `LevelProgressManager.cs`, `HomePageRouter.cs`, and `BattleDemoController.cs`.
2. In Play Mode, select stage 1-1 -> start reading -> advance each line. Finishing must record only story-read state, not stage-clear state.
3. Skip must not complete during the first three seconds of active reading; after that it must write the same story-read state.
4. Reopening the stage detail after reading must show `已阅读` and expose `回看剧情`.
5. Select a stage, enter battle, and confirm the battle prompt reports that selected stage title and ID.
6. Winning a stage must show that stage's configured reward preview, unlock only the next stage, and preserve repeat-challenge behavior.
7. Confirm no backend, database, or raw asset files were changed by this frontend task.

No backend work is required for this review. If a defect is found, append a REVIEW record with an explicit verdict to `docs/AI_TASK_LOG.md`.
