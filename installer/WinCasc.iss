#define PublishDir "..\artifacts\publish"
#define WinFspMsi "..\artifacts\winfsp.msi"
#define AppVersion GetVersionNumbersString(PublishDir + "\WinCasc.exe")

[Setup]
AppId={{6E0D7C1A-8B3F-4D52-9A61-2F4C8E7B1D90}
AppName=WinCasc
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}
AppPublisher=Scobalula
AppPublisherURL=https://github.com/Scobalula/WinCasc
AppSupportURL=https://github.com/Scobalula/WinCasc/issues
AppUpdatesURL=https://github.com/Scobalula/WinCasc/releases
AppCopyright=Licensed under GPLv3. Uses WinFsp - Windows File System Proxy, Copyright (C) Bill Zissimopoulos
LicenseFile=..\LICENSE
InfoBeforeFile=..\THIRD-PARTY-NOTICES.txt
DefaultDirName={autopf}\WinCasc
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installer
OutputBaseFilename=WinCasc-{#AppVersion}-setup
SetupIconFile=..\Icon.ico
WizardSmallImageFile=WizardSmallImage.png
WizardImageFile=WizardImage.png
DisableWelcomePage=no
UninstallDisplayIcon={app}\WinCasc.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"
Source: "..\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"
Source: "{#WinFspMsi}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not IsWinFspInstalled

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nWinCasc mounts Blizzard CASC storages as read-only drives so any application can read game files directly.%n%nWinCasc is free software licensed under GPLv3.%nhttps://github.com/Scobalula/WinCasc%n%nUses WinFsp - Windows File System Proxy, Copyright (C) Bill Zissimopoulos.%nhttps://github.com/winfsp/winfsp%n%nUses CascLib, Copyright (c) 2014 Ladislav Zezula.%n%nWinFsp will be installed if it is not already present.

[Registry]
Root: HKA; Subkey: "Software\Classes\Directory\shell\WinCasc.Mount"; ValueType: string; ValueData: "Mount CASC storage"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Directory\shell\WinCasc.Mount"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\WinCascShell.exe"
Root: HKA; Subkey: "Software\Classes\Directory\shell\WinCasc.Mount\command"; ValueType: string; ValueData: """{app}\WinCascShell.exe"" mount ""%V"" --background"
Root: HKA; Subkey: "Software\Classes\Directory\Background\shell\WinCasc.Mount"; ValueType: string; ValueData: "Mount CASC storage"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Directory\Background\shell\WinCasc.Mount"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\WinCascShell.exe"
Root: HKA; Subkey: "Software\Classes\Directory\Background\shell\WinCasc.Mount\command"; ValueType: string; ValueData: """{app}\WinCascShell.exe"" mount ""%V"" --background"
Root: HKA; Subkey: "Software\Classes\Drive\shell\WinCasc.Unmount"; ValueType: string; ValueData: "Unmount CASC storage"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Drive\shell\WinCasc.Unmount"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\WinCascShell.exe"
Root: HKA; Subkey: "Software\Classes\Drive\shell\WinCasc.Unmount\command"; ValueType: string; ValueData: """{app}\WinCascShell.exe"" unmount %V"

[Run]
Filename: "msiexec.exe"; Parameters: "/i ""{tmp}\winfsp.msi"" /qn /norestart"; StatusMsg: "Installing WinFsp..."; Flags: waituntilterminated; Check: not IsWinFspInstalled
Filename: "{app}\WinCasc.exe"; Parameters: "unregister"; Flags: runhidden waituntilterminated

[UninstallRun]
Filename: "{app}\WinCasc.exe"; Parameters: "unmount --all"; Flags: runhidden waituntilterminated; RunOnceId: "UnmountAll"
Filename: "{app}\WinCasc.exe"; Parameters: "unregister"; Flags: runhidden waituntilterminated; RunOnceId: "Unregister"

[Code]
function IsWinFspInstalled: Boolean;
begin
  Result := RegValueExists(HKLM32, 'SOFTWARE\WinFsp', 'InstallDir');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  if FileExists(ExpandConstant('{app}\WinCasc.exe')) then
  begin
    Exec(ExpandConstant('{app}\WinCasc.exe'), 'unmount --all', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;

  Result := '';
end;
