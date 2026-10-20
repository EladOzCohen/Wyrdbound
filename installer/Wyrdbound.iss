; Wyrdbound installer (Inno Setup 6). Build it with tools/make-release.ps1, which first builds the game
; into Builds/Release/Wyrdbound and then compiles this script into Builds/Release/Wyrdbound-Setup-<version>.exe.
; Installs per-user (no admin prompt) to %LOCALAPPDATA%\Programs\Wyrdbound, adds Start-menu and optional desktop
; shortcuts, and registers an uninstaller under Windows "Apps". Save games are kept on uninstall.

#ifndef AppVersion
  #define AppVersion "0.3.0"
#endif

[Setup]
AppId={{6B1C7E2A-5F3D-4C8E-9A11-2B7D3E4F5A60}
AppName=Wyrdbound
AppVersion={#AppVersion}
AppVerName=Wyrdbound {#AppVersion}
AppPublisher=Wyrdbound
AppPublisherURL=https://github.com/EladOzCohen/Wyrdbound
AppSupportURL=https://github.com/EladOzCohen/Wyrdbound/issues
DefaultDirName={autopf}\Wyrdbound
DefaultGroupName=Wyrdbound
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\Builds\Release
OutputBaseFilename=Wyrdbound-Setup-{#AppVersion}
SetupIconFile=Wyrdbound.ico
UninstallDisplayIcon={app}\Wyrdbound.exe
UninstallDisplayName=Wyrdbound
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=auto

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\Builds\Release\Wyrdbound\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Wyrdbound"; Filename: "{app}\Wyrdbound.exe"
Name: "{autodesktop}\Wyrdbound"; Filename: "{app}\Wyrdbound.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Wyrdbound.exe"; Description: "{cm:LaunchProgram,Wyrdbound}"; Flags: nowait postinstall skipifsilent
