using System;
using System.Windows.Forms;

namespace Unclassified.Util
{
	public class FileDrop : IDataObject
	{
		private string[] myData;

		public FileDrop()
		{
			myData = new string[] { };
		}

		public FileDrop(params string[] files)
		{
			myData = files;
		}

		#region IDataObject Members

		public object GetData(Type format)
		{
			if (format.Name == "FileDrop") return myData;
			throw new Exception("Data format not present.");
		}

		public object GetData(string format)
		{
			if (format == "FileDrop") return myData;
			throw new Exception("Data format not present.");
		}

		public object GetData(string format, bool autoConvert)
		{
			return GetData(format);
		}

		public bool GetDataPresent(Type format)
		{
			if (format.Name == "FileDrop") return true;
			return false;
		}

		public bool GetDataPresent(string format)
		{
			if (format == "FileDrop") return true;
			return false;
		}

		public bool GetDataPresent(string format, bool autoConvert)
		{
			return GetDataPresent(format);
		}

		public string[] GetFormats()
		{
			return new string[] { "FileDrop" };
		}

		public string[] GetFormats(bool autoConvert)
		{
			return new string[] { "FileDrop" };
		}

		public void SetData(object data)
		{
			if (data is string[])
				myData = data as string[];
			else
				throw new Exception("Invalid data type.");
		}

		public void SetData(Type format, object data)
		{
			if (format.Name != "FileDrop") throw new Exception("Data format not allowed.");
			if (data is string[])
				myData = data as string[];
			else
				throw new Exception("Invalid data type.");
		}

		public void SetData(string format, object data)
		{
			if (format != "FileDrop") throw new Exception("Data format not allowed.");
			if (data is string[])
				myData = data as string[];
			else
				throw new Exception("Invalid data type.");
		}

		public void SetData(string format, bool autoConvert, object data)
		{
			SetData(format, data);
		}

		#endregion IDataObject Members
	}
}
