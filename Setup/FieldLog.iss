; Determine product and file version from the application to be installed
#ifndef BuildConfig
	#define BuildConfig "Release"
#endif
#define RevFileName "..\FieldLogViewer\bin\" + BuildConfig + "\FieldLogViewer.exe"
#define RevId GetStringFileInfo(RevFileName, "ProductVersion")
#define ShortRevId GetFileVersion(RevFileName)

; Include 3rd-party software check and download support
#include "include\products.iss"
#include "include\products\stringversion.iss"
#include "include\products\winversion.iss"
#include "include\products\fileversion.iss"
#include "include\products\dotnetfxversion.iss"

; Include modules ONLY for required products to be installed
#include "include\products\msi31.iss"
#include "include\products\dotnetfx40client.iss"

; Include general helper functions
#include "include\util-code.iss"

; Include Visual Studio external tools management
#include "include\visualstudio-tool.iss"

[Setup]
; Names and versions for the Windows programs listing
AppName=FieldLog
AppVersion={#RevId}
AppCopyright=© Yves Goergen, GNU GPL v3
AppPublisher=Yves Goergen
AppPublisherURL=http://unclassified.software/source/fieldlog

; Setup file version
VersionInfoDescription=FieldLog Setup
VersionInfoVersion={#ShortRevId}
VersionInfoCompany=Yves Goergen

; General application information
AppId={{52CCCE83-0A6F-447D-AAE0-506431641858}
AppMutex=Global\Unclassified.FieldLogViewer,Unclassified.FieldLogViewer
MinVersion=0,5.01sp3
ArchitecturesInstallIn64BitMode=x64
; isxdl.dll may not be DEP compatible
DEPCompatible=no

; General setup information
DefaultDirName={pf}\Unclassified\FieldLog
AllowUNCPath=False
DefaultGroupName=FieldLog
DisableWelcomePage=no
DisableDirPage=auto
DisableProgramGroupPage=auto
ShowLanguageDialog=no
ChangesAssociations=yes

; Setup design
; Large image max. 164x314 pixels, small image max. 55x58 pixels
WizardImageStretch=no
WizardImageFile=FieldLog_144.bmp
WizardSmallImageFile=FieldLog_48.bmp

; Uninstaller configuration
UninstallDisplayName=FieldLog
UninstallDisplayIcon={app}\FieldLogViewer.exe

; Setup package creation
OutputDir=out
OutputBaseFilename=FieldLogSetup-{#RevId}
SolidCompression=True
InternalCompressLevel=max

; This file must be included after other setup settings
#include "include\previous-installation.iss"

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[LangOptions]
; More setup design
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

[CustomMessages]
Upgrade=&Upgrade
UpdatedHeadingLabel=%n%n%n%n%nFieldLog was upgraded.
Task_VSTool=Register as External Tool in Visual Studio (2010–2015)
NgenMessage=Optimising application performance (this may take a moment)
OpenSingleFileCommand=Open single file
Uninstall_DeleteConfig=Do you want to delete the configuration data incl. logs?

; Add translations after messages have been defined
#include "FieldLog.de.iss"

[Tasks]
Name: VSTool; Description: "{cm:Task_VSTool}"

[Files]
; FieldLogViewer application files
Source: "..\FieldLogViewer\bin\{#BuildConfig}\FieldLogViewer.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\FieldLogViewer\bin\{#BuildConfig}\FieldLogViewer.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\FieldLogViewer\bin\{#BuildConfig}\TaskDialog.dll"; DestDir: "{app}"; Flags: ignoreversion
;Source: "..\FieldLog Documentation.pdf"; DestDir: "{app}"
; This is the signed version of the DLL:
Source: "..\FieldLog\bin\{#BuildConfig}NET40\Unclassified.FieldLog.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\FieldLog\FileFormat.html"; DestDir: "{app}"
Source: "..\FieldLogViewer\bin\{#BuildConfig}\Sounds\*.mp3"; DestDir: "{app}\Sounds"
; Sample configuration
Source: "example-config.txt"; DestDir: "{app}"; DestName: "FieldLogViewer.exe.flconfig"; Permissions: users-modify

; PdbConvert tool
Source: "..\PdbConvert\bin\{#BuildConfig}\PdbConvert.exe"; DestDir: "{app}"; Flags: ignoreversion

; LogSubmit tool
Source: "..\LogSubmit\bin\x86\{#BuildConfig}\LogSubmit.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "submit.config"; DestDir: "{app}"

; FieldLog assembly
Source: "..\FieldLog\bin\{#BuildConfig}NET20\Unclassified.FieldLog.dll"; DestDir: "{app}\FieldLog assembly (.NET 2.0)"; Flags: ignoreversion
Source: "..\FieldLog\bin\{#BuildConfig}NET20\Unclassified.FieldLog.xml"; DestDir: "{app}\FieldLog assembly (.NET 2.0)"
Source: "..\FieldLog\bin\{#BuildConfig}NET40\Unclassified.FieldLog.dll"; DestDir: "{app}\FieldLog assembly (.NET 4.0)"; Flags: ignoreversion
Source: "..\FieldLog\bin\{#BuildConfig}NET40\Unclassified.FieldLog.xml"; DestDir: "{app}\FieldLog assembly (.NET 4.0)"
Source: "..\FieldLog\bin\{#BuildConfig}ASPNET40\Unclassified.FieldLog.dll"; DestDir: "{app}\FieldLog assembly (ASP.NET 4.0)"; Flags: ignoreversion
Source: "..\FieldLog\bin\{#BuildConfig}ASPNET40\Unclassified.FieldLog.xml"; DestDir: "{app}\FieldLog assembly (ASP.NET 4.0)"

; FieldLog source code
Source: "..\FieldLog\AppErrorDialog.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\CheckTimeThread.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\CustomTimers.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\Enums.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\Exceptions.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\FieldLogEventEnvironment.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\FieldLogFileEnumerator.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\FieldLogFileGroupReader.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\FieldLogFileReader.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\FieldLogFileWriter.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\FieldLogTraceListener.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\FieldLogWebRequestData.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\FL.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\LogItems.cs"; DestDir: "{app}\FieldLog source code"
Source: "..\FieldLog\OSInfo.cs"; DestDir: "{app}\FieldLog source code"

; Tx dictionary with FieldLog messages
Source: "..\FieldLog\FieldLog.txd"; DestDir: "{app}\FieldLog source code"

; License files
Source: "..\LICENSE-GPL"; DestDir: "{app}"
Source: "..\LICENSE-LGPL"; DestDir: "{app}"

[Dirs]
; Create user-writable log directory in the installation directory.
; FieldLog will first try to write log file there.
Name: "{app}\log"; Permissions: users-modify

[InstallDelete]
; Older version of submit.config (added 2015-07)
Type: files; Name: "{app}\submit.conf"

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
; Start menu
Name: "{group}\FieldLogViewer"; Filename: "{app}\FieldLogViewer.exe"; IconFilename: "{app}\FieldLogViewer.exe"
;Name: "{group}\FieldLog Documentation"; Filename: "{app}\FieldLog Documentation.pdf"
Name: "{group}\FieldLog website"; Filename: "http://unclassified.software/source/fieldlog"
Name: "{group}\FieldLog assembly (.NET 4.0)"; Filename: "{app}\FieldLog assembly (.NET 4.0)\"
Name: "{group}\FieldLog assembly (.NET 2.0)"; Filename: "{app}\FieldLog assembly (.NET 2.0)\"
Name: "{group}\FieldLog assembly (ASP.NET 4.0)"; Filename: "{app}\FieldLog assembly (ASP.NET 4.0)\"
Name: "{group}\FieldLog source code"; Filename: "{app}\FieldLog source code\"

[Run]
Filename: {win}\Microsoft.NET\Framework\v4.0.30319\ngen.exe; Parameters: "install ""{app}\FieldLogViewer.exe"""; StatusMsg: "{cm:NgenMessage}"; Flags: runhidden
Filename: {app}\FieldLogViewer.exe; WorkingDir: {app}; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: {win}\Microsoft.NET\Framework\v4.0.30319\ngen.exe; Parameters: "uninstall ""{app}\FieldLogViewer.exe"""; Flags: runhidden

[Code]
function InitializeSetup: Boolean;
begin
	Result := InitCheckDowngrade;

	if Result then
	begin
		// Initialise 3rd-party requirements management
		initwinversion();

		msi31('3.1');

		// If no .NET 4.0 is found, install the client profile (smallest)
		if (not netfxinstalled(NetFx40Client, '') and not netfxinstalled(NetFx40Full, '') and not netfxinstalled(NetFx4x, '')) then
			dotnetfx40client();
	end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
	// Make upgrade install quicker
	Result := ((PageID = wpSelectTasks) or ((PageID = wpReady) and (GetArrayLength(products) = 0))) and PrevInstallExists;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
	if CurPageID = wpWelcome then
	begin
		if PrevInstallExists then
		begin
			// Change "Next" button to "Upgrade" on the first page, because it won't ask any more
			WizardForm.NextButton.Caption := ExpandConstant('{cm:Upgrade}');
			WizardForm.FinishedHeadingLabel.Caption := ExpandConstant('{cm:UpdatedHeadingLabel}');
		end;
	end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
	if (CurStep = ssPostInstall) and IsTaskSelected('VSTool') then
	begin
		// Register FieldLogViewer as external tool in all Visual Studio versions after setup
		RegisterVSTool('10.0', 'FieldLogViewer', '{app}\FieldLogViewer.exe', '/w');
		RegisterVSTool('11.0', 'FieldLogViewer', '{app}\FieldLogViewer.exe', '/w');
		RegisterVSTool('12.0', 'FieldLogViewer', '{app}\FieldLogViewer.exe', '/w');
		RegisterVSTool('14.0', 'FieldLogViewer', '{app}\FieldLogViewer.exe', '/w');
	end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
	if CurUninstallStep = usUninstall then
	begin
		if IsCommandLineParamSet('verysilent') or
			IsCommandLineParamSet('suppressmsgboxes') or
			(MsgBox(ExpandConstant('{cm:Uninstall_DeleteConfig}'), mbConfirmation, MB_YESNO) = IDYES) then
		begin
			DeleteFile(ExpandConstant('{userappdata}\Unclassified\FieldLog\FieldLogViewer.conf'));
			DeleteFile(ExpandConstant('{userappdata}\Unclassified\FieldLog\FieldLogViewer.conf.bak'));
			RemoveDir(ExpandConstant('{userappdata}\Unclassified\FieldLog'));
			RemoveDir(ExpandConstant('{userappdata}\Unclassified'));
			
			DelTree(ExpandConstant('{app}\log'), true, true, true);
			RemoveDir(ExpandConstant('{app}'));
		end;
	end;
	if CurUninstallStep = usPostUninstall then
	begin
		// Unregister FieldLogViewer as external tool in all Visual Studio versions after uninstall
		UnregisterVSTool('10.0', 'FieldLogViewer');
		UnregisterVSTool('11.0', 'FieldLogViewer');
		UnregisterVSTool('12.0', 'FieldLogViewer');
		UnregisterVSTool('14.0', 'FieldLogViewer');
	end;
end;
