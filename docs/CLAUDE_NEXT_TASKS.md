# Claude Code Next Tasks

Updated: 2026-07-29 21:30 Asia/Shanghai

This file is a handoff board, not the execution log. Please write review results to `docs/AI_TASK_LOG.md`.

## Backend-Check-R1: health endpoint and UTF-8 JSON headers

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
