// FieldLog – .NET logging solution
// © Yves Goergen, Made in Germany
// Website: http://unclassified.software/source/fieldlog
//
// This library is free software: you can redistribute it and/or modify it under the terms of
// the GNU Lesser General Public License as published by the Free Software Foundation, version 3.
//
// This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along with this
// library. If not, see <http://www.gnu.org/licenses/>.

using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyProduct("FieldLog")]
[assembly: AssemblyTitle("FieldLog")]
[assembly: AssemblyDescription("FieldLog library")]
[assembly: AssemblyCopyright("© Yves Goergen, GNU LGPL v3")]
[assembly: AssemblyCompany("unclassified software development")]

// Assembly version, also used for Win32 file version resource.
// Must be a plain numeric version definition:
// 1. Major version number, should be increased with major new versions or rewrites of the application
// 2. Minor version number, should ne increased with minor feature changes or new features
// 3. Bugfix number, should be set or increased for bugfix releases of a previous version
// 4. Unused
[assembly: AssemblyVersion("1.0.0")]
// Informational version string, used for the About dialog, error reports and the setup script.
// Can be any freely formatted string containing punctuation, letters and revision codes.
// Should be set to the same value as AssemblyVersion if only the basic numbering scheme is applied.
[assembly: AssemblyInformationalVersion("{bmin:2014:4}.{commit:6}{!:+}")]

#if NET20
#if DEBUG
[assembly: AssemblyConfiguration("Debug, NET20")]
#else
[assembly: AssemblyConfiguration("Release, NET20")]
#endif
#elif ASPNET
#if DEBUG
[assembly: AssemblyConfiguration("Debug, ASPNET40")]
#else
[assembly: AssemblyConfiguration("Release, ASPNET40")]
#endif
#else
#if DEBUG
[assembly: AssemblyConfiguration("Debug, NET40")]
#else
[assembly: AssemblyConfiguration("Release, NET40")]
#endif
#endif

[assembly: ComVisible(false)]
