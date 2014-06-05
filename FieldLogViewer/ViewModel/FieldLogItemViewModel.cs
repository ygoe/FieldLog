﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class FieldLogItemViewModel : LogItemViewModelBase
	{
		public static FieldLogItemViewModel Create(FieldLogItem item)
		{
			FieldLogTextItem textItem = item as FieldLogTextItem;
			if (textItem != null)
			{
				return new FieldLogTextItemViewModel(textItem);
			}
			FieldLogDataItem dataItem = item as FieldLogDataItem;
			if (dataItem != null)
			{
				return new FieldLogDataItemViewModel(dataItem);
			}
			FieldLogExceptionItem exceptionItem = item as FieldLogExceptionItem;
			if (exceptionItem != null)
			{
				return new FieldLogExceptionItemViewModel(exceptionItem);
			}
			FieldLogScopeItem scopeItem = item as FieldLogScopeItem;
			if (scopeItem != null)
			{
				return new FieldLogScopeItemViewModel(scopeItem);
			}
			return null;
		}

		private FieldLogItem item;
		protected FieldLogItem Item
		{
			get
			{
				return item;
			}
			set
			{
				item = value;
				this.EventCounter = item.EventCounter;
				this.Time = item.Time;
			}
		}

		public FieldLogPriority Priority { get { return this.Item.Priority; } }
		public Guid SessionId { get { return this.Item.SessionId; } }
		public int ThreadId { get { return this.Item.ThreadId; } }
		public uint WebRequestId { get { return this.Item.WebRequestId; } }
		public string LogItemSourceFileName { get { return this.Item.LogItemSourceFileName; } }

		public FieldLogScopeItemViewModel LastLogStartItem { get; set; }
		public FieldLogScopeItemViewModel LastWebRequestStartItem { get; set; }

		public string EventCounterAndSourceFile
		{
			get { return EventCounter + " in " + System.IO.Path.GetFileName(LogItemSourceFileName) + " (v" + item.FileFormatVersion + ")"; }
		}

		public string PrioTitle
		{
			get
			{
				switch (this.Priority)
				{
					case FieldLogPriority.Trace: return "Trace";
					case FieldLogPriority.Checkpoint: return "Checkpoint";
					case FieldLogPriority.Info: return "Information";
					case FieldLogPriority.Notice: return "Notice";
					case FieldLogPriority.Warning: return "Warning";
					case FieldLogPriority.Error: return "Error";
					case FieldLogPriority.Critical: return "Critical";
					default: return "Unknown (" + (int) this.Priority + ")";
				}
			}
		}
		
		public string PrioImageSource
		{
			get
			{
				switch (this.Priority)
				{
					case FieldLogPriority.Trace: return "/Images/Prio_Trace_14.png";
					case FieldLogPriority.Checkpoint: return "/Images/Prio_Checkpoint_14.png";
					case FieldLogPriority.Info: return "/Images/Prio_Info_14.png";
					case FieldLogPriority.Notice: return "/Images/Prio_Notice_14.png";
					case FieldLogPriority.Warning: return "/Images/Prio_Warning_14.png";
					case FieldLogPriority.Error: return "/Images/Prio_Error_14.png";
					case FieldLogPriority.Critical: return "/Images/Prio_Critical_14.png";
					default: return null;
				}
			}
		}

		public Color BackColor
		{
			get
			{
				switch (Priority)
				{
					case FieldLogPriority.Trace: return Color.FromArgb(255, 255, 255, 255);
					case FieldLogPriority.Checkpoint: return Color.FromArgb(255, 242, 243, 252);
					case FieldLogPriority.Info: return Color.FromArgb(255, 223, 245, 249);
					case FieldLogPriority.Notice: return Color.FromArgb(255, 229, 246, 218);
					case FieldLogPriority.Warning: return Color.FromArgb(255, 246, 239, 190);
					case FieldLogPriority.Error: return Color.FromArgb(255, 255, 195, 155);
					case FieldLogPriority.Critical: return Color.FromArgb(255, 255, 145, 145);
					default: return Colors.Transparent;
				}
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

		public Brush DetailBackground
		{
			get
			{
				LinearGradientBrush brush = new LinearGradientBrush(this.BackColor, Colors.Transparent, new Point(), new Point(0, 150));
				brush.MappingMode = BrushMappingMode.Absolute;
				return brush;
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

		public Visibility WebRequestIdVisibility
		{
			get { return WebRequestId != 0 ? Visibility.Visible : Visibility.Collapsed; }
		}

		private bool isSelected;
		public bool IsSelected
		{
			get { return this.isSelected; }
			set
			{
				if (CheckUpdate(value, ref isSelected, "IsSelected", "Background", "Foreground"))
				{
					//System.Diagnostics.Debug.WriteLine("Item " + (isSelected ? "selected" : "deselected") + ": " + Time);
				}
			}
		}

		/// <summary>
		/// Gets a new UtcOffset value from the log item.
		/// </summary>
		/// <param name="utcOffset">The new UtcOffset value from this log item instance.</param>
		/// <returns>true if a new UtcOffset value was set; otherwise, false.</returns>
		public virtual bool TryGetUtcOffsetData(out int utcOffset)
		{
			utcOffset = 0;
			return false;
		}

		/// <summary>
		/// Gets a new IndentLevel value from the log item.
		/// </summary>
		/// <param name="indentLevel">The new IndentLevel value from this log item instance.</param>
		/// <returns>true if a new IndentLevel value was set; otherwise, false.</returns>
		public virtual bool TryGetIndentLevelData(out int indentLevel)
		{
			indentLevel = 0;
			return false;
		}
	}
}
