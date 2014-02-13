using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using Unclassified;
using Unclassified.FieldLog;
using Unclassified.FieldLogViewer.View;
using Unclassified.FieldLogViewer.ViewModel;

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
			FL.AcceptLogFileBasePath();

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
			// Make sure the settings are properly saved in the end
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
		}

		#endregion Constructors

		#region Startup

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Initialise the settings system
			AppSettings.InitializeInstance();

			// Remember the version of the application.
			// If we need to react on settings changes from previous application versions, here is
			// the place to check the version currently in the settings, before it's overwritten.
			AppSettings.Instance.LastAppVersion = FL.AppVersion;

			// Make some more worker threads for the ThreadPool. We need around 10 threads for
			// reading a set of log files, and since some of them may be waiting for a long time,
			// blocking other files from reading, this is sometimes a bottleneck. Depending on what
			// files we have and what exactly is in them. ThreadPool will create a new worker
			// thread on demand only every 0.5 second which results in 1-2 seconds delay on loading
			// certain log file sets.
			// Source: http://stackoverflow.com/a/6000891/143684
			int workerThreads, ioThreads;
			ThreadPool.GetMinThreads(out workerThreads, out ioThreads);
			ThreadPool.SetMinThreads(20, ioThreads);

			// Create main window and view model
			var view = new MainWindow();
			var viewModel = new MainViewModel();
			view.DataContext = viewModel;

			if (e.Args.Length > 0)
			{
				bool singleFile = false;
				string fileName = e.Args[0];
				if (fileName == "/s")
				{
					if (e.Args.Length > 1)
					{
						singleFile = true;
						fileName = e.Args[1];
					}
					else
					{
						fileName = null;
					}
				}

				if (!string.IsNullOrWhiteSpace(fileName))
				{
					string prefix = fileName;
					if (!singleFile)
					{
						prefix = viewModel.GetPrefixFromPath(fileName);
					}
					if (prefix != null)
					{
						viewModel.OpenFiles(prefix, singleFile);
					}
					else
					{
						viewModel.OpenFiles(fileName, singleFile);
					}
				}
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
