# PowerShell build framework
# Project-specific control file

Begin-BuildScript "FieldLog"

# Find revision format from the source code, require Git checkout
Set-VcsVersion "" "/require git"

# FieldLog.*NET* projects are overlapping, don't build them in parallel
Disable-ParallelBuild

# Debug builds
if (IsSelected "build-debug")
{
	Build-Solution "FieldLog.sln" "Debug" "Any CPU" 8

	if (IsSelected "sign-lib")
	{
		Sign-File "FieldLog\bin\DebugNET40\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword" 1
		Sign-File "FieldLog\bin\DebugNET20\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword" 1
		Sign-File "FieldLog\bin\DebugASPNET40\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword" 1
	}
	if (IsSelected "sign-app")
	{
		Sign-File "FieldLogViewer\bin\Debug\FieldLogViewer.exe" "$signKeyFile" "$signPassword" 1
		Sign-File "PdbConvert\bin\Debug\PdbConvert.exe" "$signKeyFile" "$signPassword" 1
	}
}

# Release builds
if ((IsSelected "build-release") -or (IsSelected "commit") -or (IsSelected "publish"))
{
	Build-Solution "FieldLog.sln" "Release" "Any CPU" 8
	
	# Archive debug symbols for later source lookup
	EnsureDirExists ".local"
	Exec-Console "PdbConvert\bin\Release\PdbConvert.exe" "$rootDir\FieldLogViewer\bin\Release\* /srcbase $rootDir /optimize /outfile $rootDir\.local\FieldLog-$revId.pdbx" 1

	if ((IsSelected "sign-lib") -or (IsSelected "publish"))
	{
		Sign-File "FieldLog\bin\ReleaseNET40\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword" 1
		Sign-File "FieldLog\bin\ReleaseNET20\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword" 1
		Sign-File "FieldLog\bin\ReleaseASPNET40\Unclassified.FieldLog.dll" "$signKeyFile" "$signPassword" 1
	}
	if ((IsSelected "sign-app") -or (IsSelected "publish"))
	{
		Sign-File "FieldLogViewer\bin\Release\FieldLogViewer.exe" "$signKeyFile" "$signPassword" 1
		Sign-File "PdbConvert\bin\Release\PdbConvert.exe" "$signKeyFile" "$signPassword" 1
	}

	Create-NuGet "FieldLog\Unclassified.FieldLog.nuspec" "FieldLog\bin" 2
}

# Release setups
if ((IsSelected "setup-release") -or (IsSelected "commit") -or (IsSelected "publish"))
{
	Create-Setup "Setup\FieldLog.iss" Release 1

	if ((IsSelected "sign-setup") -or (IsSelected "publish"))
	{
		Sign-File "Setup\bin\FieldLogSetup-$revId.exe" "$signKeyFile" "$signPassword" 1
	}
}

# Install release setup
if (IsSelected "install")
{
	Exec-File "Setup\bin\FieldLogSetup-$revId.exe" "/norestart /verysilent" 1
}

# Commit to repository
if (IsSelected "commit")
{
	# Clean up test build files
	Delete-File "Setup\bin\FieldLogSetup-$revId.exe" 0
	Delete-File ".local\FieldLog-$revId.pdbx" 0

	Git-Commit 5
}

# Prepare publishing a release
if (IsSelected "publish")
{
	Git-Log ".local\FieldLogChanges.txt" 1
}

# Copy to website (local)
if (IsSelected "transfer-web")
{
	Copy-File "Setup\bin\FieldLogSetup-$revId.exe" "$webDir\files\source\fieldlog\" 0
	Copy-File ".local\FieldLogChanges.txt" "$webDir\files\source\fieldlog\" 0
}

# Upload to NuGet
if (IsSelected "transfer-nuget")
{
	Push-NuGet "FieldLog\bin\Unclassified.FieldLog" $nuGetApiKey 45
}

End-BuildScript
