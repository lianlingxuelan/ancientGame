# Mainline Story Catalog API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Provide a safe, reusable read API for the existing six Chapter One story sequences without changing current UI routing or story text.

**Architecture:** Keep all changes inside `MainlineStoryCatalog.cs`. The existing `Get(stageId)` fallback remains compatible with current UI behavior, while new query methods let later stage-detail and story-replay UI detect invalid IDs and enumerate the chapter safely.

**Tech Stack:** Unity 2020.3 C#, PowerShell static validation.

## Global Constraints

- Do not modify `HomePageRouter.cs`, `Scene_Boot.unity`, backend, database, assets, recharge/payment, or damage formulas.
- Use Chinese comments for new C# logic.
- Add only append-only task and review records under `docs/`.
- Do not stage, commit, push, or delete files.

---

### Task 1: Story catalog safe-query API

**Files:**

- Modify: `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/Data/MainlineStoryCatalog.cs`
- Create: `tools/verify_mainline_story_catalog.ps1`

**Interfaces:**

- Produces: `MainlineStorySequence.LineCount`, `MainlineStorySequence.GetLine(int)`, `MainlineStoryCatalog.TryGet(int, out MainlineStorySequence)`, `MainlineStoryCatalog.GetStageIds()`.
- Preserves: `MainlineStoryCatalog.Get(int)` returns the first sequence as a non-null fallback for legacy callers.

- [ ] **Step 1: Write the failing test**

Create a PowerShell static validator that requires all four APIs and the six configured sequence IDs.

- [ ] **Step 2: Run test to verify it fails**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_mainline_story_catalog.ps1`

Expected: FAIL because the new safe-query APIs do not exist yet.

- [ ] **Step 3: Write minimal implementation**

Add the four APIs with bounds checking, defensive stage-ID copying, and a `TryGet` method that returns `false` with a null sequence for invalid IDs. Refactor `Get` to reuse that lookup and retain its existing first-stage fallback.

- [ ] **Step 4: Run test to verify it passes**

Run the new validator and the related mainline progression, stage-guidance, reward-grant, character-leveling, formation-linkage, battle-loop, and immediate-defeat validators.

- [ ] **Step 5: Handoff**

Append one protocol-compliant `Todo40-FE-R1-CODE` record and a separate Claude review request. Do not commit.
