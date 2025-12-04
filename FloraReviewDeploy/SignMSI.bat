@echo off

REM Get the installer path from the first argument

SET InstallerPath=%1

REM Define tool paths and settings
SET timestampUrl=http://timestamp.digicert.com
SET signtool=C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe

REM Execute the signtool command
REM Use quotes around paths that may contain spaces to ensure they are handled correctly.
REM The '&' operator in PowerShell is replaced by directly calling the executable in batch.
"%signtool%" sign -a /tr %timestampUrl% /td sha256 /fd sha256 "%InstallerPath%"

REM Check the error level after execution:
IF %ERRORLEVEL% NEQ 0 (
    ECHO ERROR: SignTool failed with error code %ERRORLEVEL%.
    EXIT /B %ERRORLEVEL%
) ELSE (
    ECHO SignTool completed successfully.
)

REM Call via the following command in the .csproj file:
REM call "$(ProjectDir)SignMSI.bat" "$(BuiltOuputPath)"