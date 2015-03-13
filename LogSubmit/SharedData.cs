using System;
using System.Collections.Generic;
using Unclassified.LogSubmit.Transports;

namespace Unclassified.LogSubmit
{
	internal class SharedData
	{
		#region Singleton pattern

		private static SharedData instance;

		public static SharedData Instance
		{
			get
			{
				if (instance == null) instance = new SharedData();
				return instance;
			}
		}

		private SharedData()
		{
			OpenDisposables = new List<IDisposable>();
			TempFiles = new List<string>();
		}

		#endregion Singleton pattern

		#region Data properties

		public string[] LogBasePaths { get; set; }
		public DateTime LastLogUpdateTime { get; set; }
		public TimeSpan LogTimeSpan { get; set; }
		public string Notes { get; set; }
		public string EMailAddress { get; set; }
		public string ArchiveFileName { get; set; }
		public long ArchiveFileSize { get; set; }
		public TransportBase Transport { get; set; }
		public bool FromErrorDialog { get; set; }
		public bool FromShortcut { get; set; }
		public List<IDisposable> OpenDisposables { get; private set; }
		public List<string> TempFiles { get; private set; }
		public bool InteractiveEMail { get; set; }
		public string MailTransportRecipientAddress { get; set; }

		#endregion Data properties
	}
}
