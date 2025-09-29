@echo off
REM ======== 配置区 ========
set PORT=8080
set SECRET=123456
set SAVEROOT=C:\ServerFiles\Uploads
set EXTRACTROOT=C:\ServerFiles\Extracted

REM ======== 启动 ServerReceiver.exe ========
echo [INFO] 启动服务器...
echo [INFO] 端口: %PORT%
echo [INFO] 密钥: %SECRET%
echo [INFO] 保存目录: %SAVEROOT%
echo [INFO] 解压目录: %EXTRACTROOT%

"%~dp0ServerReceiver.exe" %PORT% %SECRET% "%SAVEROOT%" "%EXTRACTROOT%"

echo.
echo [INFO] 服务器已退出，按任意键关闭窗口...
pause >nul
