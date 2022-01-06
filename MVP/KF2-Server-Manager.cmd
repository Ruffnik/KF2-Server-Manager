:MAIN
"%~DP0KF2 Server Manager.exe"
DEL /Q "%~DP0dumps\*.*"
:LOOP
TIMEOUT 3600
TASKLIST|FIND /i "KFServer.exe"&&GOTO LOOP
DEL /Q "%~DP0steamapps\common\kf2server\KFGame\Logs\*.*"
GOTO MAIN