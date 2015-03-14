using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Unclassified.LogSubmit
{
	internal static class CommonFormats
	{
		private const string TimeFormat = "yyyy-MM-dd  HH:mm";

		public static string DateTimeToString(DateTime time)
		{
			return time.ToString(TimeFormat);
		}

		public static string DateTimeToShortString(DateTime time)
		{
			DateTime now = DateTime.Now;
			if (time.Date == now.Date)
			{
				return time.ToString("HH:mm");
			}
			if (time.Date >= now.Date.AddDays(-6))
			{
				return time.ToString("ddd, HH:mm");
			}
			return time.ToString("yyyy-MM-dd");
		}

		// TODO: Replace by TxLib
		public static string TimeSpanToString(TimeSpan time)
		{
			if (time.TotalMinutes >= 55)
			{
				int hours = (int) Math.Round(time.TotalHours);
				if (hours == 1)
				{
					return hours + " hour";
				}
				else
				{
					return hours + " hours";
				}
			}
			else if (time.TotalSeconds >= 55)
			{
				int minutes = (int) Math.Round(time.TotalMinutes);
				if (minutes == 1)
				{
					return minutes + " minute";
				}
				else
				{
					return minutes + " minutes";
				}
			}
			else
			{
				int seconds = (int) Math.Round(time.TotalSeconds);
				if (seconds == 1)
				{
					return seconds + " second";
				}
				else
				{
					return seconds + " seconds";
				}
			}
		}

		// TODO: Replace by TxLib
		public static string DataSizeToString(long size)
		{
			if (size > 10 * 1024 * 1024)   // 10 MB
			{
				return ((double) size / 1024 / 1024).ToString("0") + " MB";
			}
			else if (size > 1024 * 1024)   // 1 MB
			{
				return ((double) size / 1024 / 1024).ToString("0.0") + " MB";
			}
			if (size > 10 * 1024)   // 10 kB
			{
				return ((double) size / 1024).ToString("0") + " kB";
			}
			else
			{
				return ((double) size / 1024).ToString("0.0") + " kB";
			}
		}
	}
}
