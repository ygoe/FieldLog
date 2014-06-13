using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows;

namespace Unclassified.UI
{
	public static class ListBoxExtensions
	{
		public static void FocusItem(this ListBox listBox, object item)
		{
			//EventHandler icgStatusChanged = null;
			//icgStatusChanged = (sender, args) =>
			//{
			//    if (listBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
			//    {
			//        listBox.ItemContainerGenerator.StatusChanged -= icgStatusChanged;
			//        Dispatcher.CurrentDispatcher.BeginInvoke(
			//            DispatcherPriority.Input,
			//            new Action(() =>
			//            {
			//                var uie = listBox.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
			//                if (uie != null)
			//                {
			//                    uie.Focus();
			//                }
			//            }));
			//    }
			//};
			
			//listBox.ItemContainerGenerator.StatusChanged += icgStatusChanged;
			////listBox.SelectedItem = item;


			Dispatcher.CurrentDispatcher.BeginInvoke(
				DispatcherPriority.Input,
				new Action(() =>
				{
					var uie = listBox.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
					if (uie != null)
					{
						uie.Focus();
					}
				}));
		}
	}
}
