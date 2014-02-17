using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

			comparison = FilterComparison.Contains;
			value = "";
			isEnabled = true;
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
						"AvailableComparisons",
						"ValueTextVisibility", "ValueListVisibility");
					// Set value to an acceptable default
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
					// Check whether the selected comparison is still valid
					if (AvailableComparisons != null)
					{
						if (!AvailableComparisons.Any(c => c.Value == Comparison))
						{
							Comparison = AvailableComparisons.First().Value;
						}
					}
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

		public IEnumerable<EnumerationExtension.EnumerationMember> AvailableValues
		{
			get
			{
				switch (column)
				{
					case FilterColumn.Type:
						return new EnumerationExtension(typeof(FilterItemType), true).ProvideTypedValue();
					case FilterColumn.Priority:
						return new EnumerationExtension(typeof(FilterPriority), true).ProvideTypedValue();
					case FilterColumn.ScopeType:
						return new EnumerationExtension(typeof(FilterScopeType), true).ProvideTypedValue();
					default:
						return null;
				}
			}
		}

		public IEnumerable<EnumerationExtension<FilterComparison>.EnumerationMember> AvailableComparisons
		{
			get
			{
				switch (column)
				{
					case FilterColumn.AnyText:
					case FilterColumn.SessionId:
					case FilterColumn.TextText:
					case FilterColumn.TextDetails:
					case FilterColumn.DataName:
					case FilterColumn.DataValue:
					case FilterColumn.ExceptionType:
					case FilterColumn.ExceptionMessage:
					case FilterColumn.ExceptionData:
					case FilterColumn.ExceptionContext:
					case FilterColumn.ScopeName:
					case FilterColumn.EnvironmentCultureName:
					case FilterColumn.EnvironmentCurrentDirectory:
					case FilterColumn.EnvironmentEnvironmentVariables:
					case FilterColumn.EnvironmentUserName:
					case FilterColumn.EnvironmentCommandLine:
					case FilterColumn.EnvironmentAppVersion:
						return new EnumerationExtension<FilterComparison>(typeof(UseForStringColumnAttribute)).ProvideTypedValue();

					case FilterColumn.Type:
					case FilterColumn.ScopeType:
						return new EnumerationExtension<FilterComparison>(typeof(UseForEnumColumnAttribute)).ProvideTypedValue();

					case FilterColumn.Time:
						return new EnumerationExtension<FilterComparison>(typeof(UseForTimeColumnAttribute)).ProvideTypedValue();

					case FilterColumn.Priority:
					case FilterColumn.ThreadId:
					case FilterColumn.ExceptionCode:
					case FilterColumn.ScopeLevel:
					case FilterColumn.EnvironmentProcessMemory:
					case FilterColumn.EnvironmentPeakProcessMemory:
					case FilterColumn.EnvironmentTotalMemory:
					case FilterColumn.EnvironmentAvailableMemory:
						return new EnumerationExtension<FilterComparison>(typeof(UseForNumberColumnAttribute)).ProvideTypedValue();

					case FilterColumn.ScopeIsBackgroundThread:
					case FilterColumn.ScopeIsPoolThread:
					case FilterColumn.EnvironmentIsShuttingDown:
					case FilterColumn.EnvironmentIsInteractive:
						return new EnumerationExtension<FilterComparison>(typeof(UseForBoolColumnAttribute)).ProvideTypedValue();

					default:
						return null;
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

		private bool isEnabled;
		public bool IsEnabled
		{
			get { return isEnabled; }
			set
			{
				if (CheckUpdate(value, ref isEnabled, "IsEnabled", "Opacity"))
				{
					OnFilterChanged(true);
				}
			}
		}

		public double Opacity
		{
			get { return IsEnabled ? 1.0 : 0.4; }
		}

		#endregion Data properties

		#region Loading and saving

		public void LoadFromString(string data)
		{
			string[] chunks = data.Split(new char[] { ',' }, 6);
			// chunk[0] and chunk[1] are control fields already parsed along the way down here
			enableFilterChangedEvent = false;
			IsEnabled = chunks[2] == "on";
			Column = (FilterColumn) Enum.Parse(typeof(FilterColumn), chunks[3]);
			Comparison = (FilterComparison) Enum.Parse(typeof(FilterComparison), chunks[4]);
			Value = chunks[5];
			enableFilterChangedEvent = true;
		}

		public string SaveToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(IsEnabled ? "on" : "off");
			sb.Append(",");
			sb.Append(Column.ToString());
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
		/// Determines whether the specified item matches this condition.
		/// </summary>
		/// <param name="item">The item to evaluate.</param>
		/// <returns></returns>
		public bool IsMatch(object item)
		{
			FieldLogItemViewModel flItem = null;
			FieldLogTextItemViewModel textItem = null;
			FieldLogDataItemViewModel dataItem = null;
			FieldLogExceptionItemViewModel exItem = null;
			FieldLogScopeItemViewModel scopeItem = null;
			DebugMessageViewModel dbgMsg = null;

			// Negation must be handled after all field comparisons are done because the negation
			// may need logical parentheses around the rest of the expression. All type-specific
			// compare methods below will ignore the special negated comparison and act as if its
			// corresponding non-negated comparison was set.
			bool negate = false;
			switch (Comparison)
			{
				case FilterComparison.NotContains:
				case FilterComparison.NotEndsWith:
				case FilterComparison.NotEquals:
				case FilterComparison.NotRegex:
				case FilterComparison.NotSet:
				case FilterComparison.NotStartsWith:
					negate = true;
					break;
			}

			bool result = false;   // Default for wrong type
			switch (Column)
			{
				case FilterColumn.Type:
					result = CompareItemType(item);
					break;
				case FilterColumn.Time:
					flItem = item as FieldLogItemViewModel;
					if (flItem == null)
						dbgMsg = item as DebugMessageViewModel;

					if (flItem != null)
						result = CompareTime(flItem.Time);
					else if (dbgMsg != null)
						result = CompareTime(dbgMsg.Time);
					break;
				case FilterColumn.Priority:
					flItem = item as FieldLogItemViewModel;
					if (flItem != null)
						result = ComparePriority(flItem.Priority);
					break;
				case FilterColumn.SessionId:
					flItem = item as FieldLogItemViewModel;
					if (flItem != null)
						result = CompareString(flItem.SessionId.ToString("D"));
					break;
				case FilterColumn.ThreadId:
					flItem = item as FieldLogItemViewModel;
					if (flItem != null)
						result = CompareInt(flItem.ThreadId);
					break;
				case FilterColumn.AnyText:
					textItem = item as FieldLogTextItemViewModel;
					if (textItem == null)
						dataItem = item as FieldLogDataItemViewModel;
					if (textItem == null && dataItem == null)
						exItem = item as FieldLogExceptionItemViewModel;
					if (textItem == null && dataItem == null && exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;
					if (textItem == null && dataItem == null && exItem == null && scopeItem == null)
						dbgMsg = item as DebugMessageViewModel;

					if (textItem != null)
						result = CompareString(textItem.Text) || CompareString(textItem.Details);
					else if (dataItem != null)
						result = CompareString(dataItem.Name) || CompareString(dataItem.Value);
					else if (exItem != null)
						result = CompareString(exItem.Context) ||
							CompareExceptionTypeRecursive(exItem.Exception) ||
							CompareExceptionMessageRecursive(exItem.Exception) ||
							CompareExceptionDataRecursive(exItem.Exception) ||
							!FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData) &&
								(CompareString(exItem.EnvironmentData.AppCompatLayer) ||
								CompareString(exItem.EnvironmentData.AppVersion) ||
								CompareString(exItem.EnvironmentData.CommandLine) ||
								CompareString(exItem.EnvironmentData.CultureName) ||
								CompareString(exItem.EnvironmentData.CurrentDirectory) ||
								CompareString(exItem.EnvironmentData.EnvironmentVariables) ||
								CompareString(exItem.EnvironmentData.ExecutablePath) ||
								CompareString(exItem.EnvironmentData.HostName) ||
								CompareString(exItem.EnvironmentData.OSLanguage) ||
								CompareString(exItem.EnvironmentData.OSProductName) ||
								CompareString(exItem.EnvironmentData.UserName));
					else if (scopeItem != null)
						result = CompareString(scopeItem.Name) ||
							!FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData) &&
								(CompareString(scopeItem.EnvironmentData.AppCompatLayer) ||
								CompareString(scopeItem.EnvironmentData.AppVersion) ||
								CompareString(scopeItem.EnvironmentData.CommandLine) ||
								CompareString(scopeItem.EnvironmentData.CultureName) ||
								CompareString(scopeItem.EnvironmentData.CurrentDirectory) ||
								CompareString(scopeItem.EnvironmentData.EnvironmentVariables) ||
								CompareString(scopeItem.EnvironmentData.ExecutablePath) ||
								CompareString(scopeItem.EnvironmentData.HostName) ||
								CompareString(scopeItem.EnvironmentData.OSLanguage) ||
								CompareString(scopeItem.EnvironmentData.OSProductName) ||
								CompareString(scopeItem.EnvironmentData.UserName));
					else if (dbgMsg != null)
						result = CompareString(dbgMsg.Message);
					break;
				
				case FilterColumn.TextText:
					textItem = item as FieldLogTextItemViewModel;
					if (textItem == null)
						dbgMsg = item as DebugMessageViewModel;

					if (textItem != null)
						result = CompareString(textItem.Text);
					else if (dbgMsg != null)
						result = CompareString(dbgMsg.Message);
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

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = CompareString(exItem.EnvironmentData.CultureName);
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = CompareString(scopeItem.EnvironmentData.CultureName);
					break;
				case FilterColumn.EnvironmentIsShuttingDown:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = exItem.EnvironmentData.IsShuttingDown;
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = scopeItem.EnvironmentData.IsShuttingDown;
					break;
				case FilterColumn.EnvironmentCurrentDirectory:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = CompareString(exItem.EnvironmentData.CurrentDirectory);
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = CompareString(scopeItem.EnvironmentData.CurrentDirectory);
					break;
				case FilterColumn.EnvironmentEnvironmentVariables:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = CompareString(exItem.EnvironmentData.EnvironmentVariables);
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = CompareString(scopeItem.EnvironmentData.EnvironmentVariables);
					break;
				case FilterColumn.EnvironmentHostName:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;
					if ((exItem == null || FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData)) &&
						(scopeItem == null || FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData)))
					{
						flItem = item as FieldLogItemViewModel;
						if (flItem != null)
							scopeItem = flItem.LastLogStartItem;
					}

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = CompareString(exItem.EnvironmentData.HostName);
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = CompareString(scopeItem.EnvironmentData.HostName);
					break;
				case FilterColumn.EnvironmentUserName:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;
					if ((exItem == null || FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData)) &&
						(scopeItem == null || FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData)))
					{
						flItem = item as FieldLogItemViewModel;
						if (flItem != null)
							scopeItem = flItem.LastLogStartItem;
					}

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = CompareString(exItem.EnvironmentData.UserName);
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = CompareString(scopeItem.EnvironmentData.UserName);
					break;
				case FilterColumn.EnvironmentIsInteractive:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;
					if ((exItem == null || FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData)) &&
						(scopeItem == null || FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData)))
					{
						flItem = item as FieldLogItemViewModel;
						if (flItem != null)
							scopeItem = flItem.LastLogStartItem;
					}

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = exItem.EnvironmentData.IsInteractive;
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = scopeItem.EnvironmentData.IsInteractive;
					break;
				case FilterColumn.EnvironmentCommandLine:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;
					if ((exItem == null || FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData)) &&
						(scopeItem == null || FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData)))
					{
						flItem = item as FieldLogItemViewModel;
						if (flItem != null)
							scopeItem = flItem.LastLogStartItem;
					}

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = CompareString(exItem.EnvironmentData.CommandLine);
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = CompareString(scopeItem.EnvironmentData.CommandLine);
					break;
				case FilterColumn.EnvironmentAppVersion:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;
					if ((exItem == null || FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData)) &&
						(scopeItem == null || FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData)))
					{
						flItem = item as FieldLogItemViewModel;
						if (flItem != null)
							scopeItem = flItem.LastLogStartItem;
					}

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = CompareString(exItem.EnvironmentData.AppVersion);
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = CompareString(scopeItem.EnvironmentData.AppVersion);
					break;
				case FilterColumn.EnvironmentProcessMemory:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = CompareLong(exItem.EnvironmentData.ProcessMemory);
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = CompareLong(scopeItem.EnvironmentData.ProcessMemory);
					break;
				case FilterColumn.EnvironmentPeakProcessMemory:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = CompareLong(exItem.EnvironmentData.PeakProcessMemory);
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = CompareLong(scopeItem.EnvironmentData.PeakProcessMemory);
					break;
				case FilterColumn.EnvironmentTotalMemory:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = CompareLong(exItem.EnvironmentData.TotalMemory);
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = CompareLong(scopeItem.EnvironmentData.TotalMemory);
					break;
				case FilterColumn.EnvironmentAvailableMemory:
					exItem = item as FieldLogExceptionItemViewModel;
					if (exItem == null)
						scopeItem = item as FieldLogScopeItemViewModel;

					if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentData))
						result = CompareLong(exItem.EnvironmentData.AvailableMemory);
					else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentData))
						result = CompareLong(scopeItem.EnvironmentData.AvailableMemory);
					break;
			}

			if (negate)
				return !result;
			else
				return result;
		}

		private bool CompareTime(DateTime time)
		{
			DateTime filterTime;
			Match m;
			switch (Comparison)
			{
				case FilterComparison.LessThan:
					if (DateTime.TryParse(Value, out filterTime))
					{
						return time < filterTime;
					}
					// User typed in an invalid int number.
					return false;
				case FilterComparison.LessOrEqual:
					if (DateTime.TryParse(Value, out filterTime))
					{
						return time <= filterTime;
					}
					// User typed in an invalid int number.
					return false;
				case FilterComparison.GreaterThan:
					if (DateTime.TryParse(Value, out filterTime))
					{
						return time > filterTime;
					}
					// User typed in an invalid int number.
					return false;
				case FilterComparison.GreaterOrEqual:
					if (DateTime.TryParse(Value, out filterTime))
					{
						return time >= filterTime;
					}
					// User typed in an invalid int number.
					return false;
				case FilterComparison.InYear:
					m = Regex.Match((Value ?? "").Trim(), "^([0-9]{4})");
					if (m.Success)
					{
						return time.Year == int.Parse(m.Groups[1].Value);
					}
					// User typed in an invalid partial time.
					return false;
				case FilterComparison.InMonth:
					m = Regex.Match((Value ?? "").Trim(), "^([0-9]{4})-([0-9]{2})");
					if (m.Success)
					{
						return time.Year == int.Parse(m.Groups[1].Value) &&
							time.Month == int.Parse(m.Groups[2].Value);
					}
					else
					{
						// Try month in any year
						m = Regex.Match((Value ?? "").Trim(), "^([0-9]{2})");
						if (m.Success)
						{
							return time.Month == int.Parse(m.Groups[1].Value);
						}
					}
					// User typed in an invalid partial time.
					return false;
				case FilterComparison.InDay:
					m = Regex.Match((Value ?? "").Trim(), "^([0-9]{4})-([0-9]{2})-([0-9]{2})");
					if (m.Success)
					{
						return time.Year == int.Parse(m.Groups[1].Value) &&
							time.Month == int.Parse(m.Groups[2].Value) &&
							time.Day == int.Parse(m.Groups[3].Value);
					}
					else
					{
						// Try day in any month and year
						m = Regex.Match((Value ?? "").Trim(), "^([0-9]{2})");
						if (m.Success)
						{
							return time.Day == int.Parse(m.Groups[1].Value);
						}
					}
					// User typed in an invalid partial time.
					return false;
				case FilterComparison.InHour:
					m = Regex.Match((Value ?? "").Trim(), "^([0-9]{4})-([0-9]{2})-([0-9]{2})[T ]([0-9]{2})");
					if (m.Success)
					{
						return time.Year == int.Parse(m.Groups[1].Value) &&
							time.Month == int.Parse(m.Groups[2].Value) &&
							time.Day == int.Parse(m.Groups[3].Value) &&
							time.Hour == int.Parse(m.Groups[4].Value);
					}
					else
					{
						// Try hour in any day
						m = Regex.Match((Value ?? "").Trim(), "^([0-9]{2})");
						if (m.Success)
						{
							return time.Hour == int.Parse(m.Groups[1].Value);
						}
					}
					// User typed in an invalid partial time.
					return false;
				case FilterComparison.InMinute:
					m = Regex.Match((Value ?? "").Trim(), "^([0-9]{4})-([0-9]{2})-([0-9]{2})[T ]([0-9]{2}):([0-9]{2})");
					if (m.Success)
					{
						return time.Year == int.Parse(m.Groups[1].Value) &&
							time.Month == int.Parse(m.Groups[2].Value) &&
							time.Day == int.Parse(m.Groups[3].Value) &&
							time.Hour == int.Parse(m.Groups[4].Value) &&
							time.Minute == int.Parse(m.Groups[5].Value);
					}
					else
					{
						// Try minute in any hour
						m = Regex.Match((Value ?? "").Trim(), "^([0-9]{2})");
						if (m.Success)
						{
							return time.Minute == int.Parse(m.Groups[1].Value);
						}
					}
					// User typed in an invalid partial time.
					return false;
				default:
					throw new Exception("Invalid comparison for time column: " + Comparison);
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
					case FilterComparison.NotEquals:
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
						throw new Exception("Invalid comparison for Priority column: " + Comparison);
				}
			}
			else
			{
				// Invalid value for Priority column
				return false;
			}
		}

		private bool CompareItemType(object item)
		{
			FilterItemType filterType;
			if (Enum.TryParse(Value, out filterType))
			{
				switch (Comparison)
				{
					case FilterComparison.Equals:
					case FilterComparison.NotEquals:
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
							case FilterItemType.DebugOutput:
								return item is DebugMessageViewModel;
						}
						return false;
					default:
						throw new Exception("Invalid comparison for ItemType column: " + Comparison);
				}
			}
			else
			{
				// Invalid value for ItemType column
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
					case FilterComparison.NotEquals:
						return scopeType == filterScopeType;
					default:
						throw new Exception("Invalid comparison for ScopeType column: " + Comparison);
				}
			}
			else
			{
				// Invalid value for ScopeType column
				return false;
			}
		}

		private bool CompareString(string str)
		{
			switch (Comparison)
			{
				case FilterComparison.Equals:
				case FilterComparison.NotEquals:
					return (str ?? "") == (Value ?? "");
				case FilterComparison.GreaterOrEqual:
					return str != null && str.CompareTo(Value ?? "") >= 0;
				case FilterComparison.GreaterThan:
					return str != null && str.CompareTo(Value ?? "") > 0;
				case FilterComparison.LessOrEqual:
					return str != null && str.CompareTo(Value ?? "") <= 0;
				case FilterComparison.LessThan:
					return str != null && str.CompareTo(Value ?? "") < 0;
				case FilterComparison.Contains:
				case FilterComparison.NotContains:
					return str != null && str.ToLower().Contains((Value ?? "").ToLower());
				case FilterComparison.StartsWith:
				case FilterComparison.NotStartsWith:
					return str != null && str.ToLower().StartsWith((Value ?? "").ToLower());
				case FilterComparison.EndsWith:
				case FilterComparison.NotEndsWith:
					return str != null && str.ToLower().EndsWith((Value ?? "").ToLower());
				case FilterComparison.Regex:
				case FilterComparison.NotRegex:
					return Regex.IsMatch(str ?? "", Value ?? "", RegexOptions.IgnoreCase);
				default:
					throw new Exception("Invalid comparison for string column: " + Comparison);
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
					case FilterComparison.NotEquals:
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
						throw new Exception("Invalid comparison for int column: " + Comparison);
				}
			}
			else
			{
				// User typed in an invalid int number.
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
					case FilterComparison.NotEquals:
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
						throw new Exception("Invalid comparison for long column: " + Comparison);
				}
			}
			else
			{
				// User typed in an invalid long number.
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
			newCond.Comparison = this.Comparison;
			newCond.Value = this.Value;
			newCond.IsEnabled = this.IsEnabled;
			return newCond;
		}

		#endregion Duplicate
	}

	#region Filter definition enums

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
		[Description("Env: Host name")]
		EnvironmentHostName,
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
		[UseForEnumColumn, UseForNumberColumn, UseForStringColumn]
		[Description("=")]
		Equals,
		[UseForEnumColumn, UseForNumberColumn, UseForStringColumn]
		[Description("≠")]
		NotEquals,
		[UseForTimeColumn]
		[Description("is in year")]
		InYear,
		[UseForTimeColumn]
		[Description("is in month")]
		InMonth,
		[UseForTimeColumn]
		[Description("is in day")]
		InDay,
		[UseForTimeColumn]
		[Description("is in hour")]
		InHour,
		[UseForTimeColumn]
		[Description("is in minute")]
		InMinute,
		[UseForNumberColumn, UseForStringColumn, UseForTimeColumn]
		[Description("<")]
		LessThan,
		[UseForNumberColumn, UseForStringColumn, UseForTimeColumn]
		[Description("≤")]
		LessOrEqual,
		[UseForNumberColumn, UseForStringColumn, UseForTimeColumn]
		[Description("≥")]
		GreaterOrEqual,
		[UseForNumberColumn, UseForStringColumn, UseForTimeColumn]
		[Description(">")]
		GreaterThan,
		[UseForStringColumn]
		[Description("contains")]
		Contains,
		[UseForStringColumn]
		[Description("does not contain")]
		NotContains,
		[UseForStringColumn]
		[Description("starts with")]
		StartsWith,
		[UseForStringColumn]
		[Description("does not start with")]
		NotStartsWith,
		[UseForStringColumn]
		[Description("ends with")]
		EndsWith,
		[UseForStringColumn]
		[Description("does not end with")]
		NotEndsWith,
		[UseForStringColumn]
		[Description("matches regex")]
		Regex,
		[UseForStringColumn]
		[Description("does not match regex")]
		NotRegex,
		[UseForBoolColumn]
		[Description("is set")]
		Set,
		[UseForBoolColumn]
		[Description("is not set")]
		NotSet,
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
		// Removed because this is a value for the ItemType field and it affects how other fields
		// of exception items shall be compared (recursing into inner exceptions or not). There's
		// additional logic required to make one field comparison dependent of another condition
		// value.
		[Description("Scope")]
		Scope,
		[Description("DebugOutputString")]
		DebugOutput,
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

	#endregion Filter definition enums

	#region Filter comparison usage attributes

	class UseForEnumColumnAttribute : Attribute
	{
	}

	class UseForStringColumnAttribute : Attribute
	{
	}

	class UseForNumberColumnAttribute : Attribute
	{
	}

	class UseForBoolColumnAttribute : Attribute
	{
	}

	class UseForTimeColumnAttribute : Attribute
	{
	}

	#endregion Filter comparison usage attributes
}
