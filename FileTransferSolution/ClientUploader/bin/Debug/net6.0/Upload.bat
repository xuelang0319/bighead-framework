@echo off
REM === 配置区 ===
set FOLDER=.\test
set SERVER=http://120.26.192.253:8080/upload
set SECRET=123456

echo [INFO] 开始上传...
"%~dp0ClientUploader.exe" "%FOLDER%" "%SERVER%" "%SECRET%"

echo.
echo [INFO] 上传完成，按任意键退出...
pause >nul
