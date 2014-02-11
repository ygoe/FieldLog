using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace Unclassified.FieldLogViewer.Converters
{
	class RelativeTimeConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (values.Length == 3 &&
				values[0] is DateTime &&
				(values[1] is DateTime || values[1] == DependencyProperty.UnsetValue) &&
				values[2] is bool)
			{
				DateTime itemTime = (DateTime) values[0];
				bool showRelativeTime = (bool) values[2];

				if (showRelativeTime && values[1] != DependencyProperty.UnsetValue)
				{
					DateTime selectedTime = (DateTime) values[1];

					TimeSpan diff = itemTime - selectedTime;
					// U+2009 Thin space, U+200A Hair space
					// Keep a hyphen's width in spaces to avoid jumping content while scrolling
					// the negative times out of view.
					string minus = "\u2009\u200A\u200A";
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
						return minus +
							(diff.Days * 24 + diff.Hours).ToString() + ":" +
							diff.Minutes.ToString().PadLeft(2, '0') + ":" +
							diff.Seconds.ToString().PadLeft(2, '0');
					}
				}
				else
				{
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
						return itemTime.ToString("HH:mm:ss");
					}
				}
			}

			return DependencyProperty.UnsetValue;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
