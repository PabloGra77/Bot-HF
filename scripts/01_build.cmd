@echo off
setlocal
cd /d "%~dp0.."
echo ============================================================
echo COMPILANDO HorusAfiliadosExtractorBot
echo ============================================================
dotnet restore HorusAfiliadosExtractorBot.sln
if errorlevel 1 goto error
dotnet build HorusAfiliadosExtractorBot.sln -c Debug
if errorlevel 1 goto error
echo.
echo OK: compilacion finalizada.
pause
exit /b 0
:error
echo.
echo ERROR: fallo la compilacion.
pause
exit /b 1
