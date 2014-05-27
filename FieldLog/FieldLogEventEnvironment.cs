// FieldLog – .NET logging solution
// © Yves Goergen, Made in Germany
// Website: http://dev.unclassified.de/source/fieldlog
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Contains information about the current system and process environment for log items.
	/// </summary>
	public class FieldLogEventEnvironment
	{
		#region Static members

		/// <summary>Contains the empty environment object.</summary>
		public static readonly FieldLogEventEnvironment Empty;

		static FieldLogEventEnvironment()
		{
			Empty = new FieldLogEventEnvironment();
			Empty.Size = 148 + 14 * IntPtr.Size;   // 148 bytes + 14 null strings
		}

		/// <summary>
		/// Indicates whether the specified environment variable is null or an Empty environment.
		/// </summary>
		/// <param name="value">The variable to test.</param>
		/// <returns>true if the value parameter is null or an empty environment; otherwise, false.</returns>
		public static bool IsNullOrEmpty(FieldLogEventEnvironment value)
		{
			return value == null || value == FieldLogEventEnvironment.Empty;
		}

		#endregion Static members

		#region Data properties

		/// <summary>
		/// Gets the approximate data size of this data structure. Used for buffer size estimation.
		/// </summary>
		public int Size { get; protected set; }

		/// <summary>
		/// Gets the operating system type (client, server, core server). (From OSInfo)
		/// </summary>
		public OSType OSType { get; private set; }
		/// <summary>
		/// Gets the Windows version. (From OSInfo)
		/// </summary>
		public OSVersion OSVersion { get; private set; }
		/// <summary>
		/// Gets the Windows edition. (From OSInfo)
		/// </summary>
		public OSEdition OSEdition { get; private set; }
		/// <summary>
		/// Gets the installed service pack name. (From OSInfo)
		/// </summary>
		public string OSServicePack { get; private set; }
		/// <summary>
		/// Gets a value indicating whether a 64 bit system is running. (From OSInfo)
		/// </summary>
		public bool OSIs64Bit { get; private set; }
		/// <summary>
		/// Gets the Windows version build number. (From OSInfo)
		/// </summary>
		public int OSBuild { get; private set; }
		/// <summary>
		/// Gets the service pack build number. (From OSInfo)
		/// </summary>
		public int OSServicePackBuild { get; private set; }
		/// <summary>
		/// Gets the complete operating system product name from the registry, including Windows
		/// version and edition name. This can be used if correctness is required and the value
		/// does not need to be evaluated. (From OSInfo)
		/// </summary>
		public string OSProductName { get; private set; }
		/// <summary>
		/// Gets a value indicating whether the system is set up as application terminal server.
		/// (From OSInfo)
		/// </summary>
		public bool OSIsAppServer { get; private set; }
		/// <summary>
		/// Gets the ISO 639-1/ISO 3166 language/country code of the system language. (From OSInfo)
		/// </summary>
		public string OSLanguage { get; private set; }
		/// <summary>
		/// Gets the time when the system was last booted, in UTC. (From OSInfo)
		/// </summary>
		public DateTime OSLastBootTime { get; private set; }
		/// <summary>
		/// Gets a value indicating whether the system is started in fail-safe mode. (From OSInfo)
		/// </summary>
		public bool OSIsFailSafeBoot { get; private set; }
		/// <summary>
		/// Gets the application compatibility layers that are in effect for the current process.
		/// (From OSInfo)
		/// </summary>
		public string AppCompatLayer { get; private set; }
		/// <summary>
		/// Gets the CLR type running the current process. This is either "Microsoft .NET" or
		/// "Mono". (From OSInfo)
		/// </summary>
		public string ClrType { get; private set; }
		/// <summary>
		/// Gets the number of buttons on a mouse, or zero if no mouse is installed. (From OSInfo)
		/// </summary>
		public byte MouseButtons { get; private set; }
		/// <summary>
		/// Gets the number of supported touch points. (From OSInfo)
		/// </summary>
		public byte MaxTouchPoints { get; private set; }
		/// <summary>
		/// Gets the logical resolution of the screen. 100 % is 96 dpi. (From OSInfo)
		/// </summary>
		public ushort ScreenDpi { get; private set; }

		/// <summary>
		/// Gets the current thread culture code.
		/// </summary>
		public string CultureName { get; private set; }
		/// <summary>
		/// Gets the current working directory.
		/// </summary>
		public string CurrentDirectory { get; private set; }
		/// <summary>
		/// Gets the current environment variables.
		/// </summary>
		public string EnvironmentVariables { get; private set; }
		/// <summary>
		/// Gets the number of installed (logical) processors.
		/// </summary>
		public ushort CpuCount { get; private set; }
		/// <summary>
		/// Gets the host name of the computer.
		/// </summary>
		public string HostName { get; private set; }
		/// <summary>
		/// Gets the user name of the currently logged in user.
		/// </summary>
		public string UserName { get; private set; }
		/// <summary>
		/// Gets a value indicating whether the process is running interactively. See
		/// Environment.UserInteractive.
		/// </summary>
		public bool IsInteractive { get; private set; }
		/// <summary>
		/// Gets the file name of the process executable.
		/// </summary>
		public string ExecutablePath { get; private set; }
		/// <summary>
		/// Gets the command line passed to the current process.
		/// </summary>
		public string CommandLine { get; private set; }
		/// <summary>
		/// Gets the version of the current application.
		/// </summary>
		public string AppVersion { get; private set; }
		/// <summary>
		/// Gets a value indicating whether the running process is 64 bit.
		/// </summary>
		public bool IsProcess64Bit { get; private set; }
		/// <summary>
		/// Gets the CLR version.
		/// </summary>
		public string ClrVersion { get; private set; }
		/// <summary>
		/// Gets the UTC offset of the local time zone.
		/// </summary>
		public TimeSpan LocalTimeZoneOffset { get; private set; }
		/// <summary>
		/// Gets the private memory currently used by this process in bytes.
		/// </summary>
		public long ProcessMemory { get; private set; }
		/// <summary>
		/// Gets the peak working set memory used by this process in bytes.
		/// </summary>
		public long PeakProcessMemory { get; private set; }
		/// <summary>
		/// Gets the amount of total visible memory on the computer in bytes.
		/// </summary>
		public long TotalMemory { get; private set; }
		/// <summary>
		/// Gets the amount of available memory on the computer in bytes.
		/// </summary>
		public long AvailableMemory { get; private set; }
		/// <summary>
		/// Gets the process ID of the current process.
		/// </summary>
		public int ProcessId { get; private set; }
		/// <summary>
		/// Gets a value indicating whether the current process is running with administrator
		/// privileges.
		/// </summary>
		public bool IsAdministrator { get; private set; }
		/// <summary>
		/// Gets the width of the primary screen in pixels.
		/// </summary>
		public ushort PrimaryScreenWidth { get; private set; }
		/// <summary>
		/// Gets the height of the primary screen in pixels.
		/// </summary>
		public ushort PrimaryScreenHeight { get; private set; }
		/// <summary>
		/// Gets the bits per pixel of the primary screen, a.k.a. colour depth.
		/// </summary>
		public byte PrimaryScreenBitsPerPixel { get; private set; }
		/// <summary>
		/// Gets the left of the primary screen workspace in pixels.
		/// </summary>
		public ushort PrimaryScreenWorkingAreaLeft { get; private set; }
		/// <summary>
		/// Gets the top of the primary screen workspace in pixels.
		/// </summary>
		public ushort PrimaryScreenWorkingAreaTop { get; private set; }
		/// <summary>
		/// Gets the width of the primary screen workspace in pixels.
		/// </summary>
		public ushort PrimaryScreenWorkingAreaWidth { get; private set; }
		/// <summary>
		/// Gets the height of the primary screen workspace in pixels.
		/// </summary>
		public ushort PrimaryScreenWorkingAreaHeight { get; private set; }
		/// <summary>
		/// Gets the number of screens attached to this computer.
		/// </summary>
		public byte ScreenCount { get; private set; }

		#endregion Data properties

		#region Constructor

		private FieldLogEventEnvironment()
		{
		}

		#endregion Constructor

		#region Static Current method

		/// <summary>
		/// Returns a new instance of the FieldLogEventEnvironment class that contains information
		/// about the current environment and state of the system.
		/// </summary>
		/// <returns>The FieldLogEventEnvironment instance.</returns>
		public static FieldLogEventEnvironment Current()
		{
			FieldLogEventEnvironment env = new FieldLogEventEnvironment();
			env.OSType = OSInfo.Type;
			env.OSVersion = OSInfo.Version;
			env.OSEdition = OSInfo.Edition;
			env.OSServicePack = OSInfo.ServicePack;
			env.OSIs64Bit = OSInfo.Is64Bit;
			env.OSBuild = OSInfo.Build;
			env.OSServicePackBuild = OSInfo.ServicePackBuild;
			env.OSProductName = OSInfo.ProductName;
			env.OSIsAppServer = OSInfo.IsAppServer;
			env.OSLanguage = OSInfo.Language;
			env.OSLastBootTime = OSInfo.LastBootTime;
			env.OSIsFailSafeBoot = OSInfo.IsFailSafeBoot;
			env.AppCompatLayer = OSInfo.AppCompatLayer;
			env.ClrType = OSInfo.ClrType;
			env.MouseButtons = (byte) OSInfo.MouseButtons;
			env.MaxTouchPoints = (byte) OSInfo.MaxTouchPoints;
			env.ScreenDpi = (ushort) OSInfo.ScreenDpi;

			env.CultureName = Thread.CurrentThread.CurrentCulture.Name;
			env.CurrentDirectory = Environment.CurrentDirectory;
			List<string> envNames = new List<string>();
			foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
			{
				envNames.Add(de.Key.ToString());
			}
			envNames.Sort(StringComparer.InvariantCultureIgnoreCase);
			StringBuilder envSb = new StringBuilder();
			foreach (string envName in envNames)
			{
				envSb.Append(envName);
				envSb.Append("=");
				envSb.Append(Environment.GetEnvironmentVariable(envName));
				envSb.Append("\n");
			}
			env.EnvironmentVariables = envSb.ToString().TrimEnd();
			env.CpuCount = (ushort) Environment.ProcessorCount;
			env.HostName = Environment.MachineName;
			env.UserName = Environment.UserDomainName + "\\" + Environment.UserName;
			env.IsInteractive = Environment.UserInteractive;
			env.ExecutablePath = Assembly.GetEntryAssembly() != null ? Assembly.GetEntryAssembly().Location : "";
			env.CommandLine = Environment.CommandLine;
			env.AppVersion = FL.AppVersion;
			env.IsProcess64Bit = IntPtr.Size == 8;   // .NET 4 only: Environment.Is64BitProcess
			env.ClrVersion = Environment.Version.ToString();
#if NET20
			env.LocalTimeZoneOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.UtcNow);
#else
			env.LocalTimeZoneOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
#endif
			env.ProcessMemory = OSInfo.GetProcessPrivateMemory();
			env.PeakProcessMemory = OSInfo.GetProcessPeakMemory();
			env.TotalMemory = OSInfo.GetTotalMemorySize();
			env.AvailableMemory = OSInfo.GetAvailableMemorySize();
			env.ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
			env.IsAdministrator = OSInfo.IsCurrentUserLocalAdministrator();
			env.PrimaryScreenWidth = (ushort) System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
			env.PrimaryScreenHeight = (ushort) System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
			env.PrimaryScreenBitsPerPixel = (byte) System.Windows.Forms.Screen.PrimaryScreen.BitsPerPixel;
			env.PrimaryScreenWorkingAreaLeft = (ushort) System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Left;
			env.PrimaryScreenWorkingAreaTop = (ushort) System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Top;
			env.PrimaryScreenWorkingAreaWidth = (ushort) System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
			env.PrimaryScreenWorkingAreaHeight = (ushort) System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
			env.ScreenCount = (byte) System.Windows.Forms.Screen.AllScreens.Length;

			// Source: http://www.informit.com/guides/content.aspx?g=dotnet&seqNum=682
			int ptrSize = IntPtr.Size;
			int strSize = ptrSize == 4 ? 20 : 32;

			env.Size = 4 + 4 + 4 + 4 +
				ptrSize + (env.OSServicePack != null ? strSize + env.OSServicePack.Length * 2 : 0) +
				4 + 4 + 4 +
				ptrSize + (env.OSProductName != null ? strSize + env.OSProductName.Length * 2 : 0) +
				4 +
				ptrSize + (env.OSLanguage != null ? strSize + env.OSLanguage.Length * 2 : 0) +
				8 + 4 +
				ptrSize + (env.AppCompatLayer != null ? strSize + env.AppCompatLayer.Length * 2 : 0) +
				ptrSize + (env.ClrType != null ? strSize + env.ClrType.Length * 2 : 0) +
				4 + 4 + 4 +
				ptrSize + (env.CultureName != null ? strSize + env.CultureName.Length * 2 : 0) +
				ptrSize + (env.CurrentDirectory != null ? strSize + env.CurrentDirectory.Length * 2 : 0) +
				ptrSize + (env.EnvironmentVariables != null ? strSize + env.EnvironmentVariables.Length * 2 : 0) +
				4 +
				ptrSize + (env.HostName != null ? strSize + env.HostName.Length * 2 : 0) +
				ptrSize + (env.UserName != null ? strSize + env.UserName.Length * 2 : 0) +
				4 +
				ptrSize + (env.ExecutablePath != null ? strSize + env.ExecutablePath.Length * 2 : 0) +
				ptrSize + (env.CommandLine != null ? strSize + env.CommandLine.Length * 2 : 0) +
				ptrSize + (env.AppVersion != null ? strSize + env.AppVersion.Length * 2 : 0) +
				4 +
				ptrSize + (env.ClrVersion != null ? strSize + env.ClrVersion.Length * 2 : 0) +
				8 + 8 + 8 + 8 + 8 +
				4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;
			return env;
		}

		#endregion Static Current method

		#region Log file reading/writing

		/// <summary>
		/// Writes the FieldLogEventEnvironment data to a log file writer.
		/// </summary>
		/// <param name="writer"></param>
		internal void Write(FieldLogFileWriter writer)
		{
			writer.AddBuffer((byte) OSType);
			writer.AddBuffer((byte) OSVersion);
			writer.AddBuffer((byte) OSEdition);
			writer.AddBuffer(OSServicePack);
			writer.AddBuffer(OSBuild);
			writer.AddBuffer(OSServicePackBuild);
			writer.AddBuffer(OSProductName);
			writer.AddBuffer(OSLanguage);
			writer.AddBuffer(OSLastBootTime.Ticks);
			writer.AddBuffer(AppCompatLayer);
			writer.AddBuffer(ClrType);
			writer.AddBuffer(MouseButtons);
			writer.AddBuffer(MaxTouchPoints);
			writer.AddBuffer(ScreenDpi);

			writer.AddBuffer(CultureName);
			writer.AddBuffer(CurrentDirectory);
			writer.AddBuffer(EnvironmentVariables);
			writer.AddBuffer(CpuCount);
			writer.AddBuffer(HostName);
			writer.AddBuffer(UserName);
			writer.AddBuffer(ExecutablePath);
			writer.AddBuffer(CommandLine);
			writer.AddBuffer(AppVersion);
			writer.AddBuffer(ClrVersion);
			writer.AddBuffer((short) LocalTimeZoneOffset.TotalMinutes);
			writer.AddBuffer(ProcessMemory);
			writer.AddBuffer(PeakProcessMemory);
			writer.AddBuffer(TotalMemory);
			writer.AddBuffer(AvailableMemory);
			writer.AddBuffer(ProcessId);
			writer.AddBuffer(PrimaryScreenWidth);
			writer.AddBuffer(PrimaryScreenHeight);
			writer.AddBuffer(PrimaryScreenBitsPerPixel);
			writer.AddBuffer(PrimaryScreenWorkingAreaLeft);
			writer.AddBuffer(PrimaryScreenWorkingAreaTop);
			writer.AddBuffer(PrimaryScreenWorkingAreaWidth);
			writer.AddBuffer(PrimaryScreenWorkingAreaHeight);
			writer.AddBuffer(ScreenCount);

			byte flags = 0;
			if (OSIs64Bit) flags |= 1;
			if (OSIsAppServer) flags |= 2;
			if (OSIsFailSafeBoot) flags |= 4;
			if (IsInteractive) flags |= 8;
			if (IsProcess64Bit) flags |= 16;
			if (IsAdministrator) flags |= 32;
			writer.AddBuffer(flags);
		}

		/// <summary>
		/// Reads the FieldLogEventEnvironment data from a log file reader.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		internal static FieldLogEventEnvironment Read(FieldLogFileReader reader)
		{
			FieldLogEventEnvironment env = new FieldLogEventEnvironment();
			env.OSType = (OSType) reader.ReadByte();
			env.OSVersion = (OSVersion) reader.ReadByte();
			env.OSEdition = (OSEdition) reader.ReadByte();
			env.OSServicePack = reader.ReadString();
			env.OSBuild = reader.ReadInt32();
			env.OSServicePackBuild = reader.ReadInt32();
			env.OSProductName = reader.ReadString();
			env.OSLanguage = reader.ReadString();
			env.OSLastBootTime = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);
			env.AppCompatLayer = reader.ReadString();
			env.ClrType = reader.ReadString();
			env.MouseButtons = reader.ReadByte();
			env.MaxTouchPoints = reader.ReadByte();
			env.ScreenDpi = reader.ReadUInt16();

			env.CultureName = reader.ReadString();
			env.CurrentDirectory = reader.ReadString();
			env.EnvironmentVariables = reader.ReadString();
			env.CpuCount = reader.ReadUInt16();
			env.HostName = reader.ReadString();
			env.UserName = reader.ReadString();
			env.ExecutablePath = reader.ReadString();
			env.CommandLine = reader.ReadString();
			env.AppVersion = reader.ReadString();
			env.ClrVersion = reader.ReadString();
			env.LocalTimeZoneOffset = TimeSpan.FromMinutes(reader.ReadInt16());
			env.ProcessMemory = reader.ReadInt64();
			env.PeakProcessMemory = reader.ReadInt64();
			env.TotalMemory = reader.ReadInt64();
			env.AvailableMemory = reader.ReadInt64();
			env.ProcessId = reader.ReadInt32();
			env.PrimaryScreenWidth = reader.ReadUInt16();
			env.PrimaryScreenHeight = reader.ReadUInt16();
			env.PrimaryScreenBitsPerPixel = reader.ReadByte();
			env.PrimaryScreenWorkingAreaLeft = reader.ReadUInt16();
			env.PrimaryScreenWorkingAreaTop = reader.ReadUInt16();
			env.PrimaryScreenWorkingAreaWidth = reader.ReadUInt16();
			env.PrimaryScreenWorkingAreaHeight = reader.ReadUInt16();
			env.ScreenCount = reader.ReadByte();

			byte flags = reader.ReadByte();
			env.OSIs64Bit = (flags & 1) != 0;
			env.OSIsAppServer = (flags & 2) != 0;
			env.OSIsFailSafeBoot = (flags & 4) != 0;
			env.IsInteractive = (flags & 8) != 0;
			env.IsProcess64Bit = (flags & 16) != 0;
			env.IsAdministrator = (flags & 32) != 0;

			// Check if the environment is actually empty
			if (env.CpuCount == 0)
				return FieldLogEventEnvironment.Empty;

			return env;
		}

		#endregion Log file reading/writing
	}
}
