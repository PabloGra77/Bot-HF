@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo BOT HF - CONSTRUCCION DEL INSTALADOR
echo ============================================================
echo Paso 1/2 : publish portable
call scripts\03_publish_portable_exe.cmd
if errorlevel 1 goto err

echo.
echo Paso 2/2 : compilar instalador con Inno Setup

set "ISCC="
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"

if "%ISCC%"=="" (
  echo.
  echo No se encontro Inno Setup 6 instalado.
  echo Descargue e instale: https://jrsoftware.org/isdl.php
  echo Luego vuelva a ejecutar este script.
  goto err
)

"%ISCC%" installer\BotHF.iss
if errorlevel 1 goto err

echo.
echo OK: Instalador generado en installer\Output\
exit /b 0

:err
echo.
echo ERROR durante la construccion del instalador.
exit /b 1
