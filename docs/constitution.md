# AncientGame 项目章程

> **性质**：多 Agent 协作的"公司规章制度"  
> **适用范围**：CodeX（开发）、Claude Code（审查）、Orchestrator（调度）、人工（投资人）  
> **目标**：让 Agent 在无人监督的情况下自动协作，人工只在熔断时介入

---

## 一、角色与职责

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   投资人     │     │  调度器      │     │   开发者     │
│   (人工)     │◄────│Orchestrator │◄────│  CodeX      │
│             │     │             │     │             │
│ - 给 PRD    │     │ - 读日志     │     │ - 写代码     │
│ - 定规则    │     │ - 判状态     │     │ - 修 Bug    │
│ - 熔断兜底  │     │ - 发指令     │     │ - 写日志     │
└─────────────┘     └──────┬──────┘     └─────────────┘
                           │
                           ▼
                    ┌─────────────┐
                    │  审查者      │
                    │ Claude Code │
                    │             │
                    │ - 审代码     │
                    │ - 写测试     │
                    │ - 下裁决     │
                    └─────────────┘
```

| 角色 | 决策权 | 否决权 | 触发条件 |
|------|--------|--------|----------|
| CodeX | 代码实现 | 无 | Orchestrator 发 CODE_FIX 指令 |
| Claude | 审查裁决 | BLOCKED | Orchestrator 发 REVIEW 指令 |
| Orchestrator | 流程调度 | 强制熔断 | 检测到异常自动执行 |
| 人工 | 一切 | 一切 | Orchestrator 报警或主动查询 |

---

## 二、任务生命周期

### 2.1 正常流程

```
人工创建首条记录
      ↓
Orchestrator 检测到 [CODE_DONE]
      ↓
生成 Claude 审查指令 → Claude 执行审查 → 写 [REVIEW_DONE]
      ↓
Orchestrator 检测到 [REVIEW_DONE]
      ↓
  ├─ verdict=PASS    → 通知人工"任务完成"
  ├─ verdict=NEEDS_FIX → 生成 CodeX 修复指令 → CodeX 执行 → 写 [CODE_FIXED]
  │                      ↓
  │                    回到"Claude 审查"（循环）
  │
  └─ verdict=BLOCKED → 通知人工"需要决策"
```

### 2.2 异常分支

| 异常 | 触发条件 | 处理 |
|------|---------|------|
| **编译错误** | CodeX 代码导致 Unity 编译失败 | 本轮记录作废，CodeX 必须重新提交 |
| **日志格式错误** | Agent 未按 protocol.md 填写 | Orchestrator 标记异常，通知人工 |
| **循环拉锯** | 连续 3 轮审查问题数不变 | Orchestrator 强制熔断，转人工 |
| **轮次超限** | 单任务超过 max_rounds (3轮) | Orchestrator 强制熔断，转人工 |
| **Agent 失联** | 超过 30 分钟无新记录 | Orchestrator 报警，人工检查 |
| **需求变更** | 人工中途修改 PRD | 当前任务标记 BLOCKED，新建任务重新开始 |

---

## 三、熔断规则（核心安全网）

### 3.1 熔断类型

| 熔断代码 | 名称 | 触发条件 | 自动动作 | 人工动作 |
|----------|------|---------|---------|---------|
| **F1** | 轮次熔断 | round > max_rounds (3) | 停止自动调度 | 人工决定：拆任务 / 降需求 / 亲自修 |
| **F2** | 循环熔断 | 连续 3 轮问题总数不变 | 停止自动调度 | 人工决定：换方案 / 放宽标准 / 亲自审 |
| **F3** | 阻塞熔断 | verdict=BLOCKED | 停止自动调度 | 人工做架构决策 |
| **F4** | 错误熔断 | [TASK_ERROR] | 停止自动调度 | 人工排查原因 |
| **F5** | 质量熔断 | Claude 连续 2 轮标记 PASS 但人工发现 P1 | 暂停 Claude 审查权 | 人工复审，必要时更新审查标准 |

### 3.2 熔断后恢复流程

```
1. Orchestrator 发送报警（控制台 + 可选邮件/钉钉）
2. 人工查看日志，定位问题
3. 人工做出决策：
   a) "继续" → 人工在日志中追加一条 [CODE_DONE] 或 [REVIEW_DONE] 重启流程
   b) "关闭" → 人工追加 [TASK_ERROR] 说明原因，任务终止
   c) "重置" → 新建 task_id（如 Todo1-Retry1-R1-CODE），从零开始
4. Orchestrator 检测到新记录，恢复正常调度
```

---

## 四、冲突解决机制

### 4.1 CodeX vs Claude 意见冲突

**场景**：CodeX 认为某条 P1 不成立，Claude 坚持要修。

**规则**：
1. 第一轮：CodeX 在 `BLOCK_REVIEW_RESPONSE` 中说明理由
2. 第二轮：Claude 重新审查，如果仍坚持，必须在缺陷描述中补充更具体的证据
3. 第三轮：如果仍不一致，**自动触发 F3 阻塞熔断**，转人工裁决

**人工裁决模板**：
```text
===TASK_RECORD_START===
task_id: {原任务}-R{n}-ARBITRATION
parent_id: {冲突记录ID}
round: {n}
timestamp: {YYYY-MM-DD HH:MM:SS Asia/Shanghai}
project_spec: {项目规格}
module: {模块}
flow_status: [TASK_ERROR]
agent: human
---BLOCK_ARBITRATION_START---
争议点：{简述争议}
CodeX 立场：{...}
Claude 立场：{...}
裁决结果：{支持哪方 / 折中方案}
执行指令：{谁该做什么}
---BLOCK_ARBITRATION_END---
===TASK_RECORD_END===
```

### 4.2 前后端接口不一致

**场景**：CodeX 需要接口 A，但后端（Claude 负责审查后端）说没有 / 格式不同。

**规则**：
1. 优先使用**已有接口**，CodeX 适配后端
2. 如果确实需要新接口，Claude 负责写出最小可用版本的后端代码
3. 如果后端实现成本过高，标记 BLOCKED，人工决定是砍需求还是加人

### 4.3 代码风格冲突

**规则**：以 `codex_rules.md` 中的编码规范为准。Claude 不应因为风格偏好（如大括号换行 vs 不换行）标记缺陷，除非风格导致可读性严重下降。

---

## 五、质量标准

### 5.1 通过门槛

一个任务要关闭，必须同时满足：

- [ ] Claude verdict = PASS
- [ ] P1 = 0
- [ ] P2 ≤ 2
- [ ] 所有正向测试用例通过
- [ ] 所有异常边界测试用例通过
- [ ] Unity 编译 0 Error
- [ ] 主流程能跑通

### 5.2 技术债务管理

以下情况允许暂时不修复，但必须标记为技术债务：

| 类型 | 允许条件 | 标记方式 |
|------|---------|---------|
| PlayerPrefs 替代方案 | Demo 阶段 | 代码中写 `// TODO: TD-001 正式版改为异步存档` |
| 硬编码配置 | 只有 1-2 处且值不变 | 代码中写 `// TODO: TD-002 移至 ScriptableObject` |
| 重复代码 | 只有 2 处且逻辑简单 | 日志中记录，里程碑 2 统一重构 |
| 未优化的算法 | 数据量 < 100，无性能问题 | 日志中记录，性能测试阶段处理 |

**技术债务追踪**：所有 `TD-xxx` 标记必须在项目根目录 `TECH_DEBT.md` 中登记。

---

## 六、文件目录规范

```
ancientGame/
├── docs/
│   ├── AI_TASK_LOG.md          # 协作日志（Agent 读写）
│   ├── protocol.md             # 日志协议（只读，人工维护）
│   ├── codex_rules.md          # CodeX 行为准则（只读）
│   ├── claude_rules.md         # Claude 行为准则（只读）
│   └── constitution.md         # 项目章程（只读）
├── orchestrator.py             # 调度脚本（人工运行）
├── TECH_DEBT.md                # 技术债务登记册
├── spec/
│   └── prd.md                  # 项目需求文档（人工编写）
├── ShouyouPrototype/           # Unity 项目
│   └── Assets/_Project/Scripts/
│       ├── UI/
│       ├── Data/
│       ├── Battle/
│       └── ...
└── .orchestrator/              # 编排器临时文件
    └── prompt_*.md             # 自动生成的提示词
```

---

## 七、运行指南

### 7.1 启动一个新任务

1. 人工在 `docs/AI_TASK_LOG.md` 末尾写第一条记录：
   ```text
   ===TASK_RECORD_START===
   task_id: FeatureName-R1-CODE
   parent_id: none
   round: 1
   ...
   flow_status: [CODE_DONE]
   agent: codex
   ...
   ===TASK_RECORD_END===
   ```

2. 运行编排器：
   ```bash
   python orchestrator.py
   ```

3. 编排器自动生成 Claude 审查提示词，复制命令执行。

### 7.2 日常运行（半自动模式）

```bash
# 每次 Agent 写完日志后，运行一次
python orchestrator.py

# 根据输出提示，执行对应的 Agent 命令
# 等 Agent 完成并写日志后，再次运行
python orchestrator.py
```

### 7.3 日常运行（全自动模式）

修改 `orchestrator.py` 中 `CONFIG["auto_mode"] = True`，然后：

```bash
# 循环运行（Windows PowerShell）
while ($true) { python orchestrator.py; Start-Sleep -Seconds 60 }

# 循环运行（Linux/Mac）
while true; do python orchestrator.py; sleep 60; done
```

---

## 八、附录：快速决策卡

### Agent 遇到不确定时，按这个顺序查：

1. **该做什么？** → 看 `protocol.md` 状态机
2. **怎么写日志？** → 看 `protocol.md` 模板
3. **代码怎么写？** → 看 `codex_rules.md`
4. **审什么、怎么审？** → 看 `claude_rules.md`
5. **要不要熔断？** → 看 `constitution.md` 第三章
6. **还是不确定？** → 标记 `[TASK_ERROR]`，等人工

---

*章程版本: v1.0*  
*生效日期: 2026-07-26*  
*修订流程: 人工提案 → 双 Agent 无异议 → 生效*
