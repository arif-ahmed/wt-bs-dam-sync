@echo off
REM =======================================
REM run_publish_sh.bat
REM Runs the Bash publish.sh script from repo root
REM =======================================

REM Go to the folder where this .bat is located (repo root)
cd /d "%~dp0"

REM --- Option 1: Using Git Bash (most common on Windows) ---
REM Uncomment this line if you use Git Bash:
"C:\Program Files\Git\bin\bash.exe" ./scripts/publish.sh

REM --- Option 2: Using WSL (Ubuntu/Debian/...) ---
REM Uncomment this line if you use WSL instead:
REM wsl bash ./scripts/publish.sh

pause
