using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Unclassified.FieldLog;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class DebugMessageViewModel : LogItemViewModelBase
	{
		#region Constructor

		public DebugMessageViewModel(int pid, string text)
		{
			this.Time = FL.UtcNow;
			this.UtcOffset = (int) DateTimeOffset.Now.Offset.TotalMinutes;
			this.ProcessId = pid;
			this.Message = (text ?? "").TrimEnd();

			InitializeCommands();
		}

		#endregion Constructor

		#region Properties

		public int ProcessId { get; private set; }
		public string Message { get; private set; }
		public string TypeImageSource { get { return "/Images/Windows_14.png"; } }
		public string PrioImageSource { get { return "/Images/Transparent_14.png"; } }

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

		public Brush Background
		{
			get
			{
				if (!this.isSelected)
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

		public Brush Foreground
		{
			get
			{
				if (!this.isSelected)
				{
					return Brushes.Black;
				}
				else
				{
					return Brushes.White;
				}
			}
		}

		private bool isSelected;
		public bool IsSelected
		{
			get { return this.isSelected; }
			set { CheckUpdate(value, ref isSelected, "IsSelected", "Background", "Foreground"); }
		}

		public Visibility OpenLogFileButtonVisibility
		{
			get
			{
				return Message.StartsWith("FieldLog info: Now writing to ") ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		
		#endregion Properties

		#region Commands

		public DelegateCommand OpenLogFileCommand { get; private set; }

		private void InitializeCommands()
		{
			OpenLogFileCommand = new DelegateCommand(OnOpenLogFile);
		}

		private void OnOpenLogFile()
		{
			string s = "FieldLog info: Now writing to ";
			if (Message.StartsWith(s))
			{
				try
				{
					MainViewModel.Instance.OpenFiles(Message.Substring(s.Length).Trim());
				}
				catch (Exception ex)
				{
					FL.Error(ex, "Loading log files from DebugOutputString notification");
					MessageBox.Show(
						"Error loading the specified log files. " + ex.Message,
						"FieldLogViewer",
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				}
			}
		}

		#endregion Commands
	}
}
