; =============================================================================
; Instalador - Generador de Pautas
; Script para Inno Setup 6
; =============================================================================

#define MyAppName "Generador de Pautas"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sistema de Pautas"
#define MyAppExeName "Generador_Pautas.exe"
#define MyAppURL ""

[Setup]
AppId={{E54C3492-6A0B-46FC-8186-8151ED09B154}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=Instalador_Output
OutputBaseFilename=Generador_Pautas_Setup_{#MyAppVersion}
SetupIconFile=Generador_Pautas\icons8-pautas-64.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppVersion}
VersionInfoProductName={#MyAppName}
MinVersion=10.0

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el &Escritorio"; GroupDescription: "Accesos directos:"; Flags: checkedonce
Name: "startmenuicon"; Description: "Crear acceso directo en el &Menu Inicio"; GroupDescription: "Accesos directos:"; Flags: checkedonce

[Files]
; Ejecutable principal
Source: "Generador_Pautas\bin\Release\Generador_Pautas.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Generador_Pautas\bin\Release\Generador_Pautas.exe.config"; DestDir: "{app}"; Flags: ignoreversion

; Configuracion (no sobreescribir si ya existe - el usuario puede haber cambiado la config)
Source: "Generador_Pautas\bin\Release\config.ini"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist

; DLLs - Base de datos PostgreSQL
Source: "Generador_Pautas\bin\Release\Npgsql.dll"; DestDir: "{app}"; Flags: ignoreversion

; DLLs - Excel (ClosedXML)
Source: "Generador_Pautas\bin\Release\ClosedXML.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Generador_Pautas\bin\Release\DocumentFormat.OpenXml.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Generador_Pautas\bin\Release\ExcelNumberFormat.dll"; DestDir: "{app}"; Flags: ignoreversion

; DLLs - Audio (BASS)
Source: "Generador_Pautas\bin\Release\bass.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Generador_Pautas\bin\Release\Bass.Net.dll"; DestDir: "{app}"; Flags: ignoreversion

; DLLs - .NET Framework extras
Source: "Generador_Pautas\bin\Release\Microsoft.Bcl.AsyncInterfaces.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Generador_Pautas\bin\Release\System.Buffers.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Generador_Pautas\bin\Release\System.Memory.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Generador_Pautas\bin\Release\System.Numerics.Vectors.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Generador_Pautas\bin\Release\System.Runtime.CompilerServices.Unsafe.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Generador_Pautas\bin\Release\System.Text.Encodings.Web.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Generador_Pautas\bin\Release\System.Text.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Generador_Pautas\bin\Release\System.Threading.Tasks.Extensions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Generador_Pautas\bin\Release\System.ValueTuple.dll"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
; Crear carpeta de reportes
Name: "{app}\REPORTES"; Permissions: everyone-full

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenuicon
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"; Tasks: startmenuicon
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Ejecutar {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
// Verificar que .NET Framework 4.8 esta instalado
function IsDotNetInstalled(): Boolean;
var
  Release: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then
  begin
    // 528040 = .NET Framework 4.8
    Result := (Release >= 528040);
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNetInstalled() then
  begin
    MsgBox('Este programa requiere .NET Framework 4.8 o superior.'#13#10#13#10
           'Por favor, descargue e instale .NET Framework 4.8 desde:'#13#10
           'https://dotnet.microsoft.com/download/dotnet-framework/net48'#13#10#13#10
           'La instalacion se cancelara.',
           mbCriticalError, MB_OK);
    Result := False;
  end;
end;
