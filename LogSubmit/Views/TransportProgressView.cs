using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Unclassified.LogSubmit.Views
{
	public partial class TransportProgressView : UserControl, IView
	{
		#region Private data

		private bool finished;
		private DateTime startTime;

		#endregion Private data

		#region Constructors

		public TransportProgressView()
		{
			InitializeComponent();

			Dock = DockStyle.Fill;
		}

		#endregion Constructors

		#region Control event handlers

		private void TransportWorker_DoWork(object sender, DoWorkEventArgs args)
		{
			// Quickly set the UI state
			TransportWorker.ReportProgress(0);

			var transport = SharedData.Instance.Transport;

			transport.ProgressChanged += transport_ProgressChanged;
			try
			{
				transport.Execute(TransportWorker);
			}
			finally
			{
				transport.ProgressChanged -= transport_ProgressChanged;
			}

			if (TransportWorker.CancellationPending)
			{
				args.Cancel = true;
				return;
			}
			TransportWorker.ReportProgress(100);
		}

		private void transport_ProgressChanged(object sender, ProgressChangedEventArgs args)
		{
			TransportWorker.ReportProgress(args.ProgressPercentage);
		}

		private void TransportWorker_ProgressChanged(object sender, ProgressChangedEventArgs args)
		{
			progressBar1.Value = args.ProgressPercentage;
			if (args.ProgressPercentage == 0)
			{
				startTime = DateTime.UtcNow;
				RemainingTimeLabel.Text = "Starting…";
				RemainingTimeLabel.ForeColor = SystemColors.ControlText;
			}
			else if (args.ProgressPercentage >= 5)
			{
				double elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
				double totalSeconds = elapsedSeconds * 100 / args.ProgressPercentage;
				if (totalSeconds >= 0 && totalSeconds < int.MaxValue)
				{
					TimeSpan remainingTime = TimeSpan.FromSeconds((int) (totalSeconds - elapsedSeconds));
					RemainingTimeLabel.Text = CommonFormats.TimeSpanToString(remainingTime);
					RemainingTimeLabel.ForeColor = SystemColors.ControlText;
				}
			}
		}

		private void TransportWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
		{
			if (args.Cancelled)
			{
				RemainingTimeLabel.Text = "Cancelled";
				RemainingTimeLabel.ForeColor = SystemColors.ControlText;
				progressBar1.Value = 0;
			}
			else if (args.Error != null)
			{
				RemainingTimeLabel.Text = "Error: " + args.Error.Message;
				RemainingTimeLabel.ForeColor = Color.FromArgb(240, 0, 0);
			}
			else
			{
				RemainingTimeLabel.Text = "Completed";
				RemainingTimeLabel.ForeColor = Color.FromArgb(0, 160, 0);
				finished = true;

				if (SharedData.Instance.FromShortcut)
				{
					FinishedLabel.Text = "You can now delete the shortcut you used to start this program.";
					FinishedLabel.Show();
				}

				UpdateButtons();
			}
		}

		#endregion Control event handlers

		#region Public methods

		public void Activate(bool forward)
		{
			if (forward)
			{
				finished = false;
				if (!TransportWorker.IsBusy)
				{
					TransportWorker.RunWorkerAsync();
				}
				else
				{
					MessageBox.Show(
						"The operation could not be started because a previous operation has not yet stopped. Please wait for the current operation to stop and retry by going back and returning to this page again.",
						"Error",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);
				}
			}

			UpdateButtons();
		}

		public void Deactivate(bool forward)
		{
			if (TransportWorker.IsBusy)
			{
				TransportWorker.CancelAsync();
			}
			if (!forward)
			{
				MainForm.Instance.FinishEnabled = false;
			}
		}

		#endregion Public methods

		#region Private methods

		private void UpdateButtons()
		{
			MainForm.Instance.BackEnabled = true;
			MainForm.Instance.NextEnabled = false;
			MainForm.Instance.FinishEnabled = finished;
		}

		#endregion Private methods
	}
}
