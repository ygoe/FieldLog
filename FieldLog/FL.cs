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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
#if !NET20
using System.Windows.Threading;
#endif
#if ASPNET
using System.Linq;
using System.Web;
#endif

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Implements the FieldLog system and provides logging methods for applications.
	/// </summary>
	public static class FL
	{
		#region Native interop

		/// <summary>
		/// Defines values returned by the GetFileType function.
		/// </summary>
		private enum FileType : uint
		{
			/// <summary>The specified file is a character file, typically an LPT device or a console.</summary>
			FileTypeChar = 0x0002,
			/// <summary>The specified file is a disk file.</summary>
			FileTypeDisk = 0x0001,
			/// <summary>The specified file is a socket, a named pipe, or an anonymous pipe.</summary>
			FileTypePipe = 0x0003,
			/// <summary>Unused.</summary>
			FileTypeRemote = 0x8000,
			/// <summary>Either the type of the specified file is unknown, or the function failed.</summary>
			FileTypeUnknown = 0x0000,
		}

		/// <summary>
		/// Defines standard device handles for the GetStdHandle function.
		/// </summary>
		private enum StdHandle : int
		{
			/// <summary>The standard input device. Initially, this is the console input buffer, CONIN$.</summary>
			Input = -10,
			/// <summary>The standard output device. Initially, this is the active console screen buffer, CONOUT$.</summary>
			Output = -11,
			/// <summary>The standard error device. Initially, this is the active console screen buffer, CONOUT$.</summary>
			Error = -12,
		}

		/// <summary>
		/// Retrieves the file type of the specified file.
		/// </summary>
		/// <param name="hFile">A handle to the file.</param>
		/// <returns></returns>
		[DllImport("kernel32.dll")]
		private static extern FileType GetFileType(IntPtr hFile);

		/// <summary>
		/// Retrieves a handle to the specified standard device (standard input, standard output,
		/// or standard error).
		/// </summary>
		/// <param name="nStdHandle">The standard device.</param>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetStdHandle(StdHandle nStdHandle);

		/// <summary>
		/// Retrieves the window handle used by the console associated with the calling process.
		/// </summary>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetConsoleWindow();

		/// <summary>
		/// Determines the visibility state of the specified window.
		/// </summary>
		/// <param name="hWnd">A handle to the window to be tested.</param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		static extern bool IsWindowVisible(IntPtr hWnd);

		#endregion Native interop

		#region Constants

		/// <summary>
		/// Defines the format version of log files.
		/// </summary>
		public const byte FileFormatVersion = 2;

		/// <summary>
		/// Defines the maximum buffer size to keep.
		/// </summary>
		private const int maxBufferSize = 65535;

		/// <summary>
		/// Defines the seconds to wait for user feedback in the application error message. If the
		/// user does not respond within this time, a warning is logged and the application is
		/// terminated.
		/// </summary>
		private const int AppErrorTerminateTimeout = 180;

		/// <summary>
		/// Defines the log configuration file name extension.
		/// </summary>
		/// <remarks>
		/// Don't set this to ".config" when using it with ASP.NET applications, or it will find
		/// the Web.config file and try to read it.
		/// </remarks>
		private const string logConfigExtension = ".flconfig";

		internal const string EnsureJitTimerKey = "FieldLog.EnsureJit";

		#endregion Constants

		#region Delegates

		/// <summary>
		/// Encapsulates a method that shows an application error dialog to the user.
		/// </summary>
		/// <param name="exItem">Information about the exception to report.</param>
		/// <param name="allowContinue">true if the application can be continued, false if the application will be terminated.</param>
		public delegate void ShowAppErrorDialogDelegate(FieldLogExceptionItem exItem, bool allowContinue);

		#endregion Delegates

		#region Private static data

		/// <summary>
		/// The UTC date and time at the start of the Stopwatch.
		/// </summary>
		private static DateTime startTime;
		/// <summary>
		/// The Stopwatch to measure high-precision relative time.
		/// </summary>
		private static Stopwatch stopwatch;
		/// <summary>
		/// The log items counter. Increased with every log event. Used for correct ordering of
		/// log items with the exact same time value. May wrap around.
		/// </summary>
		private static int eventCounter;

		/// <summary>
		/// Contains all buffers that are ready to send. Synchronised by buffers.
		/// </summary>
		private static Queue<List<FieldLogItem>> buffers = new Queue<List<FieldLogItem>>();
		/// <summary>
		/// Lock object for accessing the currentBuffer field.
		/// </summary>
		private static readonly object currentBufferLock = new object();
		/// <summary>
		/// The current buffer that new items are added to. Synchronised by currentBufferLock.
		/// </summary>
		private static List<FieldLogItem> currentBuffer = new List<FieldLogItem>();
		/// <summary>
		/// The size of the current buffer. Synchronised by currentBufferLock.
		/// </summary>
		private static int currentBufferSize;
		/// <summary>
		/// Event to signal a new buffer ready for sending.
		/// </summary>
		private static AutoResetEvent newBufferEvent = new AutoResetEvent(false);
		/// <summary>
		/// Timeout for sending buffers before reaching their maximum size.
		/// </summary>
		private static Timer sendTimeout = new Timer(OnSendTimeout);
		/// <summary>
		/// Background thread for sending the buffers.
		/// </summary>
		private static Thread sendThread;
		/// <summary>
		/// Indicates whether the send thread shall be shut down. Synchronised by sendThread.
		/// </summary>
		private static bool sendThreadCancellationPending;
		/// <summary>
		/// Indicates whether the log queue has been shut down. Intentionally not synchronised.
		/// </summary>
		private static bool isShutdown;
		/// <summary>
		/// Lock object for shutting down FieldLog.
		/// </summary>
		private static readonly object shutdownLock = new object();
		/// <summary>
		/// Log file base path.
		/// </summary>
		private static string logFileBasePath;
		/// <summary>
		/// Indicates whether the path for writing log files has already been set.
		/// </summary>
		private static bool logFileBasePathSet;
		/// <summary>
		/// Application-defined custom log file prefix. Used for all log directories tested in the
		/// default strategy, but not when customLogFileBasePath is set.
		/// </summary>
		private static string customLogFilePrefix;
		/// <summary>
		/// Application-defined custom log file base path. If set, this is tried first.
		/// </summary>
		private static string customLogFileBasePath;
		/// <summary>
		/// Indicates whether an application-defined path for writing log files has already been
		/// set. If false, no log files are written and all buffers are retained, except when
		/// FieldLog is shutting down. In the latter case, the default automatic path selection is
		/// used and files are written if a working path was found. This is so that start-up errors
		/// that occur before the application could set a custom path can be logged at least
		/// somewhere.
		/// </summary>
		private static bool customLogFileBasePathSet;
		/// <summary>
		/// Lock object for setting a custom log path.
		/// </summary>
		private static readonly object customLogPathLock = new object();
		
		/// <summary>
		/// Maximum size of any single log file. Only set when the send thread is stopped.
		/// </summary>
		private static int maxFileSize;
		/// <summary>
		/// Maximum size of all log files together. Only set when the send thread is stopped.
		/// </summary>
		private static long maxTotalSize;
		/// <summary>
		/// Minimum time to keep log items of each priority. Only set when the send thread is
		/// stopped.
		/// </summary>
		private static Dictionary<FieldLogPriority, TimeSpan> priorityKeepTimes = new Dictionary<FieldLogPriority, TimeSpan>();
		/// <summary>
		/// Time when the log files of each priority were last purged. Only used in the send thread.
		/// </summary>
		private static Dictionary<FieldLogPriority, DateTime> priorityLastPurgeTimes = new Dictionary<FieldLogPriority, DateTime>();
		/// <summary>
		/// Time when the total size of all log files was last checked. Only used in the send thread.
		/// </summary>
		private static DateTime totalSizeLastPurgeTime;
		/// <summary>
		/// Indicates whether the configuration file has changed and should be reloaded.
		/// Synchronised by sendThread.
		/// </summary>
		private static bool configChanged;
		/// <summary>
		/// Detects changes to the configuration file.
		/// </summary>
		private static FileSystemWatcher configFileWatcher;

		/// <summary>
		/// Keeps all buffers that still need to be sent. Used by the send thread only.
		/// </summary>
		private static List<List<FieldLogItem>> buffersToSend = new List<List<FieldLogItem>>();
		/// <summary>
		/// Keeps all open log file writers for each priority. Used by the send thread only.
		/// </summary>
		private static Dictionary<FieldLogPriority, FieldLogFileWriter> priorityLogWriters = new Dictionary<FieldLogPriority, FieldLogFileWriter>();

		/// <summary>
		/// Keeps all custom time measurement entries.
		/// </summary>
		private static Dictionary<string, CustomTimerInfo> customTimers = new Dictionary<string, CustomTimerInfo>();
		/// <summary>
		/// Locks access to custom time measurement data.
		/// </summary>
#if NET20
		private static object customTimersLock = new object();
#else
		private static ReaderWriterLockSlim customTimersLock = new ReaderWriterLockSlim();
#endif

		/// <summary>
		/// Contains all log items that are buffered in the current thread.
		/// </summary>
		[ThreadStatic]
		private static List<FieldLogItem> threadBufferedItems;

		/// <summary>
		/// The last assigned web request ID. Synchronised by Interlocked access.
		/// </summary>
		private static int LastWebRequestId;

		/// <summary>
		/// Override configuration file name. Used for ASP.NET.
		/// </summary>
		private static string configFileNameOverride;
		/// <summary>
		/// Override default log file directory. Used for ASP.NET.
		/// </summary>
		private static string logDefaultDirOverride;
		/// <summary>
		/// Indicates whether the duplicate LogStart check on writing items to the log file has
		/// been run. Used for ASP.NET.
		/// </summary>
		private static bool didDuplicateLogStartCheck;
		/// <summary>
		/// The level of first-chance exception handling. If this goes up, there is an exception
		/// in the exception handler. If this goes uncontrolled, it leads to a
		/// StackOverflowException that crashes the application.
		/// </summary>
		private static int firstChanceExceptionLevel;

		#endregion Private static data

		#region Internal static data

		/// <summary>
		/// Written from the static constructor, read in the send thread.
		/// </summary>
		internal static FieldLogScopeItem LogScopeItem;
		/// <summary>
		/// Written and read in the send thread only.
		/// </summary>
		internal static readonly Dictionary<int, FieldLogScopeItem> ThreadScopes = new Dictionary<int, FieldLogScopeItem>();
		/// <summary>
		/// Written and read in the send thread only.
		/// </summary>
		internal static readonly Dictionary<int, Stack<FieldLogScopeItem>> CurrentScopes = new Dictionary<int, Stack<FieldLogScopeItem>>();

		[ThreadStatic]
		internal static short ScopeLevel;

		[ThreadStatic]
		internal static int ThreadId;

		/// <summary>
		/// The web request ID of the web request processed by the current thread. 0 if no web
		/// request is currently processed in this thread.
		/// </summary>
		[ThreadStatic]
		internal static uint WebRequestId;

		/// <summary>
		/// A reference to the EntryAssembly. This is determined by other means for ASP.NET
		/// applications.
		/// </summary>
		internal static Assembly EntryAssembly;
		/// <summary>
		/// The entry assembly's Location value. This is determined by other means for ASP.NET
		/// applications.
		/// </summary>
		internal static string EntryAssemblyLocation;

		#endregion Internal static data

		#region Static constructor

		/// <summary>
		/// Initialises the static FieldLog environment, time measurement, worker threads and
		/// application error handlers. This is called automatically when the process is started.
		/// </summary>
		static FL()
		{
			CalibrateTime();
			CheckTimeThread.Start();

			LogFirstChanceExceptions = true;
			WaitForItemsBacklog = true;

			EntryAssembly = Assembly.GetEntryAssembly();
			if (EntryAssembly != null)
			{
				EntryAssemblyLocation = EntryAssembly.Location;
			}

			// Read or reset log configuration from file
			ReadLogConfiguration();

			// Initialise the send thread
			sendThread = new Thread(SendThread);
			sendThread.IsBackground = true;
			sendThread.Name = "FieldLog.SendThread";
			sendThread.Priority = ThreadPriority.BelowNormal;
			sendThread.Start();

			SessionId = Guid.NewGuid();

			IntPtr consoleWnd = GetConsoleWindow();
			IsInteractiveConsoleApp = Environment.UserInteractive &&
				consoleWnd != IntPtr.Zero &&
				IsWindowVisible(consoleWnd) &&
				GetFileType(GetStdHandle(StdHandle.Input)) == FileType.FileTypeChar &&
				GetFileType(GetStdHandle(StdHandle.Output)) == FileType.FileTypeChar &&
				GetFileType(GetStdHandle(StdHandle.Error)) == FileType.FileTypeChar;

			// Application error dialog localisation, default to English
			AppErrorDialogTitle = "Application error";
			AppErrorDialogContinuableGui = "An error occured and the application may not continue to work properly. " +
				"Click “OK” to continue, or “Cancel” to quit the application. " +
				"If you choose to continue, additional errors or failures may occur.";
			AppErrorDialogContinuableConsole = "An error occured and the application may not continue to work properly. " +
				"If you choose to continue, additional errors or failures may occur.";
			AppErrorDialogTerminating = "An error occured and the application cannot continue.";
			AppErrorDialogContext = "Context:";
			AppErrorDialogNote = "If the problem persists, please contact the application developer.";
			AppErrorDialogLogPath = "The log file containing detailed error information is saved to {0}.";
			AppErrorDialogNoLog = "The log file could not be written.";
			AppErrorDialogConsoleAction = "Press the Enter key to continue, or Escape to quit the application.";
			AppErrorDialogTimerNote = "The application will be terminated after {0} seconds without user response.";

			//AppErrorDialogContinuable = "Es ist ein Fehler aufgetreten, die Anwendung kann möglicherweise nicht korrekt fortgesetzt werden. " +
			//    "Drücken Sie auf „OK“, um das Programm fortzusetzen, oder „Abbrechen“, um das Programm zu beenden. " +
			//    "Falls Sie die Ausführung fortsetzen, können weitere Fehler oder Störungen auftreten.";
			//AppErrorDialogTerminating = "Es ist ein Fehler aufgetreten, die Anwendung kann nicht fortgesetzt werden.";
			//AppErrorDialogNote = "Falls das Problem weiterhin bestehen sollte, wenden Sie sich bitte an den Programmentwickler.";

			// Use default implementation to show an application error dialog
			ShowAppErrorDialog = DefaultShowAppErrorDialog;

			LogScope(FieldLogScopeType.LogStart, null);
			AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;
			AppDomain.CurrentDomain.DomainUnload += AppDomain_DomainUnload;

			if (!Debugger.IsAttached)
			{
				RegisterAppErrorHandler();
			}

			// These methods are time-critical so call them once to ensure they're JITed when the
			// application need them. Disabled for debugging to avoid additional breakpoint hits.
#if !DEBUG
			FL.StartTimer(EnsureJitTimerKey);
			FL.StopTimer(EnsureJitTimerKey);
			FL.ClearTimer(EnsureJitTimerKey);
			using (FL.Timer(EnsureJitTimerKey))
			{
			}
			using (FL.Timer(EnsureJitTimerKey, true, true))
			{
			}
#endif
		}

		/// <summary>
		/// Called when the current process exits.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The processing time in this event is limited. All handlers of this event together must
		/// not take more than ca. 3 seconds. The processing will then be terminated.
		/// </para>
		/// <para>
		/// This method is called on a pool thread.
		/// </para>
		/// </remarks>
		private static void AppDomain_ProcessExit(object sender, EventArgs e)
		{
			// Flush log files, if not already done by the application
			Shutdown();
		}

		/// <summary>
		/// Called when the current AppDomains is unloaded.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This event is never raised in the default application domain.
		/// </para>
		/// <para>
		/// This method is called on a pool thread.
		/// </para>
		/// </remarks>
		private static void AppDomain_DomainUnload(object sender, EventArgs args)
		{
			Trace("AppDomain.DomainUnload");
			// Flush log files, if not already done by the application
			Shutdown();
		}

		#endregion Static constructor

		#region Time

		private static void CalibrateTime()
		{
			// Link the high-precision stopwatch with the current time for later high-precision
			// performance tracing timestamps. Wait for DateTime.UtcNow to actually change to get
			// a fresh and most-accurate time.
			stopwatch = new Stopwatch();
			DateTime t0 = DateTime.UtcNow;
			while ((startTime = DateTime.UtcNow) == t0) { }
			stopwatch.Start();
		}

		internal static void RebaseTime()
		{
			DateTime t0 = DateTime.UtcNow;
			DateTime freshTime;
			while ((freshTime = DateTime.UtcNow) == t0) { }
			startTime = freshTime - stopwatch.Elapsed;
		}

		#endregion Time

		#region Static properties

		/// <summary>
		/// Gets an ID that uniquely identifies the current execution of the application.
		/// </summary>
		public static Guid SessionId { get; private set; }

		/// <summary>
		/// Gets or sets a method that shows an application error dialog to the user.
		/// </summary>
		public static ShowAppErrorDialogDelegate ShowAppErrorDialog { get; set; }

		/// <summary>
		/// Gets the high-precision UTC time.
		/// </summary>
		/// <remarks>
		/// This call takes ~50 ns on 2013 hardware, DateTime.UtcNow takes ~20 ns.
		/// </remarks>
		public static DateTime UtcNow
		{
			get
			{
				return startTime.AddTicks(stopwatch.Elapsed.Ticks);
			}
		}

		/// <summary>
		/// Gets the currently used log file base path. This is an absolute path to a directory and
		/// a file name prefix. To write other application-specific log files to that directory,
		/// just append a file name suffix and extension (not .fl) to this value. If this value is
		/// null, no path has been specified yet (log file writing is deferred) or no working path
		/// could be found (no files will be written at all).
		/// </summary>
		public static string LogFileBasePath
		{
			get
			{
				Thread.MemoryBarrier();   // Ensure that logFileBasePathSet and logFileBasePath are current here
				if (logFileBasePathSet)
					return logFileBasePath;
				return null;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether FirstChanceException events shall be logged.
		/// </summary>
		public static bool LogFirstChanceExceptions { get; set; }

		/// <summary>
		/// Gets a value indicating whether the current application has an interactive console and
		/// is able to interact with the user through it.
		/// </summary>
		public static bool IsInteractiveConsoleApp { get; private set; }

		/// <summary>
		/// Gets the backlog of items to be written to the log files.
		/// </summary>
		/// <remarks>
		/// Not synchronised because it shouldn't be necessary for a single Int32 value and a single
		/// wrong reading is not critical.
		/// </remarks>
		public static int WriteItemsBacklog { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether a Log call should be delayed if there is a high
		/// backlog of items to be written to the log files. This avoids item loss when flushing at
		/// process shutdown but may lead to considerable delays in rare cases of high logging rate.
		/// Default is true for safety.
		/// </summary>
		public static bool WaitForItemsBacklog { get; set; }

		/// <summary>
		/// Gets a value indicating whether the log queue has been shut down.
		/// </summary>
		public static bool IsShutdown { get { return isShutdown; } }

		/// <summary>Gets or sets the application error user dialog title.</summary>
		public static string AppErrorDialogTitle { get; set; }
		/// <summary>Gets or sets the application error user dialog intro for GUI applications if the application can be continued.</summary>
		public static string AppErrorDialogContinuableGui { get; set; }
		/// <summary>Gets or sets the application error user dialog intro for console applications if the application can be continued.</summary>
		public static string AppErrorDialogContinuableConsole { get; set; }
		/// <summary>Gets or sets the application error user dialog intro if the application will be terminated.</summary>
		public static string AppErrorDialogTerminating { get; set; }
		/// <summary>Gets or sets the application error user dialog context caption, including a colon at the end.</summary>
		public static string AppErrorDialogContext { get; set; }
		/// <summary>Gets or sets the application error user dialog note at the end of the message.</summary>
		public static string AppErrorDialogNote { get; set; }
		/// <summary>Gets or sets the application error user dialog text describing the log path.</summary>
		public static string AppErrorDialogLogPath { get; set; }
		/// <summary>Gets or sets the application error user dialog text if no log is written to disk.</summary>
		public static string AppErrorDialogNoLog { get; set; }
		/// <summary>Gets or sets the application error user dialog text to ask for an action (quit or continue).</summary>
		public static string AppErrorDialogConsoleAction { get; set; }
		/// <summary>Gets or sets the application error user dialog text informing about the safety timer.</summary>
		public static string AppErrorDialogTimerNote { get; set; }

		#endregion Static properties

		#region Application error handling

		/// <summary>
		/// Registers application error handlers for all application types.
		/// </summary>
		/// <remarks>
		/// This method is called from the FL static constructor, and only if no debugger is
		/// currently attached. A debugger should catch exceptions instead of us.
		/// </remarks>
		private static void RegisterAppErrorHandler()
		{
			// Reference: http://code.msdn.microsoft.com/Handling-Unhandled-47492d0b

			// Handle UI thread exceptions
			System.Windows.Forms.Application.ThreadException +=
				delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
				{
					FL.Critical(e.Exception, "WinForms.ThreadException", true);
				};
			// Set the unhandled exception mode to force all Windows Forms errors to go through our handler
			System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);

			// Handle non-UI thread exceptions
			AppDomain.CurrentDomain.UnhandledException +=
				delegate(object sender, UnhandledExceptionEventArgs e)
				{
					FL.Critical(e.ExceptionObject as Exception, "AppDomain.UnhandledException", true);
				};

#if !NET20
			// Log first-chance exceptions, also from try/catch blocks
			AppDomain.CurrentDomain.FirstChanceException +=
				delegate(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
				{
					if (e.Exception.GetType() == typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException))
					{
						// This is normal for dynamic types, ignore it.
						// (Trying to process exceptions on dynamic types only causes more pain.)
						return;
					}

					int localLevel = Interlocked.Increment(ref firstChanceExceptionLevel);
					try
					{
						if (localLevel <= 4 && LogFirstChanceExceptions && !isShutdown)
						{
							FL.Exception(FieldLogPriority.Trace, e.Exception, "AppDomain.FirstChanceException", new StackTrace(1, true));
						}
					}
					finally
					{
						Interlocked.Decrement(ref firstChanceExceptionLevel);
					}
				};

			// Log unhandled exceptions from within Tasks that are garbage-collected. Since GC
			// happens rarely, this event may never be fired, which makes this handler somewhat
			// useless. But in case we have a chance, we'll happily log the event.
			System.Threading.Tasks.TaskScheduler.UnobservedTaskException +=
				delegate(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
				{
					FL.Trace(e.Exception, "TaskScheduler.UnobservedTaskException");
				};

			// Handle WPF UI thread exceptions
			// (The essence of System.Windows.Application.DispatcherUnhandledException)
			Dispatcher.CurrentDispatcher.UnhandledException +=
				delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
				{
					FL.Critical(e.Exception, "WPF.DispatcherUnhandledException", true);
					e.Handled = true;
				};
#endif
		}

#if !NET20
		/// <summary>
		/// Registers presentation trace handlers for a WPF application.
		/// </summary>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void RegisterPresentationTracing()
		{
			// Listen for events on all WPF trace sources
			FieldLogTraceListener.Start();
		}
#endif

		private static void DefaultShowAppErrorDialog(FieldLogExceptionItem exItem, bool allowContinue)
		{
#if DEBUG
			System.Diagnostics.Trace.Write(exItem.Exception.Exception.ToString());
#endif

			if (!Environment.UserInteractive)
			{
				// There is no user who could read the message or even decide whether to continue
				// or not. Just exit here.
				Shutdown();
				Environment.Exit(1);
				return;
			}

			// Safety timer to terminate the process if user feedback is unexpected
			System.Threading.Timer timer = new System.Threading.Timer(
				delegate(object state)
				{
					FL.Warning("Process waiting for user feedback terminated by safety timer.");
					Shutdown();
					Environment.Exit(1);
				},
				null,
				AppErrorTerminateTimeout * 1000,
				Timeout.Infinite);
			
			string msg;
			if (allowContinue)
			{
				if (IsInteractiveConsoleApp)
				{
					msg = AppErrorDialogContinuableConsole;
				}
				else
				{
					msg = AppErrorDialogContinuableGui;
				}
			}
			else
			{
				msg = AppErrorDialogTerminating;
			}
			msg += "\n\n";

			msg += ExceptionUserMessageRecursive(exItem.Exception);
			if (!string.IsNullOrEmpty(exItem.Context))
			{
				msg += AppErrorDialogContext + " " + exItem.Context + "\n";
			}
			msg += "\n";

			// Wait max. 1 second for the log file path to be set
			int pathRetry = 20;
			while (LogFileBasePath == null && logFileBasePathSet == false && pathRetry-- > 0)
			{
				Thread.Sleep(50);
			}
			
			if (LogFileBasePath != null)
			{
				msg += string.Format(AppErrorDialogLogPath, logFileBasePath + "*.fl");
			}
			else
			{
				msg += AppErrorDialogNoLog;
			}
			msg += "\n\n";

			msg += AppErrorDialogNote;

			// TODO: Offer starting external log submit tool

			if (IsInteractiveConsoleApp)
			{
				ConsoleColor foreColor = Console.ForegroundColor;
				ConsoleColor backColor = Console.BackgroundColor;

				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.Red;
				string appName = AppName;
				if (!string.IsNullOrEmpty(appName))
				{
					Console.Error.Write(appName);
					Console.Error.Write(" - ");
				}
				Console.Error.WriteLine(AppErrorDialogTitle);
				Console.Error.WriteLine(msg);

				if (allowContinue)
				{
					Console.ForegroundColor = ConsoleColor.White;
					Console.Error.WriteLine(AppErrorDialogConsoleAction);
					Console.Error.Write(string.Format(AppErrorDialogTimerNote, AppErrorTerminateTimeout));
				}

				Console.ForegroundColor = foreColor;
				Console.BackgroundColor = backColor;

				if (allowContinue)
				{
					while (true)
					{
						ConsoleKeyInfo key = Console.ReadKey(true);
						if (key.Key == ConsoleKey.Enter)
						{
							// Cancel the timer so that the process will not be terminated
							timer.Change(Timeout.Infinite, Timeout.Infinite);
							timer.Dispose();
							break;
						}
						if (key.Key == ConsoleKey.Escape)
						{
							Console.Error.WriteLine();
							Shutdown();
							Environment.Exit(1);
						}
					}
					Console.Error.WriteLine();
				}
				else
				{
					Shutdown();
					Environment.Exit(1);
				}
			}
			else if (allowContinue)
			{
				string title = AppErrorDialogTitle;
				string appName = AppName;
				if (!string.IsNullOrEmpty(appName))
				{
					title = appName + " – " + title;
				}

				msg += "\n\n" + string.Format(AppErrorDialogTimerNote, AppErrorTerminateTimeout);

				if (System.Windows.Forms.MessageBox.Show(
					msg,
					title,
					System.Windows.Forms.MessageBoxButtons.OKCancel,
					System.Windows.Forms.MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.OK)
				{
					// Cancel the timer so that the process will not be terminated
					timer.Change(Timeout.Infinite, Timeout.Infinite);
					timer.Dispose();
				}
				else
				{
					Shutdown();
					Environment.Exit(1);
				}
			}
			else
			{
				System.Windows.Forms.MessageBox.Show(
					msg,
					AppErrorDialogTitle,
					System.Windows.Forms.MessageBoxButtons.OK,
					System.Windows.Forms.MessageBoxIcon.Error);
				// Prevent Windows' application error dialog to appear and exit the process now
				Shutdown();
				Environment.Exit(1);
			}
		}

		/// <summary>
		/// Formats the message text of an exception and all inner exceptions for display in a user
		/// dialog.
		/// </summary>
		/// <param name="ex">The exception to format.</param>
		/// <returns>The formatted text for <paramref name="ex"/>.</returns>
		public static string ExceptionUserMessageRecursive(FieldLogException ex)
		{
			return ExceptionUserMessageRecursive(ex, 0);
		}

		private static string ExceptionUserMessageRecursive(FieldLogException ex, int level)
		{
			string msg;
			if (level == 0)
			{
#if !NET20
				AggregateException aggEx = ex.Exception as AggregateException;
				if (aggEx != null && aggEx.InnerExceptions.Count == 1)
				{
					// Simplify AggregateExceptions with a single InnerException
					return ExceptionUserMessageRecursive(ex.InnerExceptions[0], 0);
				}
				else
				{
#endif
					msg = ex.Message + " (" + ex.Type + ")\n";
#if !NET20
				}
#endif
			}
			else
			{
				msg = new string(' ', (level - 1) * 4) + "> " + ex.Message + " (" + ex.Type + ")\n";
			}
			if (ex.InnerExceptions != null)
			{
				foreach (FieldLogException inner in ex.InnerExceptions)
				{
					msg += ExceptionUserMessageRecursive(inner, level + 1);
				}
			}
			return msg;
		}

		#endregion Application error handling

		#region Text log methods for each priority

		/// <summary>
		/// Writes a text log item with Trace priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		public static void Trace(string text)
		{
			Text(FieldLogPriority.Trace, text);
		}

		/// <summary>
		/// Writes a text log item with Checkpoint priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		public static void Checkpoint(string text)
		{
			Text(FieldLogPriority.Checkpoint, text);
		}

		/// <summary>
		/// Writes a text log item with Info priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		public static void Info(string text)
		{
			Text(FieldLogPriority.Info, text);
		}

		/// <summary>
		/// Writes a text log item with Notice priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		public static void Notice(string text)
		{
			Text(FieldLogPriority.Notice, text);
		}

		/// <summary>
		/// Writes a text log item with Warning priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		public static void Warning(string text)
		{
			Text(FieldLogPriority.Warning, text);
		}

		/// <summary>
		/// Writes a text log item with Error priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		public static void Error(string text)
		{
			Text(FieldLogPriority.Error, text);
		}

		/// <summary>
		/// Writes a text log item with Critical priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		public static void Critical(string text)
		{
			Text(FieldLogPriority.Critical, text);
		}

		/// <summary>
		/// Writes a text log item with Trace priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		public static void Trace(string text, string details)
		{
			Text(FieldLogPriority.Trace, text, details);
		}

		/// <summary>
		/// Writes a text log item with Checkpoint priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		public static void Checkpoint(string text, string details)
		{
			Text(FieldLogPriority.Checkpoint, text, details);
		}

		/// <summary>
		/// Writes a text log item with Info priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		public static void Info(string text, string details)
		{
			Text(FieldLogPriority.Info, text, details);
		}

		/// <summary>
		/// Writes a text log item with Notice priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		public static void Notice(string text, string details)
		{
			Text(FieldLogPriority.Notice, text, details);
		}

		/// <summary>
		/// Writes a text log item with Warning priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		public static void Warning(string text, string details)
		{
			Text(FieldLogPriority.Warning, text, details);
		}

		/// <summary>
		/// Writes a text log item with Error priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		public static void Error(string text, string details)
		{
			Text(FieldLogPriority.Error, text, details);
		}

		/// <summary>
		/// Writes a text log item with Critical priority to the log file.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		public static void Critical(string text, string details)
		{
			Text(FieldLogPriority.Critical, text, details);
		}

		#endregion Text log methods for each priority

		#region Data log methods for each priority

		/// <summary>
		/// Writes a data log item with Trace priority to the log file.
		/// </summary>
		/// <param name="name">The name of the data item.</param>
		/// <param name="value">The value of the data item.</param>
		public static void TraceData(string name, object value)
		{
			Data(FieldLogPriority.Trace, name, value);
		}

		/// <summary>
		/// Writes a data log item with Checkpoint priority to the log file.
		/// </summary>
		/// <param name="name">The name of the data item.</param>
		/// <param name="value">The value of the data item.</param>
		public static void CheckpointData(string name, object value)
		{
			Data(FieldLogPriority.Checkpoint, name, value);
		}

		/// <summary>
		/// Writes a data log item with Info priority to the log file.
		/// </summary>
		/// <param name="name">The name of the data item.</param>
		/// <param name="value">The value of the data item.</param>
		public static void InfoData(string name, object value)
		{
			Data(FieldLogPriority.Info, name, value);
		}

		/// <summary>
		/// Writes a data log item with Notice priority to the log file.
		/// </summary>
		/// <param name="name">The name of the data item.</param>
		/// <param name="value">The value of the data item.</param>
		public static void NoticeData(string name, object value)
		{
			Data(FieldLogPriority.Notice, name, value);
		}

		/// <summary>
		/// Writes a data log item with Warning priority to the log file.
		/// </summary>
		/// <param name="name">The name of the data item.</param>
		/// <param name="value">The value of the data item.</param>
		public static void WarningData(string name, object value)
		{
			Data(FieldLogPriority.Warning, name, value);
		}

		/// <summary>
		/// Writes a data log item with Error priority to the log file.
		/// </summary>
		/// <param name="name">The name of the data item.</param>
		/// <param name="value">The value of the data item.</param>
		public static void ErrorData(string name, object value)
		{
			Data(FieldLogPriority.Error, name, value);
		}

		/// <summary>
		/// Writes a data log item with Critical priority to the log file.
		/// </summary>
		/// <param name="name">The name of the data item.</param>
		/// <param name="value">The value of the data item.</param>
		public static void CriticalData(string name, object value)
		{
			Data(FieldLogPriority.Critical, name, value);
		}

		#endregion Data log methods for each priority

		#region Exception log methods for each priority

		/// <summary>
		/// Writes an exception log item with Trace priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		public static void Trace(Exception ex)
		{
			Exception(FieldLogPriority.Trace, ex);
		}

		/// <summary>
		/// Writes an exception log item with Checkpoint priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		public static void Checkpoint(Exception ex)
		{
			Exception(FieldLogPriority.Checkpoint, ex);
		}

		/// <summary>
		/// Writes an exception log item with Info priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		public static void Info(Exception ex)
		{
			Exception(FieldLogPriority.Info, ex);
		}

		/// <summary>
		/// Writes an exception log item with Notice priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		public static void Notice(Exception ex)
		{
			Exception(FieldLogPriority.Notice, ex);
		}

		/// <summary>
		/// Writes an exception log item with Warning priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		public static void Warning(Exception ex)
		{
			Exception(FieldLogPriority.Warning, ex);
		}

		/// <summary>
		/// Writes an exception log item with Error priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		public static void Error(Exception ex)
		{
			Exception(FieldLogPriority.Error, ex);
		}

		/// <summary>
		/// Writes an exception log item with Critical priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		public static void Critical(Exception ex)
		{
			Exception(FieldLogPriority.Critical, ex);
		}

		/// <summary>
		/// Writes an exception log item with Trace priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		public static void Trace(Exception ex, string context)
		{
			Exception(FieldLogPriority.Trace, ex, context);
		}

		/// <summary>
		/// Writes an exception log item with Checkpoint priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		public static void Checkpoint(Exception ex, string context)
		{
			Exception(FieldLogPriority.Checkpoint, ex, context);
		}

		/// <summary>
		/// Writes an exception log item with Info priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		public static void Info(Exception ex, string context)
		{
			Exception(FieldLogPriority.Info, ex, context);
		}

		/// <summary>
		/// Writes an exception log item with Notice priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		public static void Notice(Exception ex, string context)
		{
			Exception(FieldLogPriority.Notice, ex, context);
		}

		/// <summary>
		/// Writes an exception log item with Warning priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		public static void Warning(Exception ex, string context)
		{
			Exception(FieldLogPriority.Warning, ex, context);
		}

		/// <summary>
		/// Writes an exception log item with Error priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		public static void Error(Exception ex, string context)
		{
			Exception(FieldLogPriority.Error, ex, context);
		}

		/// <summary>
		/// Writes an exception log item with Critical priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		public static void Critical(Exception ex, string context)
		{
			Exception(FieldLogPriority.Critical, ex, context);
		}

		/// <summary>
		/// Writes an exception log item with Trace priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		/// <param name="showUserDialog">true to show a user dialog about the application error.</param>
		public static void Trace(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Trace, ex, context, showUserDialog);
		}

		/// <summary>
		/// Writes an exception log item with Checkpoint priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		/// <param name="showUserDialog">true to show a user dialog about the application error.</param>
		public static void Checkpoint(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Checkpoint, ex, context, showUserDialog);
		}

		/// <summary>
		/// Writes an exception log item with Info priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		/// <param name="showUserDialog">true to show a user dialog about the application error.</param>
		public static void Info(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Info, ex, context, showUserDialog);
		}

		/// <summary>
		/// Writes an exception log item with Notice priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		/// <param name="showUserDialog">true to show a user dialog about the application error.</param>
		public static void Notice(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Notice, ex, context, showUserDialog);
		}

		/// <summary>
		/// Writes an exception log item with Warning priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		/// <param name="showUserDialog">true to show a user dialog about the application error.</param>
		public static void Warning(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Warning, ex, context, showUserDialog);
		}

		/// <summary>
		/// Writes an exception log item with Error priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		/// <param name="showUserDialog">true to show a user dialog about the application error.</param>
		public static void Error(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Error, ex, context, showUserDialog);
		}

		/// <summary>
		/// Writes an exception log item with Critical priority to the log file.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		/// <param name="showUserDialog">true to show a user dialog about the application error.</param>
		public static void Critical(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Critical, ex, context, showUserDialog);
		}

		#endregion Exception log methods for each priority

		#region Log methods with variable priority for each item type

		/// <summary>
		/// Writes a text log item to the log file.
		/// </summary>
		/// <param name="priority">The priority of the log item.</param>
		/// <param name="text">The text message.</param>
		public static void Text(FieldLogPriority priority, string text)
		{
			Log(new FieldLogTextItem(priority, text));
		}

		/// <summary>
		/// Writes a text log item to the log file.
		/// </summary>
		/// <param name="priority">The priority of the log item.</param>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		public static void Text(FieldLogPriority priority, string text, string details)
		{
			Log(new FieldLogTextItem(priority, text, details));
		}

		/// <summary>
		/// Writes a data log item to the log file.
		/// </summary>
		/// <param name="priority">The priority of the log item.</param>
		/// <param name="name">The name of the data item.</param>
		/// <param name="value">The value of the data item.</param>
		public static void Data(FieldLogPriority priority, string name, object value)
		{
			Log(new FieldLogDataItem(priority, name, value));
		}

		/// <summary>
		/// Writes an exception log item to the log file.
		/// </summary>
		/// <param name="priority">The priority of the log item.</param>
		/// <param name="ex">The exception instance.</param>
		public static void Exception(FieldLogPriority priority, Exception ex)
		{
			Log(new FieldLogExceptionItem(priority, ex));
		}

		/// <summary>
		/// Writes an exception log item to the log file.
		/// </summary>
		/// <param name="priority">The priority of the log item.</param>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		public static void Exception(FieldLogPriority priority, Exception ex, string context)
		{
			Log(new FieldLogExceptionItem(priority, ex, context));
		}

		/// <summary>
		/// Writes an exception log item to the log file.
		/// </summary>
		/// <param name="priority">The priority of the log item.</param>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		/// <param name="showUserDialog">true to show a user dialog about the application error.</param>
		public static void Exception(FieldLogPriority priority, Exception ex, string context, bool showUserDialog)
		{
			FieldLogExceptionItem item = new FieldLogExceptionItem(priority, ex, context);
			Log(item);
			if (showUserDialog)
			{
				ShowAppErrorDialog(item, item.Context != "AppDomain.UnhandledException");
			}
		}

		/// <summary>
		/// Writes an exception log item to the log file.
		/// </summary>
		/// <param name="priority">The priority of the log item.</param>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown.</param>
		/// <param name="customStackTrace">A StackTrace that shall be logged instead of the StackTrace from the Exception instance.</param>
		internal static void Exception(FieldLogPriority priority, Exception ex, string context, StackTrace customStackTrace)
		{
			Log(new FieldLogExceptionItem(priority, ex, context, customStackTrace));
		}

		#endregion Log methods with variable priority for each item type

		#region Scope log methods

		/// <summary>
		/// Writes a scope entering log item to the log file.
		/// </summary>
		/// <param name="name">The scope name. Should be application-unique and hierarchical for easier analysis.</param>
		public static void Enter(string name)
		{
			ScopeLevel++;
			Log(new FieldLogScopeItem(FieldLogScopeType.Enter, name));
		}

		/// <summary>
		/// Writes a scope leaving log item to the log file.
		/// </summary>
		/// <param name="name">The scope name. Should be the same as the corresponding Enter scope name.</param>
		public static void Leave(string name)
		{
			ScopeLevel--;
			Log(new FieldLogScopeItem(FieldLogScopeType.Leave, name));
		}

		/// <summary>
		/// Writes a scope log item to the log file.
		/// </summary>
		/// <param name="type">The scope type.</param>
		/// <param name="name">The scope name. Should be application-unique and hierarchical for easier analysis.</param>
		public static void LogScope(FieldLogScopeType type, string name)
		{
			FieldLogScopeItem scopeItem = new FieldLogScopeItem(type, name);
			if (type == FieldLogScopeType.LogStart)
			{
				LogScopeItem = scopeItem;
				Log(scopeItem);
			}
			else if (type == FieldLogScopeType.LogShutdown)
			{
				Log(scopeItem);
			}
			else if (type == FieldLogScopeType.ThreadStart)
			{
				Log(scopeItem);
			}
			else if (type == FieldLogScopeType.ThreadEnd)
			{
				Log(scopeItem);
			}
			else if (type == FieldLogScopeType.Enter)
			{
				ScopeLevel++;
				Log(scopeItem);
			}
			else if (type == FieldLogScopeType.Leave)
			{
				ScopeLevel--;
				Log(scopeItem);
			}
			else if (type == FieldLogScopeType.WebRequestStart)
			{
				throw new ArgumentException("Missing FieldLogWebRequestData instance. Use the other overloaded method", "type");
			}
			else if (type == FieldLogScopeType.WebRequestEnd)
			{
				Log(scopeItem);
				WebRequestId = 0;
			}
			else
			{
				throw new ArgumentException("Invalid scope type value.", "type");
			}
		}

		/// <summary>
		/// Writes a scope log item to the log file.
		/// </summary>
		/// <param name="type">The scope type.</param>
		/// <param name="name">The scope name. Should be application-unique and hierarchical for easier analysis.</param>
		/// <param name="webRequestData">The web request data. This parameter is required for the WebRequestStart scope type.</param>
		public static void LogScope(FieldLogScopeType type, string name, FieldLogWebRequestData webRequestData)
		{
			if (type == FieldLogScopeType.WebRequestStart)
			{
				// Interlocked.Increment is only available for Int32 but it handles the overflow, so
				// we can safely cast it to UInt32 to use the other half of the value space.
				WebRequestId = unchecked((uint) Interlocked.Increment(ref LastWebRequestId));
				FieldLogScopeItem scopeItem = new FieldLogScopeItem(FieldLogPriority.Trace, type, name, webRequestData);
				Log(scopeItem);
#if ASPNET
				// Sometimes a request is ended in a different thread than it was started. Keep a
				// backup copy of the web request ID value in the HttpContext for that case. As a
				// side effect, the value becomes available to the web application.
				if (HttpContext.Current != null)
				{
					HttpContext.Current.Items["FieldLog_WebRequestId"] = WebRequestId;
				}
#endif
			}
			else
			{
				throw new ArgumentException("Invalid scope type value for this overloaded method.", "type");
			}
		}

		#endregion Scope log methods

		#region General log method

		/// <summary>
		/// Writes a log item to the log file.
		/// </summary>
		/// <param name="item">The log item to write.</param>
		public static void Log(FieldLogItem item)
		{
			// First write any buffered items for the current thread
			if (threadBufferedItems != null)
			{
				foreach (var bufferedItem in threadBufferedItems)
				{
					LogInternal(bufferedItem);
				}
				// Clear buffer after writing it
				threadBufferedItems = null;
			}

			LogInternal(item);
		}

		private static void LogInternal(FieldLogItem item)
		{
			//System.Diagnostics.Trace.WriteLine("FieldLog.Log: New item " + item.ToString());
			// Add the item to the current buffer
			int size = item.Size;
			lock (currentBufferLock)
			{
				if (isShutdown)
				{
					System.Diagnostics.Trace.WriteLine("FieldLog warning: New messages are not accepted because the log queue has been shut down.");
					System.Diagnostics.Trace.WriteLine(item.ToString());
					return;
				}
				eventCounter++;
				item.EventCounter = eventCounter;
				CheckAddBuffer(size);
				currentBuffer.Add(item);
				currentBufferSize += size;
			}
			// Reset the send timeout
			sendTimeout.Change(200, Timeout.Infinite);
		}

		#endregion General log method

		#region Buffer log method

		/// <summary>
		/// Writes a log item to the buffer for the current thread. All buffered log items are only
		/// written to the log file if an unbuffered (normal) log item is written through another
		/// log method. The buffer can be cleared before it was written.
		/// </summary>
		/// <param name="item">The log item to add to the buffer.</param>
		public static void LogBuffer(FieldLogItem item)
		{
			if (threadBufferedItems == null)
			{
				threadBufferedItems = new List<FieldLogItem>();
			}
			threadBufferedItems.Add(item);
		}

		/// <summary>
		/// Clears the log items buffer for the current thread.
		/// </summary>
		public static void ClearLogBuffer()
		{
			threadBufferedItems = null;
		}

		#endregion Buffer log method

		#region Timeout log methods

		/// <summary>
		/// Writes a log item to the log file if the specified timeout expires.
		/// </summary>
		/// <param name="item">The log item to write.</param>
		/// <param name="milliseconds">The timeout in milliseconds.</param>
		/// <remarks>
		/// To cancel the timeout logging, call the Dispose method of the returned Timer instance.
		/// </remarks>
		public static Timer LogTimeout(FieldLogItem item, int milliseconds)
		{
			return new Timer(LogTimeoutCallback, item, milliseconds, Timeout.Infinite);
		}

		private static void LogTimeoutCallback(object item)
		{
			Log((FieldLogItem) item);
		}

		#endregion Timeout log methods

		#region Scope helpers

		/// <summary>
		/// Returns a new FieldLogScope item that implements IDisposable and can be used to log
		/// scopes with the <c>using</c> statement.
		/// </summary>
		/// <param name="name">The scope name.</param>
		/// <param name="values">Optional argument values for the scope.</param>
		/// <returns>A new <see cref="FieldLogScope"/> instance.</returns>
		/// <remarks>
		/// The <paramref name="values"/> parameter is any object whose public instance properties
		/// and fields will be dumped to a multi-line string in the log. The property and field
		/// names are determined through reflection.
		/// </remarks>
		public static FieldLogScope Scope(string name, object values = null)
		{
			FieldLogScope scope = new FieldLogScope(name);
			if (values != null)
			{
				TraceData("params", values);
			}
			return scope;
		}

		/// <summary>
		/// Returns a new FieldLogScope item that implements IDisposable and can be used to log
		/// scopes with the <c>using</c> statement. The calling method name is used as scope name.
		/// </summary>
		/// <param name="values">Optional argument values for the scope.</param>
		/// <returns>A new <see cref="FieldLogScope"/> instance.</returns>
		/// <remarks>
		/// The <paramref name="values"/> parameter is any object whose public instance properties
		/// and fields will be dumped to a multi-line string in the log. The property and field
		/// names are determined through reflection.
		/// </remarks>
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		public static FieldLogScope Scope(object values = null)
		{
			StackFrame sf = new StackFrame(1, false);
			string name = sf.GetMethod().Name;
			FieldLogScope scope = new FieldLogScope(name);
			if (values != null)
			{
				TraceData("params", values);
			}
			return scope;
		}

		/// <summary>
		/// Returns a new FieldLogThreadScope item that implements IDisposable and can be used to
		/// log thread scopes with the <c>using</c> statement.
		/// </summary>
		/// <param name="name">The thread scope name.</param>
		/// <returns>A new <see cref="FieldLogThreadScope"/> instance.</returns>
		public static FieldLogThreadScope ThreadScope(string name)
		{
			return new FieldLogThreadScope(name);
		}

#if !NET20
		/// <summary>
		/// Wraps an Action delegate in a FieldLogScope to mark its entering and returning.
		/// </summary>
		/// <param name="action">The action to execute.</param>
		/// <param name="name">The scope name. Defaults to the delegate's method name if not specified.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void ScopeAction(Action action, string name = null)
		{
			if (name == null)
			{
				name = action.Method.Name;
			}
			using (Scope(name))
			{
				action();
			}
		}

		/// <summary>
		/// Wraps an Action delegate in a FieldLogScope to mark its entering and returning.
		/// </summary>
		/// <param name="action">The action to execute.</param>
		/// <param name="arg1">The first argument passed to <paramref name="action"/>.</param>
		/// <param name="name">The scope name. Defaults to the delegate's method name if not specified.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void ScopeAction<T>(Action<T> action, T arg1, string name = null)
		{
			if (name == null)
			{
				name = action.Method.Name;
			}
			using (Scope(name, new { arg1 }))
			{
				action(arg1);
			}
		}

		/// <summary>
		/// Wraps an Action delegate in a FieldLogScope to mark its entering and returning.
		/// </summary>
		/// <param name="action">The action to execute.</param>
		/// <param name="arg1">The first argument passed to <paramref name="action"/>.</param>
		/// <param name="arg2">The second argument passed to <paramref name="action"/>.</param>
		/// <param name="name">The scope name. Defaults to the delegate's method name if not specified.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void ScopeAction<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, string name = null)
		{
			if (name == null)
			{
				name = action.Method.Name;
			}
			using (Scope(name, new { arg1, arg2 }))
			{
				action(arg1, arg2);
			}
		}

		/// <summary>
		/// Wraps a Func delegate in a FieldLogScope to mark its entering and returning.
		/// </summary>
		/// <param name="func">The function to execute.</param>
		/// <param name="name">The scope name. Defaults to the delegate's method name if not specified.</param>
		/// <returns>The return value of <paramref name="func"/>.</returns>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static TResult ScopeFunc<TResult>(Func<TResult> func, string name = null)
		{
			if (name == null)
			{
				name = func.Method.Name;
			}
			using (Scope(name))
			{
				return func();
			}
		}

		/// <summary>
		/// Wraps a Func delegate in a FieldLogScope to mark its entering and returning.
		/// </summary>
		/// <param name="func">The function to execute.</param>
		/// <param name="arg1">The first argument passed to <paramref name="func"/>.</param>
		/// <param name="name">The scope name. Defaults to the delegate's method name if not specified.</param>
		/// <returns>The return value of <paramref name="func"/>.</returns>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static TResult ScopeFunc<T, TResult>(Func<T, TResult> func, T arg1, string name = null)
		{
			if (name == null)
			{
				name = func.Method.Name;
			}
			using (Scope(name, new { arg1 }))
			{
				return func(arg1);
			}
		}

		/// <summary>
		/// Wraps a Func delegate in a FieldLogScope to mark its entering and returning.
		/// </summary>
		/// <param name="func">The function to execute.</param>
		/// <param name="arg1">The first argument passed to <paramref name="func"/>.</param>
		/// <param name="arg2">The second argument passed to <paramref name="func"/>.</param>
		/// <param name="name">The scope name. Defaults to the delegate's method name if not specified.</param>
		/// <returns>The return value of <paramref name="func"/>.</returns>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static TResult ScopeFunc<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2, string name = null)
		{
			if (name == null)
			{
				name = func.Method.Name;
			}
			using (Scope(name, new { arg1, arg2 }))
			{
				return func(arg1, arg2);
			}
		}
#endif

		#endregion Scope helpers

		#region Custom time measurement

		/// <summary>
		/// Starts a custom timer. If the key does not exist, a new timer is created.
		/// </summary>
		/// <param name="key">The custom timer key.</param>
		/// <param name="incrementCounter">Increment the counter value.</param>
		/// <returns>An instance which can be used to call the Start and Stop methods without a further key lookup.</returns>
		public static CustomTimerInfo StartTimer(string key, bool incrementCounter = true)
		{
			CustomTimerInfo cti;

#if NET20
			Monitor.Enter(customTimersLock);
#else
			customTimersLock.EnterReadLock();
#endif
			try
			{
				customTimers.TryGetValue(key, out cti);
			}
			finally
			{
#if NET20
				Monitor.Exit(customTimersLock);
#else
				customTimersLock.ExitReadLock();
#endif
			}

			if (cti == null)
			{
				// New key
				// The write lock must be outside the read lock
#if NET20
				Monitor.Enter(customTimersLock);
#else
				customTimersLock.EnterWriteLock();
#endif
				try
				{
					// Re-check because we have given up the lock shortly
					if (!customTimers.TryGetValue(key, out cti))
					{
						// Still a new key
						cti = new CustomTimerInfo(key);
						customTimers[key] = cti;
					}
				}
				finally
				{
#if NET20
					Monitor.Exit(customTimersLock);
#else
					customTimersLock.ExitWriteLock();
#endif
				}
			}

			// Last call for most precise measurement of the intended code
			cti.Start(incrementCounter);
			return cti;
		}

		/// <summary>
		/// Stops a custom timer.
		/// </summary>
		/// <param name="key">The custom timer key.</param>
		/// <param name="writeNow">true to write the timer value immediately, false for the normal delay.</param>
		public static void StopTimer(string key, bool writeNow = false)
		{
			CustomTimerInfo cti;

#if NET20
			Monitor.Enter(customTimersLock);
#else
			customTimersLock.EnterReadLock();
#endif
			try
			{
				if (!customTimers.TryGetValue(key, out cti))
				{
					throw new KeyNotFoundException("The custom timer \"" + key + "\" does not exist.");
				}
			}
			finally
			{
#if NET20
				Monitor.Exit(customTimersLock);
#else
				customTimersLock.ExitReadLock();
#endif
			}

			cti.Stop(writeNow);
		}

		/// <summary>
		/// Stops a custom timer and removes it from the dictionary.
		/// </summary>
		/// <param name="key">The custom timer key.</param>
		public static void ClearTimer(string key)
		{
			CustomTimerInfo cti;

#if NET20
			Monitor.Enter(customTimersLock);
#else
			customTimersLock.EnterWriteLock();
#endif
			try
			{
				if (customTimers.TryGetValue(key, out cti))
				{
					cti.Stop(true);
					cti.Dispose();
					customTimers.Remove(key);
				}
			}
			finally
			{
#if NET20
				Monitor.Exit(customTimersLock);
#else
				customTimersLock.ExitWriteLock();
#endif
			}
		}

		/// <summary>
		/// Returns a new CustomTimerScope item that implements IDisposable and can be used for
		/// time measuring with the <c>using</c> statement.
		/// </summary>
		/// <param name="key">The custom timer key.</param>
		/// <param name="incrementCounter">Increment the counter value.</param>
		/// <param name="writeImmediately">true to write the timer value immediately when stopping, false for the normal delay.</param>
		/// <returns>A new <see cref="CustomTimerScope"/> instance.</returns>
		public static CustomTimerScope Timer(string key, bool incrementCounter = true, bool writeImmediately = false)
		{
			return new CustomTimerScope(key, incrementCounter, writeImmediately);
		}

#if !NET20
		/// <summary>
		/// Wraps an Action delegate in a CustomTimerScope to measure the time that the action
		/// takes to execute.
		/// </summary>
		/// <param name="action">The action to execute.</param>
		/// <param name="key">The custom timer key. Defaults to the delegate's method name if not specified.</param>
		/// <param name="incrementCounter">Increment the counter value.</param>
		/// <param name="writeImmediately">true to write the timer value immediately when stopping, false for the normal delay.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void TimerAction(Action action, string key = null, bool incrementCounter = true, bool writeImmediately = false)
		{
			if (key == null)
			{
				key = action.Method.Name;
			}
			using (Timer(key, incrementCounter, writeImmediately))
			{
				action();
			}
		}

		/// <summary>
		/// Wraps an Action delegate in a CustomTimerScope to measure the time that the action
		/// takes to execute.
		/// </summary>
		/// <param name="action">The action to execute.</param>
		/// <param name="arg1">The first argument passed to <paramref name="action"/>.</param>
		/// <param name="key">The custom timer key. Defaults to the delegate's method name if not specified.</param>
		/// <param name="incrementCounter">Increment the counter value.</param>
		/// <param name="writeImmediately">true to write the timer value immediately when stopping, false for the normal delay.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void TimerAction<T>(Action<T> action, T arg1, string key = null, bool incrementCounter = true, bool writeImmediately = false)
		{
			if (key == null)
			{
				key = action.Method.Name;
			}
			using (Timer(key, incrementCounter, writeImmediately))
			{
				action(arg1);
			}
		}

		/// <summary>
		/// Wraps an Action delegate in a CustomTimerScope to measure the time that the action
		/// takes to execute.
		/// </summary>
		/// <param name="action">The action to execute.</param>
		/// <param name="arg1">The first argument passed to <paramref name="action"/>.</param>
		/// <param name="arg2">The second argument passed to <paramref name="action"/>.</param>
		/// <param name="key">The custom timer key. Defaults to the delegate's method name if not specified.</param>
		/// <param name="incrementCounter">Increment the counter value.</param>
		/// <param name="writeImmediately">true to write the timer value immediately when stopping, false for the normal delay.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void TimerAction<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, string key = null, bool incrementCounter = true, bool writeImmediately = false)
		{
			if (key == null)
			{
				key = action.Method.Name;
			}
			using (Timer(key, incrementCounter, writeImmediately))
			{
				action(arg1, arg2);
			}
		}

		/// <summary>
		/// Wraps a Func delegate in a CustomTimerScope to measure the time that the action takes
		/// to execute.
		/// </summary>
		/// <param name="func">The function to execute.</param>
		/// <param name="key">The custom timer key. Defaults to the delegate's method name if not specified.</param>
		/// <param name="incrementCounter">Increment the counter value.</param>
		/// <param name="writeImmediately">true to write the timer value immediately when stopping, false for the normal delay.</param>
		/// <returns>The return value of <paramref name="func"/>.</returns>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static TResult TimerFunc<TResult>(Func<TResult> func, string key = null, bool incrementCounter = true, bool writeImmediately = false)
		{
			if (key == null)
			{
				key = func.Method.Name;
			}
			using (Timer(key, incrementCounter, writeImmediately))
			{
				return func();
			}
		}

		/// <summary>
		/// Wraps a Func delegate in a CustomTimerScope to measure the time that the action takes
		/// to execute.
		/// </summary>
		/// <param name="func">The function to execute.</param>
		/// <param name="arg1">The first argument passed to <paramref name="func"/>.</param>
		/// <param name="key">The custom timer key. Defaults to the delegate's method name if not specified.</param>
		/// <param name="incrementCounter">Increment the counter value.</param>
		/// <param name="writeImmediately">true to write the timer value immediately when stopping, false for the normal delay.</param>
		/// <returns>The return value of <paramref name="func"/>.</returns>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static TResult TimerFunc<T, TResult>(Func<T, TResult> func, T arg1, string key = null, bool incrementCounter = true, bool writeImmediately = false)
		{
			if (key == null)
			{
				key = func.Method.Name;
			}
			using (Timer(key, incrementCounter, writeImmediately))
			{
				return func(arg1);
			}
		}

		/// <summary>
		/// Wraps a Func delegate in a CustomTimerScope to measure the time that the action takes
		/// to execute.
		/// </summary>
		/// <param name="func">The function to execute.</param>
		/// <param name="arg1">The first argument passed to <paramref name="func"/>.</param>
		/// <param name="arg2">The second argument passed to <paramref name="func"/>.</param>
		/// <param name="key">The custom timer key. Defaults to the delegate's method name if not specified.</param>
		/// <param name="incrementCounter">Increment the counter value.</param>
		/// <param name="writeImmediately">true to write the timer value immediately when stopping, false for the normal delay.</param>
		/// <returns>The return value of <paramref name="func"/>.</returns>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static TResult TimerFunc<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2, string key = null, bool incrementCounter = true, bool writeImmediately = false)
		{
			if (key == null)
			{
				key = func.Method.Name;
			}
			using (Timer(key, incrementCounter, writeImmediately))
			{
				return func(arg1, arg2);
			}
		}
#endif

		#endregion Custom time measurement

		#region WPF Dispatcher log methods

#if !NET20
		/// <summary>
		/// Writes a text log item to the log file after the WPF dispatcher has processed other
		/// queued events of the specified dispatcher priority.
		/// </summary>
		/// <param name="dispPriority">The WPF dispatcher priority to schedule the log message with.</param>
		/// <param name="logPriority">The priority of the log item.</param>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void TextOnDispatcherPriority(DispatcherPriority dispPriority, FieldLogPriority logPriority, string text, string details = null)
		{
			Dispatcher.CurrentDispatcher.BeginInvoke(new Action<FieldLogPriority, string, string>(Text), dispPriority, logPriority, text, details);
		}

		/// <summary>
		/// Writes a text log item with Trace priority to the log file after the WPF dispatcher has
		/// processed other queued events of the specified dispatcher priority.
		/// </summary>
		/// <param name="priority">The WPF dispatcher priority to schedule the log message with.</param>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void TraceOnDispatcherPriority(DispatcherPriority priority, string text, string details = null)
		{
			Dispatcher.CurrentDispatcher.BeginInvoke(new Action<string, string>(Trace), priority, text, details);
		}

		/// <summary>
		/// Writes a text log item with Trace priority to the log file after the WPF dispatcher has
		/// processed other queued events of Background priority.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void TraceOnBackground(string text, string details = null)
		{
			TraceOnDispatcherPriority(DispatcherPriority.Background, text, details);
		}

		/// <summary>
		/// Writes a text log item with Trace priority to the log file after the WPF dispatcher has
		/// processed other queued events of Input priority.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void TraceOnInput(string text, string details = null)
		{
			TraceOnDispatcherPriority(DispatcherPriority.Input, text, details);
		}

		/// <summary>
		/// Writes a text log item with Trace priority to the log file after the WPF dispatcher has
		/// processed other queued events of Loaded priority.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void TraceOnLoaded(string text, string details = null)
		{
			TraceOnDispatcherPriority(DispatcherPriority.Loaded, text, details);
		}

		/// <summary>
		/// Writes a text log item with Trace priority to the log file after the WPF dispatcher has
		/// processed other queued events of Render priority.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void TraceOnRender(string text, string details = null)
		{
			TraceOnDispatcherPriority(DispatcherPriority.Render, text, details);
		}

		/// <summary>
		/// Writes a text log item with Trace priority to the log file after the WPF dispatcher has
		/// processed other queued events of DataBind priority.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">The additional details of the log event.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void TraceOnDataBind(string text, string details = null)
		{
			TraceOnDispatcherPriority(DispatcherPriority.DataBind, text, details);
		}

		/// <summary>
		/// Stops a custom timer after the WPF dispatcher has processed other queued events of the
		/// specified dispatcher priority.
		/// </summary>
		/// <param name="priority">The WPF dispatcher priority to schedule the timer with.</param>
		/// <param name="key">The custom timer key.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void StopTimerOnDispatcherPriority(DispatcherPriority priority, string key)
		{
			Dispatcher.CurrentDispatcher.BeginInvoke(new Action<string, bool>(StopTimer), priority, key, false);
		}

		/// <summary>
		/// Starts a custom timer and stops it after the WPF dispatcher has processed other queued
		/// events of the specified dispatcher priority.
		/// </summary>
		/// <param name="priority">The WPF dispatcher priority to schedule the timer with.</param>
		/// <param name="key">The custom timer key.</param>
		/// <remarks>
		/// This method is not available in the NET20 build.
		/// </remarks>
		public static void TimerUntilDispatcherPriority(DispatcherPriority priority, string key)
		{
			Dispatcher disp = Dispatcher.CurrentDispatcher;
			if (disp == null)
			{
				throw new InvalidOperationException("There is no Dispatcher available in the current thread.");
			}
			CustomTimerInfo cti = StartTimer(key);
			disp.BeginInvoke(new Action<bool>(cti.Stop), priority, false);
		}
#endif

		#endregion WPF Dispatcher log methods

		#region ASP.NET log methods

#if ASPNET
		/// <summary>
		/// Starts logging for ASP.NET applications.
		/// </summary>
		private static void LogWebStart(Assembly callingAssembly)
		{
			if (EntryAssembly == null)
			{
				EntryAssembly = callingAssembly;
				if (EntryAssembly != null)
				{
					string binDir = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;
					string dllFileName = Path.GetFileName(EntryAssembly.Location);
					if (!string.IsNullOrEmpty(binDir) && !string.IsNullOrEmpty(dllFileName))
					{
						EntryAssemblyLocation = Path.Combine(binDir, dllFileName);
					}
				}
				// In ASP.NET, nothing must be changed in the bin directory, or the web application
				// is immediately unloaded to be restarted for the next request. This includes log
				// files and configuration that may be changed by the administrator.
				// Instead we use the App_Data\log directory as default. Anything in the App_Data
				// directory is not served out via HTTP so it should be secure from the public.
				string baseDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
				if (!string.IsNullOrEmpty(baseDir))
				{
					logDefaultDirOverride = Path.Combine(baseDir, "App_Data");
				}
			}
			if (configFileNameOverride == null)
			{
				// In ASP.NET, nothing must be changed in the bin directory, or the web application
				// is immediately unloaded to be restarted for the next request. This includes log
				// files and configuration that may be changed by the administrator.
				// Instead we put our config file where the common Web.config file is, in the
				// application base directory, and use the same prefix: web.flconfig
				string baseDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
				if (!string.IsNullOrEmpty(baseDir))
				{
					configFileNameOverride = Path.Combine(baseDir, "web" + logConfigExtension);
					lock (sendThread)
					{
						configChanged = true;
					}
				}
			}

			// Log another LogStart scope item. If the previous item is still in the send buffer,
			// it will be replaced with the new item.
			LogScope(FieldLogScopeType.LogStart, null);
		}

		/// <summary>
		/// Writes a web request start scope log item to the log file.
		/// </summary>
		/// <param name="useSession">true to access the Session, false to leave it alone.</param>
		public static void LogWebRequestStart(bool useSession = true)
		{
			LogWebRequestStart(useSession, null, null);
		}

		/// <summary>
		/// Writes a web request start scope log item to the log file.
		/// </summary>
		/// <param name="useSession">true to access the Session, false to leave it alone.</param>
		/// <param name="appUserId">The application-specific user ID, if available.</param>
		/// <param name="appUserName">The application-specific user name, if available.</param>
		public static void LogWebRequestStart(bool useSession, string appUserId, string appUserName)
		{
			FieldLogWebRequestData wrd = new FieldLogWebRequestData();
			wrd.RequestUrl = HttpContext.Current.Request.Url.ToString();
			wrd.Method = HttpContext.Current.Request.HttpMethod;
			wrd.ClientAddress = HttpContext.Current.Request.UserHostAddress;
			wrd.ClientHostName = HttpContext.Current.Request.UserHostName;
			wrd.Referrer = HttpContext.Current.Request.UrlReferrer != null ? HttpContext.Current.Request.UrlReferrer.ToString() : null;
			wrd.UserAgent = HttpContext.Current.Request.UserAgent;
			wrd.AcceptLanguages = HttpContext.Current.Request.UserLanguages != null ? HttpContext.Current.Request.UserLanguages.Aggregate((a, b) => a + "," + b) : null;
			wrd.Accept = HttpContext.Current.Request.AcceptTypes != null ? HttpContext.Current.Request.AcceptTypes.Aggregate((a, b) => a + "," + b) : null;
			if (useSession)
			{
				try
				{
					wrd.WebSessionId = HttpContext.Current.Session != null ? HttpContext.Current.Session.SessionID : null;
				}
				catch
				{
					// Sometimes it just throws. Ignore it.
				}
			}
			wrd.AppUserId = appUserId;
			wrd.AppUserName = appUserName;

			LogScope(FieldLogScopeType.WebRequestStart, null, wrd);
		}

		/// <summary>
		/// Writes a web request end scope log item to the log file.
		/// </summary>
		public static void LogWebRequestEnd()
		{
			LogScope(FieldLogScopeType.WebRequestEnd, null);
		}

		/// <summary>
		/// Writes the HTTP POST data sent from the client to the log file, if the request method is
		/// "POST".
		/// </summary>
		public static void LogWebPostData()
		{
			if (HttpContext.Current.Request.HttpMethod == "POST")
			{
				TraceData("POST data", HttpContext.Current.Request.Form);
			}
		}
#endif

		#endregion ASP.NET log methods

		#region Item buffer methods

		/// <summary>
		/// Checks the current buffer size and adds it to the sending queue and creates a new one.
		/// </summary>
		/// <param name="addSize">Size of the next item to add. If 0, the current buffer is always added for sending.</param>
		private static void CheckAddBuffer(int addSize)
		{
			List<FieldLogItem> myBuffer;
			lock (currentBufferLock)
			{
				// Check maximum buffer size
				if (addSize != 0 && currentBufferSize + addSize <= maxBufferSize) return;
				// Check used buffer
				if (currentBufferSize == 0) return;
				// Get the current buffer and assign a new one
				myBuffer = currentBuffer;
				// Create new buffer large enough for the current number of items
				currentBuffer = new List<FieldLogItem>(myBuffer.Count * 2);
				currentBufferSize = 0;
			}
			// Clear the send timeout
			sendTimeout.Change(Timeout.Infinite, Timeout.Infinite);
			// Enqueue the current buffer for sending
			lock (buffers)
			{
				buffers.Enqueue(myBuffer);
			}
			newBufferEvent.Set();

			if (WaitForItemsBacklog)
			{
				while (WriteItemsBacklog > 10000)
				{
					Thread.Sleep(10);
				}
			}
		}

		/// <summary>
		/// Sends all remaining buffers regardless of their size or the regular timeout. Buffers
		/// already dequeued by the send thread cannot be flushed.
		/// </summary>
		public static void Flush()
		{
			CheckAddBuffer(0);
		}

		/// <summary>
		/// Shuts down the log queue. All remaining items will be sent but no new items will be
		/// accepted.
		/// </summary>
		public static void Shutdown()
		{
			lock (shutdownLock)
			{
				if (isShutdown) return;

				LogScope(FieldLogScopeType.LogShutdown, null);
				isShutdown = true;
			}

			// Prevent further log items to be added from now on
			lock (currentBufferLock)
			{
				// Send all remaining buffers
				Flush();
				// Shut down the send thread
				lock (sendThread)
				{
					sendThreadCancellationPending = true;
				}
				newBufferEvent.Set();
				sendThread.Join();
			}

			// Close all open log files
			foreach (FieldLogFileWriter writer in priorityLogWriters.Values)
			{
				writer.Dispose();
			}

			// Unregister some events that are no longer needed now
			AppDomain.CurrentDomain.ProcessExit -= AppDomain_ProcessExit;
			AppDomain.CurrentDomain.DomainUnload -= AppDomain_DomainUnload;
		}

		/// <summary>
		/// Send timeout handler that adds the current buffer for sending even before it has
		/// reached its maximum size.
		/// </summary>
		/// <param name="state">Unused.</param>
		/// <remarks>
		/// This method is called on a pool thread.
		/// </remarks>
		private static void OnSendTimeout(object state)
		{
			CheckAddBuffer(0);
		}

		#endregion Item buffer methods

		#region Send thread

		/// <summary>
		/// Background thread function that waits for new buffers to send them.
		/// </summary>
		private static void SendThread()
		{
			while (true)
			{
				if (newBufferEvent.WaitOne())
				{
					SendBuffers();
				}
				bool localConfigChanged = false;
				lock (sendThread)
				{
					if (sendThreadCancellationPending) break;
					if (configChanged)
					{
						// Close all files and re-read the configuration file
						foreach (FieldLogFileWriter writer in priorityLogWriters.Values)
						{
							writer.Dispose();
						}
						priorityLogWriters.Clear();
						// Forget the current path to have it newly determined next time
						logFileBasePathSet = false;
						// Don't clear the value of customLogFileBasePath so that any previously
						// set value through code will remain in effect, but only reset the lock
						// that would prevent it from being set again (normally because the send
						// thread is currently using it - but now we're in the send thread and
						// everything is nicely synchronised).
						bool wasSet = customLogFileBasePathSet;
						customLogFileBasePathSet = false;
						// Now read the new configuration
						ReadLogConfiguration();
						// If the custom path was set before this event, set it again so that
						// TestLogPaths isn't blocked by it.
						customLogFileBasePathSet = wasSet;
						// Determine log path now so that LogFileBasePath returns something
						// immediately, not only after writing new items to the files.
						TestLogPaths();
						// Reset the flag, still in the lock before it could be set again
						configChanged = false;
						localConfigChanged = true;
					}
				}
				if (localConfigChanged)
				{
					FL.Trace("FieldLog configuration file re-read");
				}
			}
			// Send remaining buffers
			SendBuffers();
		}

		/// <summary>
		/// Dequeues all enqueued buffers and sends them.
		/// </summary>
		private static void SendBuffers()
		{
			//System.Diagnostics.Trace.WriteLine("FieldLog.SendBuffers");
			// Move all available buffers to a local list
			lock (buffers)
			{
				while (buffers.Count > 0)
				{
					buffersToSend.Add(buffers.Dequeue());
				}
			}
			// Write all collected buffers to the log file
			if (buffersToSend.Count > 0)
			{
				//System.Diagnostics.Trace.WriteLine("FieldLog.SendBuffers: Have " + buffersToSend.Count + " new buffers to send");
				if (TestLogPaths())
				{
					//System.Diagnostics.Trace.WriteLine("FieldLog.SendBuffers: Log path is OK");
					// Only write the complete buffers if a log file path has been determined.
					// This can be a valid path, or null if we know that no log files can be
					// written. Don't try to write anywhere and don't discard any buffers until
					// the log file path has been set by the application.
					
					int itemCount = 0;
					foreach (List<FieldLogItem> buffer in buffersToSend)
					{
						itemCount += buffer.Count;
					}
					WriteItemsBacklog = itemCount;

					foreach (List<FieldLogItem> buffer in buffersToSend)
					{
						AppendLogItemsToFile(buffer);
					}
					buffersToSend.Clear();

					// Delete outdated files
					PurgeAllFiles();

					// Clear the backlog counter after the work to avoid dead-locks while waiting
					// for it
					WriteItemsBacklog = 0;
				}
			}
		}

		#endregion Send thread

		#region File writing

		/// <summary>
		/// Sets an application-defined prefix for the log files. The default is the file name of
		/// the entry assembly without its extension.
		/// </summary>
		/// <param name="prefix">The new log file name prefix.</param>
		/// <remarks>
		/// This method must be called before AcceptLogFileBasePath. SetCustomLogFileBasePath
		/// effectively overwrites the file prefix so both methods cannot be used together.
		/// </remarks>
		public static void SetCustomLogFilePrefix(string prefix)
		{
			if (prefix == null) throw new ArgumentNullException("prefix");

#if ASPNET
			LogWebStart(Assembly.GetCallingAssembly());
#endif

			lock (customLogPathLock)
			{
				if (customLogFileBasePathSet)
					throw new InvalidOperationException("The custom log file path has already been set.");

				customLogFilePrefix = prefix;
			}
		}

		/// <summary>
		/// Sets an application-defined base path for writing log files to.
		/// </summary>
		/// <param name="path">Log file base path. This is an absolute path to a directory and a file name prefix.</param>
		/// <remarks>
		/// FieldLog tries to find a working path to write log files to automatically. The
		/// application can specify a custom path that will be tried before all automatic defaults.
		/// If this path is null or doesn't work, the other default paths are tested and the first
		/// working path will be used. No attempt to find a working path is made until this method
		/// or <see cref="AcceptLogFileBasePath"/> is called.
		/// </remarks>
		public static void SetCustomLogFileBasePath(string path)
		{
			if (path == null) throw new ArgumentNullException("path");

#if ASPNET
			LogWebStart(Assembly.GetCallingAssembly());
#endif

			lock (customLogPathLock)
			{
				if (customLogFileBasePathSet)
					throw new InvalidOperationException("The custom log file path has already been set.");

				customLogFileBasePath = path;
				customLogFileBasePathSet = true;
				TestLogPaths();
			}
		}

		/// <summary>
		/// Accepts FieldLogs default log path strategy or a path specified in the flconfig file.
		/// </summary>
		/// <remarks>
		/// Calling this method just tells FieldLog that the default paths are okay and logs shall
		/// be written there if not specified otherwise in the flconfig file. No attempt to find a
		/// working path is made until this method or <see cref="SetCustomLogFileBasePath"/> is
		/// called.
		/// </remarks>
		public static void AcceptLogFileBasePath()
		{
#if ASPNET
			LogWebStart(Assembly.GetCallingAssembly());
#endif

			lock (customLogPathLock)
			{
				customLogFileBasePathSet = true;
				TestLogPaths();
			}
		}

		/// <summary>
		/// Detects and stores a working path for writing log files to.
		/// </summary>
		/// <returns>true if a path has been set, false to defer writing to a later time.</returns>
		private static bool TestLogPaths()
		{
			if (logFileBasePathSet) return true;

			string execPath = EntryAssemblyLocation;
			string execFile = execPath != null ? Path.GetFileNameWithoutExtension(execPath) : null;
			if (logDefaultDirOverride != null)
			{
				if (EntryAssemblyLocation != null)
				{
					execFile = Path.GetFileNameWithoutExtension(EntryAssemblyLocation);
				}
				else
				{
					execFile = "web";
				}
				execPath = Path.Combine(logDefaultDirOverride, execFile);
			}
			if (customLogFilePrefix != null)
			{
				execFile = customLogFilePrefix;
			}
			int i = 1;
			while (true)
			{
				switch (i)
				{
					case 1:
						logFileBasePath = null;
						lock (customLogPathLock)
						{
							if (!customLogFileBasePathSet)
							{
								// Path selection is still unconfirmed
								lock (shutdownLock)
								{
									if (!isShutdown)
									{
										// Not shutting down, return telling to defer writing files
										return false;
									}
									// In shutdown, use default paths and find a path to write files
								}
							}
							logFileBasePath = customLogFileBasePath;
						}
						break;
					case 2:
						if (execPath == null || execFile == null)
						{
							// No entry assembly available, log path and prefix must be set manually
							return false;
						}
						// log subdirectory under the executable file
						logFileBasePath = Path.Combine(Path.GetDirectoryName(execPath), "log" + Path.DirectorySeparatorChar + execFile);
						break;
					case 3:
						// Same directory as the executable file
						logFileBasePath = Path.Combine(Path.GetDirectoryName(execPath), execFile);
						break;
					case 4:
						// Subdirectory in My Documents (if not in service account)
						logFileBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), execFile + "-log" + Path.DirectorySeparatorChar + execFile);
						if (logFileBasePath.ToLowerInvariant().StartsWith(Environment.GetEnvironmentVariable("windir").ToLowerInvariant()))
						{
							// (Environment.SpecialFolder.Windows is not available in .NET 2.0 but
							// the environment variable %windir% is just as good.)
							// This is a service account profile path somewhere hidden in the Windows
							// directory that cannot be easily accessed from other accounts.
							// Reject this path and try the next one instead.
							logFileBasePath = null;
						}
						break;
					case 5:
						// Subdirectory in Temp directory
						logFileBasePath = Path.Combine(Path.GetTempPath(), execFile + "-log" + Path.DirectorySeparatorChar + execFile);
						break;
					default:
						logFileBasePath = null;
						logFileBasePathSet = true;
						return true;
				}
				try
				{
					if (logFileBasePath == null) continue;

					// First try to create the directory, if it doesn't exist yet
					string logDir = Path.GetDirectoryName(logFileBasePath);
					Directory.CreateDirectory(logDir);

					// Then try to create a temporary file and delete it again if successful
					using (File.Open(logFileBasePath + ".fltmp", FileMode.Create))
					{
					}
					File.Delete(logFileBasePath + ".fltmp");

					logFileBasePathSet = true;
					Thread.MemoryBarrier();   // Ensure that logFileBasePathSet (and logFileBasePath) are current elsewhere

					System.Diagnostics.Trace.WriteLine("FieldLog info: Now writing to " + logFileBasePath);
					return true;
				}
#if DEBUG
				catch (Exception ex)
				{
					System.Diagnostics.Trace.WriteLine("FieldLog trace: Cannot write to " + logFileBasePath + ". " + ex.Message);
					// Uncomment the following line to trace log path finding in the web server
					//File.WriteAllText(@"c:\inetpub\wwwroot\test\App_Data\msg-" + FL.UtcNow.Ticks + "-error.txt", "execPath = " + execPath + "\n" + ex.ToString());
				}
#else
				catch
				{
					// Something went wrong, we can't use this path. Try the next one.
				}
#endif
				finally
				{
					i++;
				}
			}
		}

		private static void AppendLogItemsToFile(IEnumerable<FieldLogItem> logItems)
		{
			// If nothing should be kept, don't write anything
			if (maxTotalSize == 0)
			{
				return;
			}
			
			if (logFileBasePath == null) return;   // Nowhere to write the log items, drop them
			Dictionary<FieldLogFileWriter, bool> usedWriters = new Dictionary<FieldLogFileWriter, bool>();
			// NOTE: HashSet<T> would be better here, but it's not supported in .NET 2.0, so we're
			//       using a Dictionary instead. And it's just as fast anyway.

			// Remove duplicate LogStart scope items when writing items the first time.
			// For ASP.NET, a second LogStart item is generated when we have more information about
			// the running application, so the first item can be removed from the buffer.
			int skipLogStartItems = 0;
			if (!didDuplicateLogStartCheck)
			{
				foreach (FieldLogItem logItem in logItems)
				{
					FieldLogScopeItem scopeItem = logItem as FieldLogScopeItem;
					if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
					{
						skipLogStartItems++;
					}
				}
				didDuplicateLogStartCheck = true;
			}

			foreach (FieldLogItem logItem in logItems)
			{
				//System.Diagnostics.Trace.WriteLine("FieldLog.AppendLogItemsToFile: Process item " + logItem.ToString());
				// Update the list of currently open scope items to repeat them when a new log file
				// is started.
				FieldLogScopeItem scopeItem = logItem as FieldLogScopeItem;
				if (scopeItem != null)
				{
					if (scopeItem.Type == FieldLogScopeType.LogStart)
					{
						skipLogStartItems--;
						if (skipLogStartItems > 0)
						{
							// Removed by deduplication
							continue;
						}
					}
					else if (scopeItem.Type == FieldLogScopeType.Enter)
					{
						Stack<FieldLogScopeItem> stack;
						if (!CurrentScopes.TryGetValue(scopeItem.ThreadId, out stack))
						{
							stack = new Stack<FieldLogScopeItem>();
							CurrentScopes[scopeItem.ThreadId] = stack;
						}
						stack.Push(scopeItem);
					}
					else if (scopeItem.Type == FieldLogScopeType.Leave)
					{
						Stack<FieldLogScopeItem> stack;
						if (CurrentScopes.TryGetValue(scopeItem.ThreadId, out stack))
						{
							stack.Pop();
						}
					}
					else if (scopeItem.Type == FieldLogScopeType.ThreadStart)
					{
						ThreadScopes[scopeItem.ThreadId] = scopeItem;
					}
					else if (scopeItem.Type == FieldLogScopeType.ThreadEnd)
					{
						ThreadScopes.Remove(scopeItem.ThreadId);
					}
				}

				// The rest of this loop handles writing the item to some file. If the item
				// priority's keep time is zero, don't even save the item anywhere now.
				TimeSpan keepTime;
				if (priorityKeepTimes.TryGetValue(logItem.Priority, out keepTime))
				{
					if (keepTime.Ticks <= 0)
						continue;
				}

				// Determine the target file name based on the log item priority and the existing file's size
				FieldLogFileWriter writer;
				if (!priorityLogWriters.TryGetValue(logItem.Priority, out writer))
				{
					// Find previous file for this priority and below the maximum file size
					string logDir = Path.GetDirectoryName(logFileBasePath);
					string logFile = Path.GetFileName(logFileBasePath);
					DateTime latestFileTime = DateTime.MinValue;
					string latestFileName = null;
					foreach (string fileName in Directory.GetFiles(logDir, logFile + "-" + (int) logItem.Priority + "-*.fl"))
					{
						FileInfo fi = new FileInfo(fileName);
						if (fi.Length >= maxFileSize) continue;   // File is already large enough
						if (fi.CreationTime.Date < DateTime.Today) continue;   // File is from yesterday or older
						if (fi.CreationTimeUtc > latestFileTime)
						{
							// Remember the latest file
							latestFileName = fileName;
							latestFileTime = fi.CreationTimeUtc;
						}
					}
					if (latestFileName == null)
					{
						// No suitable file found, create a new one
						latestFileName = logFileBasePath + "-" + (int) logItem.Priority + "-" + FL.UtcNow.Ticks + ".fl";
					}

					bool needNewFile = false;
					try
					{
						writer = new FieldLogFileWriter(latestFileName, logItem.Priority);
					}
					catch (FormatException)
					{
						// Possibly a file with an older format version
						needNewFile = true;
					}
					catch (IOException)
					{
						needNewFile = true;
					}
					if (needNewFile)
					{
						Thread.Sleep(0);   // Ensure that a different file name will be used
						latestFileName = logFileBasePath + "-" + (int) logItem.Priority + "-" + FL.UtcNow.Ticks + ".fl";
						writer = new FieldLogFileWriter(latestFileName, logItem.Priority);
					}
					priorityLogWriters[logItem.Priority] = writer;
				}

				logItem.Write(writer);
				if (scopeItem != null)
				{
					scopeItem.WasWritten = true;
				}
				usedWriters[writer] = true;

				if (writer.Length > maxFileSize ||   // File is large enough
					writer.CreatedTime.ToLocalTime().Date < DateTime.Today)   // File is from yesterday or older
				{
					// This file is now large or old enough, close it
					writer.Dispose();
					priorityLogWriters.Remove(logItem.Priority);
					usedWriters.Remove(writer);
				}
			}

			foreach (FieldLogFileWriter writer in usedWriters.Keys)
			{
				//System.Diagnostics.Trace.WriteLine("FieldLog.AppendLogItemsToFile: Flush() " + Path.GetFileNameWithoutExtension(writer.FileName));
				writer.Flush();
			}
		}

		/// <summary>
		/// Purges log files of all priorities where necessary.
		/// </summary>
		private static void PurgeAllFiles()
		{
			// Purge each priority according to the configured minimum keep time
			foreach (FieldLogPriority prio in priorityKeepTimes.Keys)
			{
				PurgePriority(prio);
			}

			// Purge all files according to the configured maximum total file size
			DateTime now = DateTime.UtcNow;
			if (now <= totalSizeLastPurgeTime.AddSeconds(2))
			{
				return;   // No need to check again right now
			}
			totalSizeLastPurgeTime = now;

			string logDir = Path.GetDirectoryName(logFileBasePath);
			string logFile = Path.GetFileName(logFileBasePath);
			string[] fileNames = Directory.GetFiles(logDir, logFile + "-*-*.fl");
			DateTime[] fileTimes = new DateTime[fileNames.Length];
			long[] fileSizes = new long[fileNames.Length];
			long totalUsedSize = 0;
			for (int i = 0; i < fileNames.Length; i++)
			{
				FileInfo fi = new FileInfo(fileNames[i]);
				fileTimes[i] = fi.LastWriteTimeUtc;
				fileSizes[i] = FieldLogFileWriter.GetCompressedFileSize(fileNames[i]);
				totalUsedSize += fileSizes[i];

				// Check if this file is currently open
				foreach (var plw in priorityLogWriters.Values)
				{
					if (string.Equals(plw.FileName, fileNames[i], StringComparison.InvariantCultureIgnoreCase))
					{
						fileTimes[i] = DateTime.MaxValue;
						break;
					}
				}
			}
			while (totalUsedSize > maxTotalSize)
			{
				// Find oldest file
				int oldestIndex = -1;
				DateTime oldestTime = DateTime.MaxValue;
				for (int i = 0; i < fileTimes.Length; i++)
				{
					if (fileTimes[i] < oldestTime)
					{
						oldestTime = fileTimes[i];
						oldestIndex = i;
					}
				}
				if (oldestIndex == -1) break;   // Nothing more to delete

				// Delete the file and reduce the total size
				try
				{
					File.Delete(fileNames[oldestIndex]);
					totalUsedSize -= fileSizes[oldestIndex];
				}
				catch
				{
					// Try the next file
				}
				fileTimes[oldestIndex] = DateTime.MaxValue;   // Don't consider this file again
			}
		}

		/// <summary>
		/// Purges log files of the specified priority if necessary.
		/// </summary>
		/// <param name="prio"></param>
		private static void PurgePriority(FieldLogPriority prio)
		{
			// Collect relevant timing data
			DateTime lastPurgeTime;
			priorityLastPurgeTimes.TryGetValue(prio, out lastPurgeTime);
			TimeSpan keepTime;
			if (!priorityKeepTimes.TryGetValue(prio, out keepTime))
			{
				return;   // Don't purge this priority at all (should not happen)
			}

			// Determine whether it's time to check for old files
			DateTime now = DateTime.UtcNow;
			if (now <= lastPurgeTime.AddTicks(keepTime.Ticks / 4))
			{
				return;   // No need to check again right now
			}
			priorityLastPurgeTimes[prio] = now;
			
			// Evaluate all existing files for this priority
			string logDir = Path.GetDirectoryName(logFileBasePath);
			string logFile = Path.GetFileName(logFileBasePath);

			string currentFileName = null;
			FieldLogFileWriter fw;
			if (priorityLogWriters.TryGetValue(prio, out fw))
			{
				currentFileName = fw.FileName;
			}
			
			foreach (string fileName in Directory.GetFiles(logDir, logFile + "-" + (int) prio + "-*.fl"))
			{
				FileInfo fi = new FileInfo(fileName);
				if (!string.Equals(fileName, currentFileName, StringComparison.InvariantCultureIgnoreCase) &&
					fi.LastWriteTimeUtc < FL.UtcNow.Subtract(keepTime))
				{
					// File is not currently open for writing and old enough to be deleted
					try
					{
						File.Delete(fileName);
					}
					catch
					{
						// Retry next time
					}
				}
			}
		}

		#endregion File writing

		#region Log configuration

		/// <summary>
		/// Reads the log configuration from the file next to the executable file.
		/// </summary>
		private static void ReadLogConfiguration()
		{
			if (EntryAssemblyLocation == null && configFileNameOverride == null)
			{
				// No entry assembly file name available, config file not supported
				ResetLogConfiguration();
				return;
			}

			string configFileName = null;
			try
			{
				if (configFileNameOverride != null)
				{
					configFileName = configFileNameOverride;
				}
				else
				{
					string execPath = EntryAssemblyLocation;
					string execDir = Path.GetDirectoryName(execPath);
					string execFile = Path.GetFileNameWithoutExtension(execPath);
					configFileName = Path.Combine(execDir, execFile + logConfigExtension);
				}

				if (configFileWatcher == null)
				{
					configFileWatcher = new FileSystemWatcher(Path.GetDirectoryName(configFileName), Path.GetFileName(configFileName));
					configFileWatcher.Changed += configFileWatcher_Event;
					configFileWatcher.Created += configFileWatcher_Event;
					configFileWatcher.EnableRaisingEvents = true;
				}

				ResetLogConfiguration();

				if (!File.Exists(configFileName))
				{
					return;
				}

				string configLogPath = null;

				using (StreamReader sr = new StreamReader(configFileName, Encoding.Default, true))
				{
					while (!sr.EndOfStream)
					{
						string line = sr.ReadLine().Trim();

						string[] chunks = line.Split(new char[] { '=' }, 2);
						string item = chunks[0].Trim().ToLowerInvariant();
						string value = chunks.Length > 1 ? chunks[1].Trim() : null;

						if (value != null)
						{
							switch (item)
							{
								case "path":
									if (!string.IsNullOrEmpty(value))
									{
										if (!Path.IsPathRooted(value))
										{
											value = Path.Combine(Path.GetDirectoryName(configFileName), value);
										}
										configLogPath = value;
									}
									break;
								case "maxfilesize":
									long lng = ParseConfigNumber(value, maxFileSize);
									// Interpret too large value as maximum, don't ignore overflow
									// and get any meaningless (possibly negative) value
									if (lng <= int.MaxValue)
									{
										maxFileSize = (int) lng;
									}
									else
									{
										maxFileSize = int.MaxValue;
									}
									// Don't come near the technical limit of 2 GiB due to Int32 file addressing
									const int maxMaxFileSize = 1 * 1024 * 1024 * 1024; /* GiB */
									if (maxFileSize > maxMaxFileSize)
									{
										maxFileSize = maxMaxFileSize;
									}
									// Don't keep it too small so that repeated scopes (esp. LogStart) fit in
									const int minMaxFileSize = 50 * 1024; /* KiB */
									if (maxFileSize < minMaxFileSize)
									{
										maxFileSize = minMaxFileSize;
									}
									break;
								case "maxtotalsize":
									maxTotalSize = ParseConfigNumber(value, maxTotalSize);
									break;
								case "keeptrace":
									priorityKeepTimes[FieldLogPriority.Trace] = ParseConfigTimeSpan(value, priorityKeepTimes[FieldLogPriority.Trace]);
									break;
								case "keepcheckpoint":
									priorityKeepTimes[FieldLogPriority.Checkpoint] = ParseConfigTimeSpan(value, priorityKeepTimes[FieldLogPriority.Checkpoint]);
									break;
								case "keepinfo":
									priorityKeepTimes[FieldLogPriority.Info] = ParseConfigTimeSpan(value, priorityKeepTimes[FieldLogPriority.Info]);
									break;
								case "keepnotice":
									priorityKeepTimes[FieldLogPriority.Notice] = ParseConfigTimeSpan(value, priorityKeepTimes[FieldLogPriority.Notice]);
									break;
								case "keepwarning":
									priorityKeepTimes[FieldLogPriority.Warning] = ParseConfigTimeSpan(value, priorityKeepTimes[FieldLogPriority.Warning]);
									break;
								case "keeperror":
									priorityKeepTimes[FieldLogPriority.Error] = ParseConfigTimeSpan(value, priorityKeepTimes[FieldLogPriority.Error]);
									break;
								case "keepcritical":
									priorityKeepTimes[FieldLogPriority.Critical] = ParseConfigTimeSpan(value, priorityKeepTimes[FieldLogPriority.Critical]);
									break;
							}
						}
					}
				}

				if (configLogPath != null)
				{
					try
					{
						SetCustomLogFileBasePath(configLogPath);
					}
					catch
					{
						// Path already set elsewhere (close to impossible), ignore setting
					}
				}
			}
			catch (Exception ex)
			{
				// Something went really bad while reading the configuration file
				System.Diagnostics.Trace.WriteLine("FieldLog error: Reading configuration file " + (configFileName ?? "(null)"));
				System.Diagnostics.Trace.WriteLine(ex.ToString());
				// Try to log it as well
				try
				{
					FL.Warning(ex, "FieldLog.ReadLogConfiguration");
				}
				catch
				{
					// Bad luck...
				}
				// Set all values to default
				ResetLogConfiguration();
			}
		}

		private static void configFileWatcher_Event(object sender, FileSystemEventArgs e)
		{
			Thread.Sleep(500);
			lock (sendThread)
			{
				configChanged = true;
			}
		}

		/// <summary>
		/// Sets default values for the log configuration.
		/// </summary>
		private static void ResetLogConfiguration()
		{
			// A file size of 150 KiB gives a good compromise of granularity and flush time in the
			// benchmark.
			maxFileSize = 150 /*KiB*/ * 1024;
			maxTotalSize = 200L /*MiB*/ * 1024 * 1024;
			priorityKeepTimes.Clear();
			priorityKeepTimes[FieldLogPriority.Trace] = TimeSpan.FromHours(24);
			priorityKeepTimes[FieldLogPriority.Checkpoint] = TimeSpan.FromHours(24);
			priorityKeepTimes[FieldLogPriority.Info] = TimeSpan.FromDays(30);
			priorityKeepTimes[FieldLogPriority.Notice] = TimeSpan.FromDays(30);
			priorityKeepTimes[FieldLogPriority.Warning] = TimeSpan.FromDays(90);
			priorityKeepTimes[FieldLogPriority.Error] = TimeSpan.FromDays(90);
			priorityKeepTimes[FieldLogPriority.Critical] = TimeSpan.FromDays(90);
		}

		/// <summary>
		/// Parses a number description from the configuration file.
		/// </summary>
		/// <param name="value">A number value with an optional byte size suffix "k", "m" or "g" (case-insensitive).</param>
		/// <param name="defaultValue">The value to return if the value cannot be parsed or used.</param>
		/// <returns>The parsed value, or <paramref name="defaultValue"/> on error.</returns>
		private static long ParseConfigNumber(string value, long defaultValue)
		{
			value = value.Trim().ToLowerInvariant();
			Match m = Regex.Match(value, @"^([0-9]+)\s*(?:([kmg])b?)?$");
			if (m.Success)
			{
				long num = long.Parse(m.Groups[1].Value);
				string factor = m.Groups[2].Value;
				if (factor == "k")
				{
					num *= 1024;
				}
				else if (factor == "m")
				{
					num *= 1024 * 1024;
				}
				else if (factor == "g")
				{
					num *= 1024 * 1024 * 1024;
				}
				return num;
			}
			System.Diagnostics.Trace.WriteLine("FieldLog warning: Invalid size format in configuration file");
			// Try to log it as well
			try
			{
				FL.Warning("FieldLog configuration: Invalid size format", "Value provided: " + value + "\nDefault value: " + defaultValue);
			}
			catch
			{
				// Bad luck...
			}
			return defaultValue;
		}

		/// <summary>
		/// Parses a timespan description from the configuration file.
		/// </summary>
		/// <param name="value">A number value with an optional time unit suffix "s", "m", "h" or "d" (case-insensitive).</param>
		/// <param name="defaultValue">The value to return if the value cannot be parsed or used.</param>
		/// <returns>The parsed value, or <paramref name="defaultValue"/> on error.</returns>
		private static TimeSpan ParseConfigTimeSpan(string value, TimeSpan defaultValue)
		{
			value = value.Trim().ToLowerInvariant();
			Match m = Regex.Match(value, @"^([0-9]+)\s*([smhd])?$");
			if (m.Success)
			{
				int num = int.Parse(m.Groups[1].Value);
				string factor = m.Groups[2].Value;
				if (factor == "s")
				{
					return TimeSpan.FromSeconds(num);
				}
				else if (factor == "m")
				{
					return TimeSpan.FromMinutes(num);
				}
				else if (factor == "h")
				{
					return TimeSpan.FromHours(num);
				}
				else if (factor == "d")
				{
					return TimeSpan.FromDays(num);
				}
			}
			m = Regex.Match(value, @"^0+$");
			if (m.Success)
			{
				return TimeSpan.Zero;
			}
			System.Diagnostics.Trace.WriteLine("FieldLog warning: Invalid time format in configuration file");
			// Try to log it as well
			try
			{
				FL.Warning("FieldLog configuration: Invalid time format", "Value provided: " + value + "\nDefault value: " + defaultValue);
			}
			catch
			{
				// Bad luck...
			}
			return defaultValue;
		}

		#endregion Log configuration

		#region Environment data

		/// <summary>
		/// Gets the uptime of the application.
		/// </summary>
		public static TimeSpan AppUptime
		{
			get
			{
				return stopwatch.Elapsed;
			}
		}

		/// <summary>
		/// Gets the version string of the current application from the
		/// AssemblyInformationalVersionAttribute, AssemblyVersionAttribute or
		/// AssemblyFileVersionAttribute value, or null if none is set.
		/// </summary>
		public static string AppVersion
		{
			get
			{
				if (EntryAssembly == null)
				{
					return null;
				}
				object[] customAttributes = EntryAssembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyInformationalVersionAttribute) customAttributes[0]).InformationalVersion;
				}
				customAttributes = EntryAssembly.GetCustomAttributes(typeof(AssemblyVersionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyVersionAttribute) customAttributes[0]).Version;
				}
				customAttributes = EntryAssembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyFileVersionAttribute) customAttributes[0]).Version;
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the assembly configuration of the current application from the
		/// AssemblyConfigurationAttribute value, or null if none is set.
		/// </summary>
		public static string AppAsmConfiguration
		{
			get
			{
				if (EntryAssembly == null)
				{
					return null;
				}
				object[] customAttributes = EntryAssembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyConfigurationAttribute) customAttributes[0]).Configuration;
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the name of the current application from the AssemblyProductAttribute or
		/// AssemblyTitleAttribute value, or null if none is set.
		/// </summary>
		public static string AppName
		{
			get
			{
				if (EntryAssembly == null)
				{
					return null;
				}
				object[] customAttributes = EntryAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyProductAttribute) customAttributes[0]).Product;
				}
				customAttributes = EntryAssembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyTitleAttribute) customAttributes[0]).Title;
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the description of the current application from the AssemblyDescriptionAttribute
		/// value, or null if none is set.
		/// </summary>
		public static string AppDescription
		{
			get
			{
				if (EntryAssembly == null)
				{
					return null;
				}
				object[] customAttributes = EntryAssembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyDescriptionAttribute) customAttributes[0]).Description;
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the copyright note of the current application from the AssemblyCopyrightAttribute
		/// value, or null if none is set.
		/// </summary>
		public static string AppCopyright
		{
			get
			{
				if (EntryAssembly == null)
				{
					return null;
				}
				object[] customAttributes = EntryAssembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyCopyrightAttribute) customAttributes[0]).Copyright;
				}
				return null;
			}
		}

		#endregion Environment data
	}
}
