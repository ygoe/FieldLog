using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class DebugMessageViewModel : LogItemViewModelBase
	{
		public DebugMessageViewModel(int pid, string text)
		{
			this.Time = FL.UtcNow;
			this.ProcessId = pid;
			this.Message = text;
		}

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
	}
}
