@echo off
chcp 65001 > nul
cd %~dp0
setlocal
echo Running...

python A_生成AppConfig文件.py
pause