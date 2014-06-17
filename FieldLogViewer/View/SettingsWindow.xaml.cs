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

			WindowStartupLocation = WindowStartupLocation.Manual;
			Left = AppSettings.Instance.Window.SettingsLeft;
			Top = AppSettings.Instance.Window.SettingsTop;
			Width = AppSettings.Instance.Window.SettingsWidth;
			Height = AppSettings.Instance.Window.SettingsHeight;
		}

		#endregion Constructors

		#region Window event handlers

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			this.HideIcon();
			this.HideMinimizeAndMaximizeBoxes();
		}

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

			var textBox = comboBox.FindVisualChild<TextBox>("PART_EditableTextBox");
			if (textBox == null) return;

			textBox.SelectAll();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			MainWindow.Instance.Focus();
		}
	}
}
