using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.FieldLogViewer.Views
{
	public partial class SettingsWindow : Window
	{
		#region Constructors

		public SettingsWindow()
		{
			InitializeComponent();

			this.HideIcon();
			this.HideMinimizeAndMaximizeBoxes();

			Width = 600;
			Height = 300;
			SettingsHelper.BindWindowState(this, App.Settings.SettingsWindowState);
		}

		#endregion Constructors

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			var comboBox = sender as ComboBox;
			if (comboBox == null) return;

			var textBox = comboBox.FindVisualChild<TextBox>("PART_EditableTextBox");
			if (textBox == null) return;

			textBox.SelectAll();
		}

		private void Window_Closed(object sender, EventArgs args)
		{
			MainWindow.Instance.Focus();
		}
	}
}
