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
	}
}
