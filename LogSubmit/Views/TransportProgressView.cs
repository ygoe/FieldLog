using System;
using System.ComponentModel;
using System.Drawing;
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
				RemainingTimeLabel.Text = Tx.T("msg.starting");
				RemainingTimeLabel.ForeColor = SystemColors.ControlText;
			}
			else if (args.ProgressPercentage >= 5)
			{
				double elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
				double totalSeconds = elapsedSeconds * 100 / args.ProgressPercentage;
				if (totalSeconds >= 0 && totalSeconds < int.MaxValue)
				{
					TimeSpan remainingTime = TimeSpan.FromSeconds((int) (totalSeconds - elapsedSeconds));
					RemainingTimeLabel.Text = Tx.TimeSpanRaw(remainingTime, false);
					RemainingTimeLabel.ForeColor = SystemColors.ControlText;
				}
			}
		}

		private void TransportWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
		{
			if (args.Cancelled)
			{
				RemainingTimeLabel.Text = Tx.T("msg.cancelled");
				RemainingTimeLabel.ForeColor = SystemColors.ControlText;
				progressBar1.Value = 0;
			}
			else if (args.Error != null)
			{
				RemainingTimeLabel.Text = Tx.TC("msg.title.error") + " " + args.Error.Message;
				RemainingTimeLabel.ForeColor = Color.FromArgb(240, 0, 0);

				FinishedInfoLabel.Text = Tx.T("transport progress view.select another transport");
				FinishedInfoLabel.Show();
			}
			else
			{
				RemainingTimeLabel.Text = Tx.T("msg.completed");
				RemainingTimeLabel.ForeColor = Color.FromArgb(0, 160, 0);
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
