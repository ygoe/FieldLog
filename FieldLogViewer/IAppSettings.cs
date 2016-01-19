using System;
using System.ComponentModel;
using Unclassified.Util;

namespace Unclassified.FieldLogViewer
{
	public interface IAppSettings : ISettings
	{
		/// <summary>
		/// Provides settings for the main window state.
		/// </summary>
		IWindowStateSettings MainWindowState { get; }

		/// <summary>
		/// Provides settings for the settings window state.
		/// </summary>
		IWindowStateSettings SettingsWindowState { get; }

		/// <summary>
		/// Gets or sets the last started version of the application.
		/// </summary>
		string LastStartedAppVersion { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the local DebugOutputString monitor is active.
		/// </summary>
		bool IsLocalDebugMonitorActive { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the global DebugOutputString monitor is active.
		/// </summary>
		bool IsGlobalDebugMonitorActive { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the relative time of each item is shown in the
		/// log items list.
		/// </summary>
		bool ShowRelativeTime { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the log items list scrolls to new items.
		/// </summary>
		[DefaultValue(true)]
		bool IsLiveScrollingEnabled { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether a sound is played on new items.
		/// </summary>
		bool IsSoundEnabled { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the window should flash on new items.
		/// </summary>
		[DefaultValue(true)]
		bool IsFlashingEnabled { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the main window is always on top.
		/// </summary>
		bool IsWindowOnTop { get; set; }

		/// <summary>
		/// Gets or sets the scope item indenting size in pixels.
		/// </summary>
		[DefaultValue(12)]
		int IndentSize { get; set; }

		/// <summary>
		/// Gets or sets the filter definitions.
		/// </summary>
		string[] Filters { get; set; }

		/// <summary>
		/// Gets or sets the name of the selected filter.
		/// </summary>
		string SelectedFilter { get; set; }

		/// <summary>
		/// Gets or sets the time zone to use for displaying log item times.
		/// </summary>
		[DefaultValue((int)ItemTimeType.Remote)]
		ItemTimeType ItemTimeMode { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the thread ID column is visible.
		/// </summary>
		[DefaultValue(true)]
		bool ShowThreadIdColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the web request ID column is visible.
		/// </summary>
		bool ShowWebRequestIdColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether warnings and errors are marked in the scroll bar.
		/// </summary>
		[DefaultValue(true)]
		bool ShowWarningsErrorsInScrollBar { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the selected items are marked in the scroll bar.
		/// </summary>
		[DefaultValue(true)]
		bool ShowSelectionInScrollBar { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the toolbar is in the window frame.
		/// </summary>
		bool ToolBarInWindowFrame { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the metadata of a stack frame is displayed.
		/// </summary>
		bool ShowStackFrameMetadata { get; set; }

		/// <summary>
		/// Gets or sets an array containing the recently loaded files with their path and base
		/// name. The most recently loaded entry is first.
		/// </summary>
		string[] RecentlyLoadedFiles { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether a time-based separator between log items is
		/// displayed in the log items list.
		/// </summary>
		[DefaultValue(true)]
		bool ShowTimeSeparator { get; set; }
	}

	public enum ItemTimeType
	{
		Utc,
		Local,
		Remote
	}
}
