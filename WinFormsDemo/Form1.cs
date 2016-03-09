using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Unclassified.FieldLog;

namespace WinFormsDemo
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void showErrorButton_Click(object sender, EventArgs args)
		{
			FL.ShowErrorDialog("An arbitrary error message.", this);
		}

		private void throwExceptionButton_Click(object sender, EventArgs args)
		{
			throw new NotImplementedException();
		}

		private void throwThreadExceptionButton_Click(object sender, EventArgs args)
		{
			new Thread(() => { throw new NotImplementedException(); }).Start();
		}
	}
}
