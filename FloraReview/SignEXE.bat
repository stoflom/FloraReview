@echo off

REM Get the full path to the exe file from the first argument

SET ExePath=%1

REM Define tool paths and settings
SET timestampUrl=http://timestamp.digicert.com
SET signtool=C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe

REM Execute the signtool command
REM The -a flag signals automatic selection of the best signing certificate from the users store.
"%signtool%" sign -a /tr %timestampUrl% /td sha256 /fd sha256 "%ExePath%"

REM Check the error level after execution:
IF %ERRORLEVEL% NEQ 0 (
    ECHO ERROR: SignTool failed with error code %ERRORLEVEL%.
    EXIT /B %ERRORLEVEL%
) ELSE (
    ECHO SignTool completed successfully.
)

REM Call via the following commands in the .csproj file (the msi actually uses the output from the obj/Rlease..path):
REM call "$(ProjectDir)SignEXE.bat" "$(TargetDir)$(TargetFileName)"
REM call "$(ProjectDir)SignEXE.bat" "$(TargetPath)"
REM call "$(ProjectDir)SignEXE.bat" "$(IntermediateOutputPath)apphost.exe"
REM call "$(ProjectDir)SignEXE.bat" "$(IntermediateOutputPath)$(AssemblyName).dll"