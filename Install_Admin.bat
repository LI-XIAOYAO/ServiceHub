@ECHO OFF
setlocal EnableDelayedExpansion

title ��������

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