# Portrait Attack Effect Hook Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reserve a stable battle-front-end hook that identifies the attacking unit and its skill so a portrait animation can be attached later.

**Architecture:** `BattleDemoController` creates an immutable request when an attack begins and emits it through a C# event. The request carries only presentation data: attacker side/slot/name, skill id, target side/slot/name, and area-target flag. No animator, particle, asset, backend, database, damage calculation, or battle result logic changes in this task.

**Tech Stack:** Unity 2020.3, C#, Unity UI, PowerShell static-contract verification.

## Global Constraints

- Modify only the Unity battle front end and project documentation/testing files.
- Keep all newly written C# comments in Chinese.
- Do not modify `ShouyouServer`, `shouyou.db`, assets, damage formulas, AP/CD rules, or battle settlement.
- Do not commit, stage, or push; Claude Code owns review and commits.

---

### Task 1: Presentation-request contract and emission points

**Files:**
- Create: `tools/verify_portrait_attack_effect_hook.ps1`
- Modify: `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- Test: `tools/verify_portrait_attack_effect_hook.ps1`

**Interfaces:**
- Produces: `BattlePortraitEffectRequest` and `PortraitAttackEffectRequested` for a later portrait-animation presenter.
- Produces: `RequestPortraitAttackEffect(...)`, called before the existing damage paths mutate hit points.

- [ ] **Step 1: Write the failing static contract check**

Require the request type, event, dispatch method, and attacker identity fields in `BattleDemoController.cs`.

- [ ] **Step 2: Run the contract check and verify RED**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_portrait_attack_effect_hook.ps1`

Expected: failure because the portrait effect request contract is not yet present.

- [ ] **Step 3: Add the minimal presentation-only contract**

Define a request object and event in `BattleDemoController`, then invoke the event immediately before basic, single-target skill, area skill, and enemy attack damage are applied.

- [ ] **Step 4: Run the contract check and verify GREEN**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_portrait_attack_effect_hook.ps1`

Expected: `Portrait attack effect hook static validation passed.`

- [ ] **Step 5: Verify scope**

Run `git diff --check` for edited text files and `git diff -- ShouyouServer/data/shouyou.db` to confirm the database remains untouched.
