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
