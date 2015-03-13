using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace Unclassified.Util
{
	// Based on: http://stackoverflow.com/a/8341945/143684

	/// <summary>
	/// PInvoke wrapper for CopyFileEx.
	/// </summary>
	public class CopyEx
	{
		#region Public static methods

		public static void Copy(string source, string destination, bool overwrite)
		{
			new CopyEx().CopyInternal(source, destination, overwrite, false, null);
		}

		public static void Copy(string source, string destination, bool overwrite, EventHandler<CopyExEventArgs> handler)
		{
			new CopyEx().CopyInternal(source, destination, overwrite, false, handler);
		}

		public static void Copy(string source, string destination, bool overwrite, bool noBuffering)
		{
			new CopyEx().CopyInternal(source, destination, overwrite, noBuffering, null);
		}

		public static void Copy(string source, string destination, bool overwrite, bool noBuffering, EventHandler<CopyExEventArgs> handler)
		{
			new CopyEx().CopyInternal(source, destination, overwrite, noBuffering, handler);
		}

		#endregion Public static methods

		#region Private data

		private bool isCancelled;

		#endregion Private data

		#region Private events

		private event EventHandler<CopyExEventArgs> ProgressChanged;

		#endregion Private events

		#region Constructors

		private CopyEx()
		{
		}

		#endregion Constructors

		#region Private methods

		private void CopyInternal(string source, string destination, bool overwrite, bool noBuffering, EventHandler<CopyExEventArgs> handler)
		{
			try
			{
				CopyFileFlags copyFileFlags = CopyFileFlags.COPY_FILE_ALLOW_DECRYPTED_DESTINATION;
				if (!overwrite)
				{
					copyFileFlags |= CopyFileFlags.COPY_FILE_FAIL_IF_EXISTS;
				}
				if (noBuffering)
				{
					copyFileFlags |= CopyFileFlags.COPY_FILE_NO_BUFFERING;
				}

				if (handler != null)
				{
					ProgressChanged += handler;
				}

				bool result = CopyFileEx(source, destination, CopyProgressHandler, IntPtr.Zero, ref isCancelled, copyFileFlags);
				if (!result)
				{
					throw new Win32Exception(Marshal.GetLastWin32Error());
				}
			}
			finally
			{
				if (handler != null)
				{
					ProgressChanged -= handler;
				}
			}
		}

		private CopyProgressResult CopyProgressHandler(
			long totalFileSize,
			long totalBytesTransferred,
			long streamSize,
			long streamBytesTransferred,
			uint streamNumber,
			CopyProgressCallbackReason callbackReason,
			IntPtr sourceFileHandle,
			IntPtr destinationFileHandle,
			IntPtr dataPtr)
		{
			if (callbackReason == CopyProgressCallbackReason.CALLBACK_CHUNK_FINISHED)
			{
				bool cancel;
				OnProgressChanged((double) totalBytesTransferred / totalFileSize * 100, out cancel);
				if (cancel)
				{
					return CopyProgressResult.PROGRESS_CANCEL;
				}
			}
			return CopyProgressResult.PROGRESS_CONTINUE;
		}

		private void OnProgressChanged(double percent, out bool cancel)
		{
			cancel = false;
			var args = new CopyExEventArgs(percent);

			var handler = ProgressChanged;
			if (handler != null)
			{
				handler(this, args);
			}

			cancel = args.Cancel;
		}

		#endregion Private methods

		#region Native members

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CopyFileEx(
			string lpExistingFileName,
			string lpNewFileName,
			CopyProgressRoutine lpProgressRoutine,
			IntPtr lpData,
			[MarshalAs(UnmanagedType.Bool)]
			ref bool pbCancel,
			CopyFileFlags dwCopyFlags);

		private delegate CopyProgressResult CopyProgressRoutine(
			long totalFileSize,
			long totalBytesTransferred,
			long streamSize,
			long streamBytesTransferred,
			uint dwStreamNumber,
			CopyProgressCallbackReason dwCallbackReason,
			IntPtr hSourceFile,
			IntPtr hDestinationFile,
			IntPtr lpData);

		private enum CopyProgressResult : uint
		{
			PROGRESS_CONTINUE = 0,
			PROGRESS_CANCEL = 1,
			PROGRESS_STOP = 2,
			PROGRESS_QUIET = 3
		}

		private enum CopyProgressCallbackReason : uint
		{
			CALLBACK_CHUNK_FINISHED = 0,
			CALLBACK_STREAM_SWITCH = 1
		}

		[Flags]
		private enum CopyFileFlags : uint
		{
			COPY_FILE_FAIL_IF_EXISTS = 0x1,
			COPY_FILE_NO_BUFFERING = 0x1000,
			COPY_FILE_RESTARTABLE = 0x2,
			COPY_FILE_OPEN_SOURCE_FOR_WRITE = 0x4,
			COPY_FILE_ALLOW_DECRYPTED_DESTINATION = 0x8
		}

		#endregion Native members
	}

	#region EventArgs classes

	public class CopyExEventArgs : EventArgs
	{
		public CopyExEventArgs(double progress)
		{
			Progress = progress;
		}

		public double Progress { get; private set; }
		public bool Cancel { get; set; }
	}

	#endregion EventArgs classes
}
