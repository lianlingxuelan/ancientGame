# Schema First Battle Rewards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add future-ready battle-unit and reward schemas without changing current battle damage, backend, or database behavior.

**Architecture:** Extend the existing battle DTO and runtime state with inert fields that have safe defaults. Add a reusable reward DTO, keep a catalog-owned reward map keyed by stage ID (so the four-file production-code limit is preserved), and render valid reward entries with the existing text preview as fallback.

**Tech Stack:** Unity 2020.3, C#, Unity UI, JsonUtility.

## Global Constraints

- Modify only BattleDemoController.cs, ShouyouApiModels.cs, MainlineStageCatalog.cs, and HomePageRouter.cs for production code.
- Do not add a Controller, change the backend, delete or regenerate shouyou.db, or alter ApplyDamage behavior.
- `speed` is only a secondary action-order key after `actionValue`; default is 100.
- `critRate`, `critDamage`, `hitRate`, `dodgeRate`, `element`, and `buffIds` are stored only and must not affect battle calculation or UI.
- `MainlineStageInfo.cs` is outside the four-file scope, so rewards are queried through `MainlineStageCatalog.GetRewards(stageId)` rather than adding a field to that type.
- Catalog rewards render when valid; otherwise `rewardPreview` remains the visible fallback.

---

### Task 1: Establish source-contract checks for schema and ordering

**Files:**
- Modify: `Assets/_Project/Scripts/UI/BattleDemoController.cs`
- Modify: `Assets/_Project/Scripts/Network/ShouyouApiModels.cs`

**Interfaces:**
- Produces `BattleUnitDto` and `BattleUnitState` fields needed by future battle systems.
- Produces `BuildActionOrder()` ordering: actionValue desc, speed desc, existing ally tie-break after both.

- [ ] **Step 1: Run an inline source-contract check before implementation**

Run a PowerShell assertion that requires all eight new battle fields and the speed tie-break expression. Expected result: fail because those members do not exist yet.

- [ ] **Step 2: Add inert DTO/runtime fields and safe fallbacks**

Add `speed`, `critRate`, `critDamage`, `hitRate`, `dodgeRate`, `element`, `starLevel`, `breakLevel`, and `buffIds`; set defaults so old payloads preserve prior behavior. Add speed only as the second comparator in `BuildActionOrder()`.

- [ ] **Step 3: Re-run the source-contract check**

Expected result: pass. Confirm no damage method references any inert field.

### Task 2: Add reward data and safe settlement rendering

**Files:**
- Modify: `Assets/_Project/Scripts/Network/ShouyouApiModels.cs`
- Modify: `Assets/_Project/Scripts/Data/MainlineStageCatalog.cs`
- Modify: `Assets/_Project/Scripts/UI/HomePageRouter.cs`

**Interfaces:**
- Produces `RewardItem[] MainlineStageCatalog.GetRewards(int stageId)`.
- Consumes catalog rewards in `ShowBattleVictoryDetail()` and falls back to `rewardPreview`.

- [ ] **Step 1: Run an inline source-contract check before implementation**

Require `RewardItem`, `rewards`, a reward formatter, and a fallback branch in the four target files. Expected result: fail because the reward schema does not exist yet.

- [ ] **Step 2: Implement the minimal reward schema and seeded local rewards**

Define every RewardItem terminal field. Seed one or two lightweight local rewards per current stage. Preserve existing `rewardPreview` values unchanged.

- [ ] **Step 3: Render rewards defensively**

Format only valid reward entries. When the array is null, empty, or has no renderable entries, use `rewardPreview` exactly as today.

- [ ] **Step 4: Re-run source-contract checks and diff hygiene checks**

Expected result: all checks pass; targeted diff check has no whitespace errors.

### Task 3: Record and hand off for Unity review

**Files:**
- Append: `docs/AI_TASK_LOG.md`
- Append: `docs/CLAUDE_NEXT_TASKS.md`

- [ ] **Step 1: Append a protocol-compliant CODE_DONE record**

Record all four code paths, no DB impact, static verification, and Unity Editor review limits.

- [ ] **Step 2: Append the 7-point Claude review checklist**

Cover compiler health, old payload fallback, ordering, inert damage fields, reward/fallback rendering, database protection, and no scope creep.

## Self-Review

- Scope coverage: Tasks 1-2 implement all eight agreed constraints; Task 3 records them for review.
- Placeholder scan: no undecided implementation steps or omitted fallback behavior.
- Type consistency: `RewardItem` is declared in `Shouyou.Network`; `MainlineStageCatalog.cs` imports that namespace; `HomePageRouter.cs` already imports it.
