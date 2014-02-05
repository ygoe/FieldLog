using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unclassified.FieldLog;
using System.Windows;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class FieldLogScopeItemViewModel : FieldLogItemViewModel
	{
		public FieldLogScopeItemViewModel(FieldLogScopeItem item)
		{
			this.Item = item;
			base.Item = item;
		}

		public new FieldLogScopeItem Item { get; private set; }
		public FieldLogScopeType Type { get { return this.Item.Type; } }
		public int Level { get { return this.Item.Level; } }
		public string Name { get { return this.Item.Name; } }
		public bool IsBackgroundThread { get { return this.Item.IsBackgroundThread; } }
		public bool IsPoolThread { get { return this.Item.IsPoolThread; } }
		public bool IsRepeated { get { return this.Item.IsRepeated; } }
		public FieldLogEventEnvironment EnvironmentData { get { return this.Item.EnvironmentData; } }

		public string TypeTitle
		{
			get
			{
				switch (this.Type)
				{
					case FieldLogScopeType.Enter: return "Enter";
					case FieldLogScopeType.Leave: return "Leave";
					case FieldLogScopeType.ThreadStart: return "Thread start";
					case FieldLogScopeType.ThreadEnd: return "Thread end";
					case FieldLogScopeType.LogStart: return "Log start";
					case FieldLogScopeType.LogShutdown: return "Log shutdown";
					default: return "Unknown (" + (int) this.Type + ")";
				}
			}
		}
		
		public string TypeImageSource
		{
			get
			{
				switch (this.Type)
				{
					case FieldLogScopeType.Enter: return "/Images/ScopeItem_Enter_14.png";
					case FieldLogScopeType.Leave: return "/Images/ScopeItem_Leave_14.png";
					case FieldLogScopeType.ThreadStart: return "/Images/ScopeItem_ThreadStart_14.png";
					case FieldLogScopeType.ThreadEnd: return "/Images/ScopeItem_ThreadEnd_14.png";
					case FieldLogScopeType.LogStart: return "/Images/ScopeItem_LogStart_14.png";
					case FieldLogScopeType.LogShutdown: return "/Images/ScopeItem_LogShutdown_14.png";
					default: return null;
				}
			}
		}

		public string TypeAndName
		{
			get
			{
				switch (this.Type)
				{
					case FieldLogScopeType.Enter:
					case FieldLogScopeType.Leave:
						return TypeTitle + ": " + Name;

					case FieldLogScopeType.ThreadStart:
					case FieldLogScopeType.ThreadEnd:
						if (string.IsNullOrEmpty(Name))
							return TypeTitle + ": " + Name;
						else
							return TypeTitle;

					default:
						return TypeTitle;
				}
			}
		}

		public string IsRepeatedString
		{
			get
			{
				return IsRepeated ? "Yes" : "No";
			}
		}

		public Visibility EnvVisibility
		{
			get
			{
				return Type == FieldLogScopeType.LogStart ? Visibility.Visible : Visibility.Collapsed;
			}
		}
	}
}
