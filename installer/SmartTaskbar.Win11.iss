; TaskbarSense (SmartTaskbar.Win11 fork) Installer
; Dual package:
;   Framework (small):
;     ISCC /DMyAppSourceDir=...\publish-framework /DMyOutputBase=TaskbarSense_Setup_2.1.0_Framework /DMyPackageKind=framework installer\SmartTaskbar.Win11.iss
;   SelfContained (large):
;     ISCC /DMyAppSourceDir=...\publish-selfcontained /DMyOutputBase=TaskbarSense_Setup_2.1.0_SelfContained /DMyPackageKind=selfcontained installer\SmartTaskbar.Win11.iss

#define MyAppName "TaskbarSense"
#define MyAppVersion "2.1.0"
#define MyAppPublisher "baolongzhanshi"
#define MyAppURL "https://github.com/baolongzhanshi/TaskbarSense"
#define MyAppExeName "SmartTaskbar.Win11.exe"
#ifndef MyAppSourceDir
  #define MyAppSourceDir "d:\Desktop\SmartTaskbar\publish-selfcontained"
#endif
#define MyAppIcon "d:\Desktop\SmartTaskbar\Sources\SmartTaskbar.Win11\Resources\Logo-White.ico"
#define MyOutputDir "D:\Downloads"
#ifndef MyOutputBase
  #define MyOutputBase "TaskbarSense_Setup_2.1.0_SelfContained"
#endif
#ifndef MyPackageKind
  #define MyPackageKind "selfcontained"
#endif

[Setup]
AppId={{B2C3D4E5-F6A7-8901-BCDE-F12345678901}
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
#if MyPackageKind == "framework"
VersionInfoDescription=TaskbarSense (Framework-dependent, requires .NET 8 Desktop Runtime)
#else
VersionInfoDescription=TaskbarSense (Self-contained, no .NET install required)
#endif
VersionInfoProductName={#MyAppName}
MinVersion=10.0.22000
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "chinesesimplified"; MessagesFile: "languages\ChineseSimplified.isl"
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
Type: filesandordirs; Name: "{localappdata}\TaskbarSense"
Type: filesandordirs; Name: "{localappdata}\SmartTaskbar.Win11"

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
  InstallNet: Integer;
begin
  Exec('taskkill.exe', '/F /IM SmartTaskbar.Win11.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

#if MyPackageKind == "framework"
  if MsgBox(
    '这是 TaskbarSense【小体积 / 框架依赖】安装包。' + #13#10 + #13#10 +
    '运行前需要已安装：.NET 8 Desktop Runtime (x64)。' + #13#10 +
    '若尚未安装，可打开下载页面获取。' + #13#10 + #13#10 +
    '是 = 继续安装' + #13#10 +
    '否 = 打开 .NET 8 下载页面并取消安装',
    mbInformation, MB_YESNO) = IDNO then
  begin
    ShellExec('open',
      'https://dotnet.microsoft.com/download/dotnet/8.0',
      '', '', SW_SHOWNORMAL, ewNoWait, InstallNet);
    Result := False;
    exit;
  end;
#endif

  Result := True;
end;

function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM SmartTaskbar.Win11.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;