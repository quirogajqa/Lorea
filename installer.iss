; SPDX-License-Identifier: GPL-3.0-only
; Copyright (c) 2026 Lorea contributors
#define AppVersion "0.1.8"
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
Name: "svgthumbnails"; Description: "Mostrar miniaturas SVG en el Explorador (si no existe otro proveedor)"

[Files]
Source: "LICENSE"; DestDir: "{app}"; DestName: "COPYING.txt"; Flags: ignoreversion
Source: "COPYRIGHT.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "BRANDING.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "dist\source\Lorea-source.zip"; DestDir: "{app}\source"; Flags: ignoreversion
Source: "dist\Lorea-release.exe"; DestDir: "{app}"; DestName: "Lorea.exe"; Flags: ignoreversion
Source: "dist\Lorea.Thumbnails.dll"; DestDir: "{app}"; DestName: "Lorea.Thumbnails-{#AppVersion}.dll"; Flags: ignoreversion; Tasks: svgthumbnails
Source: "thumbnail-policy\policy.xml"; DestDir: "{app}\thumbnail-policy"; Flags: ignoreversion; Tasks: svgthumbnails
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
Root: HKCU; Subkey: "Software\Classes\CLSID\{{D7903169-9D1C-4CB8-9CA1-884652133E71}"; ValueType: string; ValueData: "Lorea SVG thumbnails"; Flags: uninsdeletekey; Tasks: svgthumbnails
Root: HKCU; Subkey: "Software\Classes\CLSID\{{D7903169-9D1C-4CB8-9CA1-884652133E71}\InprocServer32"; ValueType: string; ValueData: "mscoree.dll"; Tasks: svgthumbnails
Root: HKCU; Subkey: "Software\Classes\CLSID\{{D7903169-9D1C-4CB8-9CA1-884652133E71}\InprocServer32"; ValueType: string; ValueName: "ThreadingModel"; ValueData: "Both"; Tasks: svgthumbnails
Root: HKCU; Subkey: "Software\Classes\CLSID\{{D7903169-9D1C-4CB8-9CA1-884652133E71}\InprocServer32"; ValueType: string; ValueName: "Class"; ValueData: "Piccasa.SvgThumbnails"; Tasks: svgthumbnails
Root: HKCU; Subkey: "Software\Classes\CLSID\{{D7903169-9D1C-4CB8-9CA1-884652133E71}\InprocServer32"; ValueType: string; ValueName: "Assembly"; ValueData: "Lorea.Thumbnails, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"; Tasks: svgthumbnails
Root: HKCU; Subkey: "Software\Classes\CLSID\{{D7903169-9D1C-4CB8-9CA1-884652133E71}\InprocServer32"; ValueType: string; ValueName: "RuntimeVersion"; ValueData: "v4.0.30319"; Tasks: svgthumbnails
Root: HKCU; Subkey: "Software\Classes\CLSID\{{D7903169-9D1C-4CB8-9CA1-884652133E71}\InprocServer32"; ValueType: string; ValueName: "CodeBase"; ValueData: "file:///{app}\Lorea.Thumbnails-{#AppVersion}.dll"; Tasks: svgthumbnails
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

[Code]
const
  ThumbnailClass = '{D7903169-9D1C-4CB8-9CA1-884652133E71}';
  HandlerSuffix = '\shellex\{E357FCCD-A995-4576-B01F-234630154E96}';

procedure SHChangeNotify(EventID: Integer; Flags: Cardinal; Item1, Item2: Integer);
  external 'SHChangeNotify@shell32.dll stdcall';

function HasOtherProvider(Key: String): Boolean;
var Value: String;
begin
  Result := RegQueryStringValue(HKCR, Key + HandlerSuffix, '', Value) and
    (Value <> '') and (CompareText(Value, ThumbnailClass) <> 0);
end;

procedure RemoveOwnThumbnail;
var Value, Key: String;
begin
  Key := 'Software\Classes\.svg' + HandlerSuffix;
  if RegQueryStringValue(HKCU, Key, '', Value) and (CompareText(Value, ThumbnailClass) = 0) then begin
    RegDeleteValue(HKCU, Key, '');
    RegDeleteKeyIfEmpty(HKCU, Key);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var ProgID: String; Conflict: Boolean;
begin
  if CurStep = ssPostInstall then begin
    if WizardIsTaskSelected('svgthumbnails') then begin
      Conflict := HasOtherProvider('.svg') or HasOtherProvider('SystemFileAssociations\.svg');
      if RegQueryStringValue(HKCR, '.svg', '', ProgID) then Conflict := Conflict or HasOtherProvider(ProgID);
      if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.svg\UserChoice', 'ProgId', ProgID) then
        Conflict := Conflict or HasOtherProvider(ProgID);
      if not Conflict then
        RegWriteStringValue(HKCU, 'Software\Classes\.svg' + HandlerSuffix, '', ThumbnailClass)
      else Log('SVG thumbnails: another provider exists; leaving it unchanged.');
    end else RemoveOwnThumbnail;
    SHChangeNotify($08000000, 0, 0, 0);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then begin
    RemoveOwnThumbnail;
    SHChangeNotify($08000000, 0, 0, 0);
  end;
end;
