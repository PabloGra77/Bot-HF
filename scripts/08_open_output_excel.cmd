@echo off
cd /d "%~dp0.."
if exist "output\extraccion_horus_afiliados.xlsx" (
  start "" "output\extraccion_horus_afiliados.xlsx"
) else (
  echo No existe output\extraccion_horus_afiliados.xlsx todavia. Ejecute primero scripts\02_run_extractor.cmd
  pause
)
