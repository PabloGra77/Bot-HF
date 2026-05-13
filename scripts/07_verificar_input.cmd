@echo off
cd /d "%~dp0\.."
echo ============================================================
echo VERIFICANDO input\cedulas.csv
echo ============================================================
if not exist input\cedulas.csv (
  echo ERROR: No existe input\cedulas.csv
  pause
  exit /b 1
)
echo Archivo: %cd%\input\cedulas.csv
echo.
type input\cedulas.csv
echo.
echo ============================================================
echo Si aparece 1234567890, reemplacelo por las cedulas reales.
echo Formato correcto:
echo Documento
echo 20683684
echo 1006197901
echo ============================================================
pause
