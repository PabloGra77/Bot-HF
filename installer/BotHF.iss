; ============================================================
; Inno Setup script - Bot HF (Extractor de Afiliados Horus FPS)
; Genera: installer/Output/BotHF_Setup.exe
;
; Requiere Inno Setup 6+ instalado: https://jrsoftware.org/isdl.php
; Antes de compilar, ejecutar: scripts\03_publish_portable_exe.cmd
; ============================================================

#define MyAppName "Bot HF - Extractor Horus FPS"
#define MyAppShortName "BotHF"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Horus FPS"
#define MyAppExeName "HorusBotHF.exe"

[Setup]
AppId={{8A0D4E09-21DF-4B2A-90E3-CE5E9E0B6210}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppShortName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=BotHF_Setup_{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
WizardStyle=modern
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Files]
; Publicado desde scripts\03_publish_portable_exe.cmd (publish\BotHF)
Source: "..\publish\BotHF\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\input"; Permissions: users-modify
Name: "{app}\output"; Permissions: users-modify
Name: "{app}\logs"; Permissions: users-modify
Name: "{app}\evidence"; Permissions: users-modify
Name: "{app}\profiles"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Editar archivo de cedulas"; Filename: "{app}\input\cedulas.csv"
Name: "{group}\Carpeta de resultados"; Filename: "{app}\output"
Name: "{group}\Desinstalar"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Iniciar {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  if not FileExists(ExpandConstant('{src}\..\publish\BotHF\HorusBotHF.exe')) then
  begin
    MsgBox('No se encontro publish\BotHF\HorusBotHF.exe.' + #13#10 +
           'Ejecute primero: scripts\03_publish_portable_exe.cmd',
           mbError, MB_OK);
    Result := False;
  end;
end;
