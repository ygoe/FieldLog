# PowerShell build framework
# Project-specific control file

Begin-BuildScript "FieldLog"

# Find revision format from the source code, require Git checkout
Set-VcsVersion "" "/require git"

# FieldLog.*NET* projects are overlapping, don't build them in parallel
Disable-ParallelBuild

Restore-NuGetTool
Restore-NuGetPackages "FieldLog.sln"

# Release builds
if (IsAnySelected build commit publish)
{
	Build-Solution "FieldLog.sln" "Release" "Any CPU" 8
	Build-Solution "FieldLog.sln" "Release" "x86" 1
	
	# Convert debug symbols to XML
	Exec-Console "PdbConvert\bin\Release\PdbConvert.exe" "$rootDir\FieldLogViewer\bin\Release\* /srcbase $rootDir /optimize /outfile $rootDir\FieldLogViewer\bin\Release\FieldLog.pdbx"

	Create-NuGetPackage "FieldLog\Unclassified.FieldLog.nuspec" "FieldLog\bin"
	Create-Setup "Setup\FieldLog.iss" Release

	if (IsSelected sign)
	{
		Sign-File "FieldLog\bin\ReleaseNET40\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword"
		Sign-File "FieldLog\bin\ReleaseNET20\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword"
		Sign-File "FieldLog\bin\ReleaseASPNET40\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword"
		Sign-File "FieldLogViewer\bin\Release\FieldLogViewer.exe" "$signKeyFile" "$signPassword"
		Sign-File "PdbConvert\bin\Release\PdbConvert.exe" "$signKeyFile" "$signPassword"
		Sign-File "LogSubmit\bin\Release\LogSubmit.exe" "$signKeyFile" "$signPassword"
		Sign-File "LogSubmit\bin\x86\Release\LogSubmit.exe" "$signKeyFile" "$signPassword"
		Sign-File "Setup\bin\FieldLogSetup-$revId.exe" "$signKeyFile" "$signPassword"
	}
}

# Install setup
if (IsSelected install)
{
	Exec-File "Setup\bin\FieldLogSetup-$revId.exe" "/norestart /verysilent"
}

# Commit to repository
if (IsSelected commit)
{
	# Clean up test build files
	Delete-File "Setup\bin\FieldLogSetup-$revId.exe"

	Git-Commit
}

# Prepare publishing a release
if (IsSelected publish)
{
	# Copy all necessary files into their own release directory
	EnsureDirExists ".local\Release"
	Copy-File "FieldLogViewer\bin\Release\FieldLog.pdbx" ".local\Release\FieldLog-$revId.pdbx"

	Git-Log ".local\Release\FieldLogChanges.txt"
}

# Copy to website (local)
if (IsSelected transfer-web)
{
	Copy-File "Setup\bin\FieldLogSetup-$revId.exe" "$webDir\files\source\fieldlog\"
	Copy-File ".local\Release\FieldLogChanges.txt" "$webDir\files\source\fieldlog\"
	
	$today = (Get-Date -Format "yyyy-MM-dd")
	Exec-File "_scripts\bin\AutoReplace.exe" "$webDataFile fieldlog version=$revId date=$today"
}

# Upload to NuGet
if (IsSelected transfer-nuget)
{
	Push-NuGetPackage "FieldLog\bin\Unclassified.FieldLog" $nuGetApiKey 15
}

End-BuildScript
