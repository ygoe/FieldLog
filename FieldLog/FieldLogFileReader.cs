using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
#if !NET20
using System.Threading.Tasks;
#endif

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Reads log items from a log file.
	/// </summary>
	public class FieldLogFileReader : IDisposable
	{
		#region Private data

		private const string fileHeader = "FieldLog\x00";
		private const int readWaitMilliseconds = 100;

		private Dictionary<int, string> textCache = new Dictionary<int, string>();
		private FileStream fileStream;
		private long startPosition;
		private int itemCount;
		private bool waitMode;
		private readonly object waitModeLock = new object();

		private FieldLogFileReader nextReader;
		private readonly object nextReaderLock = new object();

		private ManualResetEvent closeEvent = new ManualResetEvent(false);
		private bool isClosing;

		#endregion Private data

		#region Constructor

		/// <summary>
		/// Initialises a new instance of the FieldLogFileReader class.
		/// </summary>
		/// <param name="fileName">The name of the log file to read.</param>
		/// <param name="waitMode">true to wait for more items at the end of the file, false to indicate the end of the file and return.</param>
		public FieldLogFileReader(string fileName, bool waitMode)
		{
			FileName = fileName;
			this.waitMode = waitMode;
			fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			// Validate existing file
			byte[] fileHeaderBytes = Encoding.UTF8.GetBytes(fileHeader);
			//byte[] bytes = new byte[fileHeaderBytes.Length];
			//if (fileStream.Read(bytes, 0, fileHeaderBytes.Length) < fileHeaderBytes.Length)
			//{
			//    throw new FormatException("Invalid log file. Header too short.");
			//}
			byte[] bytes = ReadBytes(fileHeaderBytes.Length);
			if (bytes == null)
				throw new FormatException("Invalid log file. Header too short.");
			for (int i = 0; i < fileHeaderBytes.Length; i++)
				if (bytes[i] != fileHeaderBytes[i])
					throw new FormatException("Invalid log file. Wrong header.");

			//int formatVersion = fileStream.ReadByte();
			bytes = ReadBytes(1);
			if (bytes == null)
				throw new FormatException("Invalid log file. Header too short.");
			int formatVersion = bytes[0];
			if (formatVersion != FL.FileFormatVersion)
			{
				throw new FormatException("Invalid log file. Unsupported file format version " + formatVersion + ", expecting " + FL.FileFormatVersion + ".");
			}

			startPosition = fileStream.Position;
		}

		#endregion Constructor

		#region Public properties

		/// <summary>
		/// Gets the name of the log file.
		/// </summary>
		public string FileName { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether a read operation should wait until all
		/// requested data is available in the file. If true, read operations will block; otherwise
		/// read operations may return null. This property is thread-safe.
		/// </summary>
		public bool WaitMode
		{
			get
			{
				lock (waitModeLock)
				{
					return waitMode;
				}
			}
			set
			{
				lock (waitModeLock)
				{
					waitMode = value;
					//System.Diagnostics.Debug.WriteLine("WaitMode=" + waitMode + " for " + Path.GetFileName(FileName) + ", ThreadId=" + Thread.CurrentThread.ManagedThreadId);
				}
			}
		}

		/// <summary>
		/// Gets the number of items that have been read by this FieldLogFileReader instance since
		/// it has been created or reset.
		/// </summary>
		public int ItemCount
		{
			get { return itemCount; }
		}

		/// <summary>
		/// Gets or sets a follow-up reader to use when this file has been read till the end.
		/// Setting a value for NextReader unsets WaitMode for this reader. This property is
		/// thread-safe.
		/// </summary>
		public FieldLogFileReader NextReader
		{
			get
			{
				lock (nextReaderLock)
				{
					return nextReader;
				}
			}
			set
			{
				lock (nextReaderLock)
				{
					nextReader = value;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether the reader is closing.
		/// </summary>
		public bool IsClosing
		{
			get
			{
				return isClosing;
			}
		}

		/// <summary>
		/// Gets or sets the wait handle that will be signalled after the file has been read to
		/// the end and if the reader is now going to wait for further data to be appended to the
		/// file.
		/// </summary>
		public ManualResetEvent ReadWaitHandle { get; set; }

		#endregion Public properties

		#region Log item read methods

		private byte[] currentBuffer;
		private int currentBufferUsed;

		/// <summary>
		/// Tries to read bytes from the file.
		/// </summary>
		/// <param name="count">Number of bytes to read.</param>
		/// <returns>Bytes read from the file, if all bytes were available; null otherwise.</returns>
		/// <remarks>
		/// If not all requested bytes were available to read, the bytes that were already
		/// available are stored in a buffer and the method returns null. The next time this
		/// method is called, the bytes that are still missing are read. This must be repeated
		/// until the method does not return null.
		/// </remarks>
		private byte[] TryReadBytes(int count)
		{
			if (currentBuffer == null)
			{
				currentBuffer = new byte[count];
				currentBufferUsed = 0;
			}
			int missing = count - currentBufferUsed;
			int read = fileStream.Read(currentBuffer, currentBufferUsed, missing);
			if (read == missing)
			{
				byte[] buffer = currentBuffer;
				currentBuffer = null;
				currentBufferUsed = 0;
				return buffer;
			}
			if (!WaitMode)
			{
				// If WaitMode was once set and is not unset, a follow-up file has been noticed.
				// That file is only created when the current file is written completely, so it
				// can always be read completely.
				// OLD: throw new Exception("Unexpected end of file.");
			}
			currentBufferUsed += read;
			return null;
		}

		/// <summary>
		/// Reads the specified number of bytes from the file. Waits until all requested bytes were
		/// available to read.
		/// </summary>
		/// <param name="count">Number of bytes to read.</param>
		/// <returns>Bytes read from the file.</returns>
		internal byte[] ReadBytes(int count)
		{
			byte[] bytes;
			while (true)
			{
				bytes = TryReadBytes(count);
				if (bytes != null)
				{
					// Something has been read. There might be more. Reset the "waiting" signal
					if (ReadWaitHandle != null)
					{
						ReadWaitHandle.Reset();
					}
					return bytes;
				}
				if (!WaitMode) return null;
				// The file has been read until the current end, signal that
				if (ReadWaitHandle != null)
				{
					ReadWaitHandle.Set();
				}
				Thread.Sleep(readWaitMilliseconds);
				if (!WaitMode || closeEvent.WaitOne(0)) return null;
			}
		}

		/// <summary>
		/// Reads a single byte from the file.
		/// </summary>
		/// <returns></returns>
		internal byte ReadByte()
		{
			byte[] bytes = ReadBytes(1);
			return bytes[0];
		}

		/// <summary>
		/// Reads a short value from the file.
		/// </summary>
		/// <returns></returns>
		internal short ReadInt16()
		{
			byte[] bytes = ReadBytes(2);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return BitConverter.ToInt16(bytes, 0);
		}

		/// <summary>
		/// Reads an int value from the file.
		/// </summary>
		/// <returns></returns>
		internal int ReadInt32()
		{
			byte[] bytes = ReadBytes(4);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return BitConverter.ToInt32(bytes, 0);
		}

		/// <summary>
		/// Reads a long value from the file.
		/// </summary>
		/// <returns></returns>
		internal long ReadInt64()
		{
			byte[] bytes = ReadBytes(8);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return BitConverter.ToInt64(bytes, 0);
		}

		/// <summary>
		/// Reads a ushort value from the file.
		/// </summary>
		/// <returns></returns>
		internal ushort ReadUInt16()
		{
			byte[] bytes = ReadBytes(2);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return BitConverter.ToUInt16(bytes, 0);
		}

		/// <summary>
		/// Reads a uint value from the file.
		/// </summary>
		/// <returns></returns>
		internal uint ReadUInt32()
		{
			byte[] bytes = ReadBytes(4);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return BitConverter.ToUInt32(bytes, 0);
		}

		/// <summary>
		/// Reads a ulong value from the file.
		/// </summary>
		/// <returns></returns>
		internal ulong ReadUInt64()
		{
			byte[] bytes = ReadBytes(8);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return BitConverter.ToUInt64(bytes, 0);
		}

		/// <summary>
		/// Reads a string from the file, using the text cache.
		/// </summary>
		/// <returns></returns>
		internal string ReadString()
		{
			int pos = ReadInt32();
			if (pos == -1) return null;
			string str;
			if (textCache.TryGetValue(pos, out str))
			{
				return str;
			}
			throw new FormatException("String data not found in the log file.");
		}

		/// <summary>
		/// Reads the next complete log item from the file.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This method will block until a complete log item is available in the file.
		/// </remarks>
		public FieldLogItem ReadLogItem()
		{
			byte[] bytes;
			FieldLogItemType type;
			int pos = -1, length;

			try
			{
				do
				{
					// Remember the log item start position in the file
					pos = (int) fileStream.Position;
					bool localWaitMode = WaitMode;
					if (!localWaitMode && pos == fileStream.Length)
					{
						return null;
					}
					// Read the item type and length
					bytes = ReadBytes(4);
					if (bytes == null)
					{
						if (closeEvent.WaitOne(0))
						{
							// Close was requested
							isClosing = true;
							WaitMode = false;
							return null;
						}
						if (!WaitMode && localWaitMode)
						{
							// Follow-up file has been noticed
							return null;
						}
						if (!localWaitMode)
						{
							throw new Exception("Unexpected end of file.");
						}
					}
					// Parse type and length data
					type = (FieldLogItemType) ((bytes[0] & 0xF0) >> 4);
					bytes[0] = (byte) (bytes[0] & 0x0F);
					if (BitConverter.IsLittleEndian)
						Array.Reverse(bytes);
					length = BitConverter.ToInt32(bytes, 0);

					// Check whether this is a text item
					if (type == FieldLogItemType.StringData)
					{
						// Read the item and add it to the text cache
						byte[] stringBytes = ReadBytes(length);
						// Parse the text data
						string str = Encoding.UTF8.GetString(stringBytes);
						// Add text to the cache
						textCache[pos] = str;
					}
				}
				while (type == FieldLogItemType.StringData);

				itemCount++;
				return FieldLogItem.Read(this, type);
			}
			catch (Exception ex)
			{
				throw new Exception(
					"Error reading from the log file \"" + FileName + "\" at position " + fileStream.Position + " (starting at " + pos + "). " + ex.Message,
					ex);
			}
		}

#if !NET20
		/// <summary>
		/// Reads the next complete log item from the file.
		/// </summary>
		/// <returns></returns>
		public Task<FieldLogItem> ReadLogItemAsync()
		{
			return new Task<FieldLogItem>(ReadLogItem);
		}
#endif

		#endregion Log item read methods

		/// <summary>
		/// Resets the current position to the beginning of the file.
		/// </summary>
		public void Reset()
		{
			try
			{
				fileStream.Seek(startPosition, SeekOrigin.Begin);
				itemCount = 0;
			}
			catch (Exception ex)
			{
				throw new Exception(
					"Error seeking the log file \"" + FileName + "\" to position " + startPosition + ". " + ex.Message,
					ex);
			}
		}

		/// <summary>
		/// Sets the close signal. This may only work if WaitMode is set.
		/// </summary>
		public void Close()
		{
			closeEvent.Set();
		}

		#region IDispose members

		/// <summary>
		/// Closes the open log file.
		/// </summary>
		public void Dispose()
		{
			if (fileStream != null)
			{
				fileStream.Close();
				fileStream = null;
			}
		}

		#endregion IDispose members
	}
}
