; SmartTaskbar.Win11 Installer
; Self-contained - no .NET 8 Desktop Runtime required

#define MyAppName "SmartTaskbar.Win11"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "SmartTaskbar.Win11"
#define MyAppURL "https://github.com/baolongzhanshi/SmartTaskbar.Win11"
#define MyAppExeName "SmartTaskbar.Win11.exe"
#define MyAppSourceDir "d:\Desktop\SmartTaskbar\publish-selfcontained"
#define MyAppIcon "d:\Desktop\SmartTaskbar\Sources\SmartTaskbar.Win11\Resources\Logo-White.ico"
#define MyOutputDir "D:\Downloads"
#define MyOutputBase "SmartTaskbar.Win11_Setup_2.0.0"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#MyOutputDir}
OutputBaseFilename={#MyOutputBase}
SetupIconFile={#MyAppIcon}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=SmartTaskbar for Windows 11 with MaximizeHide mode
VersionInfoProductName={#MyAppName}
MinVersion=10.0.22000
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "开机自动启动"; GroupDescription: "启动选项:"; Flags: unchecked

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\SmartTaskbar.Win11"

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  // Stop running instance before install
  Exec('taskkill.exe', '/F /IM SmartTaskbar.Win11.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;

function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM SmartTaskbar.Win11.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;