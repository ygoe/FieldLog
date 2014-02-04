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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SmoothListTest
{
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			//listBox1.Items.Add(new DataItem(1, "Example name"));
			//listBox1.Items.Add(new DataItem(2, "Example name"));

		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.A)
			{
				Random rnd = new Random();
				for (int i = 1; i <= 10000; i++)
				{
					switch (rnd.Next(5))
					{
						case 0:
							listBox1.Items.Add(new DataItem(i, "Example name"));
							break;
						case 1:
							listBox1.Items.Add(new DataItem(i, "Some stupid demo text"));
							break;
						case 2:
							listBox1.Items.Add(new DataItem(i, "Nothing to see here, move on"));
							break;
						case 3:
							listBox1.Items.Add(new DataItem(i, "Max Mustermann"));
							break;
						case 4:
							listBox1.Items.Add(new DataItem(i, "Your license has expired!"));
							break;
					}
				}

				e.Handled = true;
				return;
			}
			
			base.OnKeyDown(e);
		}
	}
}
