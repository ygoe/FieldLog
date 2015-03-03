# PowerShell build framework
# Project-specific control file

Begin-BuildScript "FieldLog"

# Find revision format from the source code, require Git checkout
Set-VcsVersion "" "/require git"

# FieldLog.*NET* projects are overlapping, don't build them in parallel
Disable-ParallelBuild

Restore-NuGetTool
Restore-NuGetPackages "FieldLog.sln"

# Debug builds
if (IsSelected build-debug)
{
	Build-Solution "FieldLog.sln" "Debug" "Any CPU" 8

	if (IsSelected sign-lib)
	{
		Sign-File "FieldLog\bin\DebugNET40\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword"
		Sign-File "FieldLog\bin\DebugNET20\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword"
		Sign-File "FieldLog\bin\DebugASPNET40\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword"
	}
	if (IsSelected sign-app)
	{
		Sign-File "FieldLogViewer\bin\Debug\FieldLogViewer.exe" "$signKeyFile" "$signPassword"
		Sign-File "PdbConvert\bin\Debug\PdbConvert.exe" "$signKeyFile" "$signPassword"
	}
}

# Release builds
if (IsAnySelected build-release commit publish)
{
	Build-Solution "FieldLog.sln" "Release" "Any CPU" 8
	
	# Archive debug symbols for later source lookup
	EnsureDirExists ".local"
	Exec-Console "PdbConvert\bin\Release\PdbConvert.exe" "$rootDir\FieldLogViewer\bin\Release\* /srcbase $rootDir /optimize /outfile $rootDir\.local\FieldLog-$revId.pdbx"

	if (IsAnySelected sign-lib publish)
	{
		Sign-File "FieldLog\bin\ReleaseNET40\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword"
		Sign-File "FieldLog\bin\ReleaseNET20\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword"
		Sign-File "FieldLog\bin\ReleaseASPNET40\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword"
	}
	if (IsAnySelected sign-app publish)
	{
		Sign-File "FieldLogViewer\bin\Release\FieldLogViewer.exe" "$signKeyFile" "$signPassword"
		Sign-File "PdbConvert\bin\Release\PdbConvert.exe" "$signKeyFile" "$signPassword"
	}

	Create-NuGetPackage "FieldLog\Unclassified.FieldLog.nuspec" "FieldLog\bin"
}

# Release setups
if (IsAnySelected setup-release commit publish)
{
	Create-Setup "Setup\FieldLog.iss" "Release"

	if (IsAnySelected sign-setup publish)
	{
		Sign-File "Setup\bin\FieldLogSetup-$revId.exe" "$signKeyFile" "$signPassword"
	}
}

# Install release setup
if (IsSelected install)
{
	Exec-File "Setup\bin\FieldLogSetup-$revId.exe" "/norestart /verysilent"
}

# Commit to repository
if (IsSelected commit)
{
	# Clean up test build files
	Delete-File "Setup\bin\FieldLogSetup-$revId.exe"
	Delete-File ".local\FieldLog-$revId.pdbx"

	Git-Commit
}

# Prepare publishing a release
if (IsSelected publish)
{
	Git-Log ".local\FieldLogChanges.txt"
}

# Copy to website (local)
if (IsSelected transfer-web)
{
	Copy-File "Setup\bin\FieldLogSetup-$revId.exe" "$webDir\files\source\fieldlog\"
	Copy-File ".local\FieldLogChanges.txt" "$webDir\files\source\fieldlog\"
}

# Upload to NuGet
if (IsSelected transfer-nuget)
{
	Push-NuGetPackage "FieldLog\bin\Unclassified.FieldLog" $nuGetApiKey 45
}

End-BuildScript
