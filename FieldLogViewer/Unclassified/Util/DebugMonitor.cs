using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Unclassified.Util
{
	/// <summary>
	/// Handles a received message from the DebugMonitor class.
	/// </summary>
	/// <param name="pid">The process ID of the message sender.</param>
	/// <param name="text">The message text.</param>
	public delegate void OutputDebugStringHandler(int pid, string text);

	// Based on: http://stackoverflow.com/a/1542782/143684
	/// <summary>
	/// Provides a monitor for messages sent from applications through the Windows API function
	/// OutputDebugString.
	/// </summary>
	public sealed class DebugMonitor
	{
		#region Win32 API imports

		[StructLayout(LayoutKind.Sequential)]
		private struct SECURITY_DESCRIPTOR
		{
			public byte revision;
			public byte size;
			public short control;
			public IntPtr owner;
			public IntPtr group;
			public IntPtr sacl;
			public IntPtr dacl;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct SECURITY_ATTRIBUTES
		{
			public int nLength;
			public IntPtr lpSecurityDescriptor;
			public int bInheritHandle;
		}

		[Flags]
		private enum PageProtection : uint
		{
			NoAccess = 0x1,
			Readonly = 0x2,
			ReadWrite = 0x4,
			WriteCopy = 0x8,
			Execute = 0x10,
			ExecuteRead = 0x20,
			ExecuteReadWrite = 0x40,
			ExecuteWriteCopy = 0x80,
			Guard = 0x100,
			NoCache = 0x200,
			WriteCombine = 0x400
		}

		private const int WAIT_OBJECT_0 = 0;
		private const uint INFINITE = 0xFFFFFFFF;
		private const int ERROR_ALREADY_EXISTS = 183;
		private const uint SECTION_MAP_READ = 0x4;

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint
			dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow,
			uint dwNumberOfBytesToMap);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

		[DllImport("kernel32.dll")]
		private static extern IntPtr CreateEvent(ref SECURITY_ATTRIBUTES sa, bool bManualReset, bool bInitialState, string lpName);

		[DllImport("kernel32.dll")]
		private static extern bool PulseEvent(IntPtr hEvent);

		[DllImport("kernel32.dll")]
		private static extern bool SetEvent(IntPtr hEvent);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr CreateFileMapping(IntPtr hFile,
			ref SECURITY_ATTRIBUTES lpFileMappingAttributes, PageProtection flProtect, uint dwMaximumSizeHigh,
			uint dwMaximumSizeLow, string lpName);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hHandle);

		[DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
		private static extern Int32 WaitForSingleObject(IntPtr handle, uint milliseconds);

		#endregion Win32 API imports

		#region Constructor

		/// <summary>
		/// Initialises a new instance of the DebugMonitor class.
		/// </summary>
		/// <param name="isGlobal">true to capture global messages, false for local messages.</param>
		public DebugMonitor(bool isGlobal)
		{
			this.isGlobal = isGlobal;
		}

		#endregion Constructor

		#region Events

		/// <summary>
		/// Raised when a new message is available on the debug communication interface.
		/// </summary>
		public event OutputDebugStringHandler MessageReceived;

		#endregion Events

		#region Private fields

		private readonly object syncRoot = new object();
		private bool isGlobal;
		private IntPtr ackEvent;
		private IntPtr readyEvent;
		private IntPtr sharedFile;
		private IntPtr sharedMem;
		private Thread monitorThread;
		private bool cancelRequested;

		#endregion Private fields

		#region Public properties

		/// <summary>
		/// Gets a value indicating whether the debug monitor is currently running.
		/// </summary>
		public bool IsActive
		{
			get
			{
				lock (syncRoot)
				{
					return monitorThread != null;
				}
			}
		}

		#endregion Public properties

		#region Public methods

		/// <summary>
		/// Starts the debug monitor if it is not currently running.
		/// </summary>
		public void TryStart()
		{
			lock (syncRoot)
			{
				if (!IsActive)
				{
					Start();
				}
			}
		}

		/// <summary>
		/// Starts the debug monitor in a separate thread.
		/// </summary>
		public void Start()
		{
			// Don't activate this thing of the devil if Visual Studio is debugging the process.
			// If anything happens, this will blow up the debugger and you will have to restart
			// Visual Studio and other processes to be able to debug anything again.
			if (Debugger.IsAttached) return;

			string prefix = isGlobal ? @"Global\" : "";

			lock (syncRoot)
			{
				if (monitorThread != null)
					throw new InvalidOperationException("This DebugMonitor is already started.");
				if (Environment.OSVersion.ToString().IndexOf("Microsoft") == -1)
					throw new NotSupportedException("This DebugMonitor is only supported on Microsoft operating systems.");

				SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();

				ackEvent = CreateEvent(ref sa, false, false, prefix + "DBWIN_BUFFER_READY");
				if (ackEvent == IntPtr.Zero)
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create event 'DBWIN_BUFFER_READY'");
				}
				if (WaitForSingleObject(ackEvent, 0) == WAIT_OBJECT_0)
				{
					CloseHandle(ackEvent);
					throw new InvalidOperationException("Another process is already listening for debug messages.");
				}

				readyEvent = CreateEvent(ref sa, false, false, prefix + "DBWIN_DATA_READY");
				if (readyEvent == IntPtr.Zero)
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create event 'DBWIN_DATA_READY'");
				}

				sharedFile = CreateFileMapping(new IntPtr(-1), ref sa, PageProtection.ReadWrite, 0, 4096, prefix + "DBWIN_BUFFER");
				if (sharedFile == IntPtr.Zero)
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create a file mapping to slot 'DBWIN_BUFFER'");
				}

				sharedMem = MapViewOfFile(sharedFile, SECTION_MAP_READ, 0, 0, 512);
				if (sharedMem == IntPtr.Zero)
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create a mapping view for slot 'DBWIN_BUFFER'");
				}

				monitorThread = new Thread(new ThreadStart(MonitorThread));
				monitorThread.Start();
			}
		}

		/// <summary>
		/// Stops the debug monitor thread.
		/// </summary>
		public void Stop()
		{
			lock (syncRoot)
			{
				if (monitorThread != null)
				{
					cancelRequested = true;
					Thread.MemoryBarrier();   // Update cancelRequested in the monitor thread
					PulseEvent(readyEvent);
					monitorThread.Join();
					monitorThread = null;
				}
				Dispose();
			}
		}

		#endregion Public methods

		#region Private methods

		/// <summary>
		/// Monitor thread method.
		/// </summary>
		private void MonitorThread()
		{
			try
			{
				IntPtr pString = new IntPtr(sharedMem.ToInt64() + 4);

				while (true)
				{
					SetEvent(ackEvent);

					// We could wait for INFINITE instead but that may block the process wanting to
					// stop a DebugMonitor if another process has also started a DebugMonitor. The
					// readyEvent is then caught by the other process (that should ignore the false
					// message) but this one won't unblock. That's why we have a maximum time until
					// cancelRequested is checked in any case.
					int ret = WaitForSingleObject(readyEvent, 1000);
					// A more complete approach would be waiting for multiple objects and maintain a
					// separate event (local to each process) in order to unblock and stop the
					// monitor. The Stop method would then pulse the separate event instead of the
					// shared readyEvent. This is left to do for the future...

					Thread.MemoryBarrier();   // Update cancelRequested from the control/UI thread
					if (cancelRequested)
						break;

					if (ret == WAIT_OBJECT_0)
					{
						OnMessageReceived(
							Marshal.ReadInt32(sharedMem),
							Marshal.PtrToStringAnsi(pString));
					}
				}
			}
			finally
			{
				Dispose();
			}
		}

		/// <summary>
		/// Raises the MessageReceived event.
		/// </summary>
		/// <param name="pid">The process ID of the message sender.</param>
		/// <param name="text">The message text.</param>
		private void OnMessageReceived(int pid, string text)
		{
			// pid zero can sometimes be observed when starting DebugMonitor in multiple processes
			// and then stopping one of them. The other process may catch the readyEvent that should
			// unblock the stopping process and mistake it for a real message when there's no data
			// in the memory. Ignore such false messages.
			if (MessageReceived != null && pid != 0)
			{
				MessageReceived(pid, text);
			}
		}

		/// <summary>
		/// Frees all native resources.
		/// </summary>
		private void Dispose()
		{
			if (ackEvent != IntPtr.Zero)
			{
				if (!CloseHandle(ackEvent))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to close handle for 'AckEvent'");
				}
				ackEvent = IntPtr.Zero;
			}
			if (readyEvent != IntPtr.Zero)
			{
				if (!CloseHandle(readyEvent))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to close handle for 'ReadyEvent'");
				}
				readyEvent = IntPtr.Zero;
			}
			if (sharedFile != IntPtr.Zero)
			{
				if (!CloseHandle(sharedFile))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to close handle for 'SharedFile'");
				}
				sharedFile = IntPtr.Zero;
			}
			if (sharedMem != IntPtr.Zero)
			{
				if (!UnmapViewOfFile(sharedMem))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to unmap view for slot 'DBWIN_BUFFER'");
				}
				sharedMem = IntPtr.Zero;
			}
		}

		#endregion Private methods
	}
}