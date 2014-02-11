param($config, $batchMode = "")

. (($MyInvocation.MyCommand.Definition | split-path -parent) + "\build_helpers.ps1")

Begin-BuildScript "FieldLog" "$config" ($batchMode -eq "batch")

# -----------------------------  SCRIPT CONFIGURATION  ----------------------------

# Set the path to the source files.
#
$sourcePath = $MyInvocation.MyCommand.Definition | split-path -parent | split-path -parent | split-path -parent

# Set the application version number. Disable for Git repository revision.
#
#$revId = "1.0"
#$revId = Get-AssemblyInfoVersion "ProjectDirectory\Properties\AssemblyInfo.cs" "AssemblyInformationalVersion"
$gitRevisionFormat = "{bmin:2014:4}.{commit:6}{!:+}"
$revId = Get-GitRevision

# Disable FASTBUILD mode to always include a full version number in the assembly version info.
#
$env:FASTBUILD = ""

# Disable parallel builds for overlapping projects in a solution
#
$noParallelBuild = $true

# ---------------------------------------------------------------------------------

Write-Host "Application version: $revId"

# ------------------------------  ACTION DEFINITION  ------------------------------

# ---------- Debug builds ----------

if (IsSelected("build-debug"))
{
	Build-Solution "FieldLog.sln" "Debug" "Any CPU" 6

	if (IsSelected("sign-app"))
	{
		Sign-File "FieldLog\bin\Debug\FieldLogViewer.exe" "signkey.pfx" "@signkey.password" 1
	}
}

# ---------- Release builds ----------

if (IsSelected("build-release"))
{
	Build-Solution "FieldLog.sln" "Release" "Any CPU" 6

	if (IsSelected("sign-app"))
	{
		Sign-File "FieldLogViewer\bin\Release\FieldLogViewer.exe" "signkey.pfx" "@signkey.password" 1
	}
}

# ---------- Release setups ----------

if (IsSelected("setup-release"))
{
	Create-Setup "Setup\FieldLog.iss" Release 1

	if (IsSelected("sign-setup"))
	{
		Sign-File "Setup\FieldLogSetup-$revId.exe" "signkey.pfx" "@signkey.password" 1
	}
}

# ---------- Install release setup ----------

if (IsSelected("install"))
{
	Exec-File "Setup\FieldLogSetup-$revId.exe" "/silent" 1
}

# ---------------------------------------------------------------------------------

End-BuildScript
