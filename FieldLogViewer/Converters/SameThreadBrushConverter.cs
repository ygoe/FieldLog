using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;

namespace Unclassified.FieldLogViewer.Converters
{
	class SameThreadBrushConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (values.Length == 4 &&
				values[0] is int &&
				values[1] is int &&
				values[2] is Guid &&
				values[3] is Guid)
			{
				int threadId1 = (int) values[0];
				int threadId2 = (int) values[1];
				Guid sessionId1 = (Guid) values[2];
				Guid sessionId2 = (Guid) values[3];

				if (threadId1 == threadId2 && sessionId1 == sessionId2)
				{
					Color c = SystemColors.HighlightColor;
					return new SolidColorBrush(Color.FromArgb(50, c.R, c.G, c.B));
				}
				else
				{
					return null;
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
