using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Unclassified.FieldLog;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.ViewModels
{
	internal class DebugMessageViewModel : LogItemViewModelBase
	{
		#region Constructor

		public DebugMessageViewModel(int pid, string text, bool isGlobal)
		{
			this.Time = FL.UtcNow;
			this.UtcOffset = (int) DateTimeOffset.Now.Offset.TotalMinutes;
			this.ProcessId = pid;
			this.Message = (text ?? "").TrimEnd();
			this.IsGlobal = isGlobal;

			if (MainViewModel.Instance.AutoLoadLog && GetLogFileBasePath() != null)
			{
				// Open log file automatically.
				// The log directory has already been created while directory probing, and the log
				// files don't need to exist yet on loading. So no additional waiting is required
				// here.
				Application.Current.Dispatcher.BeginInvoke(
					new Action(() =>
						{
							if (OpenLogFileCommand.TryExecute())
							{
								// Only do that once
								MainViewModel.Instance.AutoLoadLog = false;
							}
						}));
			}
		}

		#endregion Constructor

		#region Properties

		public int ProcessId { get; private set; }
		public string Message { get; private set; }
		public bool IsGlobal { get; private set; }
		public string TypeImageSource
		{
			get
			{
				return IsGlobal ? "/Images/Windows_System_14.png" : "/Images/Windows_User_14.png";
			}
		}
		public string PrioImageSource { get { return "/Images/Transparent_14.png"; } }

		public string SourceString
		{
			get
			{
				return IsGlobal ? "Global" : "Local";
			}
		}

		public string SimpleMessage
		{
			get
			{
				return this.Message.Trim().Replace("\r", "").Replace("\n", "↲");
			}
		}

		public Color BackColor
		{
			get
			{
				return Colors.Transparent;
			}
		}

		[NotifiesOn("IsSelected")]
		public Brush Background
		{
			get
			{
				if (!IsSelected)
				{
					return new SolidColorBrush(this.BackColor);
				}
				else
				{
					//Color highlight = Color.FromRgb(109, 173, 255);
					Color highlight = Color.FromRgb(51, 153, 255);
					LinearGradientBrush brush = new LinearGradientBrush(this.BackColor, highlight, new Point(34, 0), new Point(34.001, 0));
					brush.MappingMode = BrushMappingMode.Absolute;
					return brush;
				}
			}
		}

		[NotifiesOn("IsSelected")]
		public Brush Foreground
		{
			get
			{
				if (!IsSelected)
				{
					return Brushes.Black;
				}
				else
				{
					return Brushes.White;
				}
			}
		}

		public bool IsSelected
		{
			get { return GetValue<bool>("IsSelected"); }
			set { SetValue(value, "IsSelected"); }
		}

		public Visibility OpenLogFileButtonVisibility
		{
			get
			{
				return GetLogFileBasePath() != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		#endregion Properties

		#region Commands

		public DelegateCommand OpenLogFileCommand { get; private set; }

		protected override void InitializeCommands()
		{
			OpenLogFileCommand = new DelegateCommand(OnOpenLogFile, CanOpenLogFile);
		}

		private bool CanOpenLogFile()
		{
			// TODO: More robust comparing with the GetFinalPathNameByHandle function
			//       http://msdn.microsoft.com/en-us/library/aa364962%28VS.85%29.aspx
			// See also http://stackoverflow.com/q/2281531/143684
			return !string.Equals(
				MainViewModel.Instance.LoadedBasePath,
				GetLogFileBasePath(),
				StringComparison.InvariantCultureIgnoreCase);
		}

		private void OnOpenLogFile()
		{
			string path = GetLogFileBasePath();
			if (path != null)
			{
				try
				{
					MainViewModel.Instance.OpenFiles(path);
				}
				catch (Exception ex)
				{
					FL.Error(ex, "Loading log files from DebugOutputString notification");
					MessageBox.Show(
						"Error loading the specified log files. " + ex.Message + "\nSee the log file for details.",
						"FieldLogViewer",
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				}
			}
		}

		#endregion Commands

		#region Helper methods

		private string GetLogFileBasePath()
		{
			string s = "FieldLog info: Now writing to ";
			if (Message.StartsWith(s))
			{
				return Message.Substring(s.Length).Trim();
			}
			return null;
		}

		public override string ToString()
		{
			return GetType().Name + ": " + Message;
		}

		#endregion Helper methods
	}
}
