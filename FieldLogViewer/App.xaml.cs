using System;
using System.Linq;
using System.Threading;
using System.Windows;
using Unclassified.FieldLog;
using Unclassified.FieldLogViewer.ViewModels;
using Unclassified.FieldLogViewer.Views;
using Unclassified.Util;

namespace Unclassified.FieldLogViewer
{
	public partial class App : Application
	{
		#region Startup

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

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

			// Fix WPF's built-in themes
			if (OSInfo.IsWindows8OrNewer)
			{
				ReAddResourceDictionary("/Resources/RealWindows8.xaml");
			}

			// Create main window and view model
			var view = new MainWindow();
			var viewModel = new MainViewModel();
			view.DataContext = viewModel;

			//viewModel.AddObfuscationMap(@"D:\tmp\Map.xml");

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

		private void ReAddResourceDictionary(string url)
		{
			var resDict = Resources.MergedDictionaries.FirstOrDefault(r => r.Source.OriginalString == url);
			if (resDict != null)
			{
				Resources.MergedDictionaries.Remove(resDict);
			}
			Resources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri(url, UriKind.RelativeOrAbsolute)
			});
		}

		#endregion Startup

		#region Settings

		/// <summary>
		/// Provides properties to access the application settings.
		/// </summary>
		public static IAppSettings Settings { get; private set; }

		/// <summary>
		/// Initialises the application settings.
		/// </summary>
		public static void InitializeSettings()
		{
			if (Settings != null) return;   // Already done

			Settings = SettingsAdapterFactory.New<IAppSettings>(
				new FileSettingsStore(
					SettingsHelper.GetAppDataPath(@"Unclassified\FieldLog", "FieldLogViewer.conf")));

			// The settings ShowThreadIdColumn and ShowWebRequestIdColumn are mutually exclusive
			Settings.OnPropertyChanged(
				s => s.ShowThreadIdColumn,
				() =>
				{
					if (Settings.ShowThreadIdColumn) Settings.ShowWebRequestIdColumn = false;
				},
				true);
			Settings.OnPropertyChanged(
				s => s.ShowWebRequestIdColumn,
				() =>
				{
					if (Settings.ShowWebRequestIdColumn) Settings.ShowThreadIdColumn = false;
				},
				true);

			// Update settings format from old version
			FL.TraceData("LastStartedAppVersion", Settings.LastStartedAppVersion);
			if (string.IsNullOrEmpty(Settings.LastStartedAppVersion))
			{
				Settings.SettingsStore.Rename("LastAppVersion", "LastStartedAppVersion");
				Settings.SettingsStore.Rename("Window.MainLeft", "MainWindowState.Left");
				Settings.SettingsStore.Rename("Window.MainTop", "MainWindowState.Top");
				Settings.SettingsStore.Rename("Window.MainWidth", "MainWindowState.Width");
				Settings.SettingsStore.Rename("Window.MainHeight", "MainWindowState.Height");
				Settings.SettingsStore.Rename("Window.MainIsMaximized", "MainWindowState.IsMaximized");
				Settings.SettingsStore.Rename("Window.ToolBarInWindowFrame", "ToolBarInWindowFrame");
				Settings.SettingsStore.Rename("Window.SettingsLeft", "SettingsWindowState.Left");
				Settings.SettingsStore.Rename("Window.SettingsTop", "SettingsWindowState.Top");
				Settings.SettingsStore.Rename("Window.SettingsWidth", "SettingsWindowState.Width");
				Settings.SettingsStore.Rename("Window.SettingsHeight", "SettingsWindowState.Height");
			}

			// Remember the version of the application.
			// If we need to react on settings changes from previous application versions, here is
			// the place to check the version currently in the settings, before it's overwritten.
			Settings.LastStartedAppVersion = FL.AppVersion;
		}

		#endregion Settings
	}
}
