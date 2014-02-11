using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace Unclassified.FieldLogViewer.Converters
{
	class IndentLevelToWidthConverter : IMultiValueConverter
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
