using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class LogItemViewModelBase : ViewModelBase, IComparable<LogItemViewModelBase>
	{
		public int EventCounter { get; set; }
		public DateTime Time { get; set; }
		public int UtcOffset { get; set; }

		private int indentLevel;
		public int IndentLevel
		{
			get { return indentLevel; }
			set { CheckUpdate(value, ref indentLevel, "IndentLevel"); }
		}

		public string DisplayTime
		{
			get
			{
				switch (AppSettings.Instance.ItemTimeMode)
				{
					case ItemTimeType.Utc:
						return Time.ToString("yyyy-MM-dd  HH:mm:ss.ffffff") + "  UTC";
					case ItemTimeType.Local:
						return Time.ToLocalTime().ToString("yyyy-MM-dd  HH:mm:ss.ffffff");
					case ItemTimeType.Remote:
						int hours = UtcOffset / 60;
						int mins = Math.Abs(UtcOffset) % 60;
						return Time.AddMinutes(UtcOffset).ToString("yyyy-MM-dd  HH:mm:ss.ffffff") + "  " +
							hours.ToString("+00;-00;+00") + ":" + mins.ToString("00");
				}
				return null;
			}
		}

		public void RaiseDisplayTimeChanged()
		{
			OnPropertyChanged("DisplayTime");
		}

		public virtual int CompareTo(LogItemViewModelBase other)
		{
			// First compare the items by time
			int i = this.Time.CompareTo(other.Time);
			if (i != 0) return i;

			// If the time is equal, consider the event counter value and handle overflows
			const int range = 10000;
			if (EventCounter > int.MaxValue - range && other.EventCounter < int.MinValue + range)
			{
				// Overflow, other is newer
				return -1;
			}
			if (EventCounter < int.MinValue + range && other.EventCounter > int.MaxValue - range)
			{
				// Overflow, other is older
				return 1;
			}
			return EventCounter.CompareTo(other.EventCounter);
		}

		/// <summary>
		/// Refreshes all data in the item that can be resolved from other sources. Deriving
		/// classes override this method to refresh the relevant item data.
		/// </summary>
		public virtual void Refresh()
		{
		}
	}
}
