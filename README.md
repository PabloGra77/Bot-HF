# Bot HF - Extractor de Afiliados Horus FPS

Aplicación Windows (WinForms + .NET 8) que consulta afiliados en:

```text
https://fps.horus-health.com/aseguramiento/afiliados
```

Al abrirse, muestra una ventana donde el usuario selecciona el **tipo de usuario** (Afiliado / Funcionario) e ingresa **email** y **contraseña**. La aplicación entra al sistema, recorre los documentos del archivo de entrada y vuelca los datos visibles a Excel.

## Seguridad operacional

El bot está diseñado en modo **solo consulta / extracción**:

- No presiona Guardar.
- No presiona Actualizar.
- No presiona Eliminar.
- No presiona Registrar.
- No presiona Enviar.
- Solo lee el campo **Documento del afiliado**, el botón **Consultar afiliado**, **Nueva consulta** y las pestañas visibles.

Las credenciales **no se guardan**: se solicitan al inicio de cada ejecución y se mantienen únicamente en memoria.

## Estructura

```text
Bot-HF/
├── config/
│   └── appsettings.json
├── input/
│   ├── cedulas.example.csv      <- ejemplo
│   └── cedulas.csv              <- (real, no se sube al repo)
├── installer/
│   └── BotHF.iss                <- script Inno Setup
├── scripts/
│   ├── 01_build.cmd
│   ├── 02_run_extractor.cmd
│   ├── 03_publish_portable_exe.cmd
│   ├── 09_build_installer.cmd   <- genera el .exe instalador
│   └── ...
└── src/
    └── HorusAfiliadosExtractor.App/
```

## Compilar / ejecutar en desarrollo

```cmd
scripts\01_build.cmd
scripts\02_run_extractor.cmd
```

## Publicar EXE portable

```cmd
scripts\03_publish_portable_exe.cmd
```

Quedará en `publish\BotHF\HorusBotHF.exe`.

## Generar instalador (.exe)

Requiere **Inno Setup 6** instalado (https://jrsoftware.org/isdl.php).

```cmd
scripts\09_build_installer.cmd
```

Salida: `installer\Output\BotHF_Setup_1.0.0.exe`. Ese único archivo se distribuye a otros equipos.

## Flujo de uso

1. Abrir `input\cedulas.csv` y colocar los documentos:

```csv
Documento
20683684
1006197901
41794986
```

2. Ejecutar `HorusBotHF.exe`.
3. En la ventana de login:
   - Tipo de usuario: Afiliado o Funcionario.
   - Correo electrónico.
   - Contraseña.
   - (Opcional) cambiar el archivo de cédulas con **Examinar**.
4. Pulsar **Iniciar extracción**. Se abre Edge, se hace login automático, se procesan los documentos y se muestra una ventana con el progreso y el log en vivo.
5. Al terminar, pulsar **Abrir Excel resultado**.

## Resultado

```text
output\extraccion_horus_afiliados.xlsx
```

Hojas:
- `LOG_PROCESO` — estado por documento.
- `DATOS_LARGOS` — todos los campos por pestaña.
- `RESUMEN_AFILIADOS` — resumen ancho por paciente.
- `TABLAS` — celdas de tablas detectadas.
- `TEXTO_VISIBLE` — texto completo por pestaña.

## Configuración

Archivo: `config\appsettings.json`. Los selectores del formulario de login se pueden ajustar sin recompilar:

```json
"EmailFieldSelectors": "input[type='email'], ...",
"PasswordFieldSelectors": "input[type='password']",
"UserTypeSelectors": "select[name*='tipo' i], ...",
"SubmitButtonSelectors": "button[type='submit'], ..."
```

Si el login automático no completa, el bot deja la ventana de Edge abierta para terminar manualmente.

## Pestañas leídas por defecto

- DATOS BÁSICOS
- DATOS COMPLEMENTARIOS
- DATOS PENSIÓN
- DATOS TRASLADO
- BENEFICIARIOS
- HISTÓRICO NOVEDADES

Configurables en `config\appsettings.json` → `TabsToExtract`.

## Guardado incremental

El Excel se reescribe atómicamente después de cada documento. Si Edge se cierra o falla la red, se conserva lo procesado hasta el último guardado.

## Datos sensibles

`.gitignore` excluye `profiles/`, `logs/`, `evidence/`, `output/`, `input/cedulas.csv` y cualquier `.xlsx` en `input/`. **Nunca** subir esos directorios al repositorio público.

## Requisitos

- Windows 10/11 x64.
- Microsoft Edge instalado.
- .NET 8 SDK (solo para compilar). Para el instalador final no se requiere.
