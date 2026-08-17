# 第一章战斗闭环强化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让第一章的编队、回合、目标选择、技能可用状态与结算入口形成稳定可重复的 PVE 闭环。

**Architecture:** 保持 `BattleDemoController` 为战斗运行时状态的唯一权威；编队以 `demo-config` 为优先数据源，本地编队仅作为接口不可用时的兜底。`HomePageRouter` 只负责页面与结算跳转，不参与伤害或行动公式。

**Tech Stack:** Unity 2020.3、C#、Unity UI、既有 5188 本地接口。

## Global Constraints

- 不修改后端、不删除或重建 `ShouyouServer/data/shouyou.db`。
- 不修改现有伤害公式，不提前实现暴击、命中、元素或 Buff。
- 不引入梦域第二、三批内容。
- 所有新增或修改的 C# 逻辑写中文注释。

---

### Task 1: 战斗启动与阵容兜底校验

**Files:**
- Modify: `Assets/_Project/Scripts/UI/BattleDemoController.cs`

**Interfaces:**
- Consumes: `battleConfig.allies`、本地编队摘要。
- Produces: 合法的可行动友方阵容，或清晰的不可开始提示。

- [x] 启动战斗前确认至少一名友方存活单位及至少一名敌方存活单位。
- [x] 后端阵容存在时始终优先使用它，接口不可用时再使用本地编队。
- [x] 无有效阵容时禁用开始与技能入口，避免进入无角色战斗。

### Task 2: 回合、目标与技能交互收口

**Files:**
- Modify: `Assets/_Project/Scripts/UI/BattleDemoController.cs`

**Interfaces:**
- Consumes: `currentActor`、选中目标、AP、技能冷却。
- Produces: 仅当前友方行动者可使用技能，且不会攻击空位或阵亡目标。

- [x] 选中阵亡/空位时给出原因提示并保持当前有效目标。
- [x] 当前行动者、选中目标、技能不可用原因在战斗提示区保持一致。
- [x] 战斗结束后禁止一切技能与头像操作，避免重复结算。

### Task 3: 反馈与结算路径稳定化

**Files:**
- Modify: `Assets/_Project/Scripts/UI/BattleDemoController.cs`
- Modify: `Assets/_Project/Scripts/UI/HomePageRouter.cs`

**Interfaces:**
- Consumes: 单位死亡、胜利、失败、撤退状态。
- Produces: 伤害/治疗/阵亡反馈，以及唯一一次生效的结算跳转。

- [x] 阵亡目标显示明确文案并自动从后续行动中跳过。
- [x] 胜利、失败、撤退各自只触发一次，结算按钮只执行一个路由动作。
- [x] 胜利结算保留逐条奖励；失败不发奖；撤退不推进关卡。

### Task 4: 回归验证与 Claude 交接

**Files:**
- Modify: `docs/AI_TASK_LOG.md`
- Modify: `docs/CLAUDE_NEXT_TASKS.md`

- [x] 静态检查改动 C# 的引用、括号与空白差异。
- [ ] 在 Unity Play Mode 验证第一章：战斗启动、单体/群体/治疗、胜利、失败、撤退（交给 Claude/Unity 回归）。
- [x] 向 Claude 提交只读审查清单；不要求其改后端或数据库。
