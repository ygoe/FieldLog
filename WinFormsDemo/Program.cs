using System;
using System.Linq;
using System.Windows.Forms;
using Unclassified.FieldLog;

namespace WinFormsDemo
{
	internal static class Program
	{
		[STAThread]
		private static void Main()
		{
			FL.AcceptLogFileBasePath();

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}
	}
}
