:MAIN
"%~DP0KF2 Server Manager.exe"
:LOOP
TIMEOUT 3600 /NOBREAK
TASKLIST|FIND /i "KFServer.exe"&&GOTO LOOP
GOTO MAIN