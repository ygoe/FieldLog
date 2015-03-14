using System;
using System.Linq;

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
	}
}
