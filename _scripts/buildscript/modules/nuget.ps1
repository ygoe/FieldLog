# PowerShell build framework
# Copyright (c) 2015, Yves Goergen, http://unclassified.software/source/psbuild
#
# Copying and distribution of this file, with or without modification, are permitted provided the
# copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

# The nuget module provides NuGet packaging functions.

# Creates a NuGet package.
#
# $specFile = The file name of the .nuspec file.
# $outDir = The output directory of the created package.
# $version = The version of the package. If empty, $revId is used.
#
# Requires nuget.exe in the search path.
#
function Create-NuGet($specFile, $outDir, $version, $time)
{
	$action = @{ action = "Do-Create-NuGet"; specFile = $specFile; outDir = $outDir; version = $version; time = $time }
	$global:actions += $action
}

# TODO: Create push method for publishing a package

# ==============================  FUNCTION IMPLEMENTATIONS  ==============================

function Do-Create-NuGet($action)
{
	$specFile = $action.specFile
	$outDir = $action.outDir
	$version = $action.version

	if (!$version)
	{
		$version = $revId
	}
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Creating NuGet package $specFile..."

	& nuget pack (MakeRootedPath $specFile) -OutputDirectory (MakeRootedPath $outDir) -Version $version -NonInteractive
	if (-not $?)
	{
		WaitError "Creating NuGet package failed"
		exit 1
	}
}
