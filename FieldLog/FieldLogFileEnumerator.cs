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
using System.Threading;
#if !NET20
using System.Threading.Tasks;
#endif

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Supports the iteration over log items in a sequence of FieldLogFileReader instances linked
	/// by their NextReader property.
	/// </summary>
	public class FieldLogFileEnumerator : IEnumerator<FieldLogItem>
	{
		/// <summary>
		/// Occurs when there was a problem reading a log file.
		/// </summary>
		public event ErrorEventHandler Error;

		private FieldLogFileReader reader;
		private FieldLogFileReader firstReader;
		private FieldLogItem item;

		/// <summary>
		/// Initialises a new instance of the FieldLogFileEnumerator class.
		/// </summary>
		/// <param name="reader">The first file reader instance.</param>
		public FieldLogFileEnumerator(FieldLogFileReader reader)
		{
			this.reader = reader;
			this.firstReader = reader;
		}
		
		/// <summary>
		/// Gets the log item at the current position of the enumerator.
		/// </summary>
		public FieldLogItem Current
		{
			get { return item; }
		}

		/// <summary>
		/// Gets the log item at the current position of the enumerator.
		/// </summary>
		object System.Collections.IEnumerator.Current
		{
			get { return item; }
		}

		/// <summary>
		/// Disposes of all FieldLogFileReader in this enumerator, beginning with the first.
		/// </summary>
		public void Dispose()
		{
			FieldLogFileReader r = firstReader;
			while (r != null)
			{
				FieldLogFileReader r2 = r;
				r = r.NextReader;
				r2.Dispose();
			}
		}

		/// <summary>
		/// Advances the enumerator to the next log item of the currently read log file. If there
		/// are no more items in this file and there is a NextReader set, the first log item of the
		/// next file reader is selected. If there are no more items in this file and WaitMode is
		/// set, the method will block until another log item is appended to the current file or
		/// the wait operation is cancelled by a close event.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the next log item;
		/// false if the enumerator has passed the end of the collection.</returns>
		public bool MoveNext()
		{
			FieldLogFileReader nextReader = null;
			do
			{
				if (nextReader != null)
				{
					FL.Trace(reader.ItemCount + " items read from " + Path.GetFileName(reader.FileName));
					reader = nextReader;
					FL.Trace("Switching to next reader " + Path.GetFileName(reader.FileName));
				}

				try
				{
					item = reader.ReadLogItem();
				}
				catch (Exception ex)
				{
					FL.Error(ex, "Reading item from log file");
					OnError(ex);
					// Skip the rest of the current file and continue with the next one if
					// available. If this is the last file and WaitMode is set, this priority will
					// not be monitored anymore.
					item = null;
				}
				
				if (item == null && reader.IsClosing)
				{
					// Close event must have been set
					Dispose();
					return false;
				}
				nextReader = reader.NextReader;
			}
			while (item == null && nextReader != null);
			return item != null;
		}

		/// <summary>
		/// Sets the enumerator to its initial position, which is before the first log item of the
		/// first log file. WARNING: This function is not used yet and may not work as expected.
		/// It is implemented as part of the IEnumerator interface and probably does not work
		/// correctly regarding the WaitMode flag because it is re-reading from used readers.
		/// </summary>
		public void Reset()
		{
			reader = firstReader;
			reader.Reset();
		}

		/// <summary>
		/// Sets the close signal for the currently used log file reader.
		/// </summary>
		public void Close()
		{
			reader.Close();
			// Also close all other readers of this enumerator, just to be sure
			FieldLogFileReader r = firstReader;
			do
			{
				r.Close();
				r = r.NextReader;
			}
			while (r != null);
		}

		/// <summary>
		/// Gets the first log file reader of this enumerator.
		/// </summary>
		public FieldLogFileReader FirstReader
		{
			get
			{
				return firstReader;
			}
		}

		/// <summary>
		/// Gets the last log file reader of this enumerator.
		/// </summary>
		public FieldLogFileReader LastReader
		{
			get
			{
				FieldLogFileReader r = reader;
				while (r.NextReader != null)
				{
					r = r.NextReader;
				}
				return r;
			}
		}

		/// <summary>
		/// Determines whether the enumerator contains the specified log file.
		/// </summary>
		/// <param name="fileName">The full name of the log file to locate in the enumerator.</param>
		/// <returns>true if the enumerator contains the specified log file reader; otherwise, false.</returns>
		public bool ContainsFile(string fileName)
		{
			FieldLogFileReader r = firstReader;
			while (r != null)
			{
				if (r.FileName == fileName)
				{
					return true;
				}
				r = r.NextReader;
			}
			return false;
		}

		/// <summary>
		/// Appends a new FieldLogFileReader at the end of this enumerator.
		/// </summary>
		/// <param name="newReader">The new reader to append.</param>
		/// <param name="fromFsw">Indicates whether the reader was created from a FileSystemWatcher event.</param>
		public void Append(FieldLogFileReader newReader, bool fromFsw)
		{
			FieldLogFileReader currentLastReader = LastReader;
			currentLastReader.NextReader = newReader;
			// Unset wait mode for this reader, now that we know where to continue after this file
			if (fromFsw)
			{
				// The file is newly created. Take some time to actually start reading the previous
				// file before switching to this one. Once the first item has been read from the
				// file, more items will likely exist in the file, and the file is read until the
				// end. Then it will still sit there waiting for more items until the rest of this
				// delay has elapsed (which is not a big problem, if we get any items from that
				// file at all).
#if NET20
				new Thread(() =>
				{
					Thread.Sleep(1000);
					currentLastReader.WaitMode = false;
				}).Start();
#else
				Task.Factory.StartNew(() =>
				{
					Thread.Sleep(1000);
					currentLastReader.WaitMode = false;
				});
#endif
			}
			else
			{
				currentLastReader.WaitMode = false;
			}
			FL.Trace("Appending next reader", "this=" + Path.GetFileName(currentLastReader.FileName) + "\nNext=" + Path.GetFileName(newReader.FileName) +
				"\nItems read from this=" + currentLastReader.ItemCount);
		}

		/// <summary>
		/// Raises the Error event.
		/// </summary>
		/// <param name="ex">An Exception that represents the error that occurred.</param>
		protected void OnError(Exception ex)
		{
			ErrorEventHandler handler = Error;
			if (handler != null)
			{
				handler(this, new ErrorEventArgs(ex));
			}
		}
	}
}
