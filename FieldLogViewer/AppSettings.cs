using System;
using System.ComponentModel;
using System.IO;
using Unclassified;

namespace Unclassified.FieldLogViewer
{
	/// <summary>
	/// Provides properties to access the application settings.
	/// </summary>
	public class AppSettings : SettingsStore
	{
		// TODO: Don't derive from SettingsStore, but instead from SettingsView, which in turn should
		//       be changed so that it doesn't copy all Get/Set methods but always uses a store
		//       instance. Keep SettingsView portable but clean up the member list of these AppSettings
		//       classes.

		// TODO: Try a different settings base concept: Don't use a Dictionary-based store but instead
		//       use a ViewModelBase. Add an intermediate layer that provides save-on-set support
		//       through the CheckUpdate method. Serialize the whole object using .NET mechanisms.
		//       This allows to include arbitrary nested objects in the settings, as long as they're
		//       Serializable. Also support special view layers for generic hierarchy. Keep those
		//       helper methods protected to have a clean public API.

		#region Static members

		/// <summary>
		/// Occurs when the Instance property changes.
		/// </summary>
		public static event Action InstanceChanged;

		private static AppSettings instance;
		/// <summary>
		/// Gets the static instance for this settings class.
		/// </summary>
		public static AppSettings Instance
		{
			get { return instance; }
			private set
			{
				if (value != instance)
				{
					instance = value;
					var handler = InstanceChanged;
					if (handler != null)
					{
						handler();
					}
				}
			}
		}

		/// <summary>
		/// Initialises the static instance for this settings class.
		/// </summary>
		public static void InitializeInstance()
		{
			if (Instance == null)
			{
				Instance = new AppSettings(
					Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
						"Unclassified",
						"FieldLog",
						"FieldLogViewer.conf"));
			}
		}

		/// <summary>
		/// Closes the static instance for this settings class and ensures that all data is saved.
		/// After calling this method, the static settings instance can no longer be used.
		/// </summary>
		public static void CloseInstance()
		{
			if (Instance != null)
			{
				Instance.Dispose();
				Instance = null;
			}
		}

		#endregion Static members

		#region Constructors

		/// <summary>
		/// Initialises a new instance of the AppSettings class and sets up all change notifications.
		/// </summary>
		/// <param name="fileName">Full path to the settings file.</param>
		public AppSettings(string fileName)
			: base(fileName)
		{
			Window = new WindowSettingsView(this);

			AddPropertyHandler("LastAppVersion");
			AddPropertyHandler("IsDebugMonitorActive");
			AddPropertyHandler("ShowRelativeTime");
			AddPropertyHandler("IsLiveScrollingEnabled");
			AddPropertyHandler("IsFlashingEnabled");
			AddPropertyHandler("IsSoundEnabled");
			AddPropertyHandler("IsWindowOnTop");
			AddPropertyHandler("IndentSize");
			AddPropertyHandler("Filters");
			AddPropertyHandler("SelectedFilter");
			AddPropertyHandler("ItemTimeMode");
			AddPropertyHandler("ShowWarningsErrorsInScrollBar");
			AddPropertyHandler("ShowSelectionInScrollBar");
		}

		#endregion Constructors

		#region View properties

		/// <summary>
		/// Provides window-related application settings.
		/// </summary>
		public WindowSettingsView Window { get; private set; }

		#endregion View properties

		#region Data properties

		/// <summary>
		/// Gets or sets the version of the application.
		/// </summary>
		public string LastAppVersion
		{
			get { return GetString("LastAppVersion"); }
			set { Set("LastAppVersion", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the DebugOutputString monitor is active.
		/// </summary>
		public bool IsDebugMonitorActive
		{
			get { return GetBool("IsDebugMonitorActive"); }
			set { Set("IsDebugMonitorActive", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the relative time of each item is shown in the
		/// log items list.
		/// </summary>
		public bool ShowRelativeTime
		{
			get { return GetBool("ShowRelativeTime"); }
			set { Set("ShowRelativeTime", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the log items list scrolls to new items.
		/// </summary>
		public bool IsLiveScrollingEnabled
		{
			get { return GetBool("IsLiveScrollingEnabled", true); }
			set { Set("IsLiveScrollingEnabled", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether a sound is played on new items.
		/// </summary>
		public bool IsSoundEnabled
		{
			get { return GetBool("IsSoundEnabled"); }
			set { Set("IsSoundEnabled", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the window should flash on new items.
		/// </summary>
		public bool IsFlashingEnabled
		{
			get { return GetBool("IsFlashingEnabled", true); }
			set { Set("IsFlashingEnabled", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the main window is always on top.
		/// </summary>
		public bool IsWindowOnTop
		{
			get { return GetBool("IsWindowOnTop"); }
			set { Set("IsWindowOnTop", value); }
		}

		/// <summary>
		/// Gets or sets the scope item indenting size in pixels.
		/// </summary>
		public int IndentSize
		{
			get { return GetInt("IndentSize", 12); }
			set { Set("IndentSize", value); }
		}

		/// <summary>
		/// Gets or sets the filter definitions.
		/// </summary>
		public string[] Filters
		{
			get { return GetStringArray("Filters"); }
			set { Set("Filters", value); }
		}

		/// <summary>
		/// Gets or sets the name of the selected filter.
		/// </summary>
		public string SelectedFilter
		{
			get { return GetString("SelectedFilter"); }
			set { Set("SelectedFilter", value); }
		}

		/// <summary>
		/// Gets or sets the time zone to use for displaying log item times.
		/// </summary>
		public ItemTimeType ItemTimeMode
		{
			get { return (ItemTimeType) GetInt("ItemTimeMode", (int) ItemTimeType.Remote); }
			set { Set("ItemTimeMode", (int) value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether warnings and errors are marked in the scroll bar.
		/// </summary>
		public bool ShowWarningsErrorsInScrollBar
		{
			get { return GetBool("ShowWarningsErrorsInScrollBar", true); }
			set { Set("ShowWarningsErrorsInScrollBar", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the selected items are marked in the scroll bar.
		/// </summary>
		public bool ShowSelectionInScrollBar
		{
			get { return GetBool("ShowSelectionInScrollBar", true); }
			set { Set("ShowSelectionInScrollBar", value); }
		}

		#endregion Data properties
	}

	public class WindowSettingsView : SettingsView
	{
		#region Constructors

		public WindowSettingsView(SettingsStore settings)
			: base(settings, "Window")
		{
			AddPropertyHandler("MainLeft");
			AddPropertyHandler("MainTop");
			AddPropertyHandler("MainWidth");
			AddPropertyHandler("MainHeight");
			AddPropertyHandler("MainIsMaximized");

			AddPropertyHandler("SettingsLeft");
			AddPropertyHandler("SettingsTop");
			AddPropertyHandler("SettingsWidth");
			AddPropertyHandler("SettingsHeight");
		}

		#endregion Constructors

		#region Data properties

		/// <summary>
		/// Gets or sets the main window X location.
		/// </summary>
		public int MainLeft
		{
			get { return GetInt("MainLeft", 100); }
			set { Set("MainLeft", value); }
		}

		/// <summary>
		/// Gets or sets the main window Y location.
		/// </summary>
		public int MainTop
		{
			get { return GetInt("MainTop", 50); }
			set { Set("MainTop", value); }
		}

		/// <summary>
		/// Gets or sets the main window width.
		/// </summary>
		public int MainWidth
		{
			get { return GetInt("MainWidth", 1000); }
			set { Set("MainWidth", value); }
		}

		/// <summary>
		/// Gets or sets the main window height.
		/// </summary>
		public int MainHeight
		{
			get { return GetInt("MainHeight", 500); }
			set { Set("MainHeight", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the main window is maximized.
		/// </summary>
		public bool MainIsMaximized
		{
			get { return GetBool("MainIsMaximized", false); }
			set { Set("MainIsMaximized", value); }
		}

		/// <summary>
		/// Gets or sets the settings window X location.
		/// </summary>
		public int SettingsLeft
		{
			get { return GetInt("SettingsLeft", 100); }
			set { Set("SettingsLeft", value); }
		}

		/// <summary>
		/// Gets or sets the settings window Y location.
		/// </summary>
		public int SettingsTop
		{
			get { return GetInt("SettingsTop", 50); }
			set { Set("SettingsTop", value); }
		}

		/// <summary>
		/// Gets or sets the settings window width.
		/// </summary>
		public int SettingsWidth
		{
			get { return GetInt("SettingsWidth", 500); }
			set { Set("SettingsWidth", value); }
		}

		/// <summary>
		/// Gets or sets the settings window height.
		/// </summary>
		public int SettingsHeight
		{
			get { return GetInt("SettingsHeight", 300); }
			set { Set("SettingsHeight", value); }
		}

		#endregion Data properties
	}

	public enum ItemTimeType
	{
		Utc,
		Local,
		Remote
	}
}
