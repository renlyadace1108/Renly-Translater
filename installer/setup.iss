; Inno Setup Script for Renly Translater
; Standard Windows installer with customizable installation directory, shortcuts, and full uninstaller.

#define MyAppName "Renly Translater"
#define MyAppFullName "Renly Translater (四六级与雅思双语对照精读系统)"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Renly"
#define MyAppExeName "RenlyTranslater.exe"

[Setup]
AppId={{E1B9A27F-7C21-4F2A-A81E-9C80B0424D11}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppFullName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\RenlyTranslater
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\installer_output
OutputBaseFilename=Renly_Translater_Setup_v{#MyAppVersion}
SetupIconFile=..\src\ExamBilingualReader.Wpf\Assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
DisableDirPage=no
DisableProgramGroupPage=no
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UsePreviousAppDir=yes
CloseApplications=yes
RestartApplications=no
AppMutex=Renly_Translater_Desktop_Mutex

[Languages]
Name: "chinesesimp"; MessagesFile: "ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\src\ExamBilingualReader.Wpf\Assets\app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\ExamBilingualReader.Wpf\Assets\app.png"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppFullName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; AppUserModelID: "Renly.Translater.Desktop"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon; AppUserModelID: "Renly.Translater.Desktop"

[Registry]
Root: HKCU; Subkey: "Software\Classes\AppUserModelId\Renly.Translater.Desktop"; ValueType: string; ValueName: "DisplayName"; ValueData: "Renly Translater"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\AppUserModelId\Renly.Translater.Desktop"; ValueType: string; ValueName: "IconUri"; ValueData: "{app}\app.ico"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\AppUserModelId\Renly.Translater.Desktop"; ValueType: dword; ValueName: "ShowInSettings"; ValueData: "1"; Flags: uninsdeletekey

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppFullName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"

[Code]
// 检测是否存在已安装的路径
function GetExistingInstallDir(): String;
var
  InstalledPath: String;
  UninstallKey: String;
begin
  Result := '';
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1';

  // 1. 优先从当前用户 (HKCU) 卸载注册表中读取
  if RegQueryStringValue(HKCU, UninstallKey, 'Inno Setup: App Path', InstalledPath) then
  begin
    if (InstalledPath <> '') and DirExists(InstalledPath) then
    begin
      Result := InstalledPath;
      Exit;
    end;
  end;

  if RegQueryStringValue(HKCU, UninstallKey, 'InstallLocation', InstalledPath) then
  begin
    if (InstalledPath <> '') and DirExists(InstalledPath) then
    begin
      Result := InstalledPath;
      Exit;
    end;
  end;

  // 2. 尝试从本地机器 (HKLM 64-bit / 32-bit) 读取
  if RegQueryStringValue(HKLM, UninstallKey, 'Inno Setup: App Path', InstalledPath) then
  begin
    if (InstalledPath <> '') and DirExists(InstalledPath) then
    begin
      Result := InstalledPath;
      Exit;
    end;
  end;

  if RegQueryStringValue(HKLM, UninstallKey, 'InstallLocation', InstalledPath) then
  begin
    if (InstalledPath <> '') and DirExists(InstalledPath) then
    begin
      Result := InstalledPath;
      Exit;
    end;
  end;
end;

procedure InitializeWizard();
var
  PrevDir: String;
begin
  PrevDir := GetExistingInstallDir();
  if PrevDir <> '' then
  begin
    WizardForm.DirEdit.Text := PrevDir;
  end;
end;
