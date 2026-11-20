; Instalador de Windows para Sistema de inventario.
;
; Compilación local:
;   ISCC.exe installer\PosInventario.iss
;
; Compilación desde CI (versión y carpeta publicada):
;   ISCC.exe /DAppVersion=1.0.5 /DSourceDir=C:\ruta\publish installer\PosInventario.iss
;
; La aplicación guarda base, respaldos y logs en
; %LOCALAPPDATA%\SistemaInventario. Ese directorio NO se elimina al
; desinstalar: los datos del negocio deben sobrevivir a una reinstalación.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#ifndef SourceDir
  #define SourceDir "..\publish"
#endif

#define AppName "Sistema de inventario"
#define AppExeName "PosInventario.exe"

[Setup]
AppId={{1573E7EC-729B-4AEC-99A6-2FE44129698B}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=Sistema de inventario
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\dist
OutputBaseFilename=PosInventario-{#AppVersion}-win-x64-installer
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
UsePreviousAppDir=yes
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked

[InstallDelete]
; La publicación ahora es un único ejecutable. Al actualizar se eliminan los
; archivos del formato anterior para que no queden DLL sueltas que Windows
; pueda bloquear.
Type: files; Name: "{app}\*.dll"
Type: files; Name: "{app}\*.json"
Type: files; Name: "{app}\*.pdb"

[Files]
; Se copia el ejecutable único self-contained (incluye el runtime de .NET).
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Iniciar {#AppName}"; Flags: nowait postinstall skipifsilent
