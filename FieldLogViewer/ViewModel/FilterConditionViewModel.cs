using System;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Unclassified.FieldLog;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class FilterConditionViewModel : ViewModelBase
	{
		#region Private data

		private FilterConditionGroupViewModel parentConditionGroup;

		#endregion Private data

		#region Constructor

		public FilterConditionViewModel(FilterConditionGroupViewModel parentConditionGroup)
		{
			this.parentConditionGroup = parentConditionGroup;

			InitializeCommands();

			Value = "";
		}

		#endregion Constructor

		#region Commands

		public DelegateCommand DeleteCommand { get; private set; }

		private void InitializeCommands()
		{
			DeleteCommand = new DelegateCommand(OnDelete);
		}

		private void OnDelete()
		{
			parentConditionGroup.Conditions.Remove(this);
		}

		#endregion Commands

		#region Data properties

		private bool isFirst;
		public bool IsFirst
		{
			get { return isFirst; }
			set { CheckUpdate(value, ref isFirst, "IsFirst", "Margin", "AndLabelVisibility"); }
		}

		public Thickness Margin
		{
			get { return IsFirst ? new Thickness() : new Thickness(0, 4, 0, 0); }
		}

		public Visibility AndLabelVisibility
		{
			get { return IsFirst ? Visibility.Hidden : Visibility.Visible; }
		}

		private FilterColumn column;
		public FilterColumn Column
		{
			get { return column; }
			set
			{
				if (CheckUpdate(value, ref column, "Column"))
				{
					OnPropertyChanged(
						"AvailableValues",
						"Column",
						"ComparisonVisibility",
						"ValueTextVisibility", "ValueListVisibility", "ValueSetVisibility");
					switch (column)
					{
						case FilterColumn.Type:
							Value = FilterItemType.Any.ToString();
							break;
						case FilterColumn.Priority:
							Value = FilterPriority.Trace.ToString();
							break;
						case FilterColumn.ScopeType:
							Value = FilterScopeType.Enter.ToString();
							break;
					}
					OnFilterChanged(true);
				}
			}
		}

		private bool negate;
		public bool Negate
		{
			get { return negate; }
			set
			{
				if (CheckUpdate(value, ref negate, "Negate"))
				{
					OnFilterChanged(true);
				}
			}
		}

		private FilterComparison comparison;
		public FilterComparison Comparison
		{
			get { return comparison; }
			set
			{
				if (CheckUpdate(value, ref comparison, "Comparison"))
				{
					OnFilterChanged(true);
				}
			}
		}

		private string value;
		public string Value
		{
			get { return value; }
			set
			{
				if (CheckUpdate(value, ref this.value, "Value"))
				{
					OnFilterChanged(true);
				}
			}
		}

		public object AvailableValues
		{
			get
			{
				switch (column)
				{
					case FilterColumn.Type:
						return new EnumerationExtension(typeof(FilterItemType), true).ProvideValue(null);
					case FilterColumn.Priority:
						return new EnumerationExtension(typeof(FilterPriority), true).ProvideValue(null);
					case FilterColumn.ScopeType:
						return new EnumerationExtension(typeof(FilterScopeType), true).ProvideValue(null);
					default:
						return null;
				}
			}
		}

		public Visibility ComparisonVisibility
		{
			get
			{
				switch (Column)
				{
					case FilterColumn.Time:
					case FilterColumn.Priority:
					case FilterColumn.SessionId:
					case FilterColumn.ThreadId:
					case FilterColumn.AnyText:
					case FilterColumn.TextText:
					case FilterColumn.TextDetails:
					case FilterColumn.DataName:
					case FilterColumn.DataValue:
					case FilterColumn.ExceptionType:
					case FilterColumn.ExceptionMessage:
					case FilterColumn.ExceptionCode:
					case FilterColumn.ExceptionData:
					case FilterColumn.ExceptionContext:
					case FilterColumn.ScopeLevel:
					case FilterColumn.ScopeName:
					case FilterColumn.EnvironmentCultureName:
					case FilterColumn.EnvironmentCurrentDirectory:
					case FilterColumn.EnvironmentEnvironmentVariables:
					case FilterColumn.EnvironmentUserName:
					case FilterColumn.EnvironmentCommandLine:
					case FilterColumn.EnvironmentAppVersion:
					case FilterColumn.EnvironmentProcessMemory:
					case FilterColumn.EnvironmentPeakProcessMemory:
					case FilterColumn.EnvironmentTotalMemory:
					case FilterColumn.EnvironmentAvailableMemory:
						return Visibility.Visible;
					default:
						return Visibility.Collapsed;
				}
			}
		}

		public Visibility ValueTextVisibility
		{
			get
			{
				switch (Column)
				{
					case FilterColumn.Time:
					case FilterColumn.SessionId:
					case FilterColumn.ThreadId:
					case FilterColumn.AnyText:
					case FilterColumn.TextText:
					case FilterColumn.TextDetails:
					case FilterColumn.DataName:
					case FilterColumn.DataValue:
					case FilterColumn.ExceptionType:
					case FilterColumn.ExceptionMessage:
					case FilterColumn.ExceptionCode:
					case FilterColumn.ExceptionData:
					case FilterColumn.ExceptionContext:
					case FilterColumn.ScopeLevel:
					case FilterColumn.ScopeName:
					case FilterColumn.EnvironmentCultureName:
					case FilterColumn.EnvironmentCurrentDirectory:
					case FilterColumn.EnvironmentEnvironmentVariables:
					case FilterColumn.EnvironmentUserName:
					case FilterColumn.EnvironmentCommandLine:
					case FilterColumn.EnvironmentAppVersion:
					case FilterColumn.EnvironmentProcessMemory:
					case FilterColumn.EnvironmentPeakProcessMemory:
					case FilterColumn.EnvironmentTotalMemory:
					case FilterColumn.EnvironmentAvailableMemory:
						return Visibility.Visible;
					default:
						return Visibility.Hidden;
				}
			}
		}

		public Visibility ValueListVisibility
		{
			get
			{
				switch (Column)
				{
					case FilterColumn.Type:
					case FilterColumn.Priority:
					case FilterColumn.ScopeType:
						return Visibility.Visible;
					default:
						return Visibility.Hidden;
				}
			}
		}

		public Visibility ValueSetVisibility
		{
			get
			{
				switch (Column)
				{
					case FilterColumn.ScopeIsBackgroundThread:
					case FilterColumn.ScopeIsPoolThread:
					case FilterColumn.EnvironmentIsShuttingDown:
					case FilterColumn.EnvironmentIsInteractive:
						return Visibility.Visible;
					default:
						return Visibility.Hidden;
				}
			}
		}

		#endregion Data properties

		#region Loading and saving

		public void LoadFromString(string data)
		{
			string[] chunks = data.Split(new char[] { ',' }, 5);
			// chunk[0] is a control field already parsed along the way down here
			enableFilterChangedEvent = false;
			Column = (FilterColumn) Enum.Parse(typeof(FilterColumn), chunks[1]);
			Negate = chunks[2] == "not";
			Comparison = (FilterComparison) Enum.Parse(typeof(FilterComparison), chunks[3]);
			Value = chunks[4];
			enableFilterChangedEvent = true;
		}

		public string SaveToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(Column.ToString());
			sb.Append(",");
			sb.Append(Negate ? "not" : "");
			sb.Append(",");
			sb.Append(Comparison.ToString());
			sb.Append(",");
			sb.Append(Value);
			return sb.ToString();
		}

		#endregion Loading and saving

		#region Change notification

		private bool enableFilterChangedEvent = true;

		/// <summary>
		/// Raises the FilterChanged event.
		/// </summary>
		public void OnFilterChanged(bool affectsItems)
		{
			if (enableFilterChangedEvent)
			{
				parentConditionGroup.OnFilterChanged(affectsItems);
			}
		}

		#endregion Change notification

		#region Filter logic

		/// <summary>
		/// Determines whether the specified log item matches this condition.
		/// </summary>
		/// <param name="item">The log item to evaluate.</param>
		/// <returns></returns>
		public bool IsMatch(FieldLogItemViewModel item)
		{
			FieldLogTextItemViewModel textItem = null;
			FieldLogDataItemViewModel dataItem = null;
			FieldLogExceptionItemViewModel exItem = null;
			FieldLogScopeItemViewModel scopeItem = null;

			bool result = false;   // Default for wrong type
			switch (Column)
			{
				case FilterColumn.Type:
					result = CompareType(item);
					break;
				case FilterColumn.Time:
					result = CompareTime(item.Time);
					break;
				case FilterColumn.Priority:
					result = ComparePriority(item.Priority);
					break;
				case FilterColumn.SessionId:
					result = CompareString(item.SessionId.ToString("D"));
					break;
				case FilterColumn.ThreadId:
					result = CompareInt(item.ThreadId);
					break;
				case FilterColumn.AnyText:
					textItem = item as FieldLogTextItemViewModel;
					if (textItem == null)
						dataItem = item as FieldLogDataItemViewModel;
					if (dataItem == null)
						exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;
					
					if (textItem != null)
						result = CompareString(textItem.Text) || CompareString(textItem.Details);
					else if (dataItem != null)
						result = CompareString(dataItem.Name) || CompareString(dataItem.Value);
					else if (exItem != null)
						result = CompareString(exItem.Context) ||
							CompareExceptionTypeRecursive(exItem.Exception) ||
							CompareExceptionMessageRecursive(exItem.Exception) ||
							CompareExceptionDataRecursive(exItem.Exception);
					else if (scopeItem != null)
						result = CompareString(scopeItem.Name);
					break;
				
				case FilterColumn.TextText:
					textItem = item as FieldLogTextItemViewModel;
					if (textItem != null)
						result = CompareString(textItem.Text);
					break;
				case FilterColumn.TextDetails:
					textItem = item as FieldLogTextItemViewModel;
					if (textItem != null)
						result = CompareString(textItem.Details);
					break;
				
				case FilterColumn.DataName:
					dataItem = item as FieldLogDataItemViewModel;
					if (dataItem != null)
						result = CompareString(dataItem.Name);
					break;
				case FilterColumn.DataValue:
					dataItem = item as FieldLogDataItemViewModel;
					if (dataItem != null)
						result = CompareString(dataItem.Value);
					break;
				
				case FilterColumn.ExceptionType:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem != null)
						result = CompareExceptionTypeRecursive(exItem.Exception);
					break;
				case FilterColumn.ExceptionMessage:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem != null)
						result = CompareExceptionMessageRecursive(exItem.Exception);
					break;
				case FilterColumn.ExceptionCode:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem != null)
						result = CompareExceptionCodeRecursive(exItem.Exception);
					break;
				case FilterColumn.ExceptionData:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem != null)
						result = CompareExceptionDataRecursive(exItem.Exception);
					break;
				case FilterColumn.ExceptionContext:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem != null)
						result = CompareString(exItem.Context);
					break;
				
				case FilterColumn.ScopeType:
					scopeItem = item as FieldLogScopeItemViewModel;
					if (scopeItem != null)
						result = CompareScopeType(scopeItem.Type);
					break;
				case FilterColumn.ScopeLevel:
					scopeItem = item as FieldLogScopeItemViewModel;
					if (scopeItem != null)
						result = CompareInt(scopeItem.Level);
					break;
				case FilterColumn.ScopeName:
					scopeItem = item as FieldLogScopeItemViewModel;
					if (scopeItem != null)
						result = CompareString(scopeItem.Name);
					break;
				case FilterColumn.ScopeIsBackgroundThread:
					scopeItem = item as FieldLogScopeItemViewModel;
					if (scopeItem != null)
						result = scopeItem.IsBackgroundThread;
					break;
				case FilterColumn.ScopeIsPoolThread:
					scopeItem = item as FieldLogScopeItemViewModel;
					if (scopeItem != null)
						result = scopeItem.IsPoolThread;
					break;
				
				case FilterColumn.EnvironmentCultureName:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null)
						result = CompareString(exItem.EnvironmentData.CultureName);
					else if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
						result = CompareString(scopeItem.EnvironmentData.CultureName);
					break;
				case FilterColumn.EnvironmentIsShuttingDown:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null)
						result = exItem.EnvironmentData.IsShuttingDown;
					else if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
						result = scopeItem.EnvironmentData.IsShuttingDown;
					break;
				case FilterColumn.EnvironmentCurrentDirectory:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null)
						result = CompareString(exItem.EnvironmentData.CurrentDirectory);
					else if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
						result = CompareString(scopeItem.EnvironmentData.CurrentDirectory);
					break;
				case FilterColumn.EnvironmentEnvironmentVariables:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null)
						result = CompareString(exItem.EnvironmentData.EnvironmentVariables);
					else if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
						result = CompareString(scopeItem.EnvironmentData.EnvironmentVariables);
					break;
				case FilterColumn.EnvironmentUserName:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null)
						result = CompareString(exItem.EnvironmentData.UserName);
					else if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
						result = CompareString(scopeItem.EnvironmentData.UserName);
					break;
				case FilterColumn.EnvironmentIsInteractive:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null)
						result = exItem.EnvironmentData.IsInteractive;
					else if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
						result = scopeItem.EnvironmentData.IsInteractive;
					break;
				case FilterColumn.EnvironmentCommandLine:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null)
						result = CompareString(exItem.EnvironmentData.CommandLine);
					else if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
						result = CompareString(scopeItem.EnvironmentData.CommandLine);
					break;
				case FilterColumn.EnvironmentAppVersion:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null)
						result = CompareString(exItem.EnvironmentData.AppVersion);
					else if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
						result = CompareString(scopeItem.EnvironmentData.AppVersion);
					break;
				case FilterColumn.EnvironmentProcessMemory:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null)
						result = CompareLong(exItem.EnvironmentData.ProcessMemory);
					else if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
						result = CompareLong(scopeItem.EnvironmentData.ProcessMemory);
					break;
				case FilterColumn.EnvironmentPeakProcessMemory:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null)
						result = CompareLong(exItem.EnvironmentData.PeakProcessMemory);
					else if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
						result = CompareLong(scopeItem.EnvironmentData.PeakProcessMemory);
					break;
				case FilterColumn.EnvironmentTotalMemory:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null)
						result = CompareLong(exItem.EnvironmentData.TotalMemory);
					else if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
						result = CompareLong(scopeItem.EnvironmentData.TotalMemory);
					break;
				case FilterColumn.EnvironmentAvailableMemory:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null)
						result = CompareLong(exItem.EnvironmentData.AvailableMemory);
					else if (scopeItem != null && scopeItem.Type == FieldLogScopeType.LogStart)
						result = CompareLong(scopeItem.EnvironmentData.AvailableMemory);
					break;
			}

			if (Negate)
				return !result;
			else
				return result;
		}

		private bool CompareTime(DateTime time)
		{
			Match m = Regex.Match(Value.Trim(), "^([0-9]{4})-([0-9]{1,2})-([0-9]{1,2})[-:.T ]([0-9]{1,2}):([0-9]{1,2}):([0-9]{1,2}).([0-9]{1,6})$");
			// TODO

			DateTime filterTime;
			if (DateTime.TryParse(Value, out filterTime))
			{
				switch (Comparison)
				{
					case FilterComparison.Equals:
						// TODO
						break;
				}
				// TODO
				return false;
			}
			else
			{
				// TODO: Report error
				return false;
			}
		}

		private bool ComparePriority(FieldLogPriority prio)
		{
			FieldLogPriority filterPrio;
			if (Enum.TryParse(Value, out filterPrio))
			{
				switch (Comparison)
				{
					case FilterComparison.Equals:
						return prio == filterPrio;
					case FilterComparison.GreaterOrEqual:
						return prio >= filterPrio;
					case FilterComparison.GreaterThan:
						return prio > filterPrio;
					case FilterComparison.LessOrEqual:
						return prio <= filterPrio;
					case FilterComparison.LessThan:
						return prio < filterPrio;
					default:
						// TODO: Report error
						return false;
				}
			}
			else
			{
				// TODO: Report error
				return false;
			}
		}

		private bool CompareType(FieldLogItemViewModel item)
		{
			FilterItemType filterType;
			if (Enum.TryParse(Value, out filterType))
			{
				switch (Comparison)
				{
					case FilterComparison.Equals:
						switch (filterType)
						{
							case FilterItemType.Any:
								return true;
							case FilterItemType.Data:
								return item is FieldLogDataItemViewModel;
							case FilterItemType.Exception:
							//case FilterItemType.ExceptionRecursive:   // TODO: ???
								return item is FieldLogExceptionItemViewModel;
							case FilterItemType.Scope:
								return item is FieldLogScopeItemViewModel;
							case FilterItemType.Text:
								return item is FieldLogTextItemViewModel;
						}
						return false;
					default:
						// TODO: Report error
						return false;
				}
			}
			else
			{
				// TODO: Report error
				return false;
			}
		}

		private bool CompareScopeType(FieldLogScopeType scopeType)
		{
			FieldLogScopeType filterScopeType;
			if (Enum.TryParse(Value, out filterScopeType))
			{
				switch (Comparison)
				{
					case FilterComparison.Equals:
						return scopeType == filterScopeType;
					default:
						// TODO: Report error
						return false;
				}
			}
			else
			{
				// TODO: Report error
				return false;
			}
		}

		private bool CompareString(string str)
		{
			switch (Comparison)
			{
				case FilterComparison.Equals:
					return (str ?? "") == (Value ?? "");
				case FilterComparison.Contains:
					return str != null && str.Contains(Value ?? "");
				case FilterComparison.StartsWith:
					return str != null && str.StartsWith(Value ?? "");
				case FilterComparison.EndsWith:
					return str != null && str.EndsWith(Value ?? "");
				case FilterComparison.GreaterOrEqual:
					return str != null && str.CompareTo(Value ?? "") >= 0;
				case FilterComparison.GreaterThan:
					return str != null && str.CompareTo(Value ?? "") > 0;
				case FilterComparison.LessOrEqual:
					return str != null && str.CompareTo(Value ?? "") <= 0;
				case FilterComparison.LessThan:
					return str != null && str.CompareTo(Value ?? "") < 0;
				case FilterComparison.Regex:
					return Regex.IsMatch(str ?? "", Value ?? "");
				default:
					// TODO: Report error
					return false;
			}
		}

		private bool CompareInt(int i)
		{
			int filterInt;
			if (int.TryParse(Value, out filterInt))
			{
				switch (Comparison)
				{
					case FilterComparison.Equals:
						return i == filterInt;
					case FilterComparison.GreaterOrEqual:
						return i >= filterInt;
					case FilterComparison.GreaterThan:
						return i > filterInt;
					case FilterComparison.LessOrEqual:
						return i <= filterInt;
					case FilterComparison.LessThan:
						return i < filterInt;
					default:
						// TODO: Report error
						return false;
				}
			}
			else
			{
				// TODO: Report error
				return false;
			}
		}

		private bool CompareLong(long l)
		{
			long filterLong;
			if (long.TryParse(Value, out filterLong))
			{
				switch (Comparison)
				{
					case FilterComparison.Equals:
						return l == filterLong;
					case FilterComparison.GreaterOrEqual:
						return l >= filterLong;
					case FilterComparison.GreaterThan:
						return l > filterLong;
					case FilterComparison.LessOrEqual:
						return l <= filterLong;
					case FilterComparison.LessThan:
						return l < filterLong;
					default:
						// TODO: Report error
						return false;
				}
			}
			else
			{
				// TODO: Report error
				return false;
			}
		}

		private bool CompareExceptionTypeRecursive(FieldLogException ex)
		{
			bool result = CompareString(ex.Type);
			if (result) return true;

			foreach (var ie in ex.InnerExceptions)
			{
				result = CompareExceptionTypeRecursive(ie);
				if (result) return true;
			}
			return false;
		}

		private bool CompareExceptionMessageRecursive(FieldLogException ex)
		{
			bool result = CompareString(ex.Message);
			if (result) return true;

			foreach (var ie in ex.InnerExceptions)
			{
				result = CompareExceptionMessageRecursive(ie);
				if (result) return true;
			}
			return false;
		}

		private bool CompareExceptionCodeRecursive(FieldLogException ex)
		{
			bool result = CompareInt(ex.Code);
			if (result) return true;

			foreach (var ie in ex.InnerExceptions)
			{
				result = CompareExceptionCodeRecursive(ie);
				if (result) return true;
			}
			return false;
		}

		private bool CompareExceptionDataRecursive(FieldLogException ex)
		{
			bool result = CompareString(ex.Data);
			if (result) return true;

			foreach (var ie in ex.InnerExceptions)
			{
				result = CompareExceptionDataRecursive(ie);
				if (result) return true;
			}
			return false;
		}

		#endregion Filter logic

		#region Duplicate

		public FilterConditionViewModel GetDuplicate(FilterConditionGroupViewModel newParent)
		{
			FilterConditionViewModel newCond = new FilterConditionViewModel(newParent);
			newCond.Column = this.Column;
			newCond.Negate = this.Negate;
			newCond.Comparison = this.Comparison;
			newCond.Value = this.Value;
			return newCond;
		}

		#endregion Duplicate
	}

	enum FilterColumn
	{
		[Description("Any text content")]
		AnyText,

		[Description("Item type")]
		Type,
		[Description("Time")]
		Time,
		[Description("Priority")]
		Priority,
		[Description("Session ID")]
		SessionId,
		[Description("Thread ID")]
		ThreadId,

		[Description("Text: Text message")]
		TextText,
		[Description("Text: Details")]
		TextDetails,

		[Description("Data: Name")]
		DataName,
		[Description("Data: Value")]
		DataValue,

		[Description("Exception: Type")]
		ExceptionType,
		[Description("Exception: Message")]
		ExceptionMessage,
		[Description("Exception: Code value")]
		ExceptionCode,
		[Description("Exception: Other data")]
		ExceptionData,
		[Description("Exception: Context")]
		ExceptionContext,

		[Description("Scope: Type")]
		ScopeType,
		[Description("Scope: Level")]
		ScopeLevel,
		[Description("Scope: Name")]
		ScopeName,
		[Description("Scope: Background thread")]
		ScopeIsBackgroundThread,
		[Description("Scope: Pool thread")]
		ScopeIsPoolThread,

		[Description("Env: Culture code")]
		EnvironmentCultureName,
		[Description("Env: Shutting down")]
		EnvironmentIsShuttingDown,
		[Description("Env: Current directory")]
		EnvironmentCurrentDirectory,
		[Description("Env: Environment variables")]
		EnvironmentEnvironmentVariables,
		[Description("Env: User name")]
		EnvironmentUserName,
		[Description("Env: Interactive")]
		EnvironmentIsInteractive,
		[Description("Env: Command line")]
		EnvironmentCommandLine,
		[Description("Env: App version")]
		EnvironmentAppVersion,
		[Description("Env: Process memory")]
		EnvironmentProcessMemory,
		[Description("Env: Peak process memory")]
		EnvironmentPeakProcessMemory,
		[Description("Env: Total memory")]
		EnvironmentTotalMemory,
		[Description("Env: Available memory")]
		EnvironmentAvailableMemory,
	}

	enum FilterComparison
	{
		[Description("=")]
		Equals,
		[Description("contains")]
		Contains,
		[Description("starts with")]
		StartsWith,
		[Description("ends with")]
		EndsWith,
		[Description("<")]
		LessThan,
		[Description("≤")]
		LessOrEqual,
		[Description(">")]
		GreaterThan,
		[Description("≥")]
		GreaterOrEqual,
		[Description("regex")]
		Regex
	}

	enum FilterItemType
	{
		[Description("Any item type")]
		Any,
		[Description("Text")]
		Text,
		[Description("Data")]
		Data,
		[Description("Exception")]
		Exception,
		//[Description("Exception (with inner exceptions)")]
		//ExceptionRecursive,
		[Description("Scope")]
		Scope
	}

	enum FilterPriority
	{
		[Description("Trace")]
		Trace,
		[Description("Checkpoint")]
		Checkpoint,
		[Description("Info")]
		Info,
		[Description("Notice")]
		Notice,
		[Description("Warning")]
		Warning,
		[Description("Error")]
		Error,
		[Description("Critical")]
		Critical
	}

	enum FilterScopeType
	{
		[Description("Enter")]
		Enter,
		[Description("Leave")]
		Leave,
		[Description("ThreadStart")]
		ThreadStart,
		[Description("ThreadEnd")]
		ThreadEnd,
		[Description("LogStart")]
		LogStart,
		[Description("LogShutdown")]
		LogShutdown
	}
}
