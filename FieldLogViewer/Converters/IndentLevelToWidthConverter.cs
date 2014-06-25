using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Unclassified.FieldLogViewer.Converters
{
	internal class IndentLevelToWidthConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (values.Length == 2 &&
				values[0] is int &&
				values[1] is int)
			{
				int level = (int) values[0];
				int indentSize = (int) values[1];

				return new Thickness(level * indentSize, 0, 0, 0);
			}

			return new Thickness();
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
