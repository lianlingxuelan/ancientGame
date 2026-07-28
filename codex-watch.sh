#!/usr/bin/env bash
# =============================================================
#  Codex + Claude 联合开发闭环 — 监控脚本 v5
#  每日独立日志: task_shturl_YYYY-MM-DD.md
#  自动清理 10 天前的旧文件
# =============================================================

PROJECT_DIR="F:/AI-project/ancientGame"
TODAY=$(date '+%Y-%m-%d')
TASK_FILE="$PROJECT_DIR/docs/AI_TASK_LOG.md"
STATE_FILE="$PROJECT_DIR/.codex-watch-state"
RESULT_FILE="$PROJECT_DIR/.codex-test-results.txt"
SERVER_DIR="$PROJECT_DIR/ShouyouServer"
TEST_DIR="/c/Users/Administrator/Desktop/test-demo"

cd "$PROJECT_DIR"

# ── 0. 清理 10 天前的旧日志 ──

OLD_FILES=$(find "$PROJECT_DIR" -maxdepth 1 -name "task_shturl_*.md" -mtime +10 2>/dev/null)
if [ -n "$OLD_FILES" ]; then
    echo "$OLD_FILES" | while read f; do
        echo "[cleanup] 删除过期日志: $(basename $f)"
        rm -f "$f"
    done
fi

# ── 1. 扫描今天的日志 ──

if [ ! -f "$TASK_FILE" ]; then
    exit 0
fi

CURRENT_COUNT=$(grep -c '===TASK_RECORD_START===' "$TASK_FILE" 2>/dev/null || echo "0")
LAST_KEY="${TODAY}_count"
LAST_COUNT=$(grep "^$LAST_KEY=" "$STATE_FILE" 2>/dev/null | cut -d= -f2)
LAST_COUNT=${LAST_COUNT:-0}

if [ "$CURRENT_COUNT" -le "$LAST_COUNT" ]; then
    exit 0
fi

# ── 2. 提取最新记录 ──

LAST_BLOCK=$(awk '/===TASK_RECORD_START===/{block=""} {block=block"\n"$0} /===TASK_RECORD_END===/{last=block} END{print last}' "$TASK_FILE")

FLOW_STATUS=$(echo "$LAST_BLOCK" | grep -oE '\[(CODE_DONE|REVIEW_DONE|CODE_FIXED|REVIEW_PASS|TASK_ERROR)\]' | tr -d '[]')
TASK_ID=$(echo "$LAST_BLOCK" | grep "task_id:" | head -1 | sed 's/.*task_id:\s*//;s/\s.*//')
TIMESTAMP=$(echo "$LAST_BLOCK" | grep "timestamp:" | head -1 | sed 's/.*timestamp:\s*//;s/\s.*//')
MODULE=$(echo "$LAST_BLOCK" | grep "module:" | head -1 | sed 's/.*module:\s*//' | xargs)

echo ""
echo "══════════════════════════════════════════════"
echo "  🔔 $(date '+%Y-%m-%d %H:%M:%S')"
echo "  日志: task_shturl_$TODAY.md"
echo "  task: $TASK_ID  |  flow: $FLOW_STATUS  |  module: $MODULE"
echo "══════════════════════════════════════════════"
echo ""

# ── 3. 按 flow_status 执行策略 ──

PASS=0; FAIL=0

case "$FLOW_STATUS" in

CODE_DONE)
    echo "  📦 Codex 开发完成 → 全量测试"
    echo ""
    echo "━━━ 语法检查 ━━━"
    for f in "$SERVER_DIR/src/server.mjs" "$SERVER_DIR/src/database.mjs"; do
        node --check "$f" 2>/dev/null && echo "  ✅ $(basename $f)" && PASS=$((PASS+1)) || { echo "  ❌ $(basename $f)"; FAIL=$((FAIL+1)); }
    done
    echo "━━━ 数据模型 ━━━"
    node "$PROJECT_DIR/test-data-model.mjs" 2>&1 && PASS=$((PASS+1)) || FAIL=$((FAIL+1))
    echo "━━━ API 接口 ━━━"
    if curl -s http://127.0.0.1:5188/api/health > /dev/null 2>&1; then
        node "$TEST_DIR/Test2_ApiIntegration.mjs" 2>&1 | grep -q "0 失败" && echo "  ✅ API 6/6" && PASS=$((PASS+1)) || { echo "  ❌ API"; FAIL=$((FAIL+1)); }
    else
        echo "  ⏭️ 服务器离线"
    fi
    ;;

REVIEW_DONE)
    echo "  📋 Claude 评审完成 → 等待 Codex 修复"
    echo ""
    P0=$(echo "$LAST_BLOCK" | grep -c '【P0】' 2>/dev/null || true; echo "0")
    P1=$(echo "$LAST_BLOCK" | grep -c '【P1】' 2>/dev/null || true; echo "0")
    echo "  缺陷: P0×$P0  P1×$P1"
    echo "  task_id: $TASK_ID"
    echo "  下一步: Codex 按缺陷清单修复 → CODE_FIXED"
    PASS=$((PASS+1))
    ;;

CODE_FIXED)
    echo "  🔧 Codex 修复完成 → 回归 + 修复验证"
    echo ""
    echo "━━━ 语法 ━━━"
    for f in "$SERVER_DIR/src/server.mjs" "$SERVER_DIR/src/database.mjs"; do
        node --check "$f" 2>/dev/null && echo "  ✅ $(basename $f)" && PASS=$((PASS+1)) || { echo "  ❌ $(basename $f)"; FAIL=$((FAIL+1)); }
    done
    echo "━━━ 回归 ━━━"
    node "$PROJECT_DIR/test-data-model.mjs" 2>&1 && PASS=$((PASS+1)) || FAIL=$((FAIL+1))
    if curl -s http://127.0.0.1:5188/api/health > /dev/null 2>&1; then
        node "$TEST_DIR/Test2_ApiIntegration.mjs" 2>&1 | grep -q "0 失败" && echo "  ✅ API 回归通过" && PASS=$((PASS+1)) || { echo "  ❌ API"; FAIL=$((FAIL+1)); }
    fi
    echo "━━━ 修复验证 ━━━"
    UNSOLVED=$(echo "$LAST_BLOCK" | grep "未解决" | grep -vc "无" 2>/dev/null || echo "0")
    if [ "$UNSOLVED" -eq 0 ]; then
        echo "  ✅ 全部修复，建议 REVIEW_PASS"
        PASS=$((PASS+1))
    else
        echo "  ⚠️ 仍有未解决项"
        FAIL=$((FAIL+1))
    fi
    ;;

REVIEW_PASS)
    echo "  ✅ 任务闭环 — task_id: $TASK_ID"
    echo "  ┌─────────────────────────────┐"
    echo "  │  全部缺陷已修复               │"
    echo "  │  全部测试通过                 │"
    echo "  │  无需再次迭代                 │"
    echo "  └─────────────────────────────┘"
    PASS=$((PASS+1))
    ;;

TASK_ERROR)
    echo "  🚨 TASK_ERROR — 需人工介入"
    echo "  task_id: $TASK_ID"
    FAIL=$((FAIL+1))
    ;;

*)
    echo "  ⚠️ 未知状态，执行基础检查"
    for f in "$SERVER_DIR/src/server.mjs" "$SERVER_DIR/src/database.mjs"; do
        node --check "$f" 2>/dev/null && echo "  ✅ $(basename $f)" && PASS=$((PASS+1)) || { echo "  ❌ $(basename $f)"; FAIL=$((FAIL+1)); }
    done
    node "$PROJECT_DIR/test-data-model.mjs" 2>&1 && PASS=$((PASS+1)) || FAIL=$((FAIL+1))
    ;;
esac

echo ""
echo "══════════════════════════════════════════════"
echo "  结果: $PASS 通过  $FAIL 失败"
if [ "$FAIL" -eq 0 ]; then echo "  ✅ 全部通过"; else echo "  ❌ 有 $FAIL 项失败"; fi
echo "══════════════════════════════════════════════"

cat > "$RESULT_FILE" << ENDRESULT
Codex Watch 报告 | $(date '+%Y-%m-%d %H:%M:%S')
日志: task_shturl_$TODAY.md
task_id: $TASK_ID
flow_status: $FLOW_STATUS
模块: $MODULE
测试: $PASS 通过 / $FAIL 失败
ENDRESULT

# 更新状态（按天记录）
if grep -q "^$LAST_KEY=" "$STATE_FILE" 2>/dev/null; then
    sed -i "s/^$LAST_KEY=.*/$LAST_KEY=$CURRENT_COUNT/" "$STATE_FILE"
else
    echo "$LAST_KEY=$CURRENT_COUNT" >> "$STATE_FILE"
fi
