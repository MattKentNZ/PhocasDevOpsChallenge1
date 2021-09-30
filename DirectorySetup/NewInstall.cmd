NET SESSION
IF %ERRORLEVEL% NEQ 0 GOTO ELEVATE
GOTO ADMINTASKS

:ELEVATE
CD /d %~dp0
MSHTA "javascript: var shell = new ActiveXObject('shell.application'); shell.ShellExecute('%~nx0', '', '', 'runas', 1);close();"
EXIT

:ADMINTASKS
md "C:\Program Files\Terraform"
md "C:\Program Files\Terraform\VersionControl"
echo.>"C:\Windows\Temp\VersionInfo.txt"
:: Create the Text File in the Temp folder and move it so it's still editable by the user, otherwise it gets locked due to creation in a privilaged location.
move "C:\Windows\Temp\VersionInfo.txt" "C:\Program Files\Terraform\VersionControl"
END
