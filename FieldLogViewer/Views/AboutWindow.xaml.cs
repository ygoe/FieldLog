using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.Views
{
	public partial class AboutWindow : Window
	{
		public AboutWindow()
		{
			InitializeComponent();
			this.HideIcon();
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("http://unclassified.software/fieldlog");
		}
	}
}
