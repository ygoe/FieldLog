using System;
using System.ComponentModel;
using System.Linq;

namespace Unclassified.LogSubmit.Transports
{
	internal abstract class TransportBase
	{
		public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

		public virtual bool CanExecute()
		{
			return true;
		}

		public void Execute(BackgroundWorker backgroundWorker)
		{
			OnExecute(backgroundWorker);
		}

		protected abstract void OnExecute(BackgroundWorker backgroundWorker);

		protected void ReportProgress(int percentProgress)
		{
			var handler = ProgressChanged;
			if (handler != null)
			{
				handler(this, new ProgressChangedEventArgs(percentProgress, null));
			}
		}
	}
}
