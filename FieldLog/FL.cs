using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Unclassified.FieldLog
{
	public static class FL
	{
		#region Native interop

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetConsoleWindow();

		#endregion Native interop

		#region Constants

		/// <summary>
		/// Defines the format version of log files.
		/// </summary>
		public const byte FileFormatVersion = 1;

		/// <summary>
		/// Defines the maximum buffer size to keep.
		/// </summary>
		private const int maxBufferSize = 4096;

		/// <summary>
		/// Defines the log configuration file name extension.
		/// </summary>
		private const string logConfigExtension = ".flconfig";

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
		private static int maxFileSize = 1 * 1024 * 1024 /* MiB */;
		/// <summary>
		/// Maximum size of all log files together. Only set when the send thread is stopped.
		/// </summary>
		private static long maxTotalSize = 2L * 1024 * 1024 * 1024 /* GiB */;
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
		/// Keeps all buffers that still need to be sent. Used by the send thread only.
		/// </summary>
		private static List<List<FieldLogItem>> buffersToSend = new List<List<FieldLogItem>>();
		/// <summary>
		/// Keeps all open log file writers for each priority. Used by the send thread only.
		/// </summary>
		private static Dictionary<FieldLogPriority, FieldLogFileWriter> priorityLogWriters = new Dictionary<FieldLogPriority, FieldLogFileWriter>();

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

		#endregion Internal static data

		#region Static constructor

		static FL()
		{
			// Link the high-precision stopwatch with the current time for later high-precision
			// performance tracing timestamps
			stopwatch = new Stopwatch();
			stopwatch.Start();
			startTime = DateTime.UtcNow;

			// Read or reset log configuration from file
			ReadLogConfiguration();

			// Initialise the send thread
			sendThread = new Thread(SendThread);
			sendThread.IsBackground = true;
			sendThread.Name = "FieldLog.SendThread";
			sendThread.Priority = ThreadPriority.BelowNormal;
			sendThread.Start();

			SessionId = Guid.NewGuid();

			IsConsoleApp = GetConsoleWindow() != IntPtr.Zero;

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

			//AppErrorDialogContinuable = "Es ist ein Fehler aufgetreten, die Anwendung kann möglicherweise nicht korrekt fortgesetzt werden. " +
			//    "Drücken Sie auf „OK“, um das Programm fortzusetzen, oder „Abbrechen“, um das Programm zu beenden. " +
			//    "Falls Sie die Ausführung fortsetzen, können weitere Fehler oder Störungen auftreten.";
			//AppErrorDialogTerminating = "Es ist ein Fehler aufgetreten, die Anwendung kann nicht fortgesetzt werden.";
			//AppErrorDialogNote = "Falls das Problem weiterhin bestehen sollte, wenden Sie sich bitte an den Programmentwickler.";

			// Use default implementation to show an application error dialog
			ShowAppErrorDialog = DefaultShowAppErrorDialog;

			LogScope(FieldLogScopeType.LogStart, null);
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

			if (!Debugger.IsAttached)
			{
				RegisterAppErrorHandler();
			}
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
		private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			// Flush log files, if not already done by the application
			Shutdown();
		}

		#endregion Static constructor

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
		/// Gets a value indicating whether the current application is a console application.
		/// </summary>
		public static bool IsConsoleApp { get; private set; }
		
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

		#endregion Static properties

		#region Application error handling

		/// <summary>
		/// Registers application error handlers for all application types.
		/// </summary>
		private static void RegisterAppErrorHandler()
		{
			// Reference: http://code.msdn.microsoft.com/Handling-Unhandled-47492d0b

			// Handle UI thread exceptions
			System.Windows.Forms.Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
			{
				FL.Critical(e.Exception, "WinForms.ThreadException", true);
			};
			// Set the unhandled exception mode to force all Windows Forms errors to go through our handler
			System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);

			// Handle non-UI thread exceptions
			AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
			{
				FL.Critical(e.ExceptionObject as Exception, "AppDomain.UnhandledException", true);
			};

#if !NET20
			// Log first-chance exceptions, also from try/catch blocks
			AppDomain.CurrentDomain.FirstChanceException += delegate(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
			{
				// TODO: This may lead to crashes at WMI requests, so it's disabled for now.
				//       Testcase: Inspect FieldLogViewer with Snoop while debugging.
				//FL.Trace(e.Exception, "AppDomain.FirstChanceException");
			};

			System.Threading.Tasks.TaskScheduler.UnobservedTaskException += delegate(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
			{
				FL.Trace(e.Exception, "TaskScheduler.UnobservedTaskException");
			};
#endif
		}

#if !NET20
		/// <summary>
		/// Registers application error handlers for a WPF application. This must be called after
		/// the Application's constructor.
		/// </summary>
		public static void RegisterWpfErrorHandler()
		{
			RegisterWpfErrorHandler(null);
		}

		/// <summary>
		/// Registers application error handlers for a WPF application. This must be called from
		/// the Application's constructor or later.
		/// </summary>
		/// <param name="app">Application object. If null, Application.Current is used (only after Application constructor).</param>
		public static void RegisterWpfErrorHandler(System.Windows.Application app)
		{
			if (app == null)
			{
				app = System.Windows.Application.Current;
			}

			// Handle UI thread exceptions
			app.DispatcherUnhandledException += delegate(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
			{
				FL.Critical(e.Exception, "WPF.DispatcherUnhandledException", true);
				e.Handled = true;
			};
		}
#endif

		private static void DefaultShowAppErrorDialog(FieldLogExceptionItem exItem, bool allowContinue)
		{
#if DEBUG
			System.Diagnostics.Trace.Write(exItem.Exception.Exception.ToString());
#endif
			
			string msg;
			if (allowContinue)
			{
				if (IsConsoleApp)
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

			msg += ExceptionUserMessageRecursive(exItem.Exception, 0);
			if (!string.IsNullOrEmpty(exItem.Context))
			{
				msg += AppErrorDialogContext + " " + exItem.Context + "\n";
			}
			msg += "\n";

			if (logFileBasePath != null)
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

			if (IsConsoleApp)
			{
				ConsoleColor foreColor = Console.ForegroundColor;
				ConsoleColor backColor = Console.BackgroundColor;

				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Error.WriteLine(AppErrorDialogTitle);
				Console.Error.WriteLine(msg);

				if (allowContinue)
				{
					Console.ForegroundColor = ConsoleColor.White;
					Console.Error.Write(AppErrorDialogConsoleAction);
				}

				Console.ForegroundColor = foreColor;
				Console.BackgroundColor = backColor;

				if (allowContinue)
				{
					while (true)
					{
						ConsoleKeyInfo key = Console.ReadKey(true);
						if (key.Key == ConsoleKey.Enter) break;
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
				if (System.Windows.Forms.MessageBox.Show(
					msg,
					AppErrorDialogTitle,
					System.Windows.Forms.MessageBoxButtons.OKCancel,
					System.Windows.Forms.MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Cancel)
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

		public static string ExceptionUserMessageRecursive(FieldLogException ex, int level)
		{
			string msg;
			if (level == 0)
			{
				msg = ex.Message + " (" + ex.Type + ")\n";
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

		public static void Trace(string text)
		{
			Text(FieldLogPriority.Trace, text);
		}

		public static void Checkpoint(string text)
		{
			Text(FieldLogPriority.Checkpoint, text);
		}

		public static void Info(string text)
		{
			Text(FieldLogPriority.Info, text);
		}

		public static void Notice(string text)
		{
			Text(FieldLogPriority.Notice, text);
		}

		public static void Warning(string text)
		{
			Text(FieldLogPriority.Warning, text);
		}

		public static void Error(string text)
		{
			Text(FieldLogPriority.Error, text);
		}

		public static void Critical(string text)
		{
			Text(FieldLogPriority.Critical, text);
		}

		public static void Trace(string text, string details)
		{
			Text(FieldLogPriority.Trace, text, details);
		}

		public static void Checkpoint(string text, string details)
		{
			Text(FieldLogPriority.Checkpoint, text, details);
		}

		public static void Info(string text, string details)
		{
			Text(FieldLogPriority.Info, text, details);
		}

		public static void Notice(string text, string details)
		{
			Text(FieldLogPriority.Notice, text, details);
		}

		public static void Warning(string text, string details)
		{
			Text(FieldLogPriority.Warning, text, details);
		}

		public static void Error(string text, string details)
		{
			Text(FieldLogPriority.Error, text, details);
		}

		public static void Critical(string text, string details)
		{
			Text(FieldLogPriority.Critical, text, details);
		}

		#endregion Text log methods for each priority

		#region Data log methods for each priority

		public static void TraceData(string name, string value)
		{
			Data(FieldLogPriority.Trace, name, value);
		}

		public static void CheckpointData(string name, string value)
		{
			Data(FieldLogPriority.Checkpoint, name, value);
		}

		public static void InfoData(string name, string value)
		{
			Data(FieldLogPriority.Info, name, value);
		}

		public static void NoticeData(string name, string value)
		{
			Data(FieldLogPriority.Notice, name, value);
		}

		public static void WarningData(string name, string value)
		{
			Data(FieldLogPriority.Warning, name, value);
		}

		public static void ErrorData(string name, string value)
		{
			Data(FieldLogPriority.Error, name, value);
		}

		public static void CriticalData(string name, string value)
		{
			Data(FieldLogPriority.Critical, name, value);
		}

		#endregion Data log methods for each priority

		#region Exception log methods for each priority

		public static void Trace(Exception ex)
		{
			Exception(FieldLogPriority.Trace, ex);
		}

		public static void Checkpoint(Exception ex)
		{
			Exception(FieldLogPriority.Checkpoint, ex);
		}

		public static void Info(Exception ex)
		{
			Exception(FieldLogPriority.Info, ex);
		}

		public static void Notice(Exception ex)
		{
			Exception(FieldLogPriority.Notice, ex);
		}

		public static void Warning(Exception ex)
		{
			Exception(FieldLogPriority.Warning, ex);
		}

		public static void Error(Exception ex)
		{
			Exception(FieldLogPriority.Error, ex);
		}

		public static void Critical(Exception ex)
		{
			Exception(FieldLogPriority.Critical, ex);
		}

		public static void Trace(Exception ex, string context)
		{
			Exception(FieldLogPriority.Trace, ex, context);
		}

		public static void Checkpoint(Exception ex, string context)
		{
			Exception(FieldLogPriority.Checkpoint, ex, context);
		}

		public static void Info(Exception ex, string context)
		{
			Exception(FieldLogPriority.Info, ex, context);
		}

		public static void Notice(Exception ex, string context)
		{
			Exception(FieldLogPriority.Notice, ex, context);
		}

		public static void Warning(Exception ex, string context)
		{
			Exception(FieldLogPriority.Warning, ex, context);
		}

		public static void Error(Exception ex, string context)
		{
			Exception(FieldLogPriority.Error, ex, context);
		}

		public static void Critical(Exception ex, string context)
		{
			Exception(FieldLogPriority.Critical, ex, context);
		}

		public static void Trace(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Trace, ex, context, showUserDialog);
		}

		public static void Checkpoint(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Checkpoint, ex, context, showUserDialog);
		}

		public static void Info(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Info, ex, context, showUserDialog);
		}

		public static void Notice(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Notice, ex, context, showUserDialog);
		}

		public static void Warning(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Warning, ex, context, showUserDialog);
		}

		public static void Error(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Error, ex, context, showUserDialog);
		}

		public static void Critical(Exception ex, string context, bool showUserDialog)
		{
			Exception(FieldLogPriority.Critical, ex, context, showUserDialog);
		}

		#endregion Exception log methods for each priority

		#region Log methods with variable priority for each item type

		public static void Text(FieldLogPriority priority, string text)
		{
			Log(new FieldLogTextItem(priority, text));
		}

		public static void Text(FieldLogPriority priority, string text, string details)
		{
			Log(new FieldLogTextItem(priority, text, details));
		}

		public static void Data(FieldLogPriority priority, string name, string value)
		{
			Log(new FieldLogDataItem(priority, name, value));
		}

		public static void Exception(FieldLogPriority priority, Exception ex)
		{
			Log(new FieldLogExceptionItem(priority, ex));
		}

		public static void Exception(FieldLogPriority priority, Exception ex, string context)
		{
			Log(new FieldLogExceptionItem(priority, ex, context));
		}

		public static void Exception(FieldLogPriority priority, Exception ex, string context, bool showUserDialog)
		{
			FieldLogExceptionItem item = new FieldLogExceptionItem(priority, ex, context);
			Log(item);
			if (showUserDialog)
			{
				ShowAppErrorDialog(item, item.Context != "AppDomain.UnhandledException");
			}
		}

		#endregion Log methods with variable priority for each item type

		#region Scope log methods

		public static void Enter(string name)
		{
			FL.ScopeLevel++;
			Log(new FieldLogScopeItem(FieldLogScopeType.Enter, name));
		}

		public static void Leave(string name)
		{
			FL.ScopeLevel--;
			Log(new FieldLogScopeItem(FieldLogScopeType.Leave, name));
		}

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
				FL.ScopeLevel++;
				Log(scopeItem);
			}
			else if (type == FieldLogScopeType.Leave)
			{
				FL.ScopeLevel--;
				Log(scopeItem);
			}
			else
			{
				throw new ArgumentException("Invalid value.", "type");
			}
		}

		#endregion Scope log methods

		#region General log method

		public static void Log(FieldLogItem item)
		{
			//System.Diagnostics.Trace.WriteLine("FieldLog.Log: New item " + item.ToString());
			// Add the item to the current buffer
			int size = item.Size;
			lock (currentBufferLock)
			{
				if (isShutdown)
					throw new InvalidOperationException("New messages are not accepted because the log queue has been shut down.");
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
				currentBuffer = new List<FieldLogItem>();
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
				lock (sendThread)
				{
					if (sendThreadCancellationPending) break;
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
					foreach (List<FieldLogItem> buffer in buffersToSend)
					{
						AppendLogItemsToFile(buffer);
					}
					buffersToSend.Clear();

					// Delete outdated files
					PurgeAllFiles();
				}
			}
		}

		#endregion Send thread

		#region File writing

		/// <summary>
		/// Sets an application-defined base path for writing log files to.
		/// </summary>
		/// <param name="path">Log file base path. This is an absolute path to a directory and a file name prefix. Set this null to use the default set of paths.</param>
		/// <remarks>
		/// FieldLog tries to find a working path to write log files to automatically. The
		/// application can specify a custom path that will be tried before all automatic defaults.
		/// If this path is null or doesn't work, the other default paths are tested and the first
		/// working path will be used. No attempt to find a working path is made until this method
		/// is called, either with a custom path or not. Calling this method with the parameter
		/// null just tells FieldLog that the default paths are okay and logs shall be written
		/// there.
		/// </remarks>
		public static void SetCustomLogFileBasePath(string path)
		{
			lock (customLogPathLock)
			{
				if (customLogFileBasePathSet)
					throw new InvalidOperationException("The custom log file path has already been set.");

				customLogFileBasePath = path;
				customLogFileBasePathSet = true;
			}
		}

		/// <summary>
		/// Detects and stores a working path for writing log files to.
		/// </summary>
		/// <returns>true if a path has been set, false to defer writing to a later time.</returns>
		private static bool TestLogPaths()
		{
			if (logFileBasePathSet) return true;

			string execPath = Assembly.GetEntryAssembly().Location;
			string execFile = Path.GetFileNameWithoutExtension(execPath);
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
						if (logFileBasePath.ToLowerInvariant().StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows).ToLowerInvariant()))
						{
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
					return true;
				}
				catch
				{
					// Something went wrong, we can't use this path. Try the next one.
				}
				finally
				{
					i++;
				}
			}
		}

		private static void AppendLogItemsToFile(IEnumerable<FieldLogItem> logItems)
		{
			if (logFileBasePath == null) return;   // Nowhere to write the log items, drop them
			Dictionary<FieldLogFileWriter, bool> usedWriters = new Dictionary<FieldLogFileWriter, bool>();
			// NOTE: HashSet<T> would be better here, but it's not supported in .NET 2.0, so we're
			//       using a Dictionary instead. And it's just as fast anyway.

			foreach (FieldLogItem logItem in logItems)
			{
				//System.Diagnostics.Trace.WriteLine("FieldLog.AppendLogItemsToFile: Process item " + logItem.ToString());
				// Update the list of currently open scope items to repeat them when a new log file
				// is started.
				FieldLogScopeItem scopeItem = logItem as FieldLogScopeItem;
				if (scopeItem != null)
				{
					if (scopeItem.Type == FieldLogScopeType.Enter)
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
					if (keepTime.Ticks == 0)
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
						if (FieldLogFileWriter.GetCompressedFileSize(fileName) >= maxFileSize) continue;   // File is already large enough
						FileInfo fi = new FileInfo(fileName);
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

				if (writer.FileSize > maxFileSize ||   // File is large enough
					writer.Length > 1 * 1024 * 1024 * 1024 /* GiB */ ||   // File contents is getting nearer to the technical limit of 2 GiB
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

			// Log files are closed in the Shutdown method. Should that method not be called for
			// some reason, all data has been flushed to disk anyway (at least in .NET 4.0) for the
			// LogViewer to read it. Files are then implicitly closed when the process ends.
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
			DateTime now = FL.UtcNow;
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
			string execPath = Assembly.GetEntryAssembly().Location;
			string execDir = Path.GetDirectoryName(execPath);
			string execFile = Path.GetFileNameWithoutExtension(execPath);
			string configFileName = Path.Combine(execDir, execFile + logConfigExtension);

			try
			{
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
										configLogPath = value;
									}
									break;
								case "maxfilesize":
									maxFileSize = (int) ParseConfigNumber(value);
									break;
								case "maxtotalsize":
									maxTotalSize = ParseConfigNumber(value);
									break;
								case "keeptrace":
									priorityKeepTimes[FieldLogPriority.Trace] = ParseConfigTimeSpan(value);
									break;
								case "keepcheckpoint":
									priorityKeepTimes[FieldLogPriority.Checkpoint] = ParseConfigTimeSpan(value);
									break;
								case "keepinfo":
									priorityKeepTimes[FieldLogPriority.Info] = ParseConfigTimeSpan(value);
									break;
								case "keepnotice":
									priorityKeepTimes[FieldLogPriority.Notice] = ParseConfigTimeSpan(value);
									break;
								case "keepwarning":
									priorityKeepTimes[FieldLogPriority.Warning] = ParseConfigTimeSpan(value);
									break;
								case "keeperror":
									priorityKeepTimes[FieldLogPriority.Error] = ParseConfigTimeSpan(value);
									break;
								case "keepcritical":
									priorityKeepTimes[FieldLogPriority.Critical] = ParseConfigTimeSpan(value);
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
			catch
			{
				// Something went really bad while reading the configuration file.
				// Set all values to default.
				ResetLogConfiguration();
			}
		}

		/// <summary>
		/// Sets default values for the log configuration.
		/// </summary>
		private static void ResetLogConfiguration()
		{
			maxFileSize = 1 * 1024 * 1024 /* MiB */;
			maxTotalSize = 2L * 1024 * 1024 * 1024 /* GiB */;
			priorityKeepTimes.Clear();
			priorityKeepTimes[FieldLogPriority.Trace] = TimeSpan.FromHours(3);
			priorityKeepTimes[FieldLogPriority.Checkpoint] = TimeSpan.FromHours(3);
			priorityKeepTimes[FieldLogPriority.Info] = TimeSpan.FromDays(5);
			priorityKeepTimes[FieldLogPriority.Notice] = TimeSpan.FromDays(5);
			priorityKeepTimes[FieldLogPriority.Warning] = TimeSpan.FromDays(30);
			priorityKeepTimes[FieldLogPriority.Error] = TimeSpan.FromDays(30);
			priorityKeepTimes[FieldLogPriority.Critical] = TimeSpan.FromDays(30);
		}

		/// <summary>
		/// Parses a number description from the configuration file.
		/// </summary>
		/// <param name="value">A number value with an optional byte size suffix "k", "m" or "g" (case-insensitive).</param>
		/// <returns>The parsed value, or -1 on error.</returns>
		private static long ParseConfigNumber(string value)
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
			return -1;
		}

		/// <summary>
		/// Parses a timespan description from the configuration file.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static TimeSpan ParseConfigTimeSpan(string value)
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
			return TimeSpan.MinValue;
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

		public static string AppVersion
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyInformationalVersionAttribute) customAttributes[0]).InformationalVersion;
				}
				customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyVersionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyVersionAttribute) customAttributes[0]).Version;
				}
				customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyFileVersionAttribute) customAttributes[0]).Version;
				}
				return null;
			}
		}

		public static string AppName
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyProductAttribute) customAttributes[0]).Product;
				}
				customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyTitleAttribute) customAttributes[0]).Title;
				}
				return null;
			}
		}

		public static string AppDescription
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyDescriptionAttribute) customAttributes[0]).Description;
				}
				return null;
			}
		}

		public static string AppCopyright
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
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
