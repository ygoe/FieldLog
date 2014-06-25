using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Unclassified.FieldLogViewer.Converters
{
	internal class SameWebRequestBrushConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (values.Length == 4 &&
				values[0] is uint &&
				values[1] is uint &&
				values[2] is Guid &&
				values[3] is Guid)
			{
				uint webRequestId1 = (uint) values[0];
				uint webRequestId2 = (uint) values[1];
				Guid sessionId1 = (Guid) values[2];
				Guid sessionId2 = (Guid) values[3];

				if (webRequestId1 == webRequestId2 && sessionId1 == sessionId2)
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
