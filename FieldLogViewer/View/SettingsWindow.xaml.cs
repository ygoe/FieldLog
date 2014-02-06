using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.View
{
	public partial class SettingsWindow : Window
	{
		#region Constructors

		public SettingsWindow()
		{
			InitializeComponent();

			// TODO: This seems to work in a test case application, but not here and not in TxEditor
			this.HideIcon();
			this.HideMinimizeAndMaximizeBoxes();

			WindowStartupLocation = WindowStartupLocation.Manual;
			Left = AppSettings.Instance.Window.SettingsLeft;
			Top = AppSettings.Instance.Window.SettingsTop;
			Width = AppSettings.Instance.Window.SettingsWidth;
			Height = AppSettings.Instance.Window.SettingsHeight;
		}

		#endregion Constructors

		#region Window event handlers

		private void Window_LocationChanged(object sender, EventArgs e)
		{
			if (AppSettings.Instance != null)
			{
				AppSettings.Instance.Window.SettingsLeft = (int) RestoreBounds.Left;
				AppSettings.Instance.Window.SettingsTop = (int) RestoreBounds.Top;
				AppSettings.Instance.Window.SettingsWidth = (int) RestoreBounds.Width;
				AppSettings.Instance.Window.SettingsHeight = (int) RestoreBounds.Height;
			}
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Window_LocationChanged(this, EventArgs.Empty);
		}

		#endregion Window event handlers

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var comboBox = sender as ComboBox;
			if (comboBox == null) return;

			var textBox = FindChild(comboBox, "PART_EditableTextBox", typeof(TextBox)) as TextBox;
			if (textBox == null) return;

			textBox.SelectAll();
		}

		private DependencyObject FindChild(DependencyObject reference, string childName, Type childType)
		{
			DependencyObject foundChild = null;
			if (reference != null)
			{
				int childrenCount = VisualTreeHelper.GetChildrenCount(reference);
				for (int i = 0; i < childrenCount; i++)
				{
					var child = VisualTreeHelper.GetChild(reference, i);
					// If the child is not of the request child type child
					if (child.GetType() != childType)
					{
						// recursively drill down the tree
						foundChild = FindChild(child, childName, childType);
					}
					else if (!string.IsNullOrEmpty(childName))
					{
						var frameworkElement = child as FrameworkElement;
						// If the child's name is set for search
						if (frameworkElement != null && frameworkElement.Name == childName)
						{
							// if the child's name is of the request name
							foundChild = child;
							break;
						}
					}
					else
					{
						// child element found.
						foundChild = child;
						break;
					}
				}
			}
			return foundChild;
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			MainWindow.Instance.Focus();
		}
	}
}
