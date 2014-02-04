using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unclassified.FieldLog;
using System.Windows.Media;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class DebugMessageViewModel : LogItemViewModelBase
	{
		public DebugMessageViewModel(int pid, string text)
		{
			this.Time = FL.UtcNow;
			this.ProcessID = pid;
			this.Message = text;
		}

		public int ProcessID { get; private set; }
		public string Message { get; private set; }
		public string TypeImageSource { get { return "/Images/Transparent_14.png"; } }
		public string PrioImageSource { get { return "/Images/Windows_14.png"; } }

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
				return Color.FromArgb(255, 255, 255, 255);
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
					return new LinearGradientBrush(this.BackColor, this.BackColor.BlendWith(Color.FromArgb(255, 109, 173, 255), 0.25f), 0);
				}
			}
		}

		private bool isSelected;
		public bool IsSelected
		{
			get { return this.isSelected; }
			set
			{
				if (value != this.isSelected)
				{
					this.isSelected = value;
					OnPropertyChanged("IsSelected");
					OnPropertyChanged("Background");
				}
			}
		}
	}
}
