using System;
using System.Collections;
using System.Linq;
using System.Windows.Data;
using Unclassified.FieldLogViewer.ViewModels;

namespace Unclassified.FieldLogViewer.Converters
{
	internal class ListItemsConverter : IMultiValueConverter
	{
		#region IMultiValueConverter Member

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			IList selectedItems = (IList) values[0];
			int selectedCount = (int) values[1];
			int itemsCount = (int) values[2];

			if (itemsCount == 0)
			{
				return new DetailsMessageViewModel(
					"Nothing available",
					"Currently no log items are available. Either load a log file, wait for new log items to be written or adjust your filter to see existing log items."
					/*, "ArrowUp"*/)
					{
						ShowAutoLoadCheckBox = MainViewModel.Instance != null && MainViewModel.Instance.LoadedBasePath == null
					};
			}

			if (selectedItems.Count == 0)
			{
				return new DetailsMessageViewModel(
					"Nothing selected",
					"Select a log item from the list to display it."
					/*, "ArrowLeft"*/);
			}

			if (selectedItems.Count == 1)
			{
				return selectedItems[0];
			}

			Type firstType = null;
			foreach (object item in selectedItems)
			{
				if (firstType == null)
				{
					firstType = item.GetType();
				}
				else if (item.GetType() != firstType)
				{
					return new DetailsMessageViewModel(
						"Inconsistent selection",
						"Multiple items of different types are selected. Only elements of the same type can be displayed and edited concurrently.",
						"Flash");
				}
			}

			if (firstType.IsSubclassOf(typeof(FieldLogItemViewModel)))
			{
				return new DetailsMessageViewModel(
					selectedItems.Count + " items selected",
					"Multiple log items are selected. An aggregated view of log items is not yet available.");
				//TextKeyViewModel[] nodes = new TextKeyViewModel[items.Count];
				//for (int i = 0; i < items.Count; i++)
				//{
				//    nodes[i] = (TextKeyViewModel) items[i];
				//}
				//return new TextKeyMultiViewModel(nodes);
			}

			return new DetailsMessageViewModel(selectedItems.Count + " " + firstType.Name + " items selected");
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion IMultiValueConverter Member
	}
}
