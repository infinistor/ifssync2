#define MyCompany "PSPACE"
#define MyAppName "IfsSync2"
#define MyInitName "IfsSync2Init.exe"
#define MyAppExeName "IfsSync2UI.exe"
#define MyAppVersion "2.0.2"
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
UsePreviousAppDir=yes
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
; 신규 설치
Filename: "{app}\{#MyAppName}\{#MyInitName}"; Parameters: "--install -p""{app}\{#MyAppName}"""; StatusMsg: "서비스 설치 중... (Windows 서비스 등록 및 시작)"; Flags: waituntilterminated; Check: not IsUpdateMode

; 업데이트 후 작업
Filename: "{app}\{#MyAppName}\{#MyInitName}"; Parameters: "--after-update"; StatusMsg: "서비스 복구 중... (서비스 및 프로세스 재시작)"; Flags: waituntilterminated; Check: IsUpdateMode

; 프로그램 시작
Filename: "{app}\{#MyAppName}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; StatusMsg: "프로그램 시작 중..."; Flags: nowait postinstall skipifsilent runascurrentuser;

[Icons]
Name: "{group}\{cm:UninstallProgram, {#MyAppName}}"; Filename: "{uninstallexe}";
Name: {group}\{#MyAppName}; Filename: "{app}\{#MyAppName}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{userdesktop}\{#MyAppExeName}"; Filename: "{app}\{#MyAppName}\{#MyAppExeName}"; Tasks: desktopicon 

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: Unchecked;

[UninstallRun]
Filename: "{app}\{#MyAppName}\{#MyInitName}"; Parameters: "--uninstall"; StatusMsg: "Uninstalling..."; Flags: skipifdoesntexist;

[InstallDelete]
Type: filesandordirs; Name: "{group}";

[UninstallDelete]
Type: filesandordirs; Name: "{app}\{#MyAppName}";

[Code]
var
  gIsUpdateMode: Boolean;

function IsDotNetDetected(): Boolean;
var
  ResultCode: Integer;
  TempFile: String;
  Output: TArrayOfString;
  ExecStr: String;
  I: Integer;
  OutputStr: String;
begin
  Result := False;
  TempFile := ExpandConstant('{tmp}\dotnet_check.txt');
  ExecStr := Format('/C dotnet --list-runtimes > "%s"', [TempFile]);
  
  try
    if not Exec('cmd.exe', ExecStr, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      exit;
      
    if not LoadStringsFromFile(TempFile, Output) then
      exit;
      
    // 모든 라인을 하나의 문자열로 합치기
    OutputStr := '';
    for I := 0 to GetArrayLength(Output)-1 do
      OutputStr := OutputStr + Output[I] + #13#10;
      
    Result := (Pos('Microsoft.AspNetCore.App 8.', OutputStr) > 0) and 
              (Pos('Microsoft.WindowsDesktop.App 8.', OutputStr) > 0);
  finally
    DeleteFile(TempFile);
  end;
end;

function InitializeSetup(): Boolean;
begin
  if not IsDotNetDetected() then
  begin
    MsgBox('.NET 8.0 Runtime이 설치되어 있지 않습니다.' #13#13 
           '설치를 계속하기 전에 다음 항목들을 설치해주세요:' #13#13
           '1. ASP.NET Core 8.0 Runtime (x64)' #13
           '2. .NET 8.0 Desktop Runtime (x64)' #13#13
           'https://dotnet.microsoft.com/download/dotnet/8.0 에서 다운로드 할 수 있습니다.', 
           mbCriticalError, MB_OK);
    Result := False;
    exit;
  end;
  Result := True;
end;

function IsUpdateMode: Boolean;
begin
  Result := gIsUpdateMode;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  // 파일 복사 전
  if CurStep = ssInstall then
  begin
    // 파일 복사 전에 업데이트 모드인지 확인하여 전역 변수에 저장
    gIsUpdateMode := FileExists(ExpandConstant('{app}\{#MyAppName}\{#MyInitName}'));
    
    if gIsUpdateMode then
    begin
      Exec(ExpandConstant('{app}\{#MyAppName}\{#MyInitName}'),
           '--before-update',
           '',
           SW_SHOW,
           ewWaitUntilTerminated,
           ResultCode);
    end;
  end;
end;