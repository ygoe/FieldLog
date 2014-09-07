using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Unclassified.FieldLog;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.ViewModels
{
	internal class FilterConditionViewModel : ViewModelBase
	{
		#region Private data

		private FilterConditionGroupViewModel parentConditionGroup;

		#endregion Private data

		#region Constructor

		public FilterConditionViewModel(FilterConditionGroupViewModel parentConditionGroup)
		{
			this.parentConditionGroup = parentConditionGroup;

			comparison = FilterComparison.Contains;
			value = "";
			isEnabled = true;
		}

		#endregion Constructor

		#region Commands

		public DelegateCommand DeleteCommand { get; private set; }

		protected override void InitializeCommands()
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
					enableFilterChangedEvent = false;
					// Set value to an acceptable default for enum columns
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
						case FilterColumn.EnvironmentOSType:
							Value = OSType.Client.ToString();
							break;
						case FilterColumn.EnvironmentOSVersion:
							Value = OSVersion.Unknown.ToString();
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
					enableFilterChangedEvent = true;
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
					case FilterColumn.EnvironmentOSType:
						return new EnumerationExtension(typeof(OSType), true).ProvideTypedValue();
					case FilterColumn.EnvironmentOSVersion:
						return new EnumerationExtension(typeof(OSVersion), true).ProvideTypedValue();
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
					case FilterColumn.EnvironmentCommandLine:
					case FilterColumn.EnvironmentAppVersion:
					case FilterColumn.EnvironmentCurrentDirectory:
					case FilterColumn.EnvironmentCultureName:
					case FilterColumn.EnvironmentUserName:
					case FilterColumn.EnvironmentHostName:
					case FilterColumn.EnvironmentEnvironmentVariables:
					case FilterColumn.WebRequestRequestUrl:
					case FilterColumn.WebRequestMethod:
					case FilterColumn.WebRequestClientAddress:
					case FilterColumn.WebRequestClientHostName:
					case FilterColumn.WebRequestReferrer:
					case FilterColumn.WebRequestUserAgent:
					case FilterColumn.WebRequestAcceptLanguages:
					case FilterColumn.WebRequestAccept:
					case FilterColumn.WebRequestWebSessionId:
					case FilterColumn.WebRequestAppUserId:
					case FilterColumn.WebRequestAppUserName:
						return new EnumerationExtension<FilterComparison>(typeof(UseForStringColumnAttribute)).ProvideTypedValue();

					case FilterColumn.Type:
					case FilterColumn.ScopeType:
					case FilterColumn.EnvironmentOSType:
						return new EnumerationExtension<FilterComparison>(typeof(UseForEnumColumnAttribute)).ProvideTypedValue();

					case FilterColumn.Time:
						return new EnumerationExtension<FilterComparison>(typeof(UseForTimeColumnAttribute)).ProvideTypedValue();

					case FilterColumn.Priority:
					case FilterColumn.ThreadId:
					case FilterColumn.WebRequestId:
					case FilterColumn.ExceptionCode:
					case FilterColumn.ScopeLevel:
					case FilterColumn.EnvironmentProcessId:
					case FilterColumn.EnvironmentProcessMemory:
					case FilterColumn.EnvironmentPeakProcessMemory:
					case FilterColumn.EnvironmentOSVersion:
					case FilterColumn.EnvironmentCpuCount:
					case FilterColumn.EnvironmentTotalMemory:
					case FilterColumn.EnvironmentAvailableMemory:
					case FilterColumn.EnvironmentScreenDpi:
					case FilterColumn.WebRequestDuration:
						return new EnumerationExtension<FilterComparison>(typeof(UseForNumberColumnAttribute)).ProvideTypedValue();

					case FilterColumn.ScopeIsBackgroundThread:
					case FilterColumn.ScopeIsPoolThread:
					case FilterColumn.EnvironmentIsAdministrator:
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
					case FilterColumn.WebRequestId:
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
					case FilterColumn.EnvironmentProcessId:
					case FilterColumn.EnvironmentCommandLine:
					case FilterColumn.EnvironmentAppVersion:
					case FilterColumn.EnvironmentCurrentDirectory:
					case FilterColumn.EnvironmentCultureName:
					case FilterColumn.EnvironmentUserName:
					case FilterColumn.EnvironmentProcessMemory:
					case FilterColumn.EnvironmentPeakProcessMemory:
					case FilterColumn.EnvironmentCpuCount:
					case FilterColumn.EnvironmentHostName:
					case FilterColumn.EnvironmentTotalMemory:
					case FilterColumn.EnvironmentAvailableMemory:
					case FilterColumn.EnvironmentScreenDpi:
					case FilterColumn.EnvironmentEnvironmentVariables:
					case FilterColumn.WebRequestRequestUrl:
					case FilterColumn.WebRequestMethod:
					case FilterColumn.WebRequestClientAddress:
					case FilterColumn.WebRequestClientHostName:
					case FilterColumn.WebRequestReferrer:
					case FilterColumn.WebRequestUserAgent:
					case FilterColumn.WebRequestAcceptLanguages:
					case FilterColumn.WebRequestAccept:
					case FilterColumn.WebRequestWebSessionId:
					case FilterColumn.WebRequestAppUserId:
					case FilterColumn.WebRequestAppUserName:
					case FilterColumn.WebRequestDuration:
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
					case FilterColumn.EnvironmentOSType:
					case FilterColumn.EnvironmentOSVersion:
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
			if (enableFilterChangedEvent && parentConditionGroup != null)
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
			FieldLogEventEnvironment env = null;
			FieldLogEventEnvironment envFallback = null;
			FieldLogWebRequestData webRequestData = null;

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
				case FilterComparison.NotInList:
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
						result = CompareTime(flItem.Time, flItem.UtcOffset);
					else if (dbgMsg != null)
						result = CompareTime(dbgMsg.Time, dbgMsg.UtcOffset);
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
				case FilterColumn.WebRequestId:
					flItem = item as FieldLogItemViewModel;
					if (flItem != null)
						result = CompareLong(flItem.WebRequestId);
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
					env = GetEnvironmentFromItem(item, false);
					envFallback = GetEnvironmentFromItem(item, true);
					webRequestData = GetWebRequestDataFromItem(item);

					bool envFallbackResult = envFallback != null &&
						(CompareString(envFallback.ProcessId.ToString()) ||
						CompareString(envFallback.CommandLine) ||
						CompareString(envFallback.AppVersion) ||
						CompareString(envFallback.UserName) ||
						CompareString(envFallback.HostName));

					bool webRequestDataResult = webRequestData != null &&
						(CompareString(webRequestData.RequestUrl) ||
						CompareString(webRequestData.ClientAddress) ||
						CompareString(webRequestData.ClientHostName) ||
						CompareString(webRequestData.Referrer) ||
						CompareString(webRequestData.UserAgent) ||
						CompareString(webRequestData.AcceptLanguages) ||
						CompareString(webRequestData.Accept) ||
						CompareString(webRequestData.WebSessionId) ||
						CompareString(webRequestData.AppUserId) ||
						CompareString(webRequestData.AppUserName));

					if (textItem != null)
						result = CompareString(textItem.SessionId.ToString("D")) ||
							CompareString(textItem.Text) ||
							CompareString(textItem.Details) ||
							envFallbackResult ||
							webRequestDataResult;
					else if (dataItem != null)
						result = CompareString(dataItem.SessionId.ToString("D")) ||
							CompareString(dataItem.Name) ||
							CompareString(dataItem.Value) ||
							envFallbackResult ||
							webRequestDataResult;
					else if (exItem != null)
						result = CompareString(exItem.SessionId.ToString("D")) ||
							CompareString(exItem.Context) ||
							CompareExceptionTypeRecursive(exItem.Exception) ||
							CompareExceptionMessageRecursive(exItem.Exception) ||
							CompareExceptionDataRecursive(exItem.Exception) ||
							env != null &&
								(CompareString(env.CurrentDirectory) ||
								CompareString(env.CultureName) ||
								CompareString(env.EnvironmentVariables)) ||
							envFallbackResult ||
							webRequestDataResult;
					else if (scopeItem != null)
						result = CompareString(scopeItem.SessionId.ToString("D")) ||
							CompareString(scopeItem.Name) ||
							env != null &&
								(CompareString(env.CurrentDirectory) ||
								CompareString(env.CultureName) ||
								CompareString(env.EnvironmentVariables)) ||
							envFallbackResult ||
							webRequestDataResult;
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

				case FilterColumn.EnvironmentProcessId:
					env = GetEnvironmentFromItem(item, true);
					if (env != null)
						result = CompareInt(env.ProcessId);
					break;
				case FilterColumn.EnvironmentCommandLine:
					env = GetEnvironmentFromItem(item, true);
					if (env != null)
						result = CompareString(env.CommandLine);
					break;
				case FilterColumn.EnvironmentAppVersion:
					env = GetEnvironmentFromItem(item, true);
					if (env != null)
						result = CompareString(env.AppVersion);
					break;
				case FilterColumn.EnvironmentCurrentDirectory:
					env = GetEnvironmentFromItem(item, false);
					if (env != null)
						result = CompareString(env.CurrentDirectory);
					break;
				case FilterColumn.EnvironmentCultureName:
					env = GetEnvironmentFromItem(item, false);
					if (env != null)
						result = CompareString(env.CultureName);
					break;
				case FilterColumn.EnvironmentUserName:
					env = GetEnvironmentFromItem(item, true);
					if (env != null)
						result = CompareString(env.UserName);
					break;
				case FilterColumn.EnvironmentIsAdministrator:
					env = GetEnvironmentFromItem(item, true);
					if (env != null)
						result = env.IsAdministrator;
					break;
				case FilterColumn.EnvironmentIsInteractive:
					env = GetEnvironmentFromItem(item, true);
					if (env != null)
						result = env.IsInteractive;
					break;
				case FilterColumn.EnvironmentProcessMemory:
					env = GetEnvironmentFromItem(item, false);
					if (env != null)
						result = CompareLong(env.ProcessMemory);
					break;
				case FilterColumn.EnvironmentPeakProcessMemory:
					env = GetEnvironmentFromItem(item, false);
					if (env != null)
						result = CompareLong(env.PeakProcessMemory);
					break;
				case FilterColumn.EnvironmentOSType:
					env = GetEnvironmentFromItem(item, true);
					if (env != null)
						result = CompareOSType(env.OSType);
					break;
				case FilterColumn.EnvironmentOSVersion:
					env = GetEnvironmentFromItem(item, true);
					if (env != null)
						result = CompareOSVersion(env.OSVersion);
					break;
				case FilterColumn.EnvironmentCpuCount:
					env = GetEnvironmentFromItem(item, true);
					if (env != null)
						result = CompareInt(env.CpuCount);
					break;
				case FilterColumn.EnvironmentHostName:
					env = GetEnvironmentFromItem(item, true);
					if (env != null)
						result = CompareString(env.HostName);
					break;
				case FilterColumn.EnvironmentTotalMemory:
					env = GetEnvironmentFromItem(item, true);
					if (env != null)
						result = CompareLong(env.TotalMemory);
					break;
				case FilterColumn.EnvironmentAvailableMemory:
					env = GetEnvironmentFromItem(item, false);
					if (env != null)
						result = CompareLong(env.AvailableMemory);
					break;
				case FilterColumn.EnvironmentScreenDpi:
					env = GetEnvironmentFromItem(item, true);
					if (env != null)
						result = CompareInt(env.ScreenDpi);
					break;
				case FilterColumn.EnvironmentEnvironmentVariables:
					env = GetEnvironmentFromItem(item, false);
					if (env != null)
						result = CompareString(env.EnvironmentVariables);
					break;

				case FilterColumn.WebRequestRequestUrl:
					webRequestData = GetWebRequestDataFromItem(item);
					if (webRequestData != null)
						result = CompareString(webRequestData.RequestUrl);
					break;
				case FilterColumn.WebRequestMethod:
					webRequestData = GetWebRequestDataFromItem(item);
					if (webRequestData != null)
						result = CompareString(webRequestData.Method);
					break;
				case FilterColumn.WebRequestClientAddress:
					webRequestData = GetWebRequestDataFromItem(item);
					if (webRequestData != null)
						result = CompareString(webRequestData.ClientAddress);
					break;
				case FilterColumn.WebRequestClientHostName:
					webRequestData = GetWebRequestDataFromItem(item);
					if (webRequestData != null)
						result = CompareString(webRequestData.ClientHostName);
					break;
				case FilterColumn.WebRequestReferrer:
					webRequestData = GetWebRequestDataFromItem(item);
					if (webRequestData != null)
						result = CompareString(webRequestData.Referrer);
					break;
				case FilterColumn.WebRequestUserAgent:
					webRequestData = GetWebRequestDataFromItem(item);
					if (webRequestData != null)
						result = CompareString(webRequestData.UserAgent);
					break;
				case FilterColumn.WebRequestAcceptLanguages:
					webRequestData = GetWebRequestDataFromItem(item);
					if (webRequestData != null)
						result = CompareString(webRequestData.AcceptLanguages);
					break;
				case FilterColumn.WebRequestAccept:
					webRequestData = GetWebRequestDataFromItem(item);
					if (webRequestData != null)
						result = CompareString(webRequestData.Accept);
					break;
				case FilterColumn.WebRequestWebSessionId:
					webRequestData = GetWebRequestDataFromItem(item);
					if (webRequestData != null)
						result = CompareString(webRequestData.WebSessionId);
					break;
				case FilterColumn.WebRequestAppUserId:
					webRequestData = GetWebRequestDataFromItem(item);
					if (webRequestData != null)
						result = CompareString(webRequestData.AppUserId);
					break;
				case FilterColumn.WebRequestAppUserName:
					webRequestData = GetWebRequestDataFromItem(item);
					if (webRequestData != null)
						result = CompareString(webRequestData.AppUserName);
					break;
				case FilterColumn.WebRequestDuration:
					flItem = item as FieldLogItemViewModel;
					if (flItem != null && flItem.LastWebRequestStartItem != null)
					{
						result = CompareInt((int) Math.Round(flItem.LastWebRequestStartItem.WebRequestDataVM.RequestDuration.TotalMilliseconds));
					}
					break;
			}

			if (negate)
				return !result;
			else
				return result;
		}

		/// <summary>
		/// Finds the environment instance in the specified item.
		/// </summary>
		/// <param name="item">The item to search.</param>
		/// <param name="useLastLogStart">true to regard LastLogStartItem.</param>
		/// <returns>The FieldLogEventEnvironment instance if it is not null or Empty; otherwise, null.</returns>
		private FieldLogEventEnvironment GetEnvironmentFromItem(object item, bool useLastLogStart)
		{
			FieldLogItemViewModel flItem = null;
			FieldLogExceptionItemViewModel exItem = null;
			FieldLogScopeItemViewModel scopeItem = null;

			exItem = item as FieldLogExceptionItemViewModel;
			if (exItem == null)
				scopeItem = item as FieldLogScopeItemViewModel;
			if (useLastLogStart)
			{
				if ((exItem == null || FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentVM.Environment)) &&
					(scopeItem == null || FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentVM.Environment)))
				{
					flItem = item as FieldLogItemViewModel;
					if (flItem != null)
						scopeItem = flItem.LastLogStartItem;
				}
			}

			FieldLogEventEnvironment env = null;
			if (exItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(exItem.EnvironmentVM.Environment))
				env = exItem.EnvironmentVM.Environment;
			else if (scopeItem != null && !FieldLogEventEnvironment.IsNullOrEmpty(scopeItem.EnvironmentVM.Environment))
				env = scopeItem.EnvironmentVM.Environment;

			if (FieldLogEventEnvironment.IsNullOrEmpty(env))
				env = null;
			return env;
		}

		/// <summary>
		/// Finds the web request data instance in the specified item.
		/// </summary>
		/// <param name="item">The item to search.</param>
		/// <returns>The FieldLogWebRequestData instance if it is not null or Empty; otherwise, null.</returns>
		private FieldLogWebRequestData GetWebRequestDataFromItem(object item)
		{
			FieldLogItemViewModel flItem = null;
			FieldLogScopeItemViewModel scopeItem = null;

			scopeItem = item as FieldLogScopeItemViewModel;
			if (scopeItem == null || FieldLogWebRequestData.IsNullOrEmpty(scopeItem.WebRequestDataVM.WebRequestData))
			{
				flItem = item as FieldLogItemViewModel;
				if (flItem != null)
					scopeItem = flItem.LastWebRequestStartItem;
			}

			FieldLogWebRequestData webRequestData = null;
			if (scopeItem != null && !FieldLogWebRequestData.IsNullOrEmpty(scopeItem.WebRequestDataVM.WebRequestData))
				webRequestData = scopeItem.WebRequestDataVM.WebRequestData;

			if (FieldLogWebRequestData.IsNullOrEmpty(webRequestData))
				webRequestData = null;
			return webRequestData;
		}

		private bool CompareTime(DateTime time, int utcOffset)
		{
			switch (AppSettings.Instance.ItemTimeMode)
			{
				case ItemTimeType.Local:
					time = time.ToLocalTime();
					break;
				case ItemTimeType.Remote:
					time = time.AddMinutes(utcOffset);
					break;
			}

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

		private bool CompareOSType(OSType osType)
		{
			OSType filterOSType;
			if (Enum.TryParse(Value, out filterOSType))
			{
				switch (Comparison)
				{
					case FilterComparison.Equals:
					case FilterComparison.NotEquals:
						return osType == filterOSType;
					default:
						throw new Exception("Invalid comparison for OSType column: " + Comparison);
				}
			}
			else
			{
				// Invalid value for OSType column
				return false;
			}
		}

		private bool CompareOSVersion(OSVersion osVersion)
		{
			OSVersion filterOSVersion;
			if (Enum.TryParse(Value, out filterOSVersion))
			{
				switch (Comparison)
				{
					case FilterComparison.Equals:
					case FilterComparison.NotEquals:
						return osVersion == filterOSVersion;
					case FilterComparison.GreaterOrEqual:
						return osVersion >= filterOSVersion;
					case FilterComparison.GreaterThan:
						return osVersion > filterOSVersion;
					case FilterComparison.LessOrEqual:
						return osVersion <= filterOSVersion;
					case FilterComparison.LessThan:
						return osVersion < filterOSVersion;
					default:
						throw new Exception("Invalid comparison for OSVersion column: " + Comparison);
				}
			}
			else
			{
				// Invalid value for OSVersion column
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
				case FilterComparison.InList:
				case FilterComparison.NotInList:
					return Value.Split(';').Any(s => s.Trim() == (str ?? "").Trim());
				default:
					throw new Exception("Invalid comparison for string column: " + Comparison);
			}
		}

		private bool CompareInt(int i)
		{
			if (Comparison == FilterComparison.InList ||
				Comparison == FilterComparison.NotInList)
			{
				return Value.Split(';').Any(li => li.Trim() == i.ToString());
			}
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
			if (Comparison == FilterComparison.InList ||
				Comparison == FilterComparison.NotInList)
			{
				return Value.Split(';').Any(ll => ll.Trim() == l.ToString());
			}
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

	internal enum FilterColumn
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
		[Description("Web request ID")]
		WebRequestId,

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

		[Description("Env: Process ID")]
		EnvironmentProcessId,
		[Description("Env: Command line")]
		EnvironmentCommandLine,
		[Description("Env: App version")]
		EnvironmentAppVersion,
		[Description("Env: Current directory")]
		EnvironmentCurrentDirectory,
		[Description("Env: Culture code")]
		EnvironmentCultureName,
		[Description("Env: User name")]
		EnvironmentUserName,
		[Description("Env: Administrator")]
		EnvironmentIsAdministrator,
		[Description("Env: Interactive")]
		EnvironmentIsInteractive,
		[Description("Env: Process memory")]
		EnvironmentProcessMemory,
		[Description("Env: Peak process memory")]
		EnvironmentPeakProcessMemory,
		[Description("Env: OS type")]
		EnvironmentOSType,
		[Description("Env: OS version")]
		EnvironmentOSVersion,
		[Description("Env: CPU count")]
		EnvironmentCpuCount,
		[Description("Env: Host name")]
		EnvironmentHostName,
		[Description("Env: Total memory")]
		EnvironmentTotalMemory,
		[Description("Env: Available memory")]
		EnvironmentAvailableMemory,
		[Description("Env: Logical resolution")]
		EnvironmentScreenDpi,
		[Description("Env: Environment vars")]
		EnvironmentEnvironmentVariables,

		[Description("Web: Request URL")]
		WebRequestRequestUrl,
		[Description("Web: Method")]
		WebRequestMethod,
		[Description("Web: Client address")]
		WebRequestClientAddress,
		[Description("Web: Client host name")]
		WebRequestClientHostName,
		[Description("Web: Referrer")]
		WebRequestReferrer,
		[Description("Web: User agent")]
		WebRequestUserAgent,
		[Description("Web: Languages")]
		WebRequestAcceptLanguages,
		[Description("Web: Accepted types")]
		WebRequestAccept,
		[Description("Web: Session ID")]
		WebRequestWebSessionId,
		[Description("Web: App user ID")]
		WebRequestAppUserId,
		[Description("Web: App user name")]
		WebRequestAppUserName,
		[Description("Web: Request duration [ms]")]
		WebRequestDuration,
	}

	internal enum FilterComparison
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
		[UseForNumberColumn, UseForStringColumn]
		[Description("is in list")]
		InList,
		[UseForNumberColumn, UseForStringColumn]
		[Description("is not in list")]
		NotInList,
	}

	internal enum FilterItemType
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

	internal enum FilterPriority
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

	internal enum FilterScopeType
	{
		[Description("Enter")]
		Enter,
		[Description("Leave")]
		Leave,
		[Description("ThreadStart")]
		ThreadStart,
		[Description("ThreadEnd")]
		ThreadEnd,
		[Description("WebRequestStart")]
		WebRequestStart,
		[Description("WebRequestEnd")]
		WebRequestEnd,
		[Description("LogStart")]
		LogStart,
		[Description("LogShutdown")]
		LogShutdown
	}

	#endregion Filter definition enums

	#region Filter comparison usage attributes

	internal class UseForEnumColumnAttribute : Attribute
	{
	}

	internal class UseForStringColumnAttribute : Attribute
	{
	}

	internal class UseForNumberColumnAttribute : Attribute
	{
	}

	internal class UseForBoolColumnAttribute : Attribute
	{
	}

	internal class UseForTimeColumnAttribute : Attribute
	{
	}

	#endregion Filter comparison usage attributes
}
