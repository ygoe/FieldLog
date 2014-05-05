#ifndef RevId
	#define RevId "0.1"
#endif
#ifndef ShortRevId
	#define ShortRevId Copy(RevId, 1, Pos(".", RevId) - 1)
#endif

#include "scripts\products.iss"
#include "scripts\products\stringversion.iss"
#include "scripts\products\winversion.iss"
#include "scripts\products\fileversion.iss"
#include "scripts\products\dotnetfxversion.iss"

#include "scripts\products\msi31.iss"

#include "scripts\products\dotnetfx40client.iss"
#include "scripts\products\dotnetfx40full.iss"

[Setup]
AppCopyright=© Yves Goergen, GNU GPL v3
AppPublisher=Yves Goergen
AppPublisherURL=http://dev.unclassified.de/source/fieldlog
AppName=FieldLog
AppVersion={#ShortRevId}
AppMutex=Global\Unclassified.FieldLogViewer
AppId={{52CCCE83-0A6F-447D-AAE0-506431641858}
MinVersion=0,5.01sp3

ShowLanguageDialog=no
ChangesAssociations=yes

DefaultDirName={pf}\Unclassified\FieldLog
AllowUNCPath=False
DefaultGroupName=FieldLog
DisableDirPage=auto
DisableProgramGroupPage=auto

; Large image max. 164x314 pixels, small image max. 55x58 pixels
WizardImageFile=FieldLog_144.bmp
WizardImageBackColor=$ffffff
WizardImageStretch=no
WizardSmallImageFile=FieldLog_48.bmp

UninstallDisplayName=FieldLog
UninstallDisplayIcon={app}\FieldLogViewer.exe

OutputDir=bin
OutputBaseFilename=FieldLogSetup-{#RevId}
SolidCompression=True
InternalCompressLevel=max
VersionInfoVersion=1.0
VersionInfoCompany=Yves Goergen
VersionInfoDescription=FieldLog Setup
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[LangOptions]
;de.LanguageName=Deutsch
;de.LanguageID=$0407
DialogFontName=Segoe UI
DialogFontSize=9
WelcomeFontName=Segoe UI
WelcomeFontSize=12

[Messages]
WelcomeLabel1=%n%n%nWelcome to the FieldLog setup wizard
WelcomeLabel2=FieldLog is a fast and comprehensive logging tool for .NET applications. It is designed for high-performance, storage-efficient, always-on logging and comes with a useful log viewer application.%n%nVersion: {#RevId}
ClickNext=Click Next to continue installing FieldLogViewer, documentation, the FieldLog library and source code, or Cancel to exit the setup.
FinishedHeadingLabel=%n%n%n%n%nFieldLog is now installed.
FinishedLabelNoIcons=
FinishedLabel=The application may be launched by selecting the installed start menu icon.
ClickFinish=Click Finish to exit the setup.

de.WelcomeLabel1=%n%n%nWillkommen zum FieldLog-Setup-Assistenten
de.WelcomeLabel2=FieldLog ist ein schnelles und umfassendes Logging-Werkzeug für .NET-Anwendungen. Es ist für hohe Performance, geringen Speicherplatzbedarf und ständig aktiviertes Logging konzipiert und bringt eine nützliche Log-Betrachter-Anwendung mit.%n%nVersion: {#RevId}
de.ClickNext=Klicken Sie auf Weiter, um den FieldLogViewer, die Dokumentation und die FieldLog-Bibliothek mit Quelltext zu installieren, oder auf Abbrechen zum Beenden des Setups.
de.FinishedHeadingLabel=%n%n%n%n%nFieldLog ist jetzt installiert.
de.FinishedLabelNoIcons=
de.FinishedLabel=Die Anwendung kann über die installierte Startmenü-Verknüpfung gestartet werden.
de.ClickFinish=Klicken Sie auf Fertigstellen, um das Setup zu beenden.

[CustomMessages]
Upgrade=&Upgrade
UpdatedHeadingLabel=%n%n%n%n%nFieldLog was upgraded.
Task_VSTool=Register as External Tool in Visual Studio (2010/2012/2013)
NgenMessage=Optimising application performance (this may take a moment)
DowngradeUninstall=You are trying to install an older version than is currently installed on the system. The newer version must first be uninstalled. Would you like to do that now?%n%nPlease note than uninstallation also deletes the configuration file. If you want to keep it, you need to make a copy of it. It is located in the directory %AppData%\Unclassified\FieldLog.
OpenSingleFileCommand=Open single file

de.Upgrade=&Aktualisieren
de.UpdatedHeadingLabel=%n%n%n%n%nFieldLog wurde aktualisiert.
de.Task_VSTool=In Visual Studio (2010/2012/2013) als Externes Tool eintragen
Task_DeleteConfig=Vorhandene Konfiguration löschen
de.NgenMessage=Anwendungs-Performance optimieren (kann einen Moment dauern)
de.DowngradeUninstall=Sie versuchen, eine ältere Version zu installieren, als bereits im System installiert ist. Die neuere Version muss zuerst deinstalliert werden. Möchten Sie das jetzt tun?%n%nBitte beachten Sie, dass bei der Deinstallation auch die Einstellungen gelöscht werden. Wenn Sie diese behalten möchten, müssen Sie sie sichern. Die Datei befindet sich im Verzeichnis %AppData%\Unclassified\FieldLog.
de.OpenSingleFileCommand=Einzelne Datei öffnen

[Tasks]
;Name: VSTool; Description: "{cm:Task_VSTool}"
; TODO: Also enable UnregisterVSTool script call below
Name: DeleteConfig; Description: "{cm:Task_DeleteConfig}"; Flags: unchecked
#define Task_DeleteConfig_Index 0

[Files]
; FieldLogViewer application files
Source: "..\FieldLogViewer\bin\Release\FieldLogViewer.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\FieldLogViewer\bin\Release\InTheHand.Net.Personal.dll"; DestDir: "{app}"; Flags: ignoreversion
;Source: "..\FieldLog Documentation.pdf"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\FieldLogViewer\bin\Release\Unclassified.FieldLog.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\FieldLogViewer\bin\Release\Sounds\*.mp3"; DestDir: "{app}\Sounds"; Flags: ignoreversion

; FieldLog assembly
Source: "..\FieldLog\bin\Release\Unclassified.FieldLog.dll"; DestDir: "{app}\FieldLog assembly (.NET 4.0)"; Flags: ignoreversion
Source: "..\FieldLog\bin\Release\Unclassified.FieldLog.xml"; DestDir: "{app}\FieldLog assembly (.NET 4.0)"; Flags: ignoreversion
Source: "..\FieldLog\bin\ReleaseNET20\Unclassified.FieldLog.dll"; DestDir: "{app}\FieldLog assembly (.NET 2.0)"; Flags: ignoreversion
Source: "..\FieldLog\bin\ReleaseNET20\Unclassified.FieldLog.xml"; DestDir: "{app}\FieldLog assembly (.NET 2.0)"; Flags: ignoreversion

; FieldLog source code
Source: "..\FieldLog\CheckTimeThread.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion
Source: "..\FieldLog\CustomTimers.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion
Source: "..\FieldLog\Enums.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion
Source: "..\FieldLog\Exceptions.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion
Source: "..\FieldLog\FieldLogEventEnvironment.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion
Source: "..\FieldLog\FieldLogFileEnumerator.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion
Source: "..\FieldLog\FieldLogFileGroupReader.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion
Source: "..\FieldLog\FieldLogFileReader.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion
Source: "..\FieldLog\FieldLogFileWriter.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion
Source: "..\FieldLog\FieldLogTraceListener.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion
Source: "..\FieldLog\FL.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion
Source: "..\FieldLog\LogItems.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion
Source: "..\FieldLog\OSInfo.cs"; DestDir: "{app}\FieldLog source code"; Flags: ignoreversion

[Dirs]
Name: "{app}\log"; Permissions: users-modify

[InstallDelete]
Type: files; Name: "{userappdata}\Unclassified\FieldLog\FieldLogViewer.conf"; Tasks: DeleteConfig

[Registry]
; Register .fl file name extension
Root: HKCR; Subkey: ".fl"; ValueType: string; ValueName: ""; ValueData: "FieldLogFile"; Flags: uninsdeletevalue 
Root: HKCR; Subkey: "FieldLogFile"; ValueType: string; ValueName: ""; ValueData: "FieldLog file"; Flags: uninsdeletekey
Root: HKCR; Subkey: "FieldLogFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\FieldLogViewer.exe,1"
Root: HKCR; Subkey: "FieldLogFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FieldLogViewer.exe"" ""%1"""
Root: HKCR; Subkey: "FieldLogFile\shell\opensingle"; ValueType: string; ValueName: ""; ValueData: "{cm:OpenSingleFileCommand}"
Root: HKCR; Subkey: "FieldLogFile\shell\opensingle\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FieldLogViewer.exe"" /s ""%1"""

; Add to .fl "Open with" menu
Root: HKCR; Subkey: ".fl\OpenWithList\FieldLogViewer.exe"; ValueType: string; ValueName: ""; ValueData: ""; Flags: uninsdeletekey
Root: HKCR; Subkey: "Applications\FieldLogViewer.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "FieldLogViewer"; Flags: uninsdeletekey
Root: HKCR; Subkey: "Applications\FieldLogViewer.exe\shell\open"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "FieldLogViewer"
Root: HKCR; Subkey: "Applications\FieldLogViewer.exe\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FieldLogViewer.exe"" ""%1"""

[Icons]
Name: "{group}\FieldLogViewer"; Filename: "{app}\FieldLogViewer.exe"; IconFilename: "{app}\FieldLogViewer.exe"
;Name: "{group}\FieldLog Documentation"; Filename: "{app}\FieldLog Documentation.pdf"
Name: "{group}\FieldLog website"; Filename: "http://dev.unclassified.de/source/fieldlog"
Name: "{group}\FieldLog assembly (.NET 4.0)"; Filename: "{app}\FieldLog assembly (.NET 4.0)\"
Name: "{group}\FieldLog assembly (.NET 2.0)"; Filename: "{app}\FieldLog assembly (.NET 2.0)\"
Name: "{group}\FieldLog source code"; Filename: "{app}\FieldLog source code\"

[Run]
Filename: {win}\Microsoft.NET\Framework\v4.0.30319\ngen.exe; Parameters: "install ""{app}\FieldLogViewer.exe"""; StatusMsg: "{cm:NgenMessage}"; Flags: runhidden
Filename: {app}\FieldLogViewer.exe; WorkingDir: {app}; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: {win}\Microsoft.NET\Framework\v4.0.30319\ngen.exe; Parameters: uninstall {app}\FieldLogViewer.exe; Flags: runhidden

[UninstallDelete]
; Delete user configuration files
Type: dirifempty; Name: "{userappdata}\Unclassified\FieldLog"
Type: dirifempty; Name: "{userappdata}\Unclassified"

; Delete log files
Type: files; Name: "{app}\log\FieldLogViewer-*.fl"

[Code]
var
  IsDowngradeSetup: Boolean;

function IsUpgrade: Boolean;
var
  Value: string;
  UninstallKey: string;
begin
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' +
    ExpandConstant('{#SetupSetting("AppId")}') + '_is1';
  Result := (RegQueryStringValue(HKLM, UninstallKey, 'UninstallString', Value) or
    RegQueryStringValue(HKCU, UninstallKey, 'UninstallString', Value)) and (Value <> '');
end;

function GetQuietUninstallString: String;
var
  Value: string;
  UninstallKey: string;
begin
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' +
    ExpandConstant('{#SetupSetting("AppId")}') + '_is1';
  if not RegQueryStringValue(HKLM, UninstallKey, 'QuietUninstallString', Value) then
    RegQueryStringValue(HKCU, UninstallKey, 'QuietUninstallString', Value);
  Result := Value;
end;

function GetInstalledVersion: String;
var
  Value: string;
  UninstallKey: string;
begin
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' +
    ExpandConstant('{#SetupSetting("AppId")}') + '_is1';
  if not RegQueryStringValue(HKLM, UninstallKey, 'DisplayVersion', Value) then
    RegQueryStringValue(HKCU, UninstallKey, 'DisplayVersion', Value);
  Result := Value;
end;

function InitializeSetup(): boolean;
var
  ResultCode: Integer;
begin
  Result := true;
  
  // Check for downgrade
  if IsUpgrade then
  begin
    if '{#ShortRevId}' < GetInstalledVersion then
    begin
      if MsgBox(ExpandConstant('{cm:DowngradeUninstall}'), mbConfirmation, MB_YESNO) = IDYES then
      begin
        Exec('>', GetQuietUninstallString, '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
      end;

      // Check again
      if '{#ShortRevId}' < GetInstalledVersion then
      begin
        Result := false;
      end;

      IsDowngradeSetup := true;
    end;
  end;
  
  if Result then
  begin
    //init windows version
    initwinversion();

    msi31('3.1');

    // if no .netfx 4.0 is found, install the client (smallest)
    if (not netfxinstalled(NetFx40Client, '') and not netfxinstalled(NetFx40Full, '')) then
      dotnetfx40client();
  end;
end;

procedure RegRenameStringValue(const RootKey: Integer; const SubKeyName, ValueName, NewValueName: String);
var
	value: String;
begin
	if RegQueryStringValue(RootKey, SubKeyName, ValueName, value) then
	begin
		RegWriteStringValue(RootKey, SubKeyName, NewValueName, value);
		RegDeleteValue(RootKey, SubKeyName, ValueName);
	end;
end;

procedure RegRenameDWordValue(const RootKey: Integer; const SubKeyName, ValueName, NewValueName: String);
var
	value: Cardinal;
begin
	if RegQueryDWordValue(RootKey, SubKeyName, ValueName, value) then
	begin
		RegWriteDWordValue(RootKey, SubKeyName, NewValueName, value);
		RegDeleteValue(RootKey, SubKeyName, ValueName);
	end;
end;

procedure UnregisterVSTool(vsVersion: String);
var
	regKey: String;
	ToolNumKeys: Cardinal;
	i, j: Cardinal;
	num: Cardinal;
	str: String;
begin
	regKey := 'Software\Microsoft\VisualStudio\' + vsVersion + '\External Tools';

	if RegQueryDWordValue(HKEY_CURRENT_USER, regKey, 'ToolNumKeys', ToolNumKeys) then
	begin
		// Visual Studio is installed
		for i := 0 to ToolNumKeys - 1 do
		begin
			if RegQueryStringValue(HKEY_CURRENT_USER, regKey, 'ToolTitle' + IntToStr(i), str) then
			begin
				if str = 'FieldLogViewer' then
				begin
					// Found TxEditor at index i. Remove it and move all others one position up.
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolArg' + IntToStr(i));
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolCmd' + IntToStr(i));
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolDir' + IntToStr(i));
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolOpt' + IntToStr(i));
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolSourceKey' + IntToStr(i));
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolTitle' + IntToStr(i));
					
					for j := i + 1 to ToolNumKeys - 1 do
					begin
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolArg' + IntToStr(j), 'ToolArg' + IntToStr(j - 1));
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolCmd' + IntToStr(j), 'ToolCmd' + IntToStr(j - 1));
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolDir' + IntToStr(j), 'ToolDir' + IntToStr(j - 1));
						RegRenameDWordValue(HKEY_CURRENT_USER, regKey, 'ToolOpt' + IntToStr(j), 'ToolOpt' + IntToStr(j - 1));
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolSourceKey' + IntToStr(j), 'ToolSourceKey' + IntToStr(j - 1));
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolTitle' + IntToStr(j), 'ToolTitle' + IntToStr(j - 1));
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolTitlePkg' + IntToStr(j), 'ToolTitlePkg' + IntToStr(j - 1));
						RegRenameDWordValue(HKEY_CURRENT_USER, regKey, 'ToolTitleResID' + IntToStr(j), 'ToolTitleResID' + IntToStr(j - 1));
					end;
					RegWriteDWordValue(HKEY_CURRENT_USER, regKey, 'ToolNumKeys', ToolNumKeys - 1);
				end;
			end;
		end;
	end;
end;

procedure RegisterVSTool(vsVersion: String);
var
	regKey: String;
	ToolNumKeys: Cardinal;
begin
	regKey := 'Software\Microsoft\VisualStudio\' + vsVersion + '\External Tools';

	// Clean up existing entry before adding it
	//UnregisterVSTool(vsVersion);
	
	if RegQueryDWordValue(HKEY_CURRENT_USER, regKey, 'ToolNumKeys', ToolNumKeys) then
	begin
		// Visual Studio is installed
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolArg' + IntToStr(ToolNumKeys), '-s "$(SolutionDir)"');
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolCmd' + IntToStr(ToolNumKeys), ExpandConstant('{app}') + '\FieldLogViewer.exe');
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolDir' + IntToStr(ToolNumKeys), '');
		RegWriteDWordValue(HKEY_CURRENT_USER, regKey, 'ToolOpt' + IntToStr(ToolNumKeys), 17);
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolSourceKey' + IntToStr(ToolNumKeys), '');
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolTitle' + IntToStr(ToolNumKeys), 'FieldLogViewer');
		RegWriteDWordValue(HKEY_CURRENT_USER, regKey, 'ToolNumKeys', ToolNumKeys + 1);
	end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
	if (CurStep = ssPostInstall) and IsTaskSelected('VSTool') then
	begin
		RegisterVSTool('10.0');
		RegisterVSTool('11.0');
		RegisterVSTool('12.0');
	end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
	if CurUninstallStep = usPostUninstall then
	begin
		//UnregisterVSTool('10.0');
		//UnregisterVSTool('11.0');
		//UnregisterVSTool('12.0');
	end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := ((PageID = wpSelectTasks) or (PageID = wpReady)) and IsUpgrade;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpWelcome then
  begin
    if IsUpgrade then
    begin
      WizardForm.NextButton.Caption := ExpandConstant('{cm:Upgrade}');
      WizardForm.FinishedHeadingLabel.Caption := ExpandConstant('{cm:UpdatedHeadingLabel}');
    end;
  end;

  if CurPageID = wpSelectTasks then
  begin
    if IsDowngradeSetup then
    begin
      // Pre-select task to delete existing configuration
      // (Use the zero-based index of all rows in the tasks list GUI)
      // Source: http://stackoverflow.com/a/10490352/143684
      WizardForm.TasksList.Checked[{#Task_DeleteConfig_Index}] := true;
    end;
  end;
end;

