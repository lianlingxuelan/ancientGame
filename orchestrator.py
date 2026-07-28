#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
AncientGame Multi-Agent Orchestrator
自动调度 CodeX 与 Claude Code 的协作流程
"""

import re
import json
import sys
import time
import subprocess
from pathlib import Path
from dataclasses import dataclass, field
from datetime import datetime
from typing import Optional, List, Dict
from collections import defaultdict

# ==================== 配置区（用户按需修改） ====================
CONFIG = {
    "project_root": r"F:/AI-project/ancientGame",           # 项目根目录
    "log_file": r"F:/AI-project/ancientGame/doc/AI_Task_Log.md",  # 日志路径
    "max_rounds": 3,                                         # 单任务最大轮次
    "max_critical": 0,                                       # 通过时允许的 P1 数
    "max_warning": 2,                                        # 通过时允许的 P2 数
    "auto_mode": False,                                      # True=全自动, False=半自动（推荐）

    # 命令模板：{prompt_file} 会被替换为临时提示词文件路径
    "codex_cmd": "codex --prompt {prompt_file}",             # CodeX 调用命令
    "claude_cmd": "claude code --prompt {prompt_file}",      # Claude Code 调用命令
}

# ==================== 数据模型 ====================

@dataclass
class TaskRecord:
    raw: str
    task_id: str
    parent_id: Optional[str]
    round_num: int
    timestamp: str
    project_spec: str
    module: str
    flow_status: str
    agent: Optional[str]
    verdict: Optional[str] = None
    next_action: Optional[str] = None
    critical_count: int = 0
    warning_count: int = 0
    problems: List[str] = field(default_factory=list)
    test_cases: List[str] = field(default_factory=list)

# ==================== 日志解析器 ====================

class LogParser:
    def __init__(self, log_path: str):
        self.log_path = Path(log_path)
        self.content = self._read()

    def _read(self) -> str:
        if not self.log_path.exists():
            return ""
        return self.log_path.read_text(encoding="utf-8")

    def parse_all(self) -> List[TaskRecord]:
        """解析日志中所有 TASK_RECORD"""
        records = []
        pattern = r"===TASK_RECORD_START===(.+?)===TASK_RECORD_END==="
        for match in re.finditer(pattern, self.content, re.DOTALL):
            raw = match.group(1)
            rec = self._parse_one(raw)
            if rec:
                records.append(rec)
        return records

    def _parse_one(self, raw: str) -> Optional[TaskRecord]:
        def get(key: str, default="") -> str:
            m = re.search(rf"{key}:\s*(.+?)(?:\n|$)", raw)
            return m.group(1).strip() if m else default

        def get_block(block_name: str) -> str:
            pattern = rf"---{block_name}_START---(.+?)---{block_name}_END---"
            m = re.search(pattern, raw, re.DOTALL)
            return m.group(1).strip() if m else ""

        task_id = get("task_id")
        if not task_id:
            return None

        # 解析 verdict 区块
        verdict_block = get_block("BLOCK_VERDICT")
        verdict = None
        next_action = None
        critical_count = 0
        warning_count = 0
        if verdict_block:
            vm = re.search(r"verdict:\s*(\w+)", verdict_block)
            verdict = vm.group(1) if vm else None
            nm = re.search(r"next_action:\s*(\w+)", verdict_block)
            next_action = nm.group(1) if nm else None
            cm = re.search(r"critical_count:\s*(\d+)", verdict_block)
            critical_count = int(cm.group(1)) if cm else 0
            wm = re.search(r"warning_count:\s*(\d+)", verdict_block)
            warning_count = int(wm.group(1)) if wm else 0

        # 解析缺陷清单
        problem_block = get_block("BLOCK_REVIEW_PROBLEM")
        problems = [line.strip() for line in problem_block.split("\n") 
                    if line.strip().startswith(("1.", "2.", "3.", "4.", "5.", "【P"))]

        # 解析测试用例
        test_block = get_block("BLOCK_TEST_CASE")
        test_cases = [line.strip() for line in test_block.split("\n") if line.strip()]

        return TaskRecord(
            raw=raw,
            task_id=task_id,
            parent_id=get("parent_id") or None,
            round_num=int(get("round", "1")),
            timestamp=get("timestamp"),
            project_spec=get("project_spec", "极简速查版"),
            module=get("module", "未命名模块"),
            flow_status=get("flow_status", "[UNKNOWN]").strip(),
            agent=get("agent"),
            verdict=verdict,
            next_action=next_action,
            critical_count=critical_count,
            warning_count=warning_count,
            problems=problems,
            test_cases=test_cases,
        )

    def get_latest(self) -> Optional[TaskRecord]:
        records = self.parse_all()
        return records[-1] if records else None

    def get_task_history(self, base_name: str) -> List[TaskRecord]:
        """获取某个任务的所有轮次记录"""
        records = self.parse_all()
        return [r for r in records if r.task_id.startswith(base_name)]

# ==================== 健康检查 ====================

class HealthChecker:
    def __init__(self, parser: LogParser, config: dict):
        self.parser = parser
        self.config = config

    def check_all(self) -> Dict[str, any]:
        records = self.parser.parse_all()
        issues = []

        # 1. 检查是否有 TASK_ERROR
        errors = [r for r in records if r.flow_status == "[TASK_ERROR]"]
        if errors:
            issues.append(f"🚨 发现 {len(errors)} 个 [TASK_ERROR]，需人工介入")

        # 2. 按任务统计轮次
        task_rounds = defaultdict(int)
        for r in records:
            base = re.sub(r"-R\d+-.+$", "", r.task_id)
            task_rounds[base] = max(task_rounds[base], r.round_num)

        for task, rnd in task_rounds.items():
            if rnd > self.config["max_rounds"]:
                issues.append(f"⚠️ 任务 {task} 已进行 {rnd} 轮，超过上限 {self.config['max_rounds']}，建议人工介入")

        # 3. 检查循环（连续两轮问题数不变）
        for task_base in task_rounds:
            history = self.parser.get_task_history(task_base)
            rev_records = [r for r in history if r.flow_status == "[REVIEW_DONE]"]
            if len(rev_records) >= 3:
                last3 = rev_records[-3:]
                totals = [r.critical_count + r.warning_count for r in last3]
                if totals[0] == totals[1] == totals[2] and totals[0] > 0:
                    issues.append(f"🔄 任务 {task_base} 连续 3 轮审查问题数停滞在 {totals[0]}，陷入拉锯")

        return {
            "ok": len(issues) == 0,
            "issues": issues,
            "task_count": len(task_rounds),
            "total_records": len(records),
        }

# ==================== 指令生成器 ====================

class PromptGenerator:
    def __init__(self, config: dict):
        self.config = config

    def for_codex(self, latest: TaskRecord, history: List[TaskRecord]) -> str:
        """为 CodeX 生成提示词"""
        review = latest

        lines = [
            "# CodeX 任务指令",
            f"",
            f"## 任务信息",
            f"- task_id: {review.task_id.replace('-REV', '-CODE').replace('-PASS', '-CODE')}",
            f"- parent_id: {review.task_id}",
            f"- round: {review.round_num + 1}",
            f"- module: {review.module}",
            f"- project_spec: {review.project_spec}",
            f"",
            f"## 上轮审查结论",
            f"- verdict: {review.verdict}",
            f"- P1 (critical): {review.critical_count} 个",
            f"- P2 (warning): {review.warning_count} 个",
            f"",
            f"## 必须修复的缺陷",
        ]

        for p in review.problems:
            lines.append(f"- {p}")

        lines.extend([
            f"",
            f"## 编码要求",
            f"1. 只修改与上述缺陷相关的代码，不要过度重构。",
            f"2. 修复后必须在日志中回应每条缺陷：说明如何修复、涉及哪些文件。",
            f"3. 遵循项目编码规范（见 doc/codex_rules.md）。",
            f"4. 自测通过后再标记 [CODE_DONE]。",
            f"5. 本任务剩余轮次: {max(0, self.config['max_rounds'] - review.round_num)}",
            f"",
            f"## 日志填写要求",
            f"请在 doc/AI_Task_Log.md 末尾追加一条 TASK_RECORD，格式严格遵循 doc/protocol.md。",
            f"flow_status 必须是 [CODE_DONE] 或 [CODE_FIXED]。",
            f"",
            f"## 重要提醒",
            f"- 不要粘贴完整源代码到日志中。",
            f"- 使用相对路径引用文件。",
            f"- 如果认为某条缺陷无需修复，请在 BLOCK_REVIEW_RESPONSE 中说明理由。",
        ])

        return "\n".join(lines)

    def for_claude(self, latest: TaskRecord, history: List[TaskRecord]) -> str:
        """为 Claude Code 生成提示词"""
        code = latest

        lines = [
            "# Claude Code 审查指令",
            f"",
            f"## 任务信息",
            f"- task_id: {code.task_id.replace('-CODE', '-REV').replace('-FIX', '-REV')}",
            f"- parent_id: {code.task_id}",
            f"- round: {code.round_num}",
            f"- module: {code.module}",
            f"- project_spec: {code.project_spec}",
            f"",
            f"## 审查对象",
            f"请审查 parent_id 对应的 CodeX 代码改动。",
            f"重点关注:",
            f"1. 上轮审查的缺陷是否真正修复（不要只看日志，要看代码）。",
            f"2. 修复是否引入了新问题。",
            f"3. 测试用例是否覆盖边界情况。",
            f"",
            f"## 审查标准",
            f"- P1 (critical): 功能错误、安全漏洞、数据丢失风险、阻塞流程的 Bug",
            f"- P2 (warning): 代码异味、性能隐患、可维护性问题、边界处理不完善",
            f"",
            f"## 输出要求",
            f"请在 doc/AI_Task_Log.md 末尾追加一条 TASK_RECORD，格式严格遵循 doc/protocol.md。",
            f"",
            f"### 必须包含 BLOCK_VERDICT",
            f"```",
            f"---BLOCK_VERDICT_START---",
            f"verdict: PASS | NEEDS_FIX | BLOCKED",
            f"round: {code.round_num}",
            f"critical_count: <P1数量>",
            f"warning_count: <P2数量>",
            f"next_action: CLOSE | CODE_FIX | TEST_ONLY | MANUAL",
            f"remaining_rounds: {max(0, self.config['max_rounds'] - code.round_num)}",
            f"---BLOCK_VERDICT_END---",
            f"```",
            f"",
            f"### 裁决规则",
            f"- PASS: P1=0 且 P2<=2，且所有测试用例通过",
            f"- NEEDS_FIX: 存在 P1 或 P2>2",
            f"- BLOCKED: 发现架构级矛盾或无法自动修复的问题",
            f"",
            f"## 重要提醒",
            f"- 不要粘贴完整源代码到日志中。",
            f"- 使用文件路径 + 方法名引用。",
            f"- 写出具体、可执行的测试步骤。",
            f"- 如果连续两轮发现相同问题，请标记为 BLOCKED。",
        ])

        return "\n".join(lines)

# ==================== 主编排器 ====================

class Orchestrator:
    def __init__(self, config: dict):
        self.config = config
        self.parser = LogParser(config["log_file"])
        self.health = HealthChecker(self.parser, config)
        self.prompt_gen = PromptGenerator(config)
        self.project_root = Path(config["project_root"])
        self.tmp_dir = self.project_root / ".orchestrator"
        self.tmp_dir.mkdir(exist_ok=True)

    def run(self):
        print("=" * 60)
        print("AncientGame Multi-Agent Orchestrator")
        print(f"时间: {datetime.now().isoformat()}")
        print("=" * 60)

        # 1. 健康检查
        health = self.health.check_all()
        print(f"\n📊 项目状态: {'✅ 健康' if health['ok'] else '⚠️ 异常'}")
        print(f"   任务数: {health['task_count']}, 总记录: {health['total_records']}")
        if health["issues"]:
            for issue in health["issues"]:
                print(f"   {issue}")

        # 2. 读取最新记录
        latest = self.parser.get_latest()
        if not latest:
            print("\n📝 日志为空，请手动创建第一条 TASK_RECORD 启动流程。")
            return

        print(f"\n📌 最新记录:")
        print(f"   task_id: {latest.task_id}")
        print(f"   status: {latest.flow_status}")
        print(f"   agent: {latest.agent or '未知'}")
        print(f"   round: {latest.round_num}")
        if latest.verdict:
            print(f"   verdict: {latest.verdict}")

        # 3. 决策
        action = self._decide(latest)
        print(f"\n🎯 决策结果: {action['type']}")
        print(f"   说明: {action['desc']}")

        if action["type"] == "WAIT":
            print("\n⏳ 无需操作，等待 Agent 完成当前任务。")
            return

        if action["type"] == "ALERT":
            print(f"\n🔔 {action['message']}")
            return

        # 4. 生成提示词
        history = self.parser.get_task_history(
            re.sub(r"-R\d+-.+$", "", latest.task_id)
        )

        if action["type"] == "TRIGGER_CODEX":
            prompt = self.prompt_gen.for_codex(latest, history)
            cmd_template = self.config["codex_cmd"]
        elif action["type"] == "TRIGGER_CLAUDE":
            prompt = self.prompt_gen.for_claude(latest, history)
            cmd_template = self.config["claude_cmd"]
        else:
            print(f"\n❌ 未知动作: {action['type']}")
            return

        # 5. 写入临时提示词文件
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        prompt_file = self.tmp_dir / f"prompt_{action['type']}_{timestamp}.md"
        prompt_file.write_text(prompt, encoding="utf-8")
        print(f"\n📝 提示词已生成: {prompt_file}")

        # 6. 执行或提示
        cmd = cmd_template.format(prompt_file=str(prompt_file))
        print(f"\n💻 执行命令:")
        print(f"   {cmd}")

        if self.config["auto_mode"]:
            print("\n🚀 自动模式：正在执行...")
            try:
                result = subprocess.run(cmd, shell=True, capture_output=True, text=True)
                print(result.stdout)
                if result.returncode != 0:
                    print(f" stderr: {result.stderr}")
            except Exception as e:
                print(f"❌ 执行失败: {e}")
        else:
            print("\n👤 半自动模式：请复制上方命令执行，完成后再次运行 orchestrator.py")

    def _decide(self, latest: TaskRecord) -> dict:
        """决策引擎"""
        status = latest.flow_status

        # 错误状态
        if status == "[TASK_ERROR]":
            return {"type": "ALERT", "desc": "任务出错，需人工介入", 
                    "message": f"任务 {latest.task_id} 报告错误，请查看日志。"}

        # 刚完成编码，需要审查
        if status in ("[CODE_DONE]", "[CODE_FIXED]"):
            # 检查轮次
            if latest.round_num >= self.config["max_rounds"]:
                return {"type": "ALERT", "desc": "已达最大轮次",
                        "message": f"任务 {latest.task_id} 已进行 {latest.round_num} 轮，强制转人工。"}
            return {"type": "TRIGGER_CLAUDE", "desc": "触发 Claude Code 审查"}

        # 审查完成，需要判断
        if status == "[REVIEW_DONE]":
            if not latest.verdict:
                return {"type": "ALERT", "desc": "审查记录缺少 verdict",
                        "message": "Claude 未提供 verdict，请检查日志格式。"}

            if latest.verdict == "PASS":
                return {"type": "ALERT", "desc": "任务通过，流程结束",
                        "message": f"🎉 任务 {latest.task_id} 审查通过！可以合并代码。"}

            elif latest.verdict == "BLOCKED":
                return {"type": "ALERT", "desc": "任务阻塞，需人工介入",
                        "message": f"任务 {latest.task_id} 被标记为 BLOCKED，需要人工决策。"}

            elif latest.verdict == "NEEDS_FIX":
                if latest.round_num >= self.config["max_rounds"]:
                    return {"type": "ALERT", "desc": "需修复但已达最大轮次",
                            "message": f"任务 {latest.task_id} 需要修复但已达 {self.config['max_rounds']} 轮，转人工。"}
                return {"type": "TRIGGER_CODEX", "desc": "触发 CodeX 修复"}

            else:
                return {"type": "ALERT", "desc": "未知 verdict",
                        "message": f"未知 verdict: {latest.verdict}"}

        # 审查通过
        if status == "[REVIEW_PASS]":
            return {"type": "ALERT", "desc": "任务已关闭",
                    "message": f"任务 {latest.task_id} 已完成。"}

        return {"type": "WAIT", "desc": "等待当前 Agent 完成"}

# ==================== 入口 ====================

def main():
    orch = Orchestrator(CONFIG)
    orch.run()

if __name__ == "__main__":
    main()
