using System;
using System.ComponentModel;
using System.Linq;
using Unclassified.Util;

namespace Unclassified.LogSubmit.Transports
{
	internal class FileTransport : TransportBase
	{
		private int progress;
		private BackgroundWorker backgroundWorker;

		public string FileName { get; set; }

		protected override void OnExecute(BackgroundWorker backgroundWorker)
		{
			this.backgroundWorker = backgroundWorker;
			CopyEx.Copy(SharedData.Instance.ArchiveFileName, FileName, false, OnCopyProgress);
		}

		private void OnCopyProgress(object sender, CopyExEventArgs args)
		{
			if ((int)args.Progress > progress)
			{
				progress = (int)args.Progress;
				ReportProgress(progress);
			}

			if (backgroundWorker.CancellationPending)
			{
				args.Cancel = true;
			}
		}
	}
}
