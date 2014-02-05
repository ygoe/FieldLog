using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using Unclassified.FieldLogViewer.View;
using Unclassified.FieldLogViewer.ViewModel;
using Unclassified;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer
{
	public partial class App : Application
	{
		#region Application entry point

		/// <summary>
		/// Application entry point.
		/// </summary>
		[STAThread]
		public static void Main()
		{
			App app = new App();
			app.InitializeComponent();
			app.Run();
		}

		#endregion Application entry point

		#region Setup detection mutex

		private Mutex appMutex = new Mutex(false, "Unclassified.FieldLogViewer");

		#endregion Setup detection mutex

		#region Constructors

		/// <summary>
		/// Initialises a new instance of the App class.
		/// </summary>
		public App()
		{
			// Initialise the settings system
			AppSettings.InitializeInstance();

			// Make sure the settings are properly saved in the end
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
		}

		#endregion Constructors

		#region Startup

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			FL.AcceptLogFileBasePath();

			// Create main window and view model
			var view = new MainWindow();
			var viewModel = new MainViewModel();
			view.DataContext = viewModel;

			if (e.Args.Length > 0)
			{
				viewModel.OpenFiles(e.Args[0]);
			}

			// Show the main window
			view.Show();
		}

		#endregion Startup

		#region Event handlers

		/// <summary>
		/// Called when the current process exits.
		/// </summary>
		/// <remarks>
		/// The processing time in this event is limited. All handlers of this event together must
		/// not take more than ca. 3 seconds. The processing will then be terminated.
		/// </remarks>
		private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			AppSettings.CloseInstance();
		}

		#endregion Event handlers
	}
}
