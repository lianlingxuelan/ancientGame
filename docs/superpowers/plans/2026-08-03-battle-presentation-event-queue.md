# Battle Presentation Event Queue Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make each already-resolved battle action play as a readable ordered UI sequence without changing damage, action value, AP, cooldown, backend, or database rules.

**Architecture:** `BattleDemoController` records presentation-only events while the existing resolver produces its normal results. A single coroutine consumes the FIFO queue: attacker highlight, target impact/floating text, then defeat emphasis. The queue blocks further player input until the recorded visuals finish; it never decides battle results.

**Tech Stack:** Unity 2020.3, C#, Unity UI, existing static PowerShell verifiers.

## Global Constraints

- Do not modify `ShouyouServer`, `shouyou.db`, damage/heal formulas, AP, cooldown, action-value ordering, or settlement rules.
- Do not create a new controller or import animation/timeline packages.
- Use Chinese comments for newly added C# logic.
- Portrait attack-effect event remains the extension point for later Animator/Timeline/particle assets.

---

### Task 1: Add presentation-only event data and queue guards

**Files:**
- Modify: `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- Create: `tools/verify_battle_presentation_queue.ps1`

**Interfaces:**
- Produces: `BattlePresentationEvent`, `QueuePresentationEvent(...)`, and `IsBattleInputLocked()`.
- Consumes: existing `BattleUnitState` and `BattleUnitView` mappings.

- [ ] Write a verifier requiring the event type, FIFO queue, player coroutine, and input lock.
- [ ] Run it before the controller change and confirm it fails because the feature symbols are absent.
- [ ] Add the smallest presentation-only data object and private queue state.
- [ ] Run the verifier and confirm this first contract passes.

### Task 2: Record existing attacks, hits, heals, and defeats as visual events

**Files:**
- Modify: `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- Modify: `tools/verify_battle_presentation_queue.ps1`

**Interfaces:**
- Consumes: existing `RequestPortraitAttackEffect`, `ShowDamageText`, `ShowHealText`, and `ApplyDamage` calls.
- Produces: one ordered presentation sequence per existing battle action.

- [ ] Add verifier assertions for attack, damage/heal and defeat event recording.
- [ ] Run the verifier and confirm the assertions fail before implementation.
- [ ] Queue attack before event subscribers are notified; queue damage/heal instead of starting independent coroutines; queue defeat only when `ApplyDamage` changes a living unit to defeated.
- [ ] Run the verifier and confirm it passes.

### Task 3: Play the queued visuals serially and lock input while playing

**Files:**
- Modify: `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`
- Modify: `tools/verify_battle_presentation_queue.ps1`

**Interfaces:**
- Produces: `PlayPresentationQueue()` and per-event UI pulses using existing slot/portrait/text UI.
- Preserves: all existing battle outcome calculations and `PortraitAttackEffectRequested` payload.

- [ ] Add verifier assertions for FIFO consumption, visual refresh, and control lock.
- [ ] Run the verifier and confirm the new assertions fail before implementation.
- [ ] Implement a single coroutine that highlights attacker, displays one hit/heal text, flashes the target, and displays defeat in FIFO order.
- [ ] Gate player-facing combat buttons and callbacks with the presentation lock; do not block resolver-internal enemy/queued processing.
- [ ] Reset/stop visual queue safely when the battle resets or view disables.
- [ ] Run all relevant static verifiers and `git diff --check`.

### Manual Unity Smoke Test

1. Start battle and use a basic attack: observe attacker pulse, target hit pulse, one damage text, then normal next action.
2. Queue and then execute each of the three big skills: verify sequential texts rather than all hit/heal texts appearing together.
3. Let an enemy act: verify it uses the same ordered presentation.
4. Defeat one unit: verify the defeat label is shown after its hit event and the unit cannot act afterward.
5. During a presentation, repeatedly click a skill/start button: verify no second action starts.
6. Verify victory, defeat, retreat, reset, and backend-offline fallback still work.
