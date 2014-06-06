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

			this.EnvironmentVM = new FieldLogEnvironmentViewModel(item.EnvironmentData, this);
			this.WebRequestDataVM = new FieldLogWebRequestDataViewModel(item.WebRequestData, this);
		}

		public new FieldLogScopeItem Item { get; private set; }
		public FieldLogScopeType Type { get { return this.Item.Type; } }
		public int Level { get { return this.Item.Level; } }
		public string Name { get { return this.Item.Name; } }
		public bool IsBackgroundThread { get { return this.Item.IsBackgroundThread; } }
		public bool IsPoolThread { get { return this.Item.IsPoolThread; } }
		public bool IsRepeated { get { return this.Item.IsRepeated; } }
		public FieldLogEnvironmentViewModel EnvironmentVM { get; private set; }
		public FieldLogWebRequestDataViewModel WebRequestDataVM { get; private set; }

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
					case FieldLogScopeType.WebRequestStart: return "Web request start";
					case FieldLogScopeType.WebRequestEnd: return "Web request end";
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
					case FieldLogScopeType.WebRequestStart: return "/Images/ScopeItem_WebRequestStart_14.png";
					case FieldLogScopeType.WebRequestEnd: return "/Images/ScopeItem_WebRequestEnd_14.png";
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
						return "└ " + Name;
					case FieldLogScopeType.Leave:
						return "┌ " + Name;

					case FieldLogScopeType.ThreadStart:
					case FieldLogScopeType.ThreadEnd:
						if (string.IsNullOrEmpty(Name))
							return TypeTitle + ": " + Name;
						else
							return TypeTitle;

					case FieldLogScopeType.WebRequestStart:
						int index = WebRequestDataVM.WebRequestData.RequestUrl.IndexOf("//");
						if (index != -1)
						{
							index = WebRequestDataVM.WebRequestData.RequestUrl.IndexOf("/", index + 2);
						}
						if (index == -1)
						{
							index = 0;
						}
						string method = "";
						if (WebRequestDataVM.WebRequestData.Method != "GET")
						{
							method = WebRequestDataVM.WebRequestData.Method + " ";
						}
						return TypeTitle + ": " + method + WebRequestDataVM.WebRequestData.RequestUrl.Substring(index);
					case FieldLogScopeType.WebRequestEnd:
						if (LastWebRequestStartItem != null)
						{
							return TypeTitle + ": " + Name + " (" + LastWebRequestStartItem.WebRequestDataVM.RequestDuration.TotalMilliseconds.ToString("0.0") + " ms)";
						}
						if (WebRequestId == 0)
						{
							return TypeTitle + ": " + Name + " (orphan)";
						}
						return TypeTitle + ": " + Name;

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

		public Visibility WebRequestVisibility
		{
			get
			{
				return Type == FieldLogScopeType.WebRequestStart ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		/// <summary>
		/// Gets a new UtcOffset value from the log item.
		/// </summary>
		/// <param name="utcOffset">The new UtcOffset value from this log item instance.</param>
		/// <returns>true if a new UtcOffset value was set; otherwise, false.</returns>
		public override bool TryGetUtcOffsetData(out int utcOffset)
		{
			if (Type == FieldLogScopeType.LogStart)
			{
				utcOffset = (int) Item.EnvironmentData.LocalTimeZoneOffset.TotalMinutes;
				return true;
			}
			utcOffset = 0;
			return false;
		}

		/// <summary>
		/// Gets a new IndentLevel value from the log item.
		/// </summary>
		/// <param name="indentLevel">The new IndentLevel value from this log item instance.</param>
		/// <returns>true if a new IndentLevel value was set; otherwise, false.</returns>
		public override bool TryGetIndentLevelData(out int indentLevel)
		{
			if (Type == FieldLogScopeType.Enter)
			{
				indentLevel = Item.Level - 1;
				return true;
			}
			if (Type == FieldLogScopeType.Leave)
			{
				indentLevel = Item.Level;
				return true;
			}
			indentLevel = 0;
			return false;
		}
	}
}
