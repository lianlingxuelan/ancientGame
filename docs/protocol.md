# AI Task Log Protocol v2.0

> ** AncientGame 项目多 Agent 协作日志协议**
> 
> 所有 Agent（CodeX / Claude Code）在操作 `docs/AI_TASK_LOG.md` 时，必须 100% 遵守此规范。
> 违反规范的记录将被编排器忽略或标记为异常。

---

## 一、基本原则

1. **追加-only**：只能在文件末尾追加新记录，禁止修改、删除已有记录。
2. **不贴代码**：日志中禁止粘贴完整源代码。只写文件路径、方法名、短 diff 摘要。
3. **项目仓库是真理**：代码以 Git 仓库中的实际文件为准，日志只是协作索引。
4. **结构化**：所有记录必须使用统一的 `TASK_RECORD` 格式，包含必填区块。

---

## 二、状态机

```
[CODE_DONE]        CodeX 完成编码 / 修复
    ↓
[REVIEW_DONE]      Claude 完成审查 + 测试用例
    ↓ (verdict=NEEDS_FIX)
[CODE_FIXED]       CodeX 完成修复（回应审查）
    ↓
[REVIEW_DONE]      Claude 再次审查
    ↓ (verdict=PASS)
[REVIEW_PASS]      任务关闭

异常分支：
    ↓ (任何阶段)
[TASK_ERROR]       阻塞，需人工介入
    ↓ (verdict=BLOCKED)
[TASK_ERROR]       阻塞，需人工介入
```

---

## 三、Task ID 命名规则

格式：`{任务名}-R{轮次}-{动作}`

| 字段 | 含义 | 示例 |
|------|------|------|
| 任务名 | 简短英文/拼音，如 `Todo1`, `AuthSystem` | `Todo1` |
| R{轮次} | 第几轮，从 1 开始 | `R1`, `R2` |
| {动作} | 当前动作 | `CODE`, `REV`, `FIX`, `PASS` |

**示例：**
- `Todo1-R1-CODE`：Todo1 任务第 1 轮编码
- `Todo1-R1-REV`：Todo1 任务第 1 轮审查
- `Todo1-R2-FIX`：Todo1 任务第 2 轮修复
- `Todo1-R2-PASS`：Todo1 任务第 2 轮通过关闭

---

## 四、记录格式模板

### 4.1 CodeX 记录模板（[CODE_DONE] / [CODE_FIXED]）

```text
===TASK_RECORD_START===
task_id: {任务名}-R{轮次}-CODE
parent_id: {上一轮记录ID}
round: {轮次}
timestamp: {YYYY-MM-DD HH:MM:SS Asia/Shanghai}
project_spec: {项目规格名}
module: {模块名}
flow_status: [CODE_DONE]
agent: codex
---BLOCK_REQUIREMENT_START---
需求：
一句话说明本轮要完成什么。
---BLOCK_REQUIREMENT_END---
---BLOCK_REVIEW_RESPONSE_START---
对审查问题的修复：
1. [P1] {缺陷描述} → {修复方式}
2. [P2] {缺陷描述} → {修复方式}
（如果是第一轮编码，此区块写"首轮编码，无审查回应"）
---BLOCK_REVIEW_RESPONSE_END---
---BLOCK_CHANGE_FILES_START---
改动文件：
1. {相对路径}

关键方法：
1. {方法名}
2. {方法名}
---BLOCK_CHANGE_FILES_END---
---BLOCK_CHANGE_LOG_START---
改动点：
1. {具体改动说明}
2. {具体改动说明}

资源变更：{无 / 有，说明}
存档影响：{无 / 有，说明}
风险点：{需要人工验证的点}
---BLOCK_CHANGE_LOG_END---
---BLOCK_VERIFY_START---
Codex自测：
1. {自测步骤和结果}
2. {自测步骤和结果}

建议Claude测试：
1. {测试建议}
2. {测试建议}
---BLOCK_VERIFY_END---
===TASK_RECORD_END===
```

### 4.2 Claude Code 记录模板（[REVIEW_DONE]）

```text
===TASK_RECORD_START===
task_id: {任务名}-R{轮次}-REV
parent_id: {上一轮记录ID}
round: {轮次}
timestamp: {YYYY-MM-DD HH:MM:SS Asia/Shanghai}
project_spec: {项目规格名}
module: {模块名}
flow_status: [REVIEW_DONE]
agent: claude
---BLOCK_VERDICT_START---
verdict: PASS | NEEDS_FIX | BLOCKED
round: {轮次}
critical_count: {P1数量}
warning_count: {P2数量}
next_action: CLOSE | CODE_FIX | TEST_ONLY | MANUAL
remaining_rounds: {剩余轮次}
---BLOCK_VERDICT_END---
---BLOCK_REVIEW_PROBLEM_START---
缺陷清单：

1.【P1】{缺陷描述}
   风险：{不修复的后果}
   修复建议：{具体怎么做}

2.【P2】{缺陷描述}
   风险：{不修复的后果}
   修复建议：{具体怎么做}
（如果没有缺陷，写"本次审查无缺陷"）
---BLOCK_REVIEW_PROBLEM_END---
---BLOCK_TEST_CASE_START---
正向流程：
1. {测试步骤}
2. {测试步骤}

异常边界：
1. {边界测试}
2. {边界测试}
---BLOCK_TEST_CASE_END---
===TASK_RECORD_END===
```

### 4.3 通过记录模板（[REVIEW_PASS]）

```text
===TASK_RECORD_START===
task_id: {任务名}-R{轮次}-PASS
parent_id: {上一轮记录ID}
round: {轮次}
timestamp: {YYYY-MM-DD HH:MM:SS Asia/Shanghai}
project_spec: {项目规格名}
module: {模块名}
flow_status: [REVIEW_PASS]
agent: claude
---BLOCK_VERDICT_START---
verdict: PASS
round: {轮次}
critical_count: 0
warning_count: {允许的小于等于2}
next_action: CLOSE
remaining_rounds: {剩余轮次}
---BLOCK_VERDICT_END---
---BLOCK_SUMMARY_START---
任务总结：
- 完成内容：{简述}
- 最终状态：通过
- 遗留问题：{无 / 有，说明}
---BLOCK_SUMMARY_END---
===TASK_RECORD_END===
```

### 4.4 错误记录模板（[TASK_ERROR]）

```text
===TASK_RECORD_START===
task_id: {任务名}-R{轮次}-ERR
parent_id: {上一轮记录ID}
round: {轮次}
timestamp: {YYYY-MM-DD HH:MM:SS Asia/Shanghai}
project_spec: {项目规格名}
module: {模块名}
flow_status: [TASK_ERROR]
agent: {codex | claude}
---BLOCK_ERROR_START---
错误类型：{BLOCKED / 工具失败 / 逻辑矛盾 / 其他}
错误描述：{发生了什么}
影响范围：{哪些文件/功能受影响}
已尝试的解决：{做了什么尝试}
需要人工决策：{需要人做什么}
---BLOCK_ERROR_END---
===TASK_RECORD_END===
```

---

## 五、字段详解

### 5.1 头部字段（所有记录必填）

| 字段 | 必填 | 说明 |
|------|------|------|
| `task_id` | ✅ | 唯一标识，遵循命名规则 |
| `parent_id` | ✅ | 上一轮记录的 task_id（首轮回填"none"） |
| `round` | ✅ | 当前轮次，整数 |
| `timestamp` | ✅ | 格式：`YYYY-MM-DD HH:MM:SS Asia/Shanghai` |
| `project_spec` | ✅ | 项目规格，如"极简速查版" |
| `module` | ✅ | 模块名，如"战斗结算" |
| `flow_status` | ✅ | 必须是协议定义的状态之一 |
| `agent` | ✅ | `codex` 或 `claude` |

### 5.2 BLOCK_VERDICT（Claude 必填）

| 字段 | 必填 | 说明 |
|------|------|------|
| `verdict` | ✅ | `PASS` / `NEEDS_FIX` / `BLOCKED` |
| `round` | ✅ | 与头部 round 一致 |
| `critical_count` | ✅ | P1 缺陷数量 |
| `warning_count` | ✅ | P2 缺陷数量 |
| `next_action` | ✅ | 下一步该做什么 |
| `remaining_rounds` | ✅ | 剩余可用轮次 |

**裁决规则：**
- `PASS`：P1=0 且 P2≤2，且所有测试用例通过
- `NEEDS_FIX`：存在 P1 或 P2>2
- `BLOCKED`：发现架构矛盾、需求不明确、或连续两轮相同 P1 未修复

### 5.3 缺陷分级标准

| 级别 | 定义 | 示例 |
|------|------|------|
| **P1 (critical)** | 功能错误、安全漏洞、数据丢失、阻塞主流程 | 空指针、SQL 注入、存档损坏、按钮无响应 |
| **P2 (warning)** | 代码异味、性能隐患、可维护性差、边界处理不完善 | 魔法数字、重复代码、未处理异常、硬编码 |

---

## 六、禁止事项

1. ❌ **修改已有记录** —— 只能追加
2. ❌ **粘贴完整源代码** —— 超过 5 行的代码片段禁止放入日志
3. ❌ **使用自由格式** —— 必须按模板填写，不得省略区块
4. ❌ **跳过 BLOCK_VERDICT** —— Claude 审查必须包含 verdict
5. ❌ **复用 task_id** —— 每条记录必须有唯一 task_id
6. ❌ **遗漏 parent_id** —— 必须指向上一条记录，形成链式追溯

---

## 七、快速检查清单

在提交日志前，Agent 必须自检：

- [ ] task_id 符合 `{名}-R{n}-{动作}` 格式
- [ ] parent_id 正确指向上一轮
- [ ] timestamp 包含时区
- [ ] flow_status 是协议允许的 5 种之一
- [ ] agent 字段已填写
- [ ] 如果是 Claude 审查：包含 BLOCK_VERDICT
- [ ] 如果是 CodeX 编码：包含 BLOCK_REVIEW_RESPONSE（非首轮）
- [ ] 没有粘贴完整源代码
- [ ] 所有文件路径使用相对路径

---

*协议版本: v2.0*
*生效日期: 2026-07-26*
*维护者: Orchestrator + 人工兜底*
