using System.Collections.Generic;

namespace Unclassified.FieldLog
{
	public class FieldLogFileEnumerator : IEnumerator<FieldLogItem>
	{
		private FieldLogFileReader reader;
		private FieldLogFileReader firstReader;
		private FieldLogItem item;

		public FieldLogFileEnumerator(FieldLogFileReader reader)
		{
			this.reader = reader;
			this.firstReader = reader;
		}
		
		public FieldLogItem Current
		{
			get { return item; }
		}

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

		object System.Collections.IEnumerator.Current
		{
			get { return item; }
		}

		public bool MoveNext()
		{
			item = reader.ReadLogItem();
			if (item == null && reader.IsClosing)
			{
				// Close event must have been set
				Dispose();
				return false;
			}
			FieldLogFileReader nextReader = reader.NextReader;
			while (item == null && nextReader != null)
			{
				reader = nextReader;
				reader.Reset();
				item = reader.ReadLogItem();
				nextReader = reader.NextReader;
			}
			return item != null;
		}

		public void Reset()
		{
			reader = firstReader;
			reader.Reset();
		}

		/// <summary>
		/// Sets the close signal. This may only work if WaitMode is set.
		/// </summary>
		public void Close()
		{
			if (reader != null)
			{
				reader.Close();
			}
		}

		public FieldLogFileReader FirstReader
		{
			get
			{
				return firstReader;
			}
		}

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
	}
}
