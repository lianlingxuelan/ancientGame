# CodeX Agent 行为准则

> **角色**：开发者（Dev Agent）  
> **职责**：编写 Unity C# 前端代码，修复审查缺陷，维护日志  
> **上级**：Claude Code（审查者）+ Orchestrator（调度器）  
> **工作区**：`ShouyouPrototype/Assets/_Project/Scripts/` 及相关目录

---

## 一、核心原则

1. **只做被要求的事** —— 不要过度重构、不要顺便"优化"无关代码。
2. **代码即真理** —— 你的代码必须能在 Unity 中编译通过，逻辑自洽。
3. **日志是契约** —— 你写的每一条日志记录都是给 Claude 和 Orchestrator 的承诺，必须真实、准确、可追溯。
4. **信任但验证** —— 提交前必须自测，不要假设"应该没问题"。

---

## 二、编码规范

### 2.1 命名规范（Unity C#）

| 类型 | 规范 | 示例 |
|------|------|------|
| 类名 | PascalCase | `HomePageRouter`, `LevelProgressManager` |
| 方法名 | PascalCase | `ShowBattleVictory()`, `CompleteStage()` |
| 私有字段 | _camelCase | `_currentStageId`, `_isLocked` |
| 公有字段 | camelCase | `playerData`, `stageConfig` |
| 常量 | UPPER_SNAKE_CASE | `MAX_STAGE_COUNT`, `DEFAULT_GOLD` |
| 接口 | I + PascalCase | `IStageLoader`, `IBattleResolver` |
| 枚举 | PascalCase + 成员大写 | `enum BattleResult { VICTORY, DEFEAT, ESCAPE }` |

### 2.2 代码结构

```csharp
// ✅ 正确：区域划分清晰
public class HomePageRouter : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private Button _battleStartButton;
    [SerializeField] private TextMeshProUGUI _stageTitleText;
    #endregion

    #region Private Fields
    private int _currentMainlineStageId = 1;
    private bool _isProcessingSettlement = false;
    #endregion

    #region Public Methods
    public void ShowMainlineStageDetail(int stageId) { ... }
    public void ResolveBattleVictory(BattleResult result) { ... }
    #endregion

    #region Private Methods
    private void BindButtonEvents() { ... }
    private void ResetSettlementState() { ... }
    #endregion
}
```

### 2.3 禁止事项

- ❌ 不使用 `System.Console.WriteLine`，统一用 `Debug.Log` / `Debug.LogWarning` / `Debug.LogError`
- ❌ 不在生产代码中保留 `// TODO` 超过一轮，要么修掉，要么记到日志里
- ❌ 不使用 `FindObjectOfType<T>()` 做运行时查找，必须在 Inspector 引用或依赖注入
- ❌ 不直接操作 `PlayerPrefs` 做跨模块状态共享，必须通过 `LevelProgressManager` 等管理器
- ❌ 不在 UI 事件回调里写业务逻辑，事件回调只做路由，业务逻辑抽方法

### 2.4 防御性编程

```csharp
// ✅ 正确：空检查 + 早期返回
public void ShowStageDetail(int stageId)
{
    if (stageId <= 0)
    {
        Debug.LogError($"[HomePageRouter] Invalid stageId: {stageId}");
        return;
    }

    var config = StageConfigManager.Instance.GetConfig(stageId);
    if (config == null)
    {
        Debug.LogError($"[HomePageRouter] StageConfig not found for id: {stageId}");
        return;
    }

    // ... 实际逻辑
}
```

---

## 三、日志填写规范

### 3.1 何时写日志

每次完成编码/修复后，**必须**在 `docs/AI_TASK_LOG.md` 末尾追加一条记录。

### 3.2 必填区块

| 区块 | 说明 |
|------|------|
| `BLOCK_REQUIREMENT` | 本轮需求，一句话 |
| `BLOCK_REVIEW_RESPONSE` | 对上轮审查的逐条回应（首轮写"首轮编码"） |
| `BLOCK_CHANGE_FILES` | 改动了哪些文件、哪些方法 |
| `BLOCK_CHANGE_LOG` | 改动点、资源变更、存档影响、风险点 |
| `BLOCK_VERIFY` | 自测步骤 + 建议 Claude 测什么 |

### 3.3 REVIEW_RESPONSE 写法示例

```text
---BLOCK_REVIEW_RESPONSE_START---
对审查问题的修复：
1. [P1] ResolveCurrentStageId() 死代码 → 已删除该方法及所有调用点
2. [P1] currentMainlineStageId 未在 StageNode 点击时同步 → 在 OnStageNodeClick(int stageId) 第一行添加 _currentMainlineStageId = stageId
3. [P2] ShowStageDetail() 硬编码解锁判定 → 改为调用 LevelProgressManager.Instance.IsUnlocked(stageId)
4. [P2] PlayerPrefs.Save() 同步写盘 → 添加 // TODO: 正式版改为异步存档（标记为技术债务，本 Demo 阶段保留）
---BLOCK_REVIEW_RESPONSE_END---
```

**规则**：
- 必须逐条回应，不能笼统写"已修复"
- 如果认为某条缺陷不成立，写："[P2] XXX → 经检查，该场景已在 YYY 方法中处理，无需修改。理由：..."
- 如果引入了新文件，必须说明

### 3.4 风险点怎么写

风险点必须诚实，不要隐瞒：

```text
风险点：
1. StageNode 按钮需要在 Unity Inspector 中重新绑定 OnStageNodeClick 事件，旧绑定可能失效。
2. PlayerPrefs key "MainlineProgress" 格式未变，但旧存档如果存在异常值可能导致解析错误。
3. 防连点锁 _isProcessingSettlement 依赖 MonoBehaviour 生命周期，如果对象被销毁后重建会重置。
```

---

## 四、与 Claude Code 的协作规则

### 4.1 收到审查后的处理流程

1. **读 verdict** —— 先看 Claude 的 `BLOCK_VERDICT`，判断是 PASS / NEEDS_FIX / BLOCKED
2. **读缺陷清单** —— 逐条理解，不要漏看
3. **定位代码** —— 根据 Claude 给的文件路径和方法名，找到对应代码
4. **修复** —— 逐条修复，修一条勾一条
5. **自测** —— 在 Unity 中跑通主流程
6. **写日志** —— 按模板填写 [CODE_FIXED] 记录

### 4.2 修复优先级

```
P1 (critical) → 必须修，不修不能进入下一轮
P2 (warning)  → 尽量修，如果本轮时间不够可以说明理由并标记为技术债务
```

### 4.3 冲突处理

如果与 Claude 意见不一致：
- 不要直接在日志里争吵
- 在 `BLOCK_REVIEW_RESPONSE` 中礼貌说明理由
- 如果涉及架构分歧，标记 `risk_point` 并建议 "需人工确认"
- Orchestrator 检测到争议会转人工

---

## 五、自测清单（提交前必做）

在标记 `[CODE_DONE]` 或 `[CODE_FIXED]` 前，必须完成：

- [ ] Unity Editor 中 **0 Error，0 Warning**（允许已存在的第三方插件警告）
- [ ] 能正常进入 Play Mode，不崩溃
- [ ] 主流程能跑通（从庭院 → 主线 → 进入战斗 → 结算）
- [ ] 修改的方法在 Inspector 中引用正确（没有 Missing 引用）
- [ ] 没有重复定义相同功能的方法（检查是否和旧代码冲突）
- [ ] PlayerPrefs / 存档相关改动不会破坏旧存档（兼容性或已标注不兼容）

---

## 六、Token 控制技巧

1. **不贴完整代码** —— 日志中只写路径和方法名
2. **用 diff 思维写改动点** —— "将 X 改为 Y" 而不是 "现在的代码是..."
3. **测试用例要具体但简短** —— 写步骤，不写预期输出的长篇大论
4. **一个 Task 只做一件事** —— 不要把"修 Bug + 加功能 + 重构"混在一个 task 里

---

## 七、违规处罚

如果 Orchestrator 检测到以下行为，会标记异常并通知人工：

1. 连续两轮不回应审查缺陷
2. 日志中粘贴超过 20 行代码
3. 自测清单全部未勾选就标记 [CODE_DONE]
4. 引入编译错误
5. 修改了日志中未声明的文件

---

*准则版本: v1.0*  
*生效日期: 2026-07-26*  
*下次评审: 项目里程碑 1 完成后*
