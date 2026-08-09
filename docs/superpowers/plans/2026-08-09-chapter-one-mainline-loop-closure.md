# Chapter One Mainline Loop Closure (第一章主流程闭环)

日期：2026-08-09
阶段：Todo28-FE-R1
agent：claude（自主执行，用户已授权）

## 背景

第一章主流程（关卡选择 → 战斗 → 结算 → 解锁下一关 → 剧情节点）在 Todo13-17 已基本实现，
且解锁持久化（PlayerPrefs + 后端双写）。经通读确认，唯一让"闭环"名不副实的缺口是：

- **结算奖励只有文字展示，从不实际入账**：`ShowBattleVictoryDetail` 用 `BuildBattleRewardText`
  拼出"铜钱 ×1200"等文案，但没有调用任何"加钱/加物品"的逻辑，玩家通关后什么都没真正拿到。

本轮只补这一环，其余环节维持现状。

## 范围（In Scope）

1. 新增 `Shouyou.Data.PlayerResourceManager`（非 MonoBehaviour 单例，PlayerPrefs 持久化）：
   - `GetCount(id)`：读持有数量，未记录返回 0。
   - `GrantRewards(RewardItem[])`：按 `RewardItem.id` 累计入账，跳过无效条目，入账后立即 Save。
   - 键前缀 `Shouyou.Player.Resource.{id}`，覆盖 coin/jade/poetry_exp/letter/break_material/
     dream_fragment/cg_progress。
2. `HomePageRouter.ShowBattleVictoryDetail`：结算时先 `GrantRewards` 实际入账，再渲染文案。
3. 新增 `BuildResourceBalanceText(RewardItem[])`：按 id 去重，读取入账后余额，拼"当前持有：名称 ×数量"。
4. 清理 `HomePageRouter` 三处历史遗留 `???` 乱码注释（517-519 / 767-769 / 779-780）。
5. 新增静态校验 `tools/verify_mainline_reward_grant.ps1`（纯 ASCII）。

## 范围外（Out of Scope，仅记录）

- 后端背包/货币系统（Demo 阶段不引）。
- 体力（"体力消耗：6" 仍为展示文案，无扣除/回复系统）。
- 后端 `StageDto` 增加 rewards 字段（奖励表仍由前端 `DefaultRewardsByStageId` 提供）。
- 结算"下一关"按钮直达战斗（当前跳下一关详情属设计选择，维持）。
- 主线页常驻资源余额栏（本轮只做结算弹窗内展示，后续可加顶栏）。

## 交互结果（通关后结算弹窗示意）

```
战斗胜利
……
结算奖励：
铜钱 ×1200
词意经验 ×80
当前持有：铜钱 1200
当前持有：词意经验 80
主线进度 +1
```

## 验收

- 静态：`tools/verify_mainline_reward_grant.ps1` PASS；其余 8 项 verifier 回归 PASS。
- C# 括号配平 OK；`git diff --check` 无 trailing whitespace。
- `ShouyouServer/data/shouyou.db` 未修改。
- Unity Play Mode（人工）：通关后看结算"当前持有"随次数累加；重复挑战只加资源不推进度；
  重置/退出战斗后资源保留。
