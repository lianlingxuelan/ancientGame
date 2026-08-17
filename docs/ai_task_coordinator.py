#!/usr/bin/env python3
"""
AI Task Log 自动化协调器
监控 AI_TASK_LOG.md，自动触发 Codex/Claude Code 执行下一阶段任务
"""

import re
import time
import subprocess
import os
from pathlib import Path
from datetime import datetime
from typing import Optional, Dict

# 配置
LOG_FILE = r"F:\AI-project\ancientGame\docs\AI_TASK_LOG.md"
PROJECT_DIR = r"F:\AI-project\ancientGame"
POLL_INTERVAL = 10  # 秒

# 状态机
STATUS_TRIGGERS = {
    "[CODE_DONE]": "启动 Claude Code 审查",
    "[REVIEW_DONE]": "检查审查结果，决定是否需要 Codex 修复",
    "[CODE_FIXED]": "启动 Claude Code 复审",
    "[REVIEW_PASS]": "任务完成，记录总结",
    "[TASK_ERROR]": "标记需要人工介入",
}

# 任务状态缓存（避免重复触发）
last_processed_task: Dict[str, str] = {}


def read_last_task_record() -> Optional[Dict]:
    """读取日志文件中最后一个任务记录"""
    if not os.path.exists(LOG_FILE):
        return None

    with open(LOG_FILE, "r", encoding="utf-8") as f:
        content = f.read()

    # 找到最后一个 TASK_RECORD_START
    records = content.split("===TASK_RECORD_START===")
    if len(records) < 2:
        return None

    last_record = records[-1]

    # 解析关键字段
    task_id = re.search(r"task_id:\s*(\S+)", last_record)
    flow_status = re.search(r"flow_status:\s*(\S+)", last_record)
    agent = re.search(r"agent:\s*(\S+)", last_record)
    parent_id = re.search(r"parent_id:\s*(\S+)", last_record)
    round_match = re.search(r"round:\s*(\d+)", last_record)
    verdict = re.search(r"verdict:\s*(\S+)", last_record)
    module = re.search(r"module:\s*(.+?)(?:\n|$)", last_record)

    if not task_id or not flow_status:
        return None

    return {
        "task_id": task_id.group(1) if task_id else "",
        "flow_status": flow_status.group(1) if flow_status else "",
        "agent": agent.group(1) if agent else "",
        "parent_id": parent_id.group(1) if parent_id else "",
        "round": int(round_match.group(1)) if round_match else 1,
        "verdict": verdict.group(1) if verdict else "",
        "module": module.group(1).strip() if module else "",
        "raw": last_record[:500],  # 保留原始片段用于调试
    }


def trigger_claude_review(task: Dict):
    """启动 Claude Code 审查"""
    print(f"\n🤖 触发 Claude Code 审查: {task['task_id']}")
    print(f"   模块: {task['module']}")

    # 构建审查 prompt
    prompt = f"""请审查 AI_TASK_LOG.md 中最新的任务记录。

任务 ID: {task['task_id']}
模块: {task['module']}
请执行代码审查，验证改动是否合理，并更新 AI_TASK_LOG.md 添加审查结果。

审查要点：
1. 检查代码逻辑是否正确
2. 检查是否有潜在 bug
3. 检查是否符合项目规范
4. 生成测试用例建议

如果通过，设置 verdict: PASS，flow_status: [REVIEW_DONE]
如果需要修复，设置 verdict: NEED_FIX，flow_status: [REVIEW_DONE]，并在 BLOCK_REVIEW_PROBLEM_START 中列出问题
"""

    # 使用 subprocess 直接调用 claude CLI
    # claude -p "prompt" 会在当前目录执行
    try:
        subprocess.Popen(
            ["claude", "-p", prompt],
            cwd=PROJECT_DIR,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            start_new_session=True  # 脱离父进程
        )
        print(f"   ✅ Claude Code 审查已启动")
    except Exception as e:
        print(f"   ❌ 启动失败: {e}")
        print(f"   请手动运行: claude -p \"{prompt[:100]}...\"")


def trigger_codex_fix(task: Dict):
    """启动 Codex 修复（需要手动操作）"""
    print(f"\n🔧 触发 Codex 修复: {task['task_id']}")
    print(f"   轮次: {task['round'] + 1}")

    # 从审查结果中提取需要修复的问题
    problems = re.search(
        r"---BLOCK_REVIEW_PROBLEM_START---(.+?)---BLOCK_REVIEW_PROBLEM_END---",
        task.get("raw", ""),
        re.DOTALL
    )

    fix_requirements = problems.group(1).strip() if problems else "需要根据审查反馈修复代码"

    print(f"\n   ⚠️ Codex 需要手动操作")
    print(f"   请在 ChatGPT 客户端中完成以下修复:")
    print(f"   ─────────────────────────────────────")
    print(f"   {fix_requirements[:200]}...")
    print(f"   ─────────────────────────────────────")
    print(f"   修复完成后，请在 AI_TASK_LOG.md 中:")
    print(f"   1. 创建新任务记录，parent_id 设为 {task['parent_id']}")
    print(f"   2. 设置 flow_status: [CODE_FIXED]")


def trigger_claude_pass(task: Dict):
    """任务通过，记录总结"""
    print(f"\n✅ 任务完成: {task['task_id']}")
    print(f"   模块: {task['module']}")
    # 可以发送通知到钉钉/飞书/Discord


def trigger_manual_intervention(task: Dict):
    """需要人工介入"""
    print(f"\n⚠️ 需要人工介入: {task['task_id']}")
    print(f"   模块: {task['module']}")
    # 可以发送告警通知


def main():
    print("=" * 60)
    print("AI Task Log 自动化协调器启动")
    print(f"监控文件: {LOG_FILE}")
    print(f"轮询间隔: {POLL_INTERVAL}秒")
    print("=" * 60)

    while True:
        try:
            task = read_last_task_record()

            if task:
                task_id = task["task_id"]
                status = task["flow_status"]

                # 检查是否已处理过这个任务状态
                cache_key = f"{task_id}:{status}"
                if cache_key not in last_processed_task:
                    print(f"\n[{datetime.now().strftime('%H:%M:%S')}] 检测到新状态: {task_id} → {status}")

                    # 根据状态触发对应操作
                    if status == "[CODE_DONE]":
                        trigger_claude_review(task)
                    elif status == "[REVIEW_DONE]":
                        if task.get("verdict") == "NEED_FIX":
                            trigger_codex_fix(task)
                        else:
                            # 如果是最终轮次或 PASS，直接标记通过
                            trigger_claude_pass(task)
                    elif status == "[CODE_FIXED]":
                        trigger_claude_review(task)
                    elif status == "[REVIEW_PASS]":
                        trigger_claude_pass(task)
                    elif status == "[TASK_ERROR]":
                        trigger_manual_intervention(task)

                    # 标记已处理
                    last_processed_task[cache_key] = datetime.now().isoformat()

                    # 清理过期的缓存（保留最近 100 条）
                    if len(last_processed_task) > 100:
                        oldest_keys = list(last_processed_task.keys())[:50]
                        for k in oldest_keys:
                            del last_processed_task[k]

            time.sleep(POLL_INTERVAL)

        except KeyboardInterrupt:
            print("\n\n👋 协调器已停止")
            break
        except Exception as e:
            print(f"错误: {e}")
            time.sleep(POLL_INTERVAL)


if __name__ == "__main__":
    main()
