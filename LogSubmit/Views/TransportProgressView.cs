using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Unclassified.TxLib;

namespace Unclassified.LogSubmit.Views
{
	public partial class TransportProgressView : UserControl, IView
	{
		#region Private data

		private bool finished;
		private DateTime startTime;
		private DateTime lastProgressTime;

		#endregion Private data

		#region Constructors

		public TransportProgressView()
		{
			InitializeComponent();

			TxDictionaryBinding.AddTextBindings(this);

			Dock = DockStyle.Fill;
		}

		#endregion Constructors

		#region Control event handlers

		private void TransportWorker_DoWork(object sender, DoWorkEventArgs args)
		{
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
			TransportWorker.ReportProgress(1000);
		}

		private void transport_ProgressChanged(object sender, ProgressChangedEventArgs args)
		{
			TransportWorker.ReportProgress(args.ProgressPercentage);
		}

		private void TransportWorker_ProgressChanged(object sender, ProgressChangedEventArgs args)
		{
			progressBar1.Value = args.ProgressPercentage;   // Permille
			if (args.ProgressPercentage == 0)
			{
				startTime = DateTime.UtcNow;
			}
			else if (args.ProgressPercentage >= 20)   // Permille
			{
				if (DateTime.UtcNow > lastProgressTime.AddMilliseconds(500))
				{
					lastProgressTime = DateTime.UtcNow;
					double elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
					double totalSeconds = elapsedSeconds * 1000 / args.ProgressPercentage;
					if (totalSeconds >= 0 && totalSeconds < int.MaxValue)
					{
						TimeSpan remainingTime = TimeSpan.FromSeconds((int)Math.Ceiling(totalSeconds - elapsedSeconds));
						RemainingTimeLabel.Text = Tx.TimeSpanRaw(remainingTime, false);
					}
				}
			}
		}

		private void TransportWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
		{
			if (args.Cancelled)
			{
				RemainingTimeLabel.Text = Tx.T("msg.cancelled");
				progressBar1.Value = 0;
			}
			else if (args.Error != null)
			{
				RemainingTimeLabel.Text = Tx.T("msg.title.error");

				ErrorLabel.Text = args.Error.Message;
				ErrorPanel.Show();

				FinishedInfoLabel.Text = Tx.T("transport progress view.select another transport");
				FinishedInfoLabel.Show();
			}
			else
			{
				RemainingTimeLabel.Text = Tx.T("msg.completed");
				finished = true;

				SuccessPanel.Show();

				if (SharedData.Instance.FromShortcut)
				{
					FinishedInfoLabel.Text = Tx.T("transport progress view.delete shortcut");
					FinishedInfoLabel.Show();
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
					RemainingTimeLabel.Text = Tx.T("msg.starting");
					TransportWorker.RunWorkerAsync();
				}
				else
				{
					MessageBox.Show(
						Tx.T("msg.previous operation"),
						Tx.T("msg.title.error"),
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
				ErrorPanel.Hide();
				SuccessPanel.Hide();
				FinishedInfoLabel.Hide();
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
