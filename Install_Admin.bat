@ECHO OFF
setlocal EnableDelayedExpansion

title 创建服务

:: 删除服务
sc delete ServiceHub >nul 2>&1
 
:: 创建服务
SET binPath="%~dp0ServiceHub.exe"
if not exist %binPath% (
	echo 当前目录不存在 ServiceHub.exe
	call:exitCMD
)

sc create ServiceHub binPath= %binPath% start= auto DisplayName= "ServiceHub"
sc description ServiceHub "脚本服务运行中心" >nul 2>&1

call:exitCMD

:exitCMD
echo 执行完毕,任意键退出
pause >nul
exit
GOTO:EOF