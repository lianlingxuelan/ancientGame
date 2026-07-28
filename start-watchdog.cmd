@echo off
chcp 65001 >nul
title Codex 代码监控

echo ==============================================
echo   Codex 代码监控（独立守护进程）
echo   每 10 分钟检查 .codex-activity.log
echo   发现提交 -> 跑测试 -> 写入结果
echo ==============================================
echo.
echo   Claude Cron 30 分钟（主）
echo   本脚本 10 分钟（备用，防 Cron 挂了）
echo   关窗口即全部停止
echo ==============================================
echo.

:loop
bash "F:\AI-project\ancientGame\codex-watch.sh"
timeout /t 600 /nobreak >nul
goto loop
