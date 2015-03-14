using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Unclassified.TxLib;

namespace Unclassified.LogSubmit.Views
{
	public partial class TimeSelectionView : UserControl, IView
	{
		#region Private data

		private List<TimeEntry> timeEntries = new List<TimeEntry>();

		#endregion Private data

		#region Constructors

		public TimeSelectionView()
		{
			timeEntries.Add(new TimeEntry(TimeSpan.FromMinutes(5)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromMinutes(15)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromMinutes(30)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromHours(1)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromHours(3)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromHours(6)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromHours(12)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromDays(1)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromDays(2)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromDays(3)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromDays(7)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromDays(10)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromDays(14)));
			timeEntries.Add(new TimeEntry(TimeSpan.FromDays(30)));
			timeEntries.Add(new TimeEntry { Text = Tx.T("time selection.everything"), TimeSpan = TimeSpan.MaxValue });

			InitializeComponent();

			TxDictionaryBinding.AddTextBindings(this);

			Dock = DockStyle.Fill;

			TimeTrackBar.Value = (timeEntries.Count - 1) - 4;
		}

		#endregion Constructors

		#region Event handlers

		private void TimeSelectionView_SizeChanged(object sender, EventArgs e)
		{
			SetTimeSpanLabelMargin();
		}

		#endregion Event handlers

		#region Control event handlers

		private void TimeTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TimeEntry te = timeEntries[(timeEntries.Count - 1) - TimeTrackBar.Value];
			SharedData.Instance.LogTimeSpan = te.TimeSpan;
			TimeSpanLabel.Text = te.Text;

			SetTimeSpanLabelMargin();

			UpdateTimeLabels();
		}

		private void WebLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start("http://unclassified.software/source/fieldlog");
		}

		#endregion Control event handlers

		#region Public methods

		public void Activate(bool forward)
		{
			UpdateTimeLabels();
			UpdateButtons();
		}

		public void Deactivate(bool forward)
		{
		}

		#endregion Public methods

		#region Private methods

		private void UpdateButtons()
		{
			MainForm.Instance.BackEnabled = true;
			MainForm.Instance.NextEnabled = true;
		}

		private void SetTimeSpanLabelMargin()
		{
			int desiredX = 12 + TimeTrackBar.Value * (TimeTrackBar.Width - 24) / TimeTrackBar.Maximum;
			Padding margin = new Padding();
			if (desiredX < TimeTrackBar.Width / 2)
			{
				margin.Left = desiredX - TimeSpanLabel.PreferredSize.Width / 2;
				TimeSpanLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			}
			else
			{
				margin.Right = TimeTrackBar.Width - desiredX - TimeSpanLabel.PreferredSize.Width / 2;
				TimeSpanLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			}
			TimeSpanLabel.Margin = margin;
		}

		private void UpdateTimeLabels()
		{
			DateTime time = SharedData.Instance.LastLogUpdateTime;
			if (time == DateTime.MinValue) return;
			time = time.ToLocalTime();
			LastUpdateTimeLabel.Text = CommonFormats.DateTimeToString(time);
			if (SharedData.Instance.LogTimeSpan != TimeSpan.MaxValue)
			{
				time -= SharedData.Instance.LogTimeSpan;
				LogMinTimeLabel.Text = CommonFormats.DateTimeToString(time);
			}
			else
			{
				LogMinTimeLabel.Text = Tx.T("time selection.everything");
			}
		}

		#endregion Private methods

		#region Classes

		private class TimeEntry
		{
			public TimeEntry()
			{
			}

			public TimeEntry(TimeSpan span)
			{
				Text = Tx.TimeSpanRaw(span, false);
				TimeSpan = span;
			}

			public string Text { get; set; }
			public TimeSpan TimeSpan { get; set; }

			public override string ToString()
			{
				return Text;
			}
		}

		#endregion Classes
	}
}
