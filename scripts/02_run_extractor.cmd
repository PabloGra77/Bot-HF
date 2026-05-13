@echo off
setlocal
cd /d "%~dp0.."
echo ============================================================
echo BOT HF - HORUS FPS - EXTRACCION DE AFILIADOS
echo ============================================================
echo 1. Coloque las cedulas en input\cedulas.csv
echo 2. La aplicacion abrira una ventana para ingresar tipo, email y password.
echo.
dotnet run --project src\HorusAfiliadosExtractor.App\HorusAfiliadosExtractor.App.csproj -- --config config\appsettings.json
set EXITCODE=%ERRORLEVEL%
echo Codigo salida: %EXITCODE%
exit /b %EXITCODE%
