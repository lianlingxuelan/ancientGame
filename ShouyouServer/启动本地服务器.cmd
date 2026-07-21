@echo off
chcp 65001 > nul
cd /d "%~dp0"
echo 正在启动手游本地服务器……
echo.
npm.cmd start
pause
