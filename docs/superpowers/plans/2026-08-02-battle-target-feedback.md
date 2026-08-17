# 战斗目标选择反馈 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在不改变行动值、技能、伤害或接口的前提下，让当前行动者与已选敌方目标在战场头像区更容易辨认。

**Architecture:** `BattleDemoController` 继续作为 Demo 战斗的展示协调者。它根据已有的 `currentActor`、`selectedEnemyIndex` 和 `selectedAllyIndex` 生成回合提示及头像底板颜色；不新增控制器，也不改变现有点击、伤害和结算路径。

**Tech Stack:** Unity 2020.3、UGUI、C#、PowerShell 静态契约校验。

## Global Constraints

- 仅改前端展示层；不改后端、数据库、伤害公式、AP/CD、结算数据或资产。
- 保持“行动值决定当前行动者；点击头像只查看我方状态或选择敌方目标”。
- 所有新增 C# 注释使用中文。
- 不提交、暂存或推送 Git；`shouyou.db` 禁止改动。

---

### Task 1: 目标与行动状态的可视反馈

**Files:**
- Create: `tools/verify_battle_target_feedback.ps1`
- Modify: `ShouyouPrototype/ShouyouPrototype/Assets/_Project/Scripts/UI/BattleDemoController.cs`

**Interfaces:**
- Consumes: `currentActor`、`selectedEnemyIndex`、`selectedAllyIndex` 及既有 `BattleUnitView`。
- Produces: `BuildBattleRoundTip()`、`GetSelectedEnemyName()`、`GetSlotBackgroundColor(...)`。

- [ ] **Step 1: 写入失败的静态契约校验**

校验新提示、目标名读取、区分敌我卡片的底板颜色方法以及两个阵营调用点。

- [ ] **Step 2: 运行校验确认 RED**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_battle_target_feedback.ps1`

Expected: FAIL，提示缺少目标反馈契约。

- [ ] **Step 3: 最小实现**

为回合顶部文本追加当前目标；为当前行动者、我方查看对象与敌方目标提供不同的卡片色彩和描边；保留现有阵亡灰化与点击逻辑。

- [ ] **Step 4: 运行静态校验与既有战斗回归校验**

Run:
`powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_battle_target_feedback.ps1`
`powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify_battle_loop.ps1`

Expected: 两条脚本均 PASS。
