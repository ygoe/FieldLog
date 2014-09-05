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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Unclassified.FieldLog
{
	internal class FieldLogFileWriter : IDisposable
	{
		#region Native interop

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern int DeviceIoControl(IntPtr hDevice, int dwIoControlCode,
			ref short lpInBuffer, int nInBufferSize, IntPtr lpOutBuffer, int nOutBufferSize,
			ref int lpBytesReturned, IntPtr lpOverlapped);

		private const int FSCTL_SET_COMPRESSION = 0x9C040;

		[DllImport("kernel32.dll")]
		private static extern uint GetCompressedFileSizeW(
			[MarshalAs(UnmanagedType.LPWStr)] string lpFileName, out uint lpFileSizeHigh);

		private static bool supportsGetCompressedFileSize = true;

		#endregion Native interop

		#region Private data

		private const string fileHeader = "FieldLog\x00";

		private Dictionary<string, int> textCache = new Dictionary<string, int>();
		private List<byte> buffer = new List<byte>();
		private byte itemType;
		private FileStream fileStream;
		private DateTime createdTime;

		#endregion Private data

		#region Constructor

		public FieldLogFileWriter(string fileName, FieldLogPriority prio)
		{
			FileName = fileName;
			fileStream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

			//Match m = Regex.Match(fileName, "[^0-9]([0-9]{8}-[0-9]{6})[^0-9]");
			//if (m.Success)
			//{
			//    createdTime = DateTime.ParseExact(m.Groups[1].Value, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
			//}
			FileInfo fi = new FileInfo(fileName);
			createdTime = fi.CreationTimeUtc;

			try
			{
				// Setting the NTFS compression needs the file to be opened with FileAccess.ReadWrite
				int lpBytesReturned = 0;
				short COMPRESSION_FORMAT_DEFAULT = 1;
				int result = DeviceIoControl(fileStream.SafeFileHandle.DangerousGetHandle(),
					FSCTL_SET_COMPRESSION, ref COMPRESSION_FORMAT_DEFAULT, 2 /*sizeof(short)*/,
					IntPtr.Zero, 0, ref lpBytesReturned, IntPtr.Zero);
				int err = Marshal.GetLastWin32Error();
			}
			catch
			{
				// NTFS or Win32 may not be available
			}

			byte[] fileHeaderBytes = Encoding.UTF8.GetBytes(fileHeader);
			if (fileStream.Length == 0)
			{
				// Initialise file with header
				fileStream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length);

				fileStream.WriteByte(FL.FileFormatVersion);   // File format version

				// Rewrite currently open scope items with RepeatedScope item type
				if (FL.LogScopeItem != null)
				{
					if (FL.LogScopeItem.Priority == prio && FL.LogScopeItem.WasWritten)
					{
						FL.LogScopeItem.IsRepeated = true;
						FL.LogScopeItem.Write(this);
					}
				}
				foreach (FieldLogScopeItem item in new List<FieldLogScopeItem>(FL.ThreadScopes.Values))
				{
					if (item.Priority == prio && item.WasWritten)
					{
						if (!item.Thread.IsAlive)
						{
							// Don't repeat dead threads
							FL.ThreadScopes.Remove(item.ThreadId);
							continue;
						}
						item.IsRepeated = true;
						item.Write(this);
					}
				}
				foreach (FieldLogScopeItem item in new List<FieldLogScopeItem>(FL.WebRequestScopes.Values))
				{
					if (item.Priority == prio && item.WasWritten)
					{
						item.IsRepeated = true;
						item.Write(this);
					}
				}
				foreach (var stack in FL.CurrentScopes.Values)
				{
					foreach (FieldLogScopeItem item in stack.ToArray())
					{
						if (item.Priority == prio && item.WasWritten)
						{
							item.IsRepeated = true;
							item.Write(this);
						}
					}
				}
			}
			else
			{
				// Validate existing file
				byte[] bytes = new byte[fileHeaderBytes.Length];
				if (fileStream.Read(bytes, 0, fileHeaderBytes.Length) < fileHeaderBytes.Length)
				{
					throw new FormatException("Invalid log file to append to. Header too short.");
				}
				for (int i = 0; i < fileHeaderBytes.Length; i++)
					if (bytes[i] != fileHeaderBytes[i])
						throw new FormatException("Invalid log file to append to. Wrong header.");

				int formatVersion = fileStream.ReadByte();
				if (formatVersion != FL.FileFormatVersion)
				{
					throw new FormatException("Invalid log file to append to. Unsupported file format version (" + formatVersion + ").");
				}

				// Read existing texts into the cache to re-use them
				ReadTextCache();
			}

			// Append any new log items to the end of the file (just to be sure)
			fileStream.Seek(0, SeekOrigin.End);
		}

		#endregion Constructor

		#region Public properties

		/// <summary>
		/// Gets the full name of the file that this writer writes to.
		/// </summary>
		public string FileName { get; private set; }

		/// <summary>
		/// Gets the file contents size.
		/// </summary>
		public int Length
		{
			get
			{
				return (int) fileStream.Position;
			}
		}

		/// <summary>
		/// Gets the time when the file was created, in UTC. Parses the file name's encoded timestamp.
		/// </summary>
		public DateTime CreatedTime
		{
			get
			{
				return createdTime;
			}
		}

		#endregion Public properties

		#region Text cache

		private void ReadTextCache()
		{
			byte[] bytes = new byte[4];

			// Read from the file till the end
			while (fileStream.Position < fileStream.Length)
			{
				// Remember the log item start position in the file
				int pos = (int) fileStream.Position;
				// Read the item type and length
				if (fileStream.Read(bytes, 0, bytes.Length) < bytes.Length)
				{
					throw new FormatException("Invalid log file to append to. Log item header too short.");
				}
				// Parse type and length data
				FieldLogItemType type = (FieldLogItemType) ((bytes[0] & 0xF0) >> 4);
				bytes[0] = (byte) (bytes[0] & 0x0F);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(bytes);
				int length = BitConverter.ToInt32(bytes, 0);
				// Check whether this is a text item
				if (type == FieldLogItemType.StringData)
				{
					// Read the item and add it to the text cache
					byte[] stringBytes = new byte[length];
					if (fileStream.Read(stringBytes, 0, length) < length)
					{
						throw new FormatException("Invalid log file to append to. Text log item shorter than indicated.");
					}
					// Parse the text data
					string str = Encoding.UTF8.GetString(stringBytes);
					// Add text to the cache
					textCache[str] = pos;
				}
				else
				{
					// Seek over the item and verify that it worked
					long expected = fileStream.Position + length;
					if (fileStream.Seek(length, SeekOrigin.Current) != expected)
					{
						throw new FormatException("Invalid log file to append to. Log item shorter than indicated.");
					}
				}
			}
		}

		public int GetText(string text)
		{
			if (text != null)
			{
				int pos;
				if (textCache.TryGetValue(text, out pos))
				{
					return pos;
				}

				pos = (int) fileStream.Position;   // Log files are restricted to < 2 GiB by convention
				byte[] bytes = Encoding.UTF8.GetBytes(text);
				byte[] lengthBytes = BitConverter.GetBytes(bytes.Length);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(lengthBytes);
				lengthBytes[0] = (byte) (lengthBytes[0] & 0x0F | ((byte) FieldLogItemType.StringData << 4));

				fileStream.Write(lengthBytes, 0, lengthBytes.Length);
				fileStream.Write(bytes, 0, bytes.Length);

				textCache[text] = pos;
				return pos;
			}
			else
			{
				return -1;
			}
		}

		#endregion Text cache

		#region Data buffer

		internal void SetItemType(FieldLogItemType itemType)
		{
			this.itemType = (byte) itemType;
		}

		internal void AddBuffer(byte b)
		{
			buffer.Add(b);
		}

		internal void AddBuffer(short s)
		{
			byte[] bytes = BitConverter.GetBytes(s);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			buffer.AddRange(bytes);
		}

		internal void AddBuffer(int i)
		{
			byte[] bytes = BitConverter.GetBytes(i);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			buffer.AddRange(bytes);
		}

		internal void AddBuffer(long l)
		{
			byte[] bytes = BitConverter.GetBytes(l);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			buffer.AddRange(bytes);
		}

		internal void AddBuffer(ushort s)
		{
			byte[] bytes = BitConverter.GetBytes(s);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			buffer.AddRange(bytes);
		}

		internal void AddBuffer(uint i)
		{
			byte[] bytes = BitConverter.GetBytes(i);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			buffer.AddRange(bytes);
		}

		internal void AddBuffer(ulong l)
		{
			byte[] bytes = BitConverter.GetBytes(l);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			buffer.AddRange(bytes);
		}

		internal void AddBuffer(byte[] bytes)
		{
			buffer.AddRange(bytes);
		}

		internal void AddBuffer(string str)
		{
			int pos = GetText(str);
			byte[] bytes = BitConverter.GetBytes(pos);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			buffer.AddRange(bytes);
		}

		internal void WriteBuffer()
		{
			int length = buffer.Count;
			if (length > 268435455)   // 2^28 - 1
			{
				// The item is longer than the maximum length that can be stored.
				// Drop the item to keep the log file readable.
				buffer.Clear();
				return;
			}
			byte[] bytes = BitConverter.GetBytes(length);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			bytes[0] |= (byte) (itemType << 4);
			fileStream.Write(bytes, 0, bytes.Length);

			fileStream.Write(buffer.ToArray(), 0, buffer.Count);
			buffer.Clear();
		}

		#endregion Data buffer

		#region Public methods

		/// <summary>
		/// Flushes the file stream.
		/// </summary>
		public void Flush()
		{
			fileStream.Flush();
		}

		/// <summary>
		/// Gets the file size on disk. Considers NTFS compression if available and used.
		/// </summary>
		public static int GetCompressedFileSize(string fileName)
		{
			if (supportsGetCompressedFileSize)
			{
				try
				{
					uint highDword;
					uint size = GetCompressedFileSizeW(fileName, out highDword);
					return (int) size;
				}
				catch
				{
					supportsGetCompressedFileSize = false;
				}
			}
			return (int) new FileInfo(fileName).Length;
		}

		#endregion Public methods

		#region IDispose members

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
