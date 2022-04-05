#define MyCompany "PSPACE"
#define MyAppName "IfsSync2"
#define MyInitName "IfsSync2Init.exe"
#define MyAppExeName "IfsSync2UI.exe"
#define MyAppVersion "2.0.0.7"
#define MyAppPublisher "PSPACE Technology"
#define MyAppURL "http://www.pspace.com" 
#define MyDateTimeString GetDateTimeString('yyyymmddhhnnss', '-', ':');

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyCompany}
DefaultGroupName={#MyCompany}\{#MyAppName}
AllowNoIcons=yes
OutputDir={#SourcePath}\Setup
OutputBaseFilename={#MyAppName}_{#MyAppVersion}_{#MyDateTimeString}_Setup_x64   
SetupIconFile={#SourcePath}\Ifssync2\file.ico
Compression=lzma
SolidCompression=yes
DisableDirPage=true
DisableProgramGroupPage=yes
DisableWelcomePage=yes
AlwaysRestart=yes

; "ArchitecturesAllowed=x64" specifies that Setup cannot run on
; anything but x64.                                                  
ArchitecturesAllowed=x64
; "ArchitecturesInstallIn64BitMode=x64" requests that the install be
; done in "64-bit mode" on x64, meaning it should use the native
; 64-bit Program Files directory and the 64-bit view of the registry.
ArchitecturesInstallIn64BitMode=x64


[Files]
Source: "{#SourcePath}\Ifssync2\*"; DestDir: "{app}\{#MyAppName}"; Flags: recursesubdirs createallsubdirs; Excludes: "*.xml,*.manifest,*.exp,*.a,*.lib,*.pdb,*.suo,*.config,dbghelp.dll,*.txt,*.yes,*.ilk"
Source: "{#SourcePath}\Ifssync2\*Config.xml"; DestDir: "{app}\{#MyAppName}"; Flags: recursesubdirs createallsubdirs; 

[Run]    
Filename: "{app}\{#MyAppName}\IfsSync2Init.exe"; Parameters: "-s ""{app}\{#MyAppName}"""; StatusMsg: "Installing..."; Flags: skipifsilent;
Filename: "{app}\{#MyAppName}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[Icons]
Name: "{group}\{cm:UninstallProgram, {#MyAppName}}"; Filename: "{uninstallexe}";               
Name: {group}\{#MyAppName}; Filename: "{app}\{#MyAppName}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{userdesktop}\{#MyAppExeName}"; Filename: "{app}\{#MyAppName}\{#MyAppExeName}"; Tasks: desktopicon 

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: Unchecked;

[UninstallRun] 
Filename: "{app}\{#MyAppName}\IfsSync2Init.exe"; Parameters: "-d"; StatusMsg: "Uninstalling..."; Flags: skipifdoesntexist;

[InstallDelete]
Type: filesandordirs; Name: "{group}";

[UninstallDelete]
Type: filesandordirs; Name: "{app}\{#MyAppName}";

[code]
procedure CurPageChanged(CurPageID: Integer);
var  
  FilePath: String;
  ResultCode: Integer;
  Results: bool;                                                                                                                           
begin
  if CurPageID = wpReady then
  begin
    FilePath:= 'C:\Program Files\PSPACE\IfsSync2\IfsSync2Init.exe'
    Results:= FileExists(FilePath);
    if Results then
    begin                
        Exec(ExpandConstant(FilePath), '-d', '', SW_SHOW, ewWaitUntilTerminated, ResultCode)
    end;
  end;
end;