@ECHO OFF
setlocal EnableDelayedExpansion

title ��������
 
PUSHD %~DP0 & cd /d "%~dp0"
%1 %2
mshta vbscript:createobject("shell.application").shellexecute("%~s0","goto :runas","","runas",1)(window.close)&goto :eof
:runas
 
:: ɾ������
sc delete ServiceHub >nul 2>&1
 
:: ��������
SET binPath="%~dp0ServiceHub.exe"
if not exist %binPath% (
	echo ��ǰĿ¼������ ServiceHub.exe
	call:exitCMD
)

sc create ServiceHub binPath= %binPath% start= auto DisplayName= "ServiceHub"
sc description ServiceHub "�ű�������������" >nul 2>&1

call:exitCMD

:exitCMD
echo ִ�����,������˳�
pause >nul
exit
GOTO:EOF