; SPDX-License-Identifier: GPL-3.0-only
; Copyright (c) 2026 Lorea contributors
#define AppVersion "0.1.6"
[Setup]
AppId={{26C012DA-5431-49E7-9F30-C70390DFD7B1}
AppName=Lorea
AppVersion={#AppVersion}
AppPublisher=Lorea
DefaultDirName={localappdata}\Programs\Lorea
DefaultGroupName=Lorea
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=dist\installer
OutputBaseFilename=Lorea-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=assets\lorea.ico
UninstallDisplayIcon={app}\Lorea.exe
ChangesAssociations=yes
CloseApplications=yes
RestartApplications=no
DisableProgramGroupPage=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; Flags: unchecked

[Files]
Source: "LICENSE"; DestDir: "{app}"; DestName: "COPYING.txt"; Flags: ignoreversion
Source: "COPYRIGHT.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "BRANDING.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "dist\source\Lorea-source.zip"; DestDir: "{app}\source"; Flags: ignoreversion
Source: "dist\Lorea-release.exe"; DestDir: "{app}"; DestName: "Lorea.exe"; Flags: ignoreversion
Source: "dist\codecs\magick.exe"; DestDir: "{app}\codecs"; Flags: ignoreversion
Source: "dist\codecs\*.xml"; DestDir: "{app}\codecs"; Flags: ignoreversion
Source: "dist\codecs\*.icc"; DestDir: "{app}\codecs"; Flags: ignoreversion
Source: "dist\codecs\LICENSE.txt"; DestDir: "{app}\codecs"; Flags: ignoreversion
Source: "dist\codecs\NOTICE.txt"; DestDir: "{app}\codecs"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Lorea"; Filename: "{app}\Lorea.exe"
Name: "{autodesktop}\Lorea"; Filename: "{app}\Lorea.exe"; Tasks: desktopicon

[InstallDelete]
Type: files; Name: "{app}\Piccasa.exe"
Type: files; Name: "{autoprograms}\Piccasa.lnk"
Type: files; Name: "{autodesktop}\Piccasa.lnk"

[Registry]
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueName: "Piccasa"; Flags: deletevalue
Root: HKCU; Subkey: "Software\Classes\Applications\Piccasa.exe"; Flags: deletekey
Root: HKCU; Subkey: "Software\Classes\Piccasa.Image"; ValueType: string; ValueData: "Imagen de Lorea"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Piccasa.Image\DefaultIcon"; ValueType: string; ValueData: "{app}\Lorea.exe,0"
Root: HKCU; Subkey: "Software\Classes\Piccasa.Image\shell\open\command"; ValueType: string; ValueData: """{app}\Lorea.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\Applications\Lorea.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "Lorea"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Applications\Lorea.exe\shell\open\command"; ValueType: string; ValueData: """{app}\Lorea.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Piccasa\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "Lorea"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Piccasa\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Visor de imágenes local"
Root: HKCU; Subkey: "Software\Piccasa\Capabilities"; ValueType: string; ValueName: "ApplicationIcon"; ValueData: "{app}\Lorea.exe,0"
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "Lorea"; ValueData: "Software\Piccasa\Capabilities"; Flags: uninsdeletevalue
#include "tmp\associations.iss"

[Run]
Filename: "{app}\Lorea.exe"; Description: "Abrir Lorea"; Flags: nowait postinstall skipifsilent unchecked
Filename: "ms-settings:defaultapps?registeredAppUser=Lorea"; Description: "Elegir Lorea como visor predeterminado en Windows"; Flags: shellexec postinstall skipifsilent
