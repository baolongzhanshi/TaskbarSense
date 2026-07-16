; TaskbarSense Installer
; Dual package:
;   Framework:
;     ISCC /DMyAppSourceDir=...\publish-framework /DMyOutputBase=TaskbarSense_Setup_2.2.0_Framework /DMyPackageKind=framework installer\SmartTaskbar.Win11.iss
;   SelfContained (recommended for most users):
;     ISCC /DMyAppSourceDir=...\publish-selfcontained /DMyOutputBase=TaskbarSense_Setup_2.2.0_SelfContained /DMyPackageKind=selfcontained installer\SmartTaskbar.Win11.iss

#define MyAppName "TaskbarSense"
#define MyAppVersion "2.2.0"
#define MyAppPublisher "baolongzhanshi"
#define MyAppURL "https://github.com/baolongzhanshi/TaskbarSense"
#define MyAppExeName "TaskbarSense.exe"
#ifndef MyAppSourceDir
  #define MyAppSourceDir "d:\Desktop\SmartTaskbar\publish-selfcontained"
#endif
#define MyAppIcon "d:\Desktop\SmartTaskbar\Sources\SmartTaskbar.Win11\Resources\Logo-White.ico"
#ifndef MyOutputDir
  #define MyOutputDir "D:\Downloads"
#endif
#ifndef MyOutputBase
  #define MyOutputBase "TaskbarSense_Setup_2.2.0_SelfContained"
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
DefaultDirName={localappdata}\{#MyAppName}
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
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
#if MyPackageKind == "framework"
VersionInfoDescription=TaskbarSense (小体积，需要 .NET 8 桌面运行时)
#else
VersionInfoDescription=TaskbarSense (自包含，推荐，无需安装 .NET)
#endif
VersionInfoProductName={#MyAppName}
MinVersion=10.0.22000
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "chinesesimplified"; MessagesFile: "languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
; Startup is managed in-app (tray menu) to avoid duplicate Run entries.

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "Windows 11 任务栏智能隐藏"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "立即运行 TaskbarSense（托盘图标）"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\TaskbarSense"
Type: filesandordirs; Name: "{localappdata}\SmartTaskbar.Win11"
Type: files; Name: "{userstartup}\TaskbarSense.lnk"
Type: files; Name: "{userstartup}\SmartTaskbar.Win11.lnk"
Type: files; Name: "{userstartup}\SmartTaskbar.lnk"

[Code]
function IsDotNet8DesktopInstalled(): Boolean;
var
  FindRec: TFindRec;
  SharedDir: String;
begin
  Result := False;
  SharedDir := ExpandConstant('{pf}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if not DirExists(SharedDir) then
    exit;
  if FindFirst(SharedDir + '\*', FindRec) then
  try
    repeat
      if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0)
         and (FindRec.Name <> '.') and (FindRec.Name <> '..') then
      begin
        if Copy(FindRec.Name, 1, 2) = '8.' then
        begin
          Result := True;
          Break;
        end;
      end;
    until not FindNext(FindRec);
  finally
    FindClose(FindRec);
  end;
end;

procedure KillLegacyProcesses();
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM TaskbarSense.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill.exe', '/F /IM SmartTaskbar.Win11.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill.exe', '/F /IM SmartTaskbar.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CleanupLegacyStartup();
begin
  RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'SmartTaskbar.Win11');
  RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'SmartTaskbar');
  RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'TaskbarSense.Win11');
  DeleteFile(ExpandConstant('{userstartup}\SmartTaskbar.Win11.lnk'));
  DeleteFile(ExpandConstant('{userstartup}\SmartTaskbar.lnk'));
  DeleteFile(ExpandConstant('{userstartup}\TaskbarSense.lnk'));
end;

function InitializeSetup(): Boolean;
var
  Dummy: Integer;
begin
  KillLegacyProcesses();
  CleanupLegacyStartup();

#if MyPackageKind == "framework"
  if not IsDotNet8DesktopInstalled() then
  begin
    if MsgBox(
      '未检测到 .NET 8 桌面运行时（Desktop Runtime x64）。' + #13#10 + #13#10 +
      '本安装包为【小体积版】，需要先安装运行时。' + #13#10 +
      '若不想安装运行时，请改下 SelfContained（自包含）安装包。' + #13#10 + #13#10 +
      '是 = 打开 .NET 8 下载页面并取消安装' + #13#10 +
      '否 = 仍继续安装（可能无法启动）',
      mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open',
        'https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-desktop-8.0-windows-x64-installer',
        '', '', SW_SHOWNORMAL, ewNoWait, Dummy);
      Result := False;
      exit;
    end;
  end
  else
  begin
    MsgBox(
      '这是【小体积】安装包，已检测到 .NET 8 桌面运行时。' + #13#10 +
      '安装后程序在系统托盘运行，右键图标可设置。',
      mbInformation, MB_OK);
  end;
#else
  MsgBox(
    '这是【推荐 / 自包含】安装包，无需单独安装 .NET。' + #13#10 +
    '安装后程序在系统托盘运行（可能在右下角 ^ 里），右键图标可设置。' + #13#10 +
    '开机自启请在托盘菜单中开启。',
    mbInformation, MB_OK);
#endif

  Result := True;
end;

function InitializeUninstall(): Boolean;
begin
  KillLegacyProcesses();
  CleanupLegacyStartup();
  RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'TaskbarSense');
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Best-effort restore normal taskbar via PowerShell if possible is heavy;
    // document that user can toggle auto-hide in Windows Settings if needed.
  end;
end;