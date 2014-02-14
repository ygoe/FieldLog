using System;
using System.Collections.Generic;
using System.IO;

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
					reader = nextReader;
					reader.Reset();
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
		/// first log file.
		/// </summary>
		public void Reset()
		{
			reader = firstReader;
			reader.Reset();
		}

		/// <summary>
		/// Sets the close signal for the currently used log file reader. This may only have the
		/// desired effect if WaitMode is set for this log file reader.
		/// </summary>
		public void Close()
		{
			if (reader != null)
			{
				reader.Close();
			}
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
