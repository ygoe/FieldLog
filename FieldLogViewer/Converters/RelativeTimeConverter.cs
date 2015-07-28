using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Unclassified.FieldLogViewer.Converters
{
	internal class RelativeTimeConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			// Convert a time to a time of day, milliseconds or microseconds string or a relative time span
			if (values.Length == 4 &&
				values[0] is DateTime &&
				(values[1] is DateTime || values[1] == DependencyProperty.UnsetValue) &&
				values[2] is bool &&
				values[3] is int)
			{
				DateTime itemTime = (DateTime)values[0];
				bool showRelativeTime = (bool)values[2];
				int utcOffset = (int)values[3];

				if (showRelativeTime && values[1] != DependencyProperty.UnsetValue)
				{
					DateTime selectedTime = (DateTime)values[1];

					TimeSpan diff = itemTime - selectedTime;
					// U+2005 4-per-m space, U+2006 6-per-m space, U+2007 Figure space
					// Keep a hyphen's width in spaces to avoid jumping content while scrolling
					// the positive or negative times out of view.
					string minus = "\u2006\u2005";
					if (diff.Ticks < 0)
					{
						minus = "-";
						diff = diff.Negate();
					}

					if (parameter as string == "ms")
					{
						return "." + diff.Milliseconds.ToString().PadLeft(3, '0');
					}
					else if (parameter as string == "us")
					{
						return ((diff.Ticks / 10) % 1000).ToString().PadLeft(3, '0');
					}
					else
					{
						if (diff.Days == 0 && diff.Hours < 10)
						{
							// Keep a digit's width in spaces to have the same width as a time of
							// day string when no reference item is selected
							minus = "\u2007" + minus;
						}

						return minus +
							(diff.Days * 24 + diff.Hours).ToString() + ":" +
							diff.Minutes.ToString().PadLeft(2, '0') + ":" +
							diff.Seconds.ToString().PadLeft(2, '0');
					}
				}
				else
				{
					switch (App.Settings.ItemTimeMode)
					{
						case ItemTimeType.Local:
							itemTime = itemTime.ToLocalTime();
							break;
						case ItemTimeType.Remote:
							itemTime = itemTime.AddMinutes(utcOffset);
							break;
					}

					if (parameter as string == "ms")
					{
						return "." + itemTime.Millisecond.ToString().PadLeft(3, '0');
					}
					else if (parameter as string == "us")
					{
						return ((itemTime.Ticks / 10) % 1000).ToString().PadLeft(3, '0');
					}
					else
					{
						// Keep a hyphen's width in spaces (only for relative time and no selection)
						return (showRelativeTime ? "\u2006\u2005" : "") + itemTime.ToString("HH:mm:ss");
					}
				}
			}

			// Convert a time to a date string
			if (values.Length == 2 &&
				values[0] is DateTime &&
				values[1] is int)
			{
				DateTime itemTime = (DateTime)values[0];
				int utcOffset = (int)values[1];

				switch (App.Settings.ItemTimeMode)
				{
					case ItemTimeType.Local:
						itemTime = itemTime.ToLocalTime();
						break;
					case ItemTimeType.Remote:
						itemTime = itemTime.AddMinutes(utcOffset);
						break;
				}

				return itemTime.ToString("dd.MM.");
			}

			return DependencyProperty.UnsetValue;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
