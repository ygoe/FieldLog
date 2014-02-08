using System;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Collections.Generic;

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
			Empty.Size = 4 + 4 + 4 + 4 +
				0 +
				1 + 4 + 4 +
				0 +
				1 +
				0 +
				8 +
				0 +
				0 +
				0 +
				1 +
				0 +
				0 +
				4 +
				0 +
				0 +
				1 +
				0 +
				0 +
				0 +
				1 +
				0 +
				8 + 8 + 8 + 8;
		}

		#endregion Static members

		#region Data properties

		/// <summary>Approximate data size of this data structure. Used for buffer size estimation.</summary>
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
		/// Gets the current thread culture code.
		/// </summary>
		public string CultureName { get; private set; }
		/// <summary>
		/// Gets a value indicating whether the system is currently shutting down.
		/// </summary>
		public bool IsShuttingDown { get; private set; }
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
		public int CpuCount { get; private set; }
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

		// TODO: Add local time zone offset

		#endregion Data properties

		#region Constructor

		private FieldLogEventEnvironment()
		{
		}

		#endregion Constructor

		#region Static Current method

		/// <summary>
		/// Gets a new instance of the FieldLogEventEnvironment class that contains information
		/// about the current environment and state of the system.
		/// </summary>
		/// <returns></returns>
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
			env.AppCompatLayer = OSInfo.AppCompatLayer;
			env.ClrType = OSInfo.ClrType;

			env.CultureName = Thread.CurrentThread.CurrentCulture.Name;
			env.IsShuttingDown = Environment.HasShutdownStarted;
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
			env.CpuCount = Environment.ProcessorCount;
			env.HostName = Environment.MachineName;
			env.UserName = Environment.UserDomainName + "\\" + Environment.UserName;
			env.IsInteractive = Environment.UserInteractive;
			env.ExecutablePath = Assembly.GetEntryAssembly().Location;
			env.CommandLine = Environment.CommandLine;
			env.AppVersion = FL.AppVersion;
			//env.IsProcess64Bit = Environment.Is64BitProcess;   // .NET 4 only
			env.IsProcess64Bit = IntPtr.Size == 8;
			env.ClrVersion = Environment.Version.ToString();
			env.ProcessMemory = OSInfo.GetProcessPrivateMemory();
			env.PeakProcessMemory = OSInfo.GetProcessPeakMemory();
			env.TotalMemory = OSInfo.GetTotalMemorySize();
			env.AvailableMemory = OSInfo.GetAvailableMemorySize();

			env.Size = 4 + 4 + 4 + 4 +
				(env.OSServicePack != null ? env.OSServicePack.Length * 2 : 0) +
				1 + 4 + 4 +
				(env.OSProductName != null ? env.OSProductName.Length * 2 : 0) +
				1 +
				(env.OSLanguage != null ? env.OSLanguage.Length * 2 : 0) +
				8 +
				(env.AppCompatLayer != null ? env.AppCompatLayer.Length * 2 : 0) +
				(env.ClrType != null ? env.ClrType.Length * 2 : 0) +
				(env.CultureName != null ? env.CultureName.Length * 2 : 0) +
				1 +
				(env.CurrentDirectory != null ? env.CurrentDirectory.Length * 2 : 0) +
				(env.EnvironmentVariables != null ? env.EnvironmentVariables.Length * 2 : 0) +
				4 +
				(env.HostName != null ? env.HostName.Length * 2 : 0) +
				(env.UserName != null ? env.UserName.Length * 2 : 0) +
				1 +
				(env.ExecutablePath != null ? env.ExecutablePath.Length * 2 : 0) +
				(env.CommandLine != null ? env.CommandLine.Length * 2 : 0) +
				(env.AppVersion != null ? env.AppVersion.Length * 2 : 0) +
				1 +
				(env.ClrVersion != null ? env.ClrVersion.Length * 2 : 0) +
				8 + 8 + 8 + 8;
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
			writer.AddBuffer(ProcessMemory);
			writer.AddBuffer(PeakProcessMemory);
			writer.AddBuffer(TotalMemory);
			writer.AddBuffer(AvailableMemory);

			byte flags = 0;
			if (OSIs64Bit) flags |= 1;
			if (OSIsAppServer) flags |= 2;
			if (IsShuttingDown) flags |= 4;
			if (IsInteractive) flags |= 8;
			if (IsProcess64Bit) flags |= 16;
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

			env.CultureName = reader.ReadString();
			env.CurrentDirectory = reader.ReadString();
			env.EnvironmentVariables = reader.ReadString();
			env.CpuCount = reader.ReadInt32();
			env.HostName = reader.ReadString();
			env.UserName = reader.ReadString();
			env.ExecutablePath = reader.ReadString();
			env.CommandLine = reader.ReadString();
			env.AppVersion = reader.ReadString();
			env.ClrVersion = reader.ReadString();
			env.ProcessMemory = reader.ReadInt64();
			env.PeakProcessMemory = reader.ReadInt64();
			env.TotalMemory = reader.ReadInt64();
			env.AvailableMemory = reader.ReadInt64();

			byte flags = reader.ReadByte();
			env.OSIs64Bit = (flags & 1) != 0;
			env.OSIsAppServer = (flags & 2) != 0;
			env.IsShuttingDown = (flags & 4) != 0;
			env.IsInteractive = (flags & 8) != 0;
			env.IsProcess64Bit = (flags & 16) != 0;
			return env;
		}

		#endregion Log file reading/writing
	}
}
