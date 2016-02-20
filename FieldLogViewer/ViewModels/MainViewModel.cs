using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;
using TaskDialogInterop;
using Unclassified.FieldLog;
using Unclassified.FieldLogViewer.SourceInfo;
using Unclassified.FieldLogViewer.Views;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.FieldLogViewer.ViewModels
{
	internal class MainViewModel : ViewModelBase, IViewCommandSource
	{
		#region Static data

		public static MainViewModel Instance { get; private set; }

		#endregion Static data

		#region Private data

		private Dispatcher dispatcher;
		private ObservableCollection<LogItemViewModelBase> logItems = new ObservableCollection<LogItemViewModelBase>();
		private CollectionViewSource sortedFilters = new CollectionViewSource();
		private CollectionViewSource filteredLogItems = new CollectionViewSource();
		private string loadedBasePath;
		private FieldLogFileGroupReader logFileGroupReader;
		private bool isLiveStopped = true;
		private DateTime insertingItemsSince;
		private Task readerTask;
		private FilterConditionViewModel adhocFilterCondition;
		private SourceResolver sourceResolver = new SourceResolver();
		private Deobfuscator deobfuscator = new Deobfuscator();
		private SettingsWindow openSettingsWindow;
		private DebugMonitor localDebugMonitor = new DebugMonitor(false);
		private DebugMonitor globalDebugMonitor = new DebugMonitor(true);
		private bool autoLoadLog;
		private bool tempActivatedLocalDebugMonitor;

		/// <summary>
		/// Buffer for all read items that are collected in the separate Task thread and then
		/// pushed to the UI thread as a new ObservableCollection instance.
		/// </summary>
		private List<LogItemViewModelBase> localLogItems;
		/// <summary>
		/// Synchronises access to the localLogItems variable.
		/// </summary>
		private ReaderWriterLockSlim localLogItemsLock = new ReaderWriterLockSlim();
		/// <summary>
		/// The number of new log items queued for inserting in the UI thread. Must always be
		/// accessed with the Interlocked class.
		/// </summary>
		private int queuedNewItemsCount;
		/// <summary>
		/// Set if the UI thread requests the read thread to stop sending new items to the UI thread
		/// separately and use a local list instead.
		/// </summary>
		private AutoResetEvent returnToLocalLogItemsList = new AutoResetEvent(false);

		#endregion Private data

		#region Constructors

		public MainViewModel()
		{
			Instance = this;
			dispatcher = Dispatcher.CurrentDispatcher;

			UpdateWindowTitle();
			SetJumpList();

			// Setup toolbar and settings events
			this.BindProperty(vm => vm.IsLocalDebugMonitorActive, App.Settings, s => s.IsLocalDebugMonitorActive);
			this.BindProperty(vm => vm.IsGlobalDebugMonitorActive, App.Settings, s => s.IsGlobalDebugMonitorActive);
			App.Settings.OnPropertyChanged(
				s => s.IsLiveScrollingEnabled,
				v => { if (v) ViewCommandManager.Invoke("ScrollToEnd"); });
			App.Settings.OnPropertyChanged(
				s => s.IsWindowOnTop,
				v => MainWindow.Instance.Topmost = v,
				true);
			App.Settings.OnPropertyChanged(
				s => s.IndentSize,
				() =>
				{
					DecreaseIndentSizeCommand.RaiseCanExecuteChanged();
					IncreaseIndentSizeCommand.RaiseCanExecuteChanged();
				});
			App.Settings.OnPropertyChanged(
				s => s.ItemTimeMode,
				() => RefreshLogItemsFilterView());
			App.Settings.OnPropertyChanged(
				s => s.ShowStackFrameMetadata,
				() => { if (SelectedItems != null) SelectedItems.ForEach(i => i.Refresh()); });

			// Setup filter events
			Filters = new ObservableCollection<FilterViewModel>();
			Filters.ForAddedRemoved(
				f => f.FilterChanged += LogItemsFilterChanged,
				f => f.FilterChanged -= LogItemsFilterChanged);
			Filters.Add(new FilterViewModel(true));
			Filters.CollectionChanged += (s, e) =>
			{
				// Trigger saving the new filter collection.
				// Wait a moment or the new filter will appear twice in the filter lists until
				// something else has changed and we probably come here again. (Unsure why.)
				TaskHelper.WhenLoaded(() => LogItemsFilterChanged(false));
			};

			// Load filters
			foreach (string s in App.Settings.Filters)
			{
				FilterViewModel f = new FilterViewModel();
				try
				{
					f.LoadFromString(s);
				}
				catch (Exception ex)
				{
					App.ErrorMessage("A filter could not be restored from the settings.", ex, "Loading filters");
					continue;
				}
				Filters.Add(f);
			}
			// If no filter is defined, create some basic filters for a start
			if (Filters.Count == 1)
			{
				// Only the "show all" filter is present
				CreateBasicFilters();
			}

			// Restore filter selection
			FilterViewModel selectedFilterVM = Filters.FirstOrDefault(f => f.DisplayName == App.Settings.SelectedFilter);
			if (selectedFilterVM != null)
			{
				SelectedFilter = selectedFilterVM;
			}
			else
			{
				SelectedFilter = Filters[0];
			}

			// Setup sorted filters view
			sortedFilters.Source = Filters;
			sortedFilters.SortDescriptions.Add(new SortDescription("AcceptAll", ListSortDirection.Descending));
			sortedFilters.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));

			// Setup filtered log items view
			filteredLogItems.Source = logItems;
			filteredLogItems.Filter += filteredLogItems_Filter;

			// Setup debug message monitor events
			localDebugMonitor.MessageReceived += (pid, text) =>
			{
				var itemVM = new DebugMessageViewModel(pid, text, false);
				InsertLogItemThread(itemVM);
			};
			globalDebugMonitor.MessageReceived += (pid, text) =>
			{
				var itemVM = new DebugMessageViewModel(pid, text, true);
				InsertLogItemThread(itemVM);
			};
		}

		#endregion Constructors

		#region Public properties

		public string LoadedBasePath { get { return loadedBasePath; } }

		public SourceResolver SourceResolver { get { return sourceResolver; } }

		public Deobfuscator Deobfuscator { get { return deobfuscator; } }

		public bool AutoLoadLog
		{
			get
			{
				return autoLoadLog;
			}
			set
			{
				if (value != autoLoadLog)
				{
					autoLoadLog = value;
					if (autoLoadLog)
					{
						// Start local debug monitor temporarily (stop it again when done)
						tempActivatedLocalDebugMonitor = !IsLocalDebugMonitorActive;
						IsLocalDebugMonitorActive = true;
					}
					else
					{
						if (tempActivatedLocalDebugMonitor) IsLocalDebugMonitorActive = false;
					}
				}
			}
		}

		#endregion Public properties

		#region Command definition

		// Toolbar commands
		public DelegateCommand LoadLogCommand { get; private set; }
		public DelegateCommand StopLiveCommand { get; private set; }
		public DelegateCommand ClearCommand { get; private set; }
		public DelegateCommand LoadSymbolsCommand { get; private set; }
		public DelegateCommand LoadObfuscationMapCommand { get; private set; }
		public DelegateCommand DecreaseIndentSizeCommand { get; private set; }
		public DelegateCommand IncreaseIndentSizeCommand { get; private set; }
		public DelegateCommand DeleteFilterCommand { get; private set; }
		public DelegateCommand ClearSearchTextCommand { get; private set; }
		public DelegateCommand SettingsCommand { get; private set; }
		public DelegateCommand AboutCommand { get; private set; }

		// Log items list context menu commands
		public DelegateCommand QuickFilterSessionCommand { get; private set; }
		public DelegateCommand QuickFilterThreadCommand { get; private set; }
		public DelegateCommand QuickFilterTypeCommand { get; private set; }
		public DelegateCommand QuickFilterMinPrioCommand { get; private set; }
		public DelegateCommand QuickFilterNotBeforeCommand { get; private set; }
		public DelegateCommand QuickFilterNotAfterCommand { get; private set; }
		public DelegateCommand QuickFilterExcludeTextCommand { get; private set; }
		public DelegateCommand QuickFilterDataNameCommand { get; private set; }
		public DelegateCommand QuickFilterExceptionTypeCommand { get; private set; }
		public DelegateCommand QuickFilterWebRequestCommand { get; private set; }
		public DelegateCommand QuickFilterWebRequestUrlCommand { get; private set; }
		public DelegateCommand QuickFilterWebClientAddressCommand { get; private set; }
		public DelegateCommand QuickFilterWebUserAgentCommand { get; private set; }
		public DelegateCommand QuickFilterWebSessionCommand { get; private set; }
		public DelegateCommand QuickFilterWebUserCommand { get; private set; }

		protected override void InitializeCommands()
		{
			LoadLogCommand = new DelegateCommand(OnLoadLog, CanLoadLog);
			StopLiveCommand = new DelegateCommand(OnStopLive, CanStopLive);
			ClearCommand = new DelegateCommand(OnClear, CanClear);
			LoadSymbolsCommand = new DelegateCommand(OnLoadSymbols);
			LoadObfuscationMapCommand = new DelegateCommand(OnLoadObfuscationMap);
			DecreaseIndentSizeCommand = new DelegateCommand(OnDecreaseIndentSize, CanDecreaseIndentSize);
			IncreaseIndentSizeCommand = new DelegateCommand(OnIncreaseIndentSize, CanIncreaseIndentSize);
			DeleteFilterCommand = new DelegateCommand(OnDeleteFilter, CanDeleteFilter);
			ClearSearchTextCommand = new DelegateCommand(OnClearSearchText);
			SettingsCommand = new DelegateCommand(OnSettings);
			AboutCommand = new DelegateCommand(OnAbout);

			QuickFilterSessionCommand = new DelegateCommand(OnQuickFilterSession, CanQuickFilterSession);
			QuickFilterThreadCommand = new DelegateCommand(OnQuickFilterThread, CanQuickFilterThread);
			QuickFilterTypeCommand = new DelegateCommand(OnQuickFilterType, CanQuickFilterType);
			QuickFilterMinPrioCommand = new DelegateCommand(OnQuickFilterMinPrio, CanQuickFilterMinPrio);
			QuickFilterNotBeforeCommand = new DelegateCommand(OnQuickFilterNotBefore, CanQuickFilterNotBefore);
			QuickFilterNotAfterCommand = new DelegateCommand(OnQuickFilterNotAfter, CanQuickFilterNotAfter);
			QuickFilterExcludeTextCommand = new DelegateCommand(OnQuickFilterExcludeText, CanQuickFilterExcludeText);
			QuickFilterDataNameCommand = new DelegateCommand(OnQuickFilterDataName, CanQuickFilterDataName);
			QuickFilterExceptionTypeCommand = new DelegateCommand(OnQuickFilterExceptionType, CanQuickFilterExceptionType);
			QuickFilterWebRequestCommand = new DelegateCommand(OnQuickFilterWebRequest, CanQuickFilterWebRequest);
			QuickFilterWebRequestUrlCommand = new DelegateCommand(OnQuickFilterWebRequestUrl, CanQuickFilterWebRequestUrl);
			QuickFilterWebClientAddressCommand = new DelegateCommand(OnQuickFilterWebClientAddress, CanQuickFilterWebClientAddress);
			QuickFilterWebUserAgentCommand = new DelegateCommand(OnQuickFilterWebUserAgent, CanQuickFilterWebUserAgent);
			QuickFilterWebSessionCommand = new DelegateCommand(OnQuickFilterWebSession, CanQuickFilterWebSession);
			QuickFilterWebUserCommand = new DelegateCommand(OnQuickFilterWebUser, CanQuickFilterWebUser);
		}

		private void InvalidateToolbarCommandsLoading()
		{
			LoadLogCommand.RaiseCanExecuteChanged();
			StopLiveCommand.RaiseCanExecuteChanged();
			ClearCommand.RaiseCanExecuteChanged();
		}

		private void InvalidateQuickFilterCommands()
		{
			QuickFilterSessionCommand.RaiseCanExecuteChanged();
			QuickFilterThreadCommand.RaiseCanExecuteChanged();
			QuickFilterTypeCommand.RaiseCanExecuteChanged();
			QuickFilterMinPrioCommand.RaiseCanExecuteChanged();
			QuickFilterNotBeforeCommand.RaiseCanExecuteChanged();
			QuickFilterNotAfterCommand.RaiseCanExecuteChanged();
			QuickFilterWebRequestCommand.RaiseCanExecuteChanged();
		}

		#endregion Command definition

		#region Toolbar command handlers

		private bool CanLoadLog()
		{
			return !IsLoadingFiles && !IsLoadingFilesAgain;
		}

		private void OnLoadLog()
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "FieldLog files|*.fl|All files|*.*";
			dlg.Title = "Select a file from the log file group to load";
			if (dlg.ShowDialog() == true)
			{
				string prefix = GetPrefixFromPath(dlg.FileName);
				if (prefix != null)
				{
					if (CanStopLive())
					{
						OnStopLive();
					}
					OpenFiles(prefix);
				}
			}
		}

		private bool CanStopLive()
		{
			return !isLiveStopped;
		}

		private void OnStopLive()
		{
			if (logFileGroupReader != null)
			{
				logFileGroupReader.Close();
				IsLoadingFiles = false;
				IsLoadingFilesAgain = false;
				isLiveStopped = true;
				StopLiveCommand.RaiseCanExecuteChanged();
			}
		}

		private bool CanClear()
		{
			return !IsLoadingFiles;
		}

		private void OnClear()
		{
			logItems.Clear();
		}

		private void OnLoadSymbols()
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "Symbol files|*.pdbx;*.pdb.xml;*.pdb.xml.gz|All files|*.*";
			dlg.InitialDirectory = Path.GetDirectoryName(loadedBasePath);
			dlg.Multiselect = true;
			dlg.Title = "Select the symbol file(s) to load";
			if (dlg.ShowDialog() == true)
			{
				foreach (string fileName in dlg.FileNames)
				{
					AddSymbols(fileName);
				}

				logItems.ForEach(i => i.Refresh());
				RefreshLogItemsFilterView();
			}
		}

		public void AddSymbols(string fileName)
		{
			sourceResolver.AddFile(fileName);
		}

		private void OnLoadObfuscationMap()
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "Obfuscation map files|*.mapz;*.xml;*.xml.gz|All files|*.*";
			dlg.InitialDirectory = Path.GetDirectoryName(loadedBasePath);
			dlg.Multiselect = true;
			dlg.Title = "Select the obfuscation map file(s) to load";
			if (dlg.ShowDialog() == true)
			{
				foreach (string fileName in dlg.FileNames)
				{
					AddObfuscationMap(fileName);
				}

				logItems.ForEach(i => i.Refresh());
				RefreshLogItemsFilterView();
			}
		}

		public void AddObfuscationMap(string fileName)
		{
			deobfuscator.AddFile(fileName);
		}

		private bool CanDecreaseIndentSize()
		{
			return App.Settings.IndentSize > 4;
		}

		private void OnDecreaseIndentSize()
		{
			App.Settings.IndentSize -= 4;
			if (App.Settings.IndentSize < 4)
			{
				App.Settings.IndentSize = 4;
			}
		}

		private bool CanIncreaseIndentSize()
		{
			return App.Settings.IndentSize < 32;
		}

		private void OnIncreaseIndentSize()
		{
			App.Settings.IndentSize += 4;
			if (App.Settings.IndentSize > 32)
			{
				App.Settings.IndentSize = 32;
			}
		}

		private bool CanDeleteFilter()
		{
			return SelectedFilter != null && !SelectedFilter.AcceptAll;
		}

		private void OnDeleteFilter()
		{
			if (SelectedFilter != null && !SelectedFilter.AcceptAll)
			{
				if (!SelectedFilter.IsQuickFilter)
				{
					if (App.YesNoQuestion("Would you like to delete the selected filter “" + SelectedFilter.DisplayName + "”?"))
					{
						FilterViewModel filter = SelectedFilter;
						SelectedFilter = Filters[0];
						Filters.Remove(filter);
					}
				}
				else
				{
					FilterViewModel filter = SelectedFilter;
					if (filter.QuickPreviousFilter != null)
					{
						SelectedFilter = filter.QuickPreviousFilter;
					}
					else
					{
						SelectedFilter = Filters[0];
					}
					Filters.Remove(filter);
				}
			}
		}

		private void OnClearSearchText()
		{
			// Defer until after Render to make it look faster
			Dispatcher.CurrentDispatcher.BeginInvoke(
				new Action(() => AdhocSearchText = ""),
				DispatcherPriority.Background);
		}

		private void OnSettings()
		{
			if (openSettingsWindow == null || !openSettingsWindow.IsVisible)
			{
				SettingsWindow win = new SettingsWindow();
				SettingsViewModel vm = new SettingsViewModel();
				win.DataContext = vm;
				win.Owner = MainWindow.Instance;
				win.Show();
				openSettingsWindow = win;
			}
			else
			{
				openSettingsWindow.Focus();
			}
		}

		private void OnAbout()
		{
			var win = new AboutWindow();
			win.Owner = MainWindow.Instance;
			win.ShowDialog();
		}

		#endregion Toolbar command handlers

		#region Log items list context menu command handlers

		private static string quickFilterSuffix = "*";

		private FilterViewModel GetQuickFilter(out bool isNew, bool leaveEmpty = false)
		{
			isNew = false;
			var filter = SelectedFilter;
			if (!filter.IsQuickFilter)
			{
				isNew = true;
				filter = filter.GetDuplicate();
				filter.QuickModifiedTime = DateTime.UtcNow;
			}
			else if (filter.QuickModifiedTime.AddSeconds(10) < DateTime.UtcNow)
			{
				isNew = true;
				filter = filter.GetDuplicate();
				filter.QuickModifiedTime = DateTime.UtcNow;
			}
			if (filter.ConditionGroups.Count(c => !c.IsExclude) == 0 && !leaveEmpty)
			{
				filter.ConditionGroups.Add(new FilterConditionGroupViewModel(filter));
			}
			return filter;
		}

		/// <summary>
		/// Makes the name of the filter unique by adding a counter.
		/// </summary>
		/// <param name="filter">The new filter that may have a generated name that already exists.</param>
		/// <remarks>
		/// While a filter does not technically require a unique name, it can be confusing to the
		/// user to see two or more filters with the same name. Also, when deleting a quick filter,
		/// the previous filter is automatically selected, and it can be irritating when that name
		/// does not change. So after a filter is created and added to the Filters list, its name
		/// can be made unique by calling this method.
		/// </remarks>
		private void MakeFilterNameUnique(FilterViewModel filter)
		{
			string baseFilterName = filter.DisplayName;
			int counter = 1;
			while (Filters.Count(f => f.DisplayName == filter.DisplayName) > 1)
			{
				counter++;
				filter.DisplayName = baseFilterName + " (" + counter + ")";
			}
		}

		/// <summary>
		/// Sets up a quick filter for a single filter column and one or multiple values.
		/// </summary>
		/// <typeparam name="T">The type of the values to use in the filter.</typeparam>
		/// <param name="values">The values to use in the filter.</param>
		/// <param name="singlePrefix">The filter name prefix for a single value.</param>
		/// <param name="multiPrefix">The filter name prefix for multiple values.</param>
		/// <param name="filterColumn">The filter column to set.</param>
		/// <param name="useRegex">true to use the Regex comparison for multiple values (required for strings), false to use the InList comparison (for numbers).</param>
		private void MultiValueQuickFilter<T>(IEnumerable<T> values, string singlePrefix, string multiPrefix, FilterColumn filterColumn, bool useRegex)
		{
			bool isNew;
			var filter = GetQuickFilter(out isNew);
			int typeCount = values.Count();
			switch (typeCount)
			{
				case 1:
					filter.DisplayName = singlePrefix + values.First() + quickFilterSuffix;
					break;
				default:
					filter.DisplayName = multiPrefix + values.Aggregate(", ", " and ") + quickFilterSuffix;
					break;
			}
			foreach (var cg in filter.ConditionGroups.ToList())   // Filtering conditions may remove the condition group, so enumerate a copy of the list
			{
				// Remove all existing conditions
				cg.Conditions.Filter(c => c.Column != filterColumn);
			}
			if (filter.ConditionGroups.Count == 0)
			{
				filter.ConditionGroups.Add(new FilterConditionGroupViewModel(filter));
			}
			foreach (var cg in filter.ConditionGroups)
			{
				if (!cg.IsExclude)
				{
					if (typeCount == 1)
					{
						cg.Conditions.Add(new FilterConditionViewModel(cg)
						{
							Column = filterColumn,
							Comparison = FilterComparison.Equals,
							Value = values.First().ToString()
						});
					}
					else if (useRegex)
					{
						cg.Conditions.Add(new FilterConditionViewModel(cg)
						{
							Column = filterColumn,
							Comparison = FilterComparison.Regex,
							Value = "^(" + values.Select(v => Regex.Escape(v.ToString())).Aggregate("|") + ")$"
						});
					}
					else
					{
						cg.Conditions.Add(new FilterConditionViewModel(cg)
						{
							Column = filterColumn,
							Comparison = FilterComparison.InList,
							Value = values.Aggregate(";")
						});
					}
				}
			}
			filter.ReorderCommand.TryExecute();
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				MakeFilterNameUnique(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterSession()
		{
			return SelectedItemsSessionIds.Any();
		}

		private void OnQuickFilterSession()
		{
			bool isNew;
			var filter = GetQuickFilter(out isNew);
			int sessionCount = SelectedItemsSessionIds.Count();
			switch (sessionCount)
			{
				case 1:
					filter.DisplayName = "By session" + quickFilterSuffix;
					break;
				default:
					filter.DisplayName = "By " + sessionCount + " sessions" + quickFilterSuffix;
					break;
			}
			foreach (var cg in filter.ConditionGroups.ToList())   // Filtering conditions may remove the condition group, so enumerate a copy of the list
			{
				// Remove all session and thread ID conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.SessionId);
			}
			if (filter.ConditionGroups.Count == 0)
			{
				filter.ConditionGroups.Add(new FilterConditionGroupViewModel(filter));
			}
			foreach (var cg in filter.ConditionGroups)
			{
				if (!cg.IsExclude)
				{
					if (sessionCount == 1)
					{
						cg.Conditions.Add(new FilterConditionViewModel(cg)
						{
							Column = FilterColumn.SessionId,
							Comparison = FilterComparison.Equals,
							Value = SelectedItemsSessionIds.First().ToString("D")
						});
					}
					else
					{
						cg.Conditions.Add(new FilterConditionViewModel(cg)
						{
							Column = FilterColumn.SessionId,
							Comparison = FilterComparison.InList,
							Value = SelectedItemsSessionIds.Select(s => s.ToString("D")).Aggregate(";")
						});
					}
				}
			}
			filter.ReorderCommand.TryExecute();
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				MakeFilterNameUnique(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterThread()
		{
			return SelectedItemsThreadIds.Any();
		}

		private void OnQuickFilterThread()
		{
			bool isNew;
			var filter = GetQuickFilter(out isNew);
			int threadCount = SelectedItemsThreadIds.Count();
			switch (threadCount)
			{
				case 1:
					filter.DisplayName = "Thread ID " + SelectedItemsThreadIds.First() + quickFilterSuffix;
					break;
				default:
					filter.DisplayName = "Thread IDs " + SelectedItemsThreadIds.Aggregate(", ", " and ") + quickFilterSuffix;
					break;
			}
			foreach (var cg in filter.ConditionGroups.ToList())   // Filtering conditions may remove the condition group, so enumerate a copy of the list
			{
				// Remove all session and thread ID conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.SessionId);
				cg.Conditions.Filter(c => c.Column != FilterColumn.ThreadId);
			}
			if (filter.ConditionGroups.Count == 0)
			{
				filter.ConditionGroups.Add(new FilterConditionGroupViewModel(filter));
			}
			foreach (var cg in filter.ConditionGroups)
			{
				if (!cg.IsExclude)
				{
					cg.Conditions.Add(new FilterConditionViewModel(cg)
					{
						Column = FilterColumn.SessionId,
						Comparison = FilterComparison.Equals,
						Value = SelectedItemsSessionIds.First().ToString("D")
					});
					if (threadCount == 1)
					{
						cg.Conditions.Add(new FilterConditionViewModel(cg)
						{
							Column = FilterColumn.ThreadId,
							Comparison = FilterComparison.Equals,
							Value = SelectedItemsThreadIds.First().ToString()
						});
					}
					else
					{
						cg.Conditions.Add(new FilterConditionViewModel(cg)
						{
							Column = FilterColumn.ThreadId,
							Comparison = FilterComparison.InList,
							Value = SelectedItemsThreadIds.Aggregate(";")
						});
					}
				}
			}
			filter.ReorderCommand.TryExecute();
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				MakeFilterNameUnique(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterType()
		{
			return SelectedItems != null && SelectedItems.Count == 1;
		}

		private void OnQuickFilterType()
		{
			bool isNew;
			var filter = GetQuickFilter(out isNew);
			filter.DisplayName = "Type " + EnumerationExtension<FilterItemType>.GetDescription(SelectedItemFilterItemType) + quickFilterSuffix;
			foreach (var cg in filter.ConditionGroups.ToList())   // Filtering conditions may remove the condition group, so enumerate a copy of the list
			{
				// Remove all type conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.Type);
			}
			if (filter.ConditionGroups.Count == 0)
			{
				filter.ConditionGroups.Add(new FilterConditionGroupViewModel(filter));
			}
			foreach (var cg in filter.ConditionGroups)
			{
				if (!cg.IsExclude)
				{
					cg.Conditions.Add(new FilterConditionViewModel(cg)
					{
						Column = FilterColumn.Type,
						Comparison = FilterComparison.Equals,
						Value = SelectedItemFilterItemType.ToString()
					});
				}
			}
			filter.ReorderCommand.TryExecute();
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				MakeFilterNameUnique(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterMinPrio()
		{
			return SelectedItems != null && SelectedItems.Count == 1 && SelectedItems[0] is FieldLogItemViewModel;
		}

		private void OnQuickFilterMinPrio()
		{
			FieldLogItemViewModel flItem = SelectedItems[0] as FieldLogItemViewModel;
			bool isNew;
			var filter = GetQuickFilter(out isNew);
			filter.DisplayName = "Priority " + flItem.PrioTitle + " or higher" + quickFilterSuffix;
			foreach (var cg in filter.ConditionGroups.ToList())   // Filtering conditions may remove the condition group, so enumerate a copy of the list
			{
				// Remove all priority conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.Priority);
			}
			if (filter.ConditionGroups.Count == 0)
			{
				filter.ConditionGroups.Add(new FilterConditionGroupViewModel(filter));
			}
			foreach (var cg in filter.ConditionGroups)
			{
				if (!cg.IsExclude)
				{
					cg.Conditions.Add(new FilterConditionViewModel(cg)
					{
						Column = FilterColumn.Priority,
						Comparison = FilterComparison.GreaterOrEqual,
						Value = flItem.Priority.ToString()
					});
				}
			}
			filter.ReorderCommand.TryExecute();
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				MakeFilterNameUnique(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterNotBefore()
		{
			return SelectedItems != null && SelectedItems.Count == 1;
		}

		private void OnQuickFilterNotBefore()
		{
			bool isNew;
			var filter = GetQuickFilter(out isNew);
			filter.DisplayName = "Not before..." + quickFilterSuffix;
			foreach (var cg in filter.ConditionGroups.ToList())   // Filtering conditions may remove the condition group, so enumerate a copy of the list
			{
				// Remove all time conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.Time);
			}
			if (filter.ConditionGroups.Count == 0)
			{
				filter.ConditionGroups.Add(new FilterConditionGroupViewModel(filter));
			}
			foreach (var cg in filter.ConditionGroups)
			{
				if (!cg.IsExclude)
				{
					DateTime itemTime = new DateTime(SelectedItems[0].Time.Ticks / 10 * 10);   // Round down to the next microsecond
					switch (App.Settings.ItemTimeMode)
					{
						case ItemTimeType.Local:
							itemTime = itemTime.ToLocalTime();
							break;
						case ItemTimeType.Remote:
							itemTime = itemTime.AddMinutes(SelectedItems[0].UtcOffset);
							break;
					}
					cg.Conditions.Add(new FilterConditionViewModel(cg)
					{
						Column = FilterColumn.Time,
						Comparison = FilterComparison.GreaterOrEqual,
						Value = itemTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff")
					});
				}
			}
			filter.ReorderCommand.TryExecute();
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				MakeFilterNameUnique(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterNotAfter()
		{
			return SelectedItems != null && SelectedItems.Count == 1;
		}

		private void OnQuickFilterNotAfter()
		{
			bool isNew;
			var filter = GetQuickFilter(out isNew);
			filter.DisplayName = "Not after..." + quickFilterSuffix;
			foreach (var cg in filter.ConditionGroups.ToList())   // Filtering conditions may remove the condition group, so enumerate a copy of the list
			{
				// Remove all time conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.Time);
			}
			if (filter.ConditionGroups.Count == 0)
			{
				filter.ConditionGroups.Add(new FilterConditionGroupViewModel(filter));
			}
			foreach (var cg in filter.ConditionGroups)
			{
				if (!cg.IsExclude)
				{
					DateTime itemTime = new DateTime((SelectedItems[0].Time.Ticks + 9) / 10 * 10);   // Round up to the next microsecond
					switch (App.Settings.ItemTimeMode)
					{
						case ItemTimeType.Local:
							itemTime = itemTime.ToLocalTime();
							break;
						case ItemTimeType.Remote:
							itemTime = itemTime.AddMinutes(SelectedItems[0].UtcOffset);
							break;
					}
					cg.Conditions.Add(new FilterConditionViewModel(cg)
					{
						Column = FilterColumn.Time,
						Comparison = FilterComparison.LessOrEqual,
						Value = itemTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff")
					});
				}
			}
			filter.ReorderCommand.TryExecute();
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				MakeFilterNameUnique(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterExcludeText()
		{
			return SelectedItems != null &&
				SelectedItems.Count == 1 &&
				(SelectedItems[0] is FieldLogTextItemViewModel || SelectedItems[0] is FieldLogExceptionItemViewModel);
		}

		private void OnQuickFilterExcludeText()
		{
			bool isNew;
			var filter = GetQuickFilter(out isNew, true);
			filter.DisplayName = "Exclude text" + quickFilterSuffix;
			var textItem = SelectedItems[0] as FieldLogTextItemViewModel;
			var exItem = SelectedItems[0] as FieldLogExceptionItemViewModel;
			string text = null;
			if (textItem != null)
				text = textItem.Text;
			else if (exItem != null)
				text = exItem.ExceptionVM.Message;
			if (text == null)
				return;   // Should not happen

			var cg = new FilterConditionGroupViewModel(filter);
			cg.IsExclude = true;
			cg.Conditions.Add(new FilterConditionViewModel(cg)
			{
				Column = FilterColumn.TextText,
				Comparison = FilterComparison.Equals,
				Value = text
			});
			filter.ConditionGroups.Add(cg);
			cg = new FilterConditionGroupViewModel(filter);
			cg.IsExclude = true;
			cg.Conditions.Add(new FilterConditionViewModel(cg)
			{
				Column = FilterColumn.ExceptionMessage,
				Comparison = FilterComparison.Equals,
				Value = text
			});
			filter.ConditionGroups.Add(cg);
			filter.ReorderCommand.TryExecute();
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				MakeFilterNameUnique(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterDataName()
		{
			return SelectedItemsDataNames.Any();
		}

		private void OnQuickFilterDataName()
		{
			MultiValueQuickFilter(SelectedItemsDataNames, "Data name ", "Data names ", FilterColumn.DataName, true);
		}

		private bool CanQuickFilterExceptionType()
		{
			return SelectedItemsExceptionTypes.Any();
		}

		private void OnQuickFilterExceptionType()
		{
			MultiValueQuickFilter(SelectedItemsExceptionTypes, "Exception type ", "Exception types ", FilterColumn.ExceptionType, true);
		}

		private bool CanQuickFilterWebRequest()
		{
			return SelectedItemsWebRequestIds.Any();
		}

		private void OnQuickFilterWebRequest()
		{
			bool isNew;
			var filter = GetQuickFilter(out isNew);
			int webRequestCount = SelectedItemsWebRequestIds.Count();
			switch (webRequestCount)
			{
				case 1:
					filter.DisplayName = "Web request " + SelectedItemsWebRequestIds.First() + quickFilterSuffix;
					break;
				default:
					filter.DisplayName = "Web requests " + SelectedItemsWebRequestIds.Aggregate(", ", " and ") + quickFilterSuffix;
					break;
			}
			foreach (var cg in filter.ConditionGroups.ToList())   // Filtering conditions may remove the condition group, so enumerate a copy of the list
			{
				// Remove all session and web request ID conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.SessionId);
				cg.Conditions.Filter(c => c.Column != FilterColumn.WebRequestId);
			}
			if (filter.ConditionGroups.Count == 0)
			{
				filter.ConditionGroups.Add(new FilterConditionGroupViewModel(filter));
			}
			foreach (var cg in filter.ConditionGroups)
			{
				if (!cg.IsExclude)
				{
					cg.Conditions.Add(new FilterConditionViewModel(cg)
					{
						Column = FilterColumn.SessionId,
						Comparison = FilterComparison.Equals,
						Value = SelectedItemsSessionIds.First().ToString("D")
					});
					if (webRequestCount == 1)
					{
						cg.Conditions.Add(new FilterConditionViewModel(cg)
						{
							Column = FilterColumn.WebRequestId,
							Comparison = FilterComparison.Equals,
							Value = SelectedItemsWebRequestIds.First().ToString()
						});
					}
					else
					{
						cg.Conditions.Add(new FilterConditionViewModel(cg)
						{
							Column = FilterColumn.WebRequestId,
							Comparison = FilterComparison.InList,
							Value = SelectedItemsWebRequestIds.Aggregate(";")
						});
					}
				}
			}
			filter.ReorderCommand.TryExecute();
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				MakeFilterNameUnique(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterWebRequestUrl()
		{
			return SelectedItemsWebRequestUrls.Any();
		}

		private void OnQuickFilterWebRequestUrl()
		{
			MultiValueQuickFilter(SelectedItemsWebRequestUrls, "Request URL ", "Request URLs ", FilterColumn.WebRequestRequestUrl, true);
		}

		private bool CanQuickFilterWebClientAddress()
		{
			return SelectedItemsWebClientAddresses.Any();
		}

		private void OnQuickFilterWebClientAddress()
		{
			MultiValueQuickFilter(SelectedItemsWebClientAddresses, "Client ", "Clients ", FilterColumn.WebRequestClientAddress, true);
		}

		private bool CanQuickFilterWebUserAgent()
		{
			return SelectedItemsWebUserAgents.Any();
		}

		private void OnQuickFilterWebUserAgent()
		{
			MultiValueQuickFilter(SelectedItemsWebUserAgents, "User agent ", "User agents ", FilterColumn.WebRequestUserAgent, true);
		}

		private bool CanQuickFilterWebSession()
		{
			return SelectedItemsWebSessionIds.Any();
		}

		private void OnQuickFilterWebSession()
		{
			MultiValueQuickFilter(SelectedItemsWebSessionIds, "Web session ", "Web sessions ", FilterColumn.WebRequestWebSessionId, true);
		}

		private bool CanQuickFilterWebUser()
		{
			return SelectedItemsWebUserIds.Any();
		}

		private void OnQuickFilterWebUser()
		{
			MultiValueQuickFilter(SelectedItemsWebUserIds, "Web user ", "Web users ", FilterColumn.WebRequestAppUserId, true);
		}

		#endregion Log items list context menu command handlers

		#region Data properties

		public IAppSettings Settings
		{
			get { return App.Settings; }
		}

		#region Toolbar and settings

		public IEnumerable<string> RecentlyLoadedFiles
		{
			get
			{
				// Escape underscores in file names so they won't become accelerator underlines and
				// be removed. This is undone in the click event handler.
				return App.Settings.RecentlyLoadedFiles
					.Select(path => path.Replace("_", "__"));
			}
		}

		public bool IsLocalDebugMonitorActive
		{
			get
			{
				return localDebugMonitor.IsActive;
			}
			set
			{
				try
				{
					if (value)
					{
						localDebugMonitor.TryStart();
					}
					else
					{
						localDebugMonitor.Stop();
					}
				}
				catch (Exception ex)
				{
					App.WarningMessage("The local debug message monitor could not be started.", ex, "Starting local DebugMonitor");
				}
				OnPropertyChanged("IsLocalDebugMonitorActive");
			}
		}

		public bool IsGlobalDebugMonitorActive
		{
			get
			{
				return globalDebugMonitor.IsActive;
			}
			set
			{
				try
				{
					if (value && OSInfo.IsCurrentUserLocalAdministrator())
					{
						globalDebugMonitor.TryStart();
					}
					else
					{
						globalDebugMonitor.Stop();
					}
				}
				catch (Exception ex)
				{
					App.WarningMessage("The global debug message monitor could not be started.", ex, "Starting global DebugMonitor");
				}
				OnPropertyChanged("IsGlobalDebugMonitorActive");
			}
		}

		public bool IsGlobalDebugMonitorAvailable
		{
			get
			{
				return OSInfo.IsCurrentUserLocalAdministrator();
			}
		}

		#endregion Toolbar and settings

		#region Log items list

		public int SelectionDummy
		{
			get { return 0; }
		}

		public ObservableCollection<LogItemViewModelBase> LogItems
		{
			get { return this.logItems; }
		}

		[NotifiesOn("IsLoadingFiles")]
		[NotifiesOn("IsLoadingFilesAgain")]
		public ICollectionView FilteredLogItemsView
		{
			get { return filteredLogItems.View; }
		}

		public List<LogItemViewModelBase> SelectedItems
		{
			get
			{
				return GetValue<List<LogItemViewModelBase>>("SelectedItems");
			}
			set
			{
				if (SetValue(value, "SelectedItems"))
				{
					InvalidateQuickFilterCommands();
				}
			}
		}

		public bool IsLoadingFiles
		{
			get
			{
				return GetValue<bool>("IsLoadingFiles");
			}
			set
			{
				if (SetValue(value, "IsLoadingFiles"))
				{
					InvalidateToolbarCommandsLoading();
				}
			}
		}

		[PropertyChangedHandler("IsLoadingFiles")]
		private void OnIsLoadingFilesChanged()
		{
			FL.TraceData("IsLoadingFiles", IsLoadingFiles);
			if (IsLoadingFiles)
			{
				filteredLogItems.Source = null;
			}
			else
			{
				filteredLogItems.Source = logItems;
				RefreshLogItemsFilterView();
			}
		}

		public bool IsLoadingFilesAgain
		{
			get
			{
				return GetValue<bool>("IsLoadingFilesAgain");
			}
			set
			{
				if (SetValue(value, "IsLoadingFilesAgain"))
				{
					InvalidateToolbarCommandsLoading();
				}
			}
		}

		[PropertyChangedHandler("IsLoadingFilesAgain")]
		private void OnIsLoadingFilesAgainChanged()
		{
			FL.TraceData("IsLoadingFilesAgain", IsLoadingFilesAgain);
			if (!IsLoadingFilesAgain)
			{
				filteredLogItems.Source = logItems;
				RefreshLogItemsFilterView();
			}
		}

		public int LoadedItemsCount
		{
			get { return GetValue<int>("LoadedItemsCount"); }
			set { SetValue(value, "LoadedItemsCount"); }
		}

		[NotifiesOn("IsLoadingFiles")]
		public Visibility LogItemsVisibility
		{
			get
			{
				return !IsLoadingFiles ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		[NotifiesOn("IsLoadingFiles")]
		public Visibility ItemDetailsVisibility
		{
			get
			{
				return !IsLoadingFiles ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		[NotifiesOn("IsLoadingFiles")]
		public Visibility LoadingMsgVisibility
		{
			get
			{
				return IsLoadingFiles ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public bool IsFaultyFilter
		{
			get { return GetValue<bool>("IsFaultyFilter"); }
			set { SetValue(value, "IsFaultyFilter"); }
		}

		public string FilterErrorMessage
		{
			get { return GetValue<string>("FilterErrorMessage"); }
			set { SetValue(value, "FilterErrorMessage"); }
		}

		#endregion Log items list

		#region Filter

		public ObservableCollection<FilterViewModel> Filters { get; private set; }

		public ICollectionView SortedFilters
		{
			get { return sortedFilters.View; }
		}

		public FilterViewModel SelectedFilter
		{
			get
			{
				return GetValue<FilterViewModel>("SelectedFilter");
			}
			set
			{
				if (SetValue(value, "SelectedFilter"))
				{
					DeleteFilterCommand.RaiseCanExecuteChanged();
					ViewCommandManager.Invoke("SaveScrolling");
					RefreshLogItemsFilterView();
					ViewCommandManager.Invoke("RestoreScrolling");
					if (SelectedFilter != null)
					{
						App.Settings.SelectedFilter = SelectedFilter.DisplayName;
					}
					else
					{
						App.Settings.SelectedFilter = "";
					}
				}
			}
		}

		public string AdhocSearchText
		{
			get
			{
				return GetValue<string>("AdhocSearchText");
			}
			set
			{
				if (SetValue(value, "AdhocSearchText"))
				{
					if (!string.IsNullOrWhiteSpace(AdhocSearchText))
					{
						adhocFilterCondition = new FilterConditionViewModel(null);
						adhocFilterCondition.Value = AdhocSearchText;
					}
					else
					{
						adhocFilterCondition = null;
					}

					ViewCommandManager.Invoke("SaveScrolling");
					RefreshLogItemsFilterView();
					ViewCommandManager.Invoke("RestoreScrolling");
				}
			}
		}

		#endregion Filter

		#region Quick filter

		private IEnumerable<Guid> SelectedItemsSessionIds
		{
			get
			{
				if (SelectedItems != null)
				{
					return SelectedItems
						.OfType<FieldLogItemViewModel>()
						.Select(vm => vm.SessionId)
						.Distinct()
						.OrderBy(sid => sid);
				}
				return new Guid[0];
			}
		}

		private IEnumerable<int> SelectedItemsThreadIds
		{
			get
			{
				if (SelectedItems != null)
				{
					return SelectedItems
						.OfType<FieldLogItemViewModel>()
						.Select(vm => vm.ThreadId)
						.Distinct()
						.OrderBy(tid => tid);
				}
				return new int[0];
			}
		}

		[NotifiesOn("SelectedItems")]
		public string QuickFilterThreadTitle
		{
			get
			{
				if (SelectedItemsThreadIds.Any())
				{
					string threadIdsStr = SelectedItemsThreadIds
						.Select(tid => tid.ToString())
						.Aggregate(", ", " and ");
					switch (SelectedItemsThreadIds.Count())
					{
						case 1:
							return "Filter by thread ID " + threadIdsStr + " (same session)";
						default:
							return "Filter by thread IDs " + threadIdsStr + " (same session)";
					}
				}
				return "Filter by thread";
			}
		}

		private FilterItemType SelectedItemFilterItemType
		{
			get
			{
				if (SelectedItems != null && SelectedItems.Count == 1)
				{
					if (SelectedItems[0] is FieldLogDataItemViewModel) return FilterItemType.Data;
					if (SelectedItems[0] is FieldLogExceptionItemViewModel) return FilterItemType.Exception;
					if (SelectedItems[0] is FieldLogScopeItemViewModel) return FilterItemType.Scope;
					if (SelectedItems[0] is FieldLogTextItemViewModel) return FilterItemType.Text;
					if (SelectedItems[0] is DebugMessageViewModel) return FilterItemType.DebugOutput;
				}
				return FilterItemType.Any;
			}
		}

		[NotifiesOn("SelectedItems")]
		public string QuickFilterTypeTitle
		{
			get
			{
				if (SelectedItems != null && SelectedItems.Count == 1)
				{
					return "Filter by type " + EnumerationExtension<FilterItemType>.GetDescription(SelectedItemFilterItemType);
				}
				return "Filter by type";
			}
		}

		[NotifiesOn("SelectedItems")]
		public string QuickFilterMinPrioTitle
		{
			get
			{
				if (SelectedItems != null && SelectedItems.Count == 1)
				{
					FieldLogItemViewModel flItem = SelectedItems[0] as FieldLogItemViewModel;
					if (flItem != null)
					{
						return "Filter by priority " + flItem.PrioTitle + " or higher";
					}
				}
				return "Filter by priority";
			}
		}

		private IEnumerable<string> SelectedItemsDataNames
		{
			get
			{
				if (SelectedItems != null)
				{
					return SelectedItems
						.OfType<FieldLogDataItemViewModel>()
						.Select(vm => vm.Name)
						.Distinct()
						.OrderBy(name => name);
				}
				return new string[0];
			}
		}

		private IEnumerable<string> SelectedItemsExceptionTypes
		{
			get
			{
				if (SelectedItems != null)
				{
					return SelectedItems
						.OfType<FieldLogExceptionItemViewModel>()
						.Select(vm => vm.ExceptionVM.Type)
						.Distinct()
						.OrderBy(type => type);
				}
				return new string[0];
			}
		}

		private IEnumerable<uint> SelectedItemsWebRequestIds
		{
			get
			{
				if (SelectedItems != null)
				{
					return SelectedItems
						.OfType<FieldLogItemViewModel>()
						.Select(vm => vm.WebRequestId)
						.Distinct()
						.OrderBy(tid => tid);
				}
				return new uint[0];
			}
		}

		private IEnumerable<string> SelectedItemsWebRequestUrls
		{
			get
			{
				if (SelectedItems != null)
				{
					return SelectedItems
						.OfType<FieldLogItemViewModel>()
						.Where(i => i.LastWebRequestStartItem != null)
						.Select(vm => vm.LastWebRequestStartItem.WebRequestDataVM.WebRequestData.RequestUrl)
						.Distinct()
						.OrderBy(url => url);
				}
				return new string[0];
			}
		}

		private IEnumerable<string> SelectedItemsWebClientAddresses
		{
			get
			{
				if (SelectedItems != null)
				{
					return SelectedItems
						.OfType<FieldLogItemViewModel>()
						.Where(i => i.LastWebRequestStartItem != null)
						.Select(vm => vm.LastWebRequestStartItem.WebRequestDataVM.WebRequestData.ClientAddress)
						.Distinct()
						.OrderBy(address => address);
				}
				return new string[0];
			}
		}

		private IEnumerable<string> SelectedItemsWebUserAgents
		{
			get
			{
				if (SelectedItems != null)
				{
					return SelectedItems
						.OfType<FieldLogItemViewModel>()
						.Where(i => i.LastWebRequestStartItem != null)
						.Select(vm => vm.LastWebRequestStartItem.WebRequestDataVM.WebRequestData.UserAgent)
						.Distinct()
						.OrderBy(ua => ua);
				}
				return new string[0];
			}
		}

		private IEnumerable<string> SelectedItemsWebSessionIds
		{
			get
			{
				if (SelectedItems != null)
				{
					return SelectedItems
						.OfType<FieldLogItemViewModel>()
						.Where(i => i.LastWebRequestStartItem != null)
						.Select(vm => vm.LastWebRequestStartItem.WebRequestDataVM.WebRequestData.WebSessionId)
						.Distinct()
						.OrderBy(sid => sid);
				}
				return new string[0];
			}
		}

		private IEnumerable<string> SelectedItemsWebUserIds
		{
			get
			{
				if (SelectedItems != null)
				{
					return SelectedItems
						.OfType<FieldLogItemViewModel>()
						.Where(i => i.LastWebRequestStartItem != null)
						.Select(vm => vm.LastWebRequestStartItem.WebRequestDataVM.WebRequestData.AppUserId)
						.Distinct()
						.OrderBy(uid => uid);
				}
				return new string[0];
			}
		}

		public Visibility QuickFilterDataVisibility
		{
			get
			{
				return SelectedItems != null && SelectedItems.OfType<FieldLogDataItemViewModel>().Any() ?
					Visibility.Visible :
					Visibility.Collapsed;
			}
		}

		public Visibility QuickFilterExcludeTextVisibility
		{
			get
			{
				return CanQuickFilterExcludeText() ?
					Visibility.Visible :
					Visibility.Collapsed;
			}
		}

		public Visibility QuickFilterExceptionVisibility
		{
			get
			{
				return SelectedItems != null && SelectedItems.OfType<FieldLogExceptionItemViewModel>().Any() ?
					Visibility.Visible :
					Visibility.Collapsed;
			}
		}

		public Visibility QuickFilterWebVisibility
		{
			get
			{
				return SelectedItems != null && SelectedItems.OfType<FieldLogItemViewModel>().Any(i => i.WebRequestId != 0) ?
					Visibility.Visible :
					Visibility.Collapsed;
			}
		}

		public Visibility QuickFilterWebSessionVisibility
		{
			get
			{
				return SelectedItems != null &&
					SelectedItems
						.OfType<FieldLogItemViewModel>()
						.Any(i => i.LastWebRequestStartItem != null &&
							!string.IsNullOrEmpty(i.LastWebRequestStartItem.WebRequestDataVM.WebRequestData.WebSessionId)) ?
					Visibility.Visible :
					Visibility.Collapsed;
			}
		}

		public Visibility QuickFilterWebUserVisibility
		{
			get
			{
				return SelectedItems != null &&
					SelectedItems
						.OfType<FieldLogItemViewModel>()
						.Any(i => i.LastWebRequestStartItem != null &&
							!string.IsNullOrEmpty(i.LastWebRequestStartItem.WebRequestDataVM.WebRequestData.AppUserId)) ?
					Visibility.Visible :
					Visibility.Collapsed;
			}
		}

		#endregion Quick filter

		#endregion Data properties

		#region Log items filter

		/// <summary>
		/// Filter implementation for the collection view returned by FilteredLogItemsView.
		/// </summary>
		private void filteredLogItems_Filter(object sender, FilterEventArgs args)
		{
			if (SelectedFilter != null)
			{
				if (!SelectedFilter.IsFaulty)
				{
					try
					{
						args.Accepted =
							SelectedFilter.IsMatch(args.Item) &&
							(adhocFilterCondition == null || adhocFilterCondition.IsMatch(args.Item));
						FilterErrorMessage = null;
						IsFaultyFilter = false;
					}
					catch (Exception ex)
					{
						SelectedFilter.IsFaulty = true;
						args.Accepted = false;
						FL.Warning(ex, "Evaluating filter");
						FilterErrorMessage = ex.Message;
						IsFaultyFilter = true;
					}
				}
				else
				{
					args.Accepted = false;
				}
			}
			else
			{
				args.Accepted = true;
				FilterErrorMessage = null;
				IsFaultyFilter = false;
			}
		}

		/// <summary>
		/// Called when a filter or the current filter selection has changed.
		/// </summary>
		/// <param name="affectsItems">true if the change affects the item filtering.</param>
		/// <remarks>
		/// The <paramref name="affectsItems"/> parameter is true when the definition of the
		/// current filter has changed, or another filter was selected, so that other items may be
		/// selected for display. The <paramref name="affectsItems"/> parameter is false mostly
		/// when a filter has been renamed.
		/// </remarks>
		public void LogItemsFilterChanged(bool affectsItems)
		{
			if (sortedFilters.View != null)
			{
				sortedFilters.View.Refresh();
			}
			if (affectsItems)
			{
				RefreshLogItemsFilterView();
			}
			App.Settings.Filters = Filters
				.Where(f => !f.AcceptAll)
				.Select(f => f.SaveToString())
				.Where(s => !string.IsNullOrEmpty(s))
				.ToArray();
		}

		/// <summary>
		/// Refreshes the CollectionView of all filtered items.
		/// </summary>
		/// <remarks>
		/// This causes a Reset type change notification on the list and clears the list item
		/// focus. It should only be called when the list of filtered items has substantially
		/// changed or we don't know how much of the list has changed at all.
		/// </remarks>
		public void RefreshLogItemsFilterView()
		{
			if (filteredLogItems.View != null)
			{
				filteredLogItems.View.Refresh();
			}
			if (SelectedItems != null)
			{
				foreach (var selectedItem in SelectedItems)
				{
					selectedItem.RaiseDisplayTimeChanged();
				}
			}
		}

		/// <summary>
		/// Refreshes a single item in the CollectionView of all filtered items.
		/// </summary>
		/// <param name="item">The changed item.</param>
		/// <remarks>
		/// This makes use of the IEditableObject implementation of the log item. When an item is
		/// edited through this mechanism, and the update is committed, the CollectionView will
		/// re-evaluate the item and apply the filtering accordingly. This method must be called
		/// for each item that may have been updated. Changes that are signalled through
		/// INotifyPropertyChanged are not considered by a CollectionView. Updating each single
		/// log item avoids the Reset type change notification and the focused item issue.
		/// </remarks>
		private void RefreshFilteredLogItem(LogItemViewModelBase item)
		{
			var ev = FilteredLogItemsView as IEditableCollectionView;
			if (ev != null)
			{
				ev.EditItem(item);
				ev.CommitEdit();
			}
			if (SelectedItems != null && SelectedItems.Contains(item))
			{
				item.RaiseDisplayTimeChanged();
			}
		}

		#endregion Log items filter

		#region Log file loading

		/// <summary>
		/// Gets the log file prefix from a full file path.
		/// </summary>
		/// <param name="filePath">One of the log files.</param>
		/// <returns>The file's prefix, or null if it cannot be determined.</returns>
		public string GetPrefixFromPath(string filePath)
		{
			Match m = Regex.Match(filePath, @"^(.*)-[0-9]-[0-9]{18}\.fl$");
			if (m.Success)
			{
				return m.Groups[1].Value;
			}
			return null;
		}

		/// <summary>
		/// Opens the specified log files into the view.
		/// </summary>
		/// <param name="basePath">The base path of the log files to load.</param>
		/// <param name="singleFile">true to load a single file only. <paramref name="basePath"/> must be a full file name then.</param>
		public void OpenFiles(string basePath, bool singleFile = false)
		{
			if (basePath == null) throw new ArgumentNullException("basePath");
			if (basePath.Equals(FL.LogFileBasePath, StringComparison.InvariantCultureIgnoreCase))
			{
				App.WarningMessage("You cannot open the log file that this instance of FieldLogViewer is currently writing to.\n\n" +
					"Trying to read the messages that may be generated while reading messages leads to a locking situation.");
				return;
			}
			if (!Directory.Exists(Path.GetDirectoryName(basePath)))
			{
				App.WarningMessage("The directory of the log file path does not exist. If you expect log files to be created here, " +
					"please create the directory now or retry loading when a file has been created.\n\n" +
					"Selected base path: " + basePath);
				return;
			}

			// First make sure any open reader is fully closed and won't send any new items anymore
			OnStopLive();
			if (readerTask != null)
			{
				readerTask.Wait();
				// Also process any queued operations like OnReadWaiting...
				TaskHelper.DoEvents();
			}

			ViewCommandManager.Invoke("StartedReadingFiles");
			IsLoadingFiles = true;

			logItems.Clear();

			isLiveStopped = false;
			StopLiveCommand.RaiseCanExecuteChanged();

			localLogItems = new List<LogItemViewModelBase>();

			loadedBasePath = basePath;
			UpdateWindowTitle();
			AddFileToHistory();

			// Start the log file reading in a worker thread
			TaskHelper.Start(c => ReadTask(basePath, singleFile), out readerTask);
		}

		private void AddFileToHistory()
		{
			string tempInternetFilesPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Microsoft",
				"Windows",
				"Temporary Internet Files");
			if (loadedBasePath.StartsWith(tempInternetFilesPath, StringComparison.OrdinalIgnoreCase))
			{
				// These files have been loaded from Outlook and will be gone soon.
				// Do not add them to the list of recently loaded files.
				return;
			}

			var list = new List<string>();
			// Add current path
			list.Add(loadedBasePath);
			// Add all previous items, remove current path if it exists
			list.AddRange(App.Settings.RecentlyLoadedFiles
				.Where(f => !f.Equals(loadedBasePath, StringComparison.OrdinalIgnoreCase)));
			App.Settings.RecentlyLoadedFiles = list.Take(15).ToArray();
			SetJumpList();
		}

		/// <summary>
		/// Sets the application's jump list with tasks and recently loaded logs.
		/// </summary>
		private void SetJumpList()
		{
			var jumpList = new JumpList();
			// Add tasks first, they appear after recent logs (added below)
			jumpList.JumpItems.Add(
				new JumpTask
				{
					Title = "Wait for announcement",
					ApplicationPath = FL.EntryAssemblyLocation,
					Arguments = "/w",
					IconResourcePath = FL.EntryAssemblyLocation,
					IconResourceIndex = 0
				});

			Dictionary<string, int> prefixCount = new Dictionary<string, int>();
			foreach (var path in App.Settings.RecentlyLoadedFiles)
			{
				string prefix = Path.GetFileName(path);
				string dir = Path.GetDirectoryName(path);

				if (prefixCount.ContainsKey(prefix))
				{
					prefixCount[prefix]++;
				}
				else
				{
					prefixCount[prefix] = 1;
				}
			}
			foreach (var path in App.Settings.RecentlyLoadedFiles)
			{
				string prefix = Path.GetFileName(path);
				string dir = Path.GetDirectoryName(path);

				string title = prefix;
				if (prefixCount[prefix] > 1)
				{
					// Make title unique by adding the directory
					title += " in " + dir;
				}

				jumpList.JumpItems.Add(
					new JumpTask
					{
						CustomCategory = "Recent logs",
						Title = title,
						ApplicationPath = FL.EntryAssemblyLocation,
						Arguments = path,
						IconResourcePath = FL.EntryAssemblyLocation,
						IconResourceIndex = 1,
						Description = path
					});
			}
			JumpList.SetJumpList(Application.Current, jumpList);
		}

		/// <summary>
		/// Opens and reads the specified log files and pushes back all log items to the UI thread.
		/// This method is running in a worker thread.
		/// </summary>
		/// <param name="basePath">The base path of the log files to load.</param>
		/// <param name="singleFile">true to load a single file only. <paramref name="basePath"/> must be a full file name then.</param>
		private void ReadTask(string basePath, bool singleFile)
		{
			// Set current thread name to aid debugging
			Thread.CurrentThread.Name = "MainViewModel.ReadTask";

			// Setup and connect the wait handle that is set when all data has been read and we're
			// now waiting for more items to be written to the log files.
			EventWaitHandle readWaitHandle = new AutoResetEvent(false);
			readWaitHandle.WaitAction(
				() => dispatcher.Invoke((Action)OnReadWaiting),
				() => !isLiveStopped);

			// Create the log file group reader and read each next item
			try
			{
				logFileGroupReader = new FieldLogFileGroupReader(basePath, singleFile, readWaitHandle);
			}
			catch (Exception ex)
			{
				logFileGroupReader_Error(this, new ErrorEventArgs(ex));
				return;
			}
			logFileGroupReader.Error += logFileGroupReader_Error;
			List<FieldLogScopeItemViewModel> seenScopeItemVMs = new List<FieldLogScopeItemViewModel>();
			while (true)
			{
				FieldLogItem item = logFileGroupReader.ReadLogItem();
				if (item == null)
				{
					// Signal the UI that this is it, no more items are coming. (Reader closed)
					readWaitHandle.Set();
					break;
				}
				FieldLogItemViewModel itemVM = FieldLogItemViewModel.Create(item);
				if (itemVM == null) break;   // Cannot happen actually

				var scopeItem = item as FieldLogScopeItem;
				if (scopeItem != null)
				{
					if (scopeItem.IsRepeated)
					{
						// Find existing scope item
						var originalScopeItem = seenScopeItemVMs.FirstOrDefault(si => si.SessionId == scopeItem.SessionId && si.EventCounter == scopeItem.EventCounter);
						if (originalScopeItem != null)
						{
							// Skip this item, we already have it from an earlier file.
							// We can only update the original item's additional data from this
							// repeated item in some case. Let's see...
							if (scopeItem.Type == FieldLogScopeType.LogStart)
							{
								// TODO: Use this double-write-then-update mechanism instead of buffer stealing try-single-write
							}
							else if (scopeItem.Type == FieldLogScopeType.WebRequestStart)
							{
								// WebRequestStart items are repeated when more data is available
								// in a later request lifecycle event. We can copy the
								// WebRequestData contents from the new to the original item to
								// make it available for the entire request. The instance cannot
								// just be replaced though because the view model is created for
								// the old instance and wouldn't be updated to the new target.
								originalScopeItem.WebRequestDataVM.WebRequestData.UpdateFrom(scopeItem.WebRequestData);
								// Also notify the UI to refresh the updated item in the filter
								// CollectionView so that the data change is actually regarded by
								// an active filter.
								dispatcher.BeginInvoke(new Action<LogItemViewModelBase>(RefreshFilteredLogItem), DispatcherPriority.Loaded, originalScopeItem);
							}
							continue;
						}
					}
					seenScopeItemVMs.Add((FieldLogScopeItemViewModel)itemVM);
				}

				InsertLogItemThread(itemVM);
			}
		}

		/// <summary>
		/// Inserts a new log item to either the localLogItems list or the UI list, which ever is
		/// currently used.
		/// </summary>
		/// <param name="itemVM">The item to insert.</param>
		private void InsertLogItemThread(LogItemViewModelBase itemVM)
		{
			bool upgradedLock = false;
			localLogItemsLock.EnterUpgradeableReadLock();
			try
			{
				if (localLogItems == null)
				{
					// Not using local items list. Check if we should do so.
					if (returnToLocalLogItemsList.WaitOne(0))
					{
						FL.Trace("ReadTask: returnToLocalLogItemsList was set", "Waiting for the UI queue to clear before taking the list back.");

						// Wait for all queued items to be processed by the UI thread so that
						// the list is complete and no item is lost
						while (queuedNewItemsCount > 0)
						{
							Thread.Sleep(10);
						}
						// Ensure the items list is current when the queued counter is seen zero
						Thread.MemoryBarrier();

						localLogItemsLock.EnterWriteLock();
						upgradedLock = true;

						// Setup everything as if we were still reading an initial set of log
						// files and the the read Task thread use its local buffer again.
						using (FL.Scope("Copying logItems to localLogItems"))
						{
							localLogItems = new List<LogItemViewModelBase>(logItems);
						}
						FL.Trace("ReadTask: took back the list");
					}
				}

				if (localLogItems != null)
				{
					// Using a local items list to store new log items
					if (!upgradedLock)
					{
						localLogItemsLock.EnterWriteLock();
						upgradedLock = true;
					}

					localLogItems.InsertSorted(itemVM, new Comparison<LogItemViewModelBase>((a, b) => a.CompareTo(b)));

					if ((localLogItems.Count % 5000) == 0)
					{
						int count = localLogItems.Count;
						FL.TraceData("localLogItems.Count", count);
						dispatcher.BeginInvoke(new Action(() => LoadedItemsCount = count));
					}
				}
				else
				{
					// Send each item to the UI thread for insertion.
					// Don't push a new item to the UI thread if there are currently more than
					// 20 items waiting to be processed.
					while (queuedNewItemsCount >= 20)
					{
						FL.Trace("Already too many items queued, waiting...");
						Thread.Sleep(10);
					}

					Interlocked.Increment(ref queuedNewItemsCount);
					dispatcher.BeginInvoke(
						new Action<LogItemViewModelBase>(this.InsertNewLogItem),
						itemVM);
				}
			}
			finally
			{
				if (upgradedLock)
					localLogItemsLock.ExitWriteLock();
				localLogItemsLock.ExitUpgradeableReadLock();
			}
		}

		/// <summary>
		/// Handles an error while reading the log files.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void logFileGroupReader_Error(object sender, ErrorEventArgs args)
		{
			if (!dispatcher.CheckAccess())
			{
				dispatcher.BeginInvoke(
					new ErrorEventHandler(logFileGroupReader_Error),
					sender,
					args);
			}
			else
			{
				TaskDialogResult result = TaskDialog.Show(
					owner: MainWindow.Instance,
					allowDialogCancellation: true,
					title: "FieldLogViewer",
					mainInstruction: "An error occured while reading the log files.",
					content: args.GetException().Message + "\n\n" +
						"For details, including the exact problem and the offending file name and position, please open FieldLogViewer's log file from " +
						FL.LogFileBasePath + ".\n\n" +
						"If you continue reading, the loaded items may be incomplete or may not appear until you click the Stop button.",
					customButtons: new string[] { "Continue &reading", "&Cancel" });
				if (result.CustomButtonResult != 0)
				{
					OnStopLive();
				}
			}
		}

		/// <summary>
		/// Inserts a new log item from the read thread to the UI thread's items list.
		/// </summary>
		/// <param name="item">The new log item to insert.</param>
		private void InsertNewLogItem(LogItemViewModelBase item)
		{
			int newIndex = logItems.InsertSorted(item, (a, b) => a.CompareTo(b));
			int prevIndex;

			LoadedItemsCount = logItems.Count;

			HashSet<LogItemViewModelBase> itemsToRefresh = new HashSet<LogItemViewModelBase>();

			// LastLogStartItem, IndentLevel and UtcOffset source are only supported for FieldLog items
			FieldLogItemViewModel flItem = item as FieldLogItemViewModel;
			if (flItem != null)
			{
				// Check for new IndentLevel value
				int currentIndentLevel = 0;
				int tryIndentLevel;
				if (flItem.TryGetIndentLevelData(out tryIndentLevel))
				{
					item.IndentLevel = tryIndentLevel;
					currentIndentLevel = tryIndentLevel;
					FieldLogScopeItemViewModel scopeItem = flItem as FieldLogScopeItemViewModel;
					if (scopeItem != null && scopeItem.Type == FieldLogScopeType.Enter)
					{
						// Enter scope items increase the level by 1 for the following items
						currentIndentLevel++;
					}
				}
				else
				{
					// Use IndentLevel of the previous item in the same session & thread
					prevIndex = newIndex - 1;
					while (prevIndex >= 0)
					{
						FieldLogItemViewModel prevFlItem = logItems[prevIndex] as FieldLogItemViewModel;
						if (prevFlItem != null &&
							prevFlItem.SessionId == flItem.SessionId &&
							prevFlItem.ThreadId == flItem.ThreadId)
						{
							FieldLogScopeItemViewModel prevScope = prevFlItem as FieldLogScopeItemViewModel;
							if (prevScope != null)
							{
								item.IndentLevel = prevScope.Level;
							}
							else
							{
								item.IndentLevel = prevFlItem.IndentLevel;
							}
							currentIndentLevel = item.IndentLevel;
							break;
						}
						prevIndex--;
					}
				}

				// Check for new UtcOffset value
				int tryUtcOffset;
				bool newUtcOffset = flItem.TryGetUtcOffsetData(out tryUtcOffset);
				if (newUtcOffset)
				{
					item.UtcOffset = tryUtcOffset;
					itemsToRefresh.Add(item);
				}

				FieldLogScopeItemViewModel scope = item as FieldLogScopeItemViewModel;

				// Use LastLogStartItem and UtcOffset of the previous item from the same session
				if (scope != null && scope.Type == FieldLogScopeType.LogStart)
				{
					// This is a LogStart item, don't look elsewhere
					flItem.LastLogStartItem = scope;
					itemsToRefresh.Add(flItem);
				}
				else
				{
					prevIndex = newIndex - 1;
					while (prevIndex >= 0)
					{
						FieldLogItemViewModel prevFlItem = logItems[prevIndex] as FieldLogItemViewModel;
						if (prevFlItem != null &&
							prevFlItem.SessionId == flItem.SessionId)
						{
							flItem.LastLogStartItem = prevFlItem.LastLogStartItem;
							if (!newUtcOffset)
							{
								flItem.UtcOffset = prevFlItem.UtcOffset;
							}
							itemsToRefresh.Add(flItem);
							break;
						}
						prevIndex--;
					}
				}

				// Use LastWebRequestStartItem of the previous item from the same session and web request
				if (scope != null && scope.Type == FieldLogScopeType.WebRequestStart)
				{
					// This is a WebRequestStart item, don't look elsewhere
					flItem.LastWebRequestStartItem = scope;
					itemsToRefresh.Add(flItem);
				}
				else
				{
					prevIndex = newIndex - 1;
					while (prevIndex >= 0)
					{
						FieldLogItemViewModel prevFlItem = logItems[prevIndex] as FieldLogItemViewModel;
						if (prevFlItem != null &&
							prevFlItem.SessionId == flItem.SessionId &&
							prevFlItem.WebRequestId == flItem.WebRequestId)
						{
							flItem.LastWebRequestStartItem = prevFlItem.LastWebRequestStartItem;
							itemsToRefresh.Add(flItem);
							break;
						}
						prevIndex--;
					}
				}

				// Update web request processing time
				if (scope != null && scope.Type == FieldLogScopeType.WebRequestEnd)
				{
					if (flItem.LastWebRequestStartItem != null)
					{
						flItem.LastWebRequestStartItem.WebRequestDataVM.RequestDuration =
							flItem.Time - flItem.LastWebRequestStartItem.Time;
						// Refresh all other items that share the same LastWebRequestStartItem
						for (int i = 0; i < logItems.Count; i++)
						{
							var item2 = logItems[i] as FieldLogItemViewModel;
							if (item2 != null && item2.LastWebRequestStartItem == flItem.LastWebRequestStartItem)
							{
								itemsToRefresh.Add(item2);
							}
						}
					}
				}

				// Update all items after the inserted item
				bool setIndentLevel = true;
				bool setUtcOffset = true;
				for (int index = newIndex + 1; index < logItems.Count; index++)
				{
					FieldLogItemViewModel nextFlItem = logItems[index] as FieldLogItemViewModel;
					if (nextFlItem != null &&
						nextFlItem.SessionId == flItem.SessionId)
					{
						if (nextFlItem.ThreadId == flItem.ThreadId)
						{
							// Same thread gets the IndentLevel value
							if (nextFlItem.TryGetIndentLevelData(out tryIndentLevel))
							{
								// Next item has an IndentLevel value on its own, stop updating other items
								setIndentLevel = false;
							}
							if (setIndentLevel)
							{
								// IndentLevel value should still be updated
								nextFlItem.IndentLevel = currentIndentLevel;
							}
						}

						if (nextFlItem.WebRequestId == flItem.WebRequestId)
						{
							// Same web request gets the LastWebRequestStartItem
							nextFlItem.LastWebRequestStartItem = flItem.LastWebRequestStartItem;
							// itemsToRefresh.Add(nextFlItem);
							// ^-- Will always be set later on

							// Update web request processing time
							FieldLogScopeItemViewModel nextScope = nextFlItem as FieldLogScopeItemViewModel;
							if (nextScope != null && nextScope.Type == FieldLogScopeType.WebRequestEnd)
							{
								if (nextFlItem.LastWebRequestStartItem != null)
								{
									nextFlItem.LastWebRequestStartItem.WebRequestDataVM.RequestDuration =
										nextFlItem.Time - nextFlItem.LastWebRequestStartItem.Time;
									// Refresh all other items that share the same LastWebRequestStartItem
									for (int i = 0; i < logItems.Count; i++)
									{
										var item2 = logItems[i] as FieldLogItemViewModel;
										if (item2 != null && item2.LastWebRequestStartItem == nextFlItem.LastWebRequestStartItem)
										{
											itemsToRefresh.Add(item2);
										}
									}
								}
							}
						}

						// All same session also get LastLogStartItem and UtcOffset
						nextFlItem.LastLogStartItem = flItem.LastLogStartItem;
						itemsToRefresh.Add(nextFlItem);

						if (nextFlItem.TryGetUtcOffsetData(out tryUtcOffset))
						{
							// Next item has a UtcOffset value on its own, stop updating other items
							setUtcOffset = false;
						}
						if (setUtcOffset)
						{
							// UtcOffset value should still be updated
							nextFlItem.UtcOffset = flItem.UtcOffset;
						}
					}
				}

				// Update the filter now that the item has more data (LastLogStartItem and
				// LastWebRequestStartItem) and potentially following items have been updated, too
				foreach (var i in itemsToRefresh)
				{
					RefreshFilteredLogItem(i);
				}
			}

			// Ensure the items list is current when the queued counter is decremented
			Thread.MemoryBarrier();
			if (queuedNewItemsCount == 1 && IsLoadingFilesAgain)
			{
				// We're about to hand off hte logItems list to the read thread. Make sure all
				// other queued events down to Input priority are processed to have a fluid UI.
				TaskHelper.DoEvents(DispatcherPriority.Input);
			}
			Interlocked.Decrement(ref queuedNewItemsCount);

			// Test whether the UI thread is locked because of reading too many log items at once
			if (insertingItemsSince == DateTime.MinValue)
			{
				FL.Trace("Setting insertingItemsSince");
				insertingItemsSince = DateTime.UtcNow;
				Dispatcher.CurrentDispatcher.BeginInvoke(
					new Action(() => { insertingItemsSince = DateTime.MinValue; FL.Trace("Resetting insertingItemsSince"); }),
					DispatcherPriority.Background);
			}
			if (DateTime.UtcNow > insertingItemsSince.AddMilliseconds(200))
			{
				FL.Trace("InsertNewLogItem: UI thread blocked for 200 ms", "Setting returnToLocalLogItemsList event");

				// Blocking the UI with inserting log items for 200 ms now.
				// Tell the read thread to stop sending new items to the UI thread separately, wait
				// for all items to be handled by the UI thread, and then take back the log items
				// ObservableCollection to a local List for faster inserting of many items.
				returnToLocalLogItemsList.Set();

				// The following actione still need to be performed by the UI thread
				//ViewCommandManager.Invoke("StartedReadingFiles");
				IsLoadingFilesAgain = true;

				isLiveStopped = false;
				StopLiveCommand.RaiseCanExecuteChanged();

				// Do not execute this block again as long as the UI thread is still blocked
				insertingItemsSince = insertingItemsSince.AddDays(1000);
			}
		}

		/// <summary>
		/// Called when the read wait handle has been set. All data has been read and we're now
		/// waiting for more items to be written to the log files. Until now, the read thread was
		/// adding new items to a local List for better performance. Now, this List is copied to
		/// an ObservableCollection, displayed and managed by the UI thread. From now on, new items
		/// will be posted to the UI thread separately for inserting in the items list, calling the
		/// InsertNewLogItem method.
		/// </summary>
		private void OnReadWaiting()
		{
			if (returnToLocalLogItemsList.WaitOne(0))
			{
				FL.Trace("OnReadWaiting: returnToLocalLogItemsList was set", "Reverting UI state, not touching log items lists.");

				// The UI thread has been busy inserting queued new items and has detected a long
				// blocking period. It has then decided to signal the read thread to go back to
				// inserting more items into a local List instead of the main ObservableCollection.
				// But the read thread has already finished reading existing items and has
				// indicated this by calling this method. So it won't actually go back to the local
				// items list because it doesn't currently have any new items to read. The UI
				// thread is still waiting for the read thread to return the list to the UI. This
				// needs to be resolved here.

				// returnToLocalLogItemsList is already reset just by testing it (AutoResetEvent).
				// logItems and localLogItems has not yet been touched, nothing to do with that.
				// Revert other UI state:
				IsLoadingFilesAgain = false;
				ViewCommandManager.Invoke("FinishedReadingFiles");
				return;
			}

			// Lock the local list so that no item loaded directly afterwards will get lost
			// while we're still preparing the loaded items list to be pushed to the UI
			localLogItemsLock.EnterReadLock();
			try
			{
				if (localLogItems == null) return;   // Nothing to do, just waiting once again in normal monitor mode
			}
			finally
			{
				localLogItemsLock.ExitReadLock();
			}
			FL.Trace("OnReadWaiting: EnterWriteLock");
			localLogItemsLock.EnterWriteLock();
			try
			{
				// Check again because we have released the lock since the last check
				if (localLogItems == null) return;   // Nothing to do, just waiting once again in normal monitor mode

				FL.Trace("Copying localLogItems list to UI thread, " + localLogItems.Count + " items");

				// Apply scope-based indenting and UtcOffset to all items now
				Dictionary<int, int> threadLevels = new Dictionary<int, int>();
				Dictionary<Guid, FieldLogScopeItemViewModel> logStartItems = new Dictionary<Guid, FieldLogScopeItemViewModel>();
				Dictionary<Tuple<Guid, uint>, FieldLogScopeItemViewModel> webRequestStartItems = new Dictionary<Tuple<Guid, uint>, FieldLogScopeItemViewModel>();
				int utcOffset = 0;
				foreach (var item in localLogItems)
				{
					FieldLogItemViewModel flItem = item as FieldLogItemViewModel;
					if (flItem != null)
					{
						// Check for new UtcOffset value in this item
						int tryUtcOffset;
						if (flItem.TryGetUtcOffsetData(out tryUtcOffset))
						{
							utcOffset = tryUtcOffset;
						}
						// Assign current UtcOffset value to each item
						item.UtcOffset = utcOffset;

						FieldLogScopeItemViewModel scope = item as FieldLogScopeItemViewModel;
						if (scope != null)
						{
							threadLevels[scope.ThreadId] = scope.Level;
							if (scope.Type == FieldLogScopeType.Enter)
							{
								scope.IndentLevel = scope.Level - 1;
							}
							else
							{
								scope.IndentLevel = scope.Level;
							}

							if (scope.Type == FieldLogScopeType.LogStart)
							{
								logStartItems[scope.SessionId] = scope;
							}
							if (scope.Type == FieldLogScopeType.WebRequestStart)
							{
								webRequestStartItems[new Tuple<Guid, uint>(scope.SessionId, scope.WebRequestId)] = scope;
							}
						}
						else
						{
							int level;
							if (threadLevels.TryGetValue(flItem.ThreadId, out level))
							{
								flItem.IndentLevel = level;
							}
						}

						FieldLogScopeItemViewModel tryLastLogStartScope;
						if (logStartItems.TryGetValue(flItem.SessionId, out tryLastLogStartScope))
						{
							flItem.LastLogStartItem = tryLastLogStartScope;
						}

						FieldLogScopeItemViewModel tryLastWebRequestStartScope;
						if (webRequestStartItems.TryGetValue(new Tuple<Guid, uint>(flItem.SessionId, flItem.WebRequestId), out tryLastWebRequestStartScope))
						{
							flItem.LastWebRequestStartItem = tryLastWebRequestStartScope;
						}

						// Update web request processing time
						if (scope != null && scope.Type == FieldLogScopeType.WebRequestEnd)
						{
							if (flItem.LastWebRequestStartItem != null)
							{
								flItem.LastWebRequestStartItem.WebRequestDataVM.RequestDuration =
									flItem.Time - flItem.LastWebRequestStartItem.Time;
							}
						}
					}
				}

				// Publish loaded items to the UI
				using (FL.Scope("Copying localLogItems to logItems"))
				{
					this.logItems = new ObservableCollection<LogItemViewModelBase>(localLogItems);
				}
				localLogItems = null;
			}
			finally
			{
				localLogItemsLock.ExitWriteLock();
			}
			// Notify the UI to make it show the new list of items.
			// From now on, newly loaded items are added one by one to the collection that
			// is already bound to the UI, so the new items will become visible.
			OnPropertyChanged("LogItems");
			LoadedItemsCount = logItems.Count;
			if (IsLoadingFiles)
			{
				IsLoadingFiles = false;
				UpdateWindowTitle();
				ViewCommandManager.Invoke("FinishedReadingFiles");
			}
			if (IsLoadingFilesAgain)
			{
				IsLoadingFilesAgain = false;
				ViewCommandManager.InvokeLoaded("FinishedReadingFilesAgain");
			}
		}

		#endregion Log file loading

		#region Other methods

		private void CreateBasicFilters()
		{
			FilterViewModel f;
			FilterConditionGroupViewModel fcg;
			FilterConditionViewModel fc;

			f = new FilterViewModel();
			f.DisplayName = "Errors and up";
			fcg = new FilterConditionGroupViewModel(f);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.Priority;
			fc.Comparison = FilterComparison.GreaterOrEqual;
			fc.Value = FieldLogPriority.Error.ToString();
			fcg.Conditions.Add(fc);
			f.ConditionGroups.Add(fcg);
			fcg = new FilterConditionGroupViewModel(f);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.AnyText;
			fc.Comparison = FilterComparison.Contains;
			fc.Value = "error";
			fcg.Conditions.Add(fc);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.Priority;
			fc.Comparison = FilterComparison.GreaterOrEqual;
			fc.Value = FieldLogPriority.Info.ToString();
			fcg.Conditions.Add(fc);
			f.ConditionGroups.Add(fcg);
			Filters.Add(f);

			f = new FilterViewModel();
			f.DisplayName = "Warnings and up";
			fcg = new FilterConditionGroupViewModel(f);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.Priority;
			fc.Comparison = FilterComparison.GreaterOrEqual;
			fc.Value = FieldLogPriority.Warning.ToString();
			fcg.Conditions.Add(fc);
			f.ConditionGroups.Add(fcg);
			fcg = new FilterConditionGroupViewModel(f);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.AnyText;
			fc.Comparison = FilterComparison.Contains;
			fc.Value = "warning";
			fcg.Conditions.Add(fc);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.Priority;
			fc.Comparison = FilterComparison.GreaterOrEqual;
			fc.Value = FieldLogPriority.Info.ToString();
			fcg.Conditions.Add(fc);
			f.ConditionGroups.Add(fcg);
			fcg = new FilterConditionGroupViewModel(f);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.AnyText;
			fc.Comparison = FilterComparison.Contains;
			fc.Value = "error";
			fcg.Conditions.Add(fc);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.Priority;
			fc.Comparison = FilterComparison.GreaterOrEqual;
			fc.Value = FieldLogPriority.Info.ToString();
			fcg.Conditions.Add(fc);
			f.ConditionGroups.Add(fcg);
			Filters.Add(f);

			f = new FilterViewModel();
			f.DisplayName = "Significant exceptions";
			fcg = new FilterConditionGroupViewModel(f);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.Type;
			fc.Comparison = FilterComparison.Equals;
			fc.Value = FieldLogItemType.Exception.ToString();
			fcg.Conditions.Add(fc);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.ExceptionContext;
			fc.Comparison = FilterComparison.NotEquals;
			fc.Value = "AppDomain.FirstChanceException";
			fcg.Conditions.Add(fc);
			f.ConditionGroups.Add(fcg);
			Filters.Add(f);

			f = new FilterViewModel();
			f.DisplayName = "No trace priority";
			fcg = new FilterConditionGroupViewModel(f);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.Priority;
			fc.Comparison = FilterComparison.GreaterOrEqual;
			fc.Value = FieldLogPriority.Checkpoint.ToString();
			fcg.Conditions.Add(fc);
			f.ConditionGroups.Add(fcg);
			Filters.Add(f);

			f = new FilterViewModel
			{
				DisplayName = "No WPF Stop messages"
			};
			fcg = new FilterConditionGroupViewModel(f)
			{
				IsExclude = true
			};
			fcg.Conditions.Add(new FilterConditionViewModel(fcg)
			{
				Column = FilterColumn.Type,
				Comparison = FilterComparison.Equals,
				Value = FieldLogItemType.Text.ToString()
			});
			fcg.Conditions.Add(new FilterConditionViewModel(fcg)
			{
				Column = FilterColumn.Priority,
				Comparison = FilterComparison.Equals,
				Value = FieldLogPriority.Trace.ToString()
			});
			fcg.Conditions.Add(new FilterConditionViewModel(fcg)
			{
				Column = FilterColumn.TextText,
				Comparison = FilterComparison.Contains,
				Value = "WPF: "
			});
			fcg.Conditions.Add(new FilterConditionViewModel(fcg)
			{
				Column = FilterColumn.TextText,
				Comparison = FilterComparison.EndsWith,
				Value = " [Stop]"
			});
			fcg.Conditions.Add(new FilterConditionViewModel(fcg)
			{
				Column = FilterColumn.TextText,
				Comparison = FilterComparison.NotStartsWith,
				Value = "«"
			});
			fcg.Conditions.Add(new FilterConditionViewModel(fcg)
			{
				Column = FilterColumn.TextText,
				Comparison = FilterComparison.NotContains,
				Value = "WPF: Load XAML/BAML"
			});
			f.ConditionGroups.Add(fcg);
			Filters.Add(f);

			f = new FilterViewModel();
			f.DisplayName = "No WPF tracing";
			fcg = new FilterConditionGroupViewModel(f);
			fcg.IsExclude = true;
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.Type;
			fc.Comparison = FilterComparison.Equals;
			fc.Value = FieldLogItemType.Text.ToString();
			fcg.Conditions.Add(fc);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.TextText;
			fc.Comparison = FilterComparison.StartsWith;
			fc.Value = "WPF: ";
			fcg.Conditions.Add(fc);
			f.ConditionGroups.Add(fcg);
			Filters.Add(f);

			f = new FilterViewModel();
			f.DisplayName = "No Diagnostics.Trace output";
			fcg = new FilterConditionGroupViewModel(f);
			fcg.IsExclude = true;
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.Type;
			fc.Comparison = FilterComparison.Equals;
			fc.Value = FieldLogItemType.Text.ToString();
			fcg.Conditions.Add(fc);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.TextText;
			fc.Comparison = FilterComparison.StartsWith;
			fc.Value = "Trace: ";
			fcg.Conditions.Add(fc);
			f.ConditionGroups.Add(fcg);
			Filters.Add(f);
		}

		public void ResetFilters()
		{
			Filters.Clear();
			Filters.Add(new FilterViewModel(true));
			CreateBasicFilters();
			SelectedFilter = Filters[0];
		}

		private void UpdateWindowTitle()
		{
			string prefix = Path.GetFileName(loadedBasePath);
			string dir = Path.GetDirectoryName(loadedBasePath);

			if (IsLoadingFiles)
			{
				DisplayName = "Loading " + prefix + " in " + dir + "… – FieldLogViewer";
			}
			else if (loadedBasePath != null)
			{
				DisplayName = prefix + " in " + dir + " – FieldLogViewer";
			}
			else
			{
				DisplayName = "FieldLogViewer";
			}
		}

		internal void StopDebugMonitors()
		{
			// Stop wait mode so it can stop the debug monitor permanently again
			AutoLoadLog = false;

			localDebugMonitor.Stop();
			globalDebugMonitor.Stop();
		}

		#endregion Other methods

		#region IViewCommandSource members

		private ViewCommandManager viewCommandManager = new ViewCommandManager();
		public ViewCommandManager ViewCommandManager { get { return this.viewCommandManager; } }

		#endregion IViewCommandSource members
	}
}
