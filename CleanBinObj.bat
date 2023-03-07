@ECHO OFF
REM Batch file for removing certain extra files and directories
ECHO ************************************************
ECHO * This utility will remove Bin and Obj folders *
ECHO ************************************************
ECHO.
CHOICE/C YN /M "Are you sure you want to clean the folder '%CD%'?"
IF ERRORLEVEL 2 GOTO :no
IF ERRORLEVEL 1 GOTO :yes

:yes 

REM
REM Directories
REM

ECHO Removing directories...

REM bin
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S *bin') DO RMDIR /S /Q "%%G"

REM obj
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S *obj') DO RMDIR /S /Q "%%G"

GOTO :end

:no
echo Folder was NOT cleaned.
GOTO :end
:end 
echo Finished.