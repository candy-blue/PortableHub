; -----------------------------------------------------------------------------
; Portable Hub - Inno Setup 6 Installer Script
; Windows 平台现代化轻量级便携软件启动与管理中心 安装向导脚本
; -----------------------------------------------------------------------------

#ifndef MyAppVersion
#define MyAppVersion "1.2.0"
#endif

#define MyAppName "Portable Hub"
#define MyAppPublisher "candy-blue"
#define MyAppURL "https://github.com/candy-blue/PortableHub"
#define MyAppExeName "PortableHub.exe"
#define MySourceDir "..\build_standalone"

[Setup]
; AppId 保证唯一性，用于升级识别与 Windows 控制面板卸载管理
AppId={{8B841446-52D0-4E8A-9A02-53A1B6B2E1C9}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; 双模自适应权限支持：默认当前用户免提权安装，安装向导弹窗允许选择为所有用户安装
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
OutputDir=..\release
OutputBaseFilename=PortableHub-Setup-x64
SetupIconFile=..\PortableHub.App\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
CloseApplications=yes
RestartApplications=no
DisableProgramGroupPage=yes

[Languages]
Name: "chinesesimplified"; MessagesFile: "ChineseSimplified.isl"
Name: "chinesetraditional"; MessagesFile: "ChineseTraditional.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
chinesesimplified.PromptDeleteData=是否同时删除个人数据与配置文件（包括便携软件索引数据库、图标缓存与设置）？%n%n数据目录：%1%n%n点击“是”彻底清除数据，点击“否”保留数据以便日后使用。
chinesetraditional.PromptDeleteData=是否同時刪除個人資料與設定檔（包含便攜軟體索引資料庫、圖示快取與設定）？%n%n資料目錄：%1%n%n點擊「是」徹底清除資料，點擊「否」保留資料以便日後使用。
english.PromptDeleteData=Do you also want to delete your personal data and configuration (including software database, icon cache, and settings)?%n%nData Directory: %1%n%nClick "Yes" to delete all data, or "No" to retain it for future use.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// 卸载善后处理：询问用户是否清除用户数据目录，默认聚焦“否”以防误删
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: string;
  PromptMsg: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\PortableHub');
    if DirExists(DataDir) then
    begin
      PromptMsg := FmtMessage(CustomMessage('PromptDeleteData'), [DataDir]);
      if MsgBox(PromptMsg, mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
