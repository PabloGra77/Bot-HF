@echo off
setlocal
cd /d "%~dp0.."
echo ============================================================
echo PUBLICANDO EXE PORTABLE BOT-HF
echo ============================================================
if exist publish\BotHF rmdir /s /q publish\BotHF

dotnet publish src\HorusAfiliadosExtractor.App\HorusAfiliadosExtractor.App.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  /p:PublishSingleFile=false ^
  /p:PublishTrimmed=false ^
  -o publish\BotHF

if errorlevel 1 goto error

xcopy config publish\BotHF\config\ /E /I /Y >nul
if not exist publish\BotHF\input mkdir publish\BotHF\input
if not exist publish\BotHF\output mkdir publish\BotHF\output
if not exist publish\BotHF\evidence mkdir publish\BotHF\evidence
if not exist publish\BotHF\logs mkdir publish\BotHF\logs

if exist input\cedulas.example.csv copy input\cedulas.example.csv publish\BotHF\input\cedulas.csv >nul

echo @echo off> publish\BotHF\EJECUTAR_BOT_HF.cmd
echo cd /d "%%~dp0">> publish\BotHF\EJECUTAR_BOT_HF.cmd
echo start "" HorusBotHF.exe>> publish\BotHF\EJECUTAR_BOT_HF.cmd

echo.
echo OK: publicado en publish\BotHF
echo Ejecute: publish\BotHF\HorusBotHF.exe
exit /b 0
:error
echo ERROR: fallo la publicacion.
exit /b 1
