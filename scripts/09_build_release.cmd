@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0.."

if "%~1"=="" (
  echo Uso: scripts\09_build_release.cmd ^<version^>
  echo Ejemplo: scripts\09_build_release.cmd 1.0.1
  exit /b 1
)
set "RELEASE_VERSION=%~1"

echo ============================================================
echo BOT HF - EMPAQUETADO DE RELEASE  v%RELEASE_VERSION%
echo ============================================================

echo Paso 1/3 : verificar herramienta vpk (Velopack CLI)
where vpk >nul 2>nul
if errorlevel 1 (
  echo Instalando herramienta global vpk ...
  dotnet tool install -g vpk
  if errorlevel 1 (
    echo Si ya existe, actualizando...
    dotnet tool update -g vpk
  )
)

echo.
echo Paso 2/3 : dotnet publish self-contained
if exist publish\BotHF rmdir /s /q publish\BotHF
dotnet publish src\HorusAfiliadosExtractor.App\HorusAfiliadosExtractor.App.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  /p:PublishSingleFile=false ^
  /p:PublishTrimmed=false ^
  -o publish\BotHF
if errorlevel 1 goto err

xcopy config publish\BotHF\config\ /E /I /Y >nul
if not exist publish\BotHF\input mkdir publish\BotHF\input
if exist input\cedulas.example.csv copy input\cedulas.example.csv publish\BotHF\input\cedulas.csv >nul

echo.
echo Paso 3/3 : vpk pack
if not exist releases mkdir releases
vpk pack ^
  --packId BotHF ^
  --packVersion %RELEASE_VERSION% ^
  --packDir publish\BotHF ^
  --mainExe HorusBotHF.exe ^
  --packTitle "Bot HF - Extractor Horus FPS" ^
  --packAuthors "Horus FPS" ^
  --outputDir releases
if errorlevel 1 goto err

echo.
echo OK: release empaquetado en carpeta releases\
echo Archivos generados:
dir /b releases\
echo.
echo Siguiente paso: crear release en GitHub con esos archivos
echo   - Subir TODOS los archivos de releases\ a una nueva GitHub Release con tag v%RELEASE_VERSION%
echo   - URL: https://github.com/PabloGra77/Bot-HF/releases/new
echo.
echo Los PCs con la app instalada actualizaran automaticamente en su siguiente arranque.
exit /b 0

:err
echo.
echo ERROR durante el empaquetado.
exit /b 1
