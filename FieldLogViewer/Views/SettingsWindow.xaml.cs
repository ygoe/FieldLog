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

			Width = 500;
			Height = 300;
			SettingsHelper.BindWindowState(this, App.Settings.SettingsWindow);
		}

		#endregion Constructors

		#region Window event handlers

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			this.HideIcon();
			this.HideMinimizeAndMaximizeBoxes();
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
