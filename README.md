# Bot HF - Extractor de Afiliados Horus FPS

Aplicación Windows (WinForms + .NET 8) que consulta afiliados en `https://fps.horus-health.com/aseguramiento/afiliados`.

## Características

- Interfaz gráfica profesional. Solo hay que **adjuntar** el archivo de cédulas, elegir el tipo de usuario (Afiliado / Funcionario), e ingresar email + contraseña.
- Botón para **descargar plantilla de ejemplo** del archivo de cédulas.
- **Login automático** en Horus FPS (selectores configurables).
- **Auto-actualización**: la aplicación se actualiza sola via GitHub Releases (Velopack). El usuario no tiene que reinstalar.
- Guardado incremental del Excel: si Edge falla o se interrumpe, el resultado parcial queda guardado.
- Solo lectura: el bot **no presiona Guardar, Actualizar, Eliminar, Registrar ni Enviar**.

## Seguridad

- Las credenciales **no se guardan** — solo viven en memoria durante la ejecución.
- `.gitignore` excluye `profiles/`, `logs/`, `evidence/`, `output/`, `input/cedulas.csv` y cualquier `.xlsx` en `input/`. **Nunca** subir esos datos al repo público.

## Estructura

```text
Bot-HF/
├── config/
│   └── appsettings.json
├── input/
│   ├── cedulas.example.csv      <- ejemplo de formato
│   └── cedulas.csv              <- (datos reales, no se sube)
├── scripts/
│   ├── 01_build.cmd
│   ├── 02_run_extractor.cmd
│   ├── 03_publish_portable_exe.cmd
│   ├── 09_build_release.cmd     <- empaqueta release con Velopack
│   └── ...
└── src/
    └── HorusAfiliadosExtractor.App/
        ├── Forms/         <- LoginForm + ProgressForm
        ├── Services/      <- HorusExtractorBot, UpdateService, etc.
        ├── Resources/     <- plantilla CSV embebida
        └── ...
```

## Uso en desarrollo

```cmd
scripts\02_run_extractor.cmd
```

## Publicar una nueva versión (auto-update para todos los PCs)

Cada vez que quiera distribuir una nueva versión:

1. Suba el cambio de versión en `src/HorusAfiliadosExtractor.App/HorusAfiliadosExtractor.App.csproj`:

   ```xml
   <Version>1.0.1</Version>
   ```

2. Empaquete el release:

   ```cmd
   scripts\09_build_release.cmd 1.0.1
   ```

   Esto genera, en la carpeta `releases\`:

   - `BotHF-1.0.1-full.nupkg` — paquete completo.
   - `BotHF-Setup.exe` — instalador para nuevos PCs.
   - `RELEASES` — manifiesto.

3. Cree una nueva **GitHub Release** en `https://github.com/PabloGra77/Bot-HF/releases/new`:

   - Tag: `v1.0.1` (o el número que coloque en el csproj).
   - Adjunte **todos los archivos** de la carpeta `releases\`.
   - Publique.

4. Listo. Los PCs con el bot instalado detectarán la nueva versión la próxima vez que abran la aplicación, la descargarán en segundo plano y la aplicarán al cerrar la app.

## Primer despliegue (PC nuevo)

Distribuya `BotHF-Setup.exe` (generado por el paso 2 anterior). El usuario lo ejecuta una sola vez; a partir de ahí las actualizaciones son automáticas.

## Cómo se ve la actualización para el usuario

- Al abrir la app, en la pantalla de login aparece abajo: *"Buscando actualizaciones..."* → *"Descargando actualización 1.0.1..."* → *"Actualización 1.0.1 lista. Se aplicará al cerrar"* (en verde).
- Durante la extracción, en el log en vivo aparecen mensajes con prefijo `[UPDATE]`.
- Al cerrar la aplicación, la nueva versión se aplica silenciosamente y queda instalada para la próxima ejecución.

## Configuración

Archivo: `config\appsettings.json`. Los selectores del formulario de login se pueden cambiar sin recompilar:

```json
"EmailFieldSelectors": "input[type='email'], ...",
"PasswordFieldSelectors": "input[type='password']",
"UserTypeSelectors": "select[name*='tipo' i], ...",
"SubmitButtonSelectors": "button[type='submit'], ..."
```

## Salida

```text
output\extraccion_horus_afiliados.xlsx
```

Hojas:
- `LOG_PROCESO` — estado por documento.
- `DATOS_LARGOS` — todos los campos por pestaña.
- `RESUMEN_AFILIADOS` — resumen ancho por paciente.
- `TABLAS` — celdas de tablas detectadas.
- `TEXTO_VISIBLE` — texto completo por pestaña.

## Requisitos

- Windows 10/11 x64.
- Microsoft Edge instalado.
- .NET 8 SDK (solo para compilar/empaquetar releases).
- Velopack CLI: el script `09_build_release.cmd` lo instala automáticamente la primera vez (`dotnet tool install -g vpk`).
