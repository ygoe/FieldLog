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
using System.Windows.Threading;
using Microsoft.Win32;
using TaskDialogInterop;
using Unclassified.FieldLog;
using Unclassified.FieldLogViewer.View;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class MainViewModel : ViewModelBase, IViewCommandSource
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
		private AutoResetEvent returnToLocalLogItemsList = new AutoResetEvent(false);

		#endregion Private data

		#region Constructors

		public MainViewModel()
		{
			Instance = this;
			dispatcher = Dispatcher.CurrentDispatcher;

			InitializeCommands();
			UpdateWindowTitle();

			// Setup toolbar and settings events
			this.BindProperty(vm => vm.IsDebugMonitorActive, AppSettings, s => s.IsDebugMonitorActive);
			AppSettings.OnPropertyChanged(
				s => s.IsLiveScrollingEnabled,
				v => { if (v) ViewCommandManager.Invoke("ScrollToEnd"); });
			AppSettings.OnPropertyChanged(
				s => s.IsWindowOnTop,
				v => MainWindow.Instance.Topmost = v,
				true);
			AppSettings.OnPropertyChanged(
				s => s.IndentSize,
				() =>
				{
					DecreaseIndentSizeCommand.RaiseCanExecuteChanged();
					IncreaseIndentSizeCommand.RaiseCanExecuteChanged();
				});
			AppSettings.OnPropertyChanged(
				s => s.ItemTimeMode,
				() => RefreshLogItemsFilterView());

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
			foreach (string s in AppSettings.Filters)
			{
				FilterViewModel f = new FilterViewModel();
				try
				{
					f.LoadFromString(s);
				}
				catch (Exception ex)
				{
					MessageBox.Show(
						"A filter could not be restored from the settings.\n" + ex.Message,
						"Error",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
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
			FilterViewModel selectedFilterVM = Filters.FirstOrDefault(f => f.DisplayName == AppSettings.SelectedFilter);
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
			DebugMonitor.MessageReceived += (pid, text) =>
			{
				var itemVM = new DebugMessageViewModel(pid, text);

				Interlocked.Increment(ref queuedNewItemsCount);
				dispatcher.BeginInvoke(
					new Action<LogItemViewModelBase>(this.InsertNewLogItem),
					itemVM);
			};
		}

		#endregion Constructors

		#region Public properties

		public string LoadedBasePath { get { return loadedBasePath; } }

		#endregion Public properties

		#region Commands

		// Toolbar commands
		public DelegateCommand LoadLogCommand { get; private set; }
		public DelegateCommand StopLiveCommand { get; private set; }
		public DelegateCommand ClearCommand { get; private set; }
		public DelegateCommand LoadMapCommand { get; private set; }
		public DelegateCommand DecreaseIndentSizeCommand { get; private set; }
		public DelegateCommand IncreaseIndentSizeCommand { get; private set; }
		public DelegateCommand DeleteFilterCommand { get; private set; }
		public DelegateCommand ClearSearchTextCommand { get; private set; }
		public DelegateCommand SettingsCommand { get; private set; }

		// Log items list context menu commands
		public DelegateCommand QuickFilterSessionCommand { get; private set; }
		public DelegateCommand QuickFilterThreadCommand { get; private set; }
		public DelegateCommand QuickFilterTypeCommand { get; private set; }
		public DelegateCommand QuickFilterMinPrioCommand { get; private set; }
		public DelegateCommand QuickFilterNotBeforeCommand { get; private set; }
		public DelegateCommand QuickFilterNotAfterCommand { get; private set; }

		private void InitializeCommands()
		{
			LoadLogCommand = new DelegateCommand(OnLoadLog, CanLoadLog);
			StopLiveCommand = new DelegateCommand(OnStopLive, CanStopLive);
			ClearCommand = new DelegateCommand(OnClear, CanClear);
			LoadMapCommand = new DelegateCommand(OnLoadMap);
			DecreaseIndentSizeCommand = new DelegateCommand(OnDecreaseIndentSize, CanDecreaseIndentSize);
			IncreaseIndentSizeCommand = new DelegateCommand(OnIncreaseIndentSize, CanIncreaseIndentSize);
			DeleteFilterCommand = new DelegateCommand(OnDeleteFilter, CanDeleteFilter);
			ClearSearchTextCommand = new DelegateCommand(OnClearSearchText);
			SettingsCommand = new DelegateCommand(OnSettings);

			QuickFilterSessionCommand = new DelegateCommand(OnQuickFilterSession, CanQuickFilterSession);
			QuickFilterThreadCommand = new DelegateCommand(OnQuickFilterThread, CanQuickFilterThread);
			QuickFilterTypeCommand = new DelegateCommand(OnQuickFilterType, CanQuickFilterType);
			QuickFilterMinPrioCommand = new DelegateCommand(OnQuickFilterMinPrio, CanQuickFilterMinPrio);
			QuickFilterNotBeforeCommand = new DelegateCommand(OnQuickFilterNotBefore, CanQuickFilterNotBefore);
			QuickFilterNotAfterCommand = new DelegateCommand(OnQuickFilterNotAfter, CanQuickFilterNotAfter);
		}

		private void InvalidateToolbarCommandsLoading()
		{
			LoadLogCommand.RaiseCanExecuteChanged();
			StopLiveCommand.RaiseCanExecuteChanged();
			ClearCommand.RaiseCanExecuteChanged();
			LoadMapCommand.RaiseCanExecuteChanged();
			SettingsCommand.RaiseCanExecuteChanged();
		}

		private void InvalidateQuickFilterCommands()
		{
			QuickFilterSessionCommand.RaiseCanExecuteChanged();
			QuickFilterThreadCommand.RaiseCanExecuteChanged();
			QuickFilterTypeCommand.RaiseCanExecuteChanged();
			QuickFilterMinPrioCommand.RaiseCanExecuteChanged();
			QuickFilterNotBeforeCommand.RaiseCanExecuteChanged();
			QuickFilterNotAfterCommand.RaiseCanExecuteChanged();
		}

		#region Toolbar

		private bool CanLoadLog()
		{
			return !IsLoadingFiles && !IsLoadingFilesAgain;
		}
		
		private void OnLoadLog()
		{
			OpenFileDialog dlg = new OpenFileDialog();
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

		private void OnLoadMap()
		{
		}

		private bool CanDecreaseIndentSize()
		{
			return AppSettings.IndentSize > 4;
		}

		private void OnDecreaseIndentSize()
		{
			AppSettings.IndentSize -= 4;
			if (AppSettings.IndentSize < 4)
			{
				AppSettings.IndentSize = 4;
			}
		}

		private bool CanIncreaseIndentSize()
		{
			return AppSettings.IndentSize < 32;
		}

		private void OnIncreaseIndentSize()
		{
			AppSettings.IndentSize += 4;
			if (AppSettings.IndentSize > 32)
			{
				AppSettings.IndentSize = 32;
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
					if (MessageBox.Show(
						"Would you like to delete the selected filter “" + SelectedFilter.DisplayName + "”?",
						"FieldLogViewer",
						MessageBoxButton.YesNo,
						MessageBoxImage.Question) == MessageBoxResult.Yes)
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
			SettingsWindow win = new SettingsWindow();
			SettingsViewModel vm = new SettingsViewModel();
			win.DataContext = vm;
			win.Owner = MainWindow.Instance;
			win.Show();
		}

		#endregion Toolbar

		#region Log items list context menu

		private FilterViewModel GetQuickFilter(out bool isNew)
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
			if (filter.ConditionGroups.Count == 0)
			{
				filter.ConditionGroups.Add(new FilterConditionGroupViewModel(filter));
			}
			return filter;
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
					filter.DisplayName = "By session";
					break;
				default:
					filter.DisplayName = "By " + sessionCount + " sessions";
					break;
			}
			foreach (var cg in filter.ConditionGroups)
			{
				// Remove all session and thread ID conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.SessionId);
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
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
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
					filter.DisplayName = "Thread ID " + SelectedItemsThreadIds.First();
					break;
				default:
					filter.DisplayName = "Thread IDs " + SelectedItemsThreadIds.Aggregate(", ", " and ");
					break;
			}
			foreach (var cg in filter.ConditionGroups)
			{
				// Remove all session and thread ID conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.SessionId);
				cg.Conditions.Filter(c => c.Column != FilterColumn.ThreadId);
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
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterType()
		{
			return SelectedItems.Count == 1;
		}

		private void OnQuickFilterType()
		{
			bool isNew;
			var filter = GetQuickFilter(out isNew);
			filter.DisplayName = "Type " + EnumerationExtension<FilterItemType>.GetDescription(SelectedItemFilterItemType);
			foreach (var cg in filter.ConditionGroups)
			{
				// Remove all session and thread ID conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.Type);
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
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterMinPrio()
		{
			return SelectedItems.Count == 1 && SelectedItems[0] is FieldLogItemViewModel;
		}

		private void OnQuickFilterMinPrio()
		{
			FieldLogItemViewModel flItem = SelectedItems[0] as FieldLogItemViewModel;
			bool isNew;
			var filter = GetQuickFilter(out isNew);
			filter.DisplayName = "Priority " + flItem.PrioTitle + " or higher";
			foreach (var cg in filter.ConditionGroups)
			{
				// Remove all session and thread ID conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.Priority);
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
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterNotBefore()
		{
			return SelectedItems.Count == 1;
		}

		private void OnQuickFilterNotBefore()
		{
			bool isNew;
			var filter = GetQuickFilter(out isNew);
			filter.DisplayName = "Not before...";
			foreach (var cg in filter.ConditionGroups)
			{
				// Remove all session and thread ID conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.Time);
				if (!cg.IsExclude)
				{
					DateTime itemTime = new DateTime(SelectedItems[0].Time.Ticks / 10 * 10);   // Round down to the next microsecond
					switch (AppSettings.ItemTimeMode)
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
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				SelectedFilter = filter;
			}
		}

		private bool CanQuickFilterNotAfter()
		{
			return SelectedItems.Count == 1;
		}

		private void OnQuickFilterNotAfter()
		{
			bool isNew;
			var filter = GetQuickFilter(out isNew);
			filter.DisplayName = "Not after...";
			foreach (var cg in filter.ConditionGroups)
			{
				// Remove all session and thread ID conditions
				cg.Conditions.Filter(c => c.Column != FilterColumn.Time);
				if (!cg.IsExclude)
				{
					DateTime itemTime = new DateTime((SelectedItems[0].Time.Ticks + 9) / 10 * 10);   // Round up to the next microsecond
					switch (AppSettings.ItemTimeMode)
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
			if (isNew)
			{
				filter.QuickPreviousFilter = SelectedFilter;
				Filters.Add(filter);
				SelectedFilter = filter;
			}
		}

		#endregion Log items list context menu

		#endregion Commands

		#region Data properties

		public AppSettings AppSettings
		{
			get { return AppSettings.Instance; }
		}

		#region Toolbar and settings

		public bool IsDebugMonitorActive
		{
			get { return DebugMonitor.IsActive; }
			set
			{
				if (value)
				{
					DebugMonitor.TryStart();
				}
				else
				{
					DebugMonitor.Stop();
				}
				OnPropertyChanged("IsDebugMonitorActive");
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

		public ICollectionView FilteredLogItemsView
		{
			get { return filteredLogItems.View; }
		}

		public List<LogItemViewModelBase> selectedItems;
		public List<LogItemViewModelBase> SelectedItems
		{
			get { return selectedItems; }
			set
			{
				if (CheckUpdate(value, ref selectedItems, "SelectedItems", "QuickFilterThreadTitle", "QuickFilterTypeTitle", "QuickFilterMinPrioTitle"))
				{
					InvalidateQuickFilterCommands();
				}
			}
		}

		private bool isLoadingFiles;
		public bool IsLoadingFiles
		{
			get { return isLoadingFiles; }
			set
			{
				if (CheckUpdate(value, ref isLoadingFiles, "IsLoadingFiles"))
				{
					FL.TraceData("IsLoadingFiles", IsLoadingFiles);
					if (isLoadingFiles)
					{
						filteredLogItems.Source = null;
					}
					else
					{
						filteredLogItems.Source = logItems;
						RefreshLogItemsFilterView();
					}
					OnPropertyChanged("FilteredLogItemsView");
					OnPropertyChanged("LogItemsVisibility");
					OnPropertyChanged("ItemDetailsVisibility");
					OnPropertyChanged("LoadingMsgVisibility");
					InvalidateToolbarCommandsLoading();
				}
			}
		}

		private bool isLoadingFilesAgain;
		public bool IsLoadingFilesAgain
		{
			get { return isLoadingFilesAgain; }
			set
			{
				if (CheckUpdate(value, ref isLoadingFilesAgain, "IsLoadingFilesAgain"))
				{
					FL.TraceData("IsLoadingFilesAgain", IsLoadingFilesAgain);
					if (!isLoadingFilesAgain)
					{
						filteredLogItems.Source = logItems;
						RefreshLogItemsFilterView();
					}
					OnPropertyChanged("FilteredLogItemsView");
					InvalidateToolbarCommandsLoading();
				}
			}
		}

		private int loadedItemsCount;
		public int LoadedItemsCount
		{
			get { return loadedItemsCount; }
			set { CheckUpdate(value, ref loadedItemsCount, "LoadedItemsCount"); }
		}

		public Visibility LogItemsVisibility
		{
			get
			{
				return !IsLoadingFiles ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public Visibility ItemDetailsVisibility
		{
			get
			{
				return !IsLoadingFiles ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public Visibility LoadingMsgVisibility
		{
			get
			{
				return IsLoadingFiles ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		#endregion Log items list

		#region Filter

		public ObservableCollection<FilterViewModel> Filters { get; private set; }

		public ICollectionView SortedFilters
		{
			get { return sortedFilters.View; }
		}

		private FilterViewModel selectedFilter;
		public FilterViewModel SelectedFilter
		{
			get { return selectedFilter; }
			set
			{
				if (CheckUpdate(value, ref selectedFilter, "SelectedFilter"))
				{
					DeleteFilterCommand.RaiseCanExecuteChanged();
					ViewCommandManager.Invoke("SaveScrolling");
					RefreshLogItemsFilterView();
					ViewCommandManager.Invoke("RestoreScrolling");
					if (selectedFilter != null)
					{
						AppSettings.SelectedFilter = selectedFilter.DisplayName;
					}
					else
					{
						AppSettings.SelectedFilter = "";
					}
				}
			}
		}

		private string adhocSearchText;
		public string AdhocSearchText
		{
			get { return adhocSearchText; }
			set
			{
				if (CheckUpdate(value, ref adhocSearchText, "AdhocSearchText"))
				{
					if (!string.IsNullOrWhiteSpace(adhocSearchText))
					{
						adhocFilterCondition = new FilterConditionViewModel(null);
						adhocFilterCondition.Value = adhocSearchText;
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

		#endregion Quick filter

		#endregion Data properties

		#region Log items filter

		/// <summary>
		/// Filter implementation for the collection view returned by FilteredLogItemsView.
		/// </summary>
		private void filteredLogItems_Filter(object sender, FilterEventArgs e)
		{
			if (SelectedFilter != null)
			{
				e.Accepted =
					SelectedFilter.IsMatch(e.Item) &&
					(adhocFilterCondition == null || adhocFilterCondition.IsMatch(e.Item));
			}
			else
			{
				e.Accepted = true;
			}
		}

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
			AppSettings.Filters = Filters
				.Where(f => !f.AcceptAll)
				.Select(f => f.SaveToString())
				.Where(s => !string.IsNullOrEmpty(s))
				.ToArray();
		}

		public void RefreshLogItemsFilterView()
		{
			if (filteredLogItems.View != null)
			{
				filteredLogItems.View.Refresh();
			}
			ViewCommandManager.Invoke("UpdateDisplayTime");
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
				MessageBox.Show(
					"You cannot open the log file that this instance of FieldLogViewer is currently writing to.\n\n" +
						"Trying to read the messages that may be generated while reading messages leads to a locking situation.",
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}
			if (!Directory.Exists(Path.GetDirectoryName(basePath)))
			{
				MessageBox.Show(
					"The directory of the log file path does not exist. If you expect log files to be created here, " +
						"please create the directory now or retry loading when a file has been created.\n\n" +
						"Selected base path: " + basePath,
					"Error",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
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

			this.logItems.Clear();

			isLiveStopped = false;
			StopLiveCommand.RaiseCanExecuteChanged();

			localLogItems = new List<LogItemViewModelBase>();

			loadedBasePath = basePath;
			UpdateWindowTitle();

			// Start the log file reading in a worker thread
			readerTask = Task.Factory.StartNew(() => ReadTask(basePath, singleFile));
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
				() => dispatcher.Invoke((Action) OnReadWaiting),
				() => !isLiveStopped);

			// Create the log file group reader and read each next item
			logFileGroupReader = new FieldLogFileGroupReader(basePath, singleFile, readWaitHandle);
			logFileGroupReader.Error += logFileGroupReader_Error;
			List<FieldLogScopeItem> seenScopeItems = new List<FieldLogScopeItem>();
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
						if (seenScopeItems.Any(si => si.SessionId == scopeItem.SessionId && si.EventCounter == scopeItem.EventCounter))
						{
							// Skip this item, we already have it from an earlier file
							continue;
						}
					}
					seenScopeItems.Add(scopeItem);
				}

				bool upgradedLock = false;
				localLogItemsLock.EnterUpgradeableReadLock();
				try
				{
					if (localLogItems == null)
					{
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
							using (FL.NewScope("Copying logItems to localLogItems"))
							{
								localLogItems = new List<LogItemViewModelBase>(logItems);
							}
							FL.Trace("ReadTask: took back the list");
						}
					}

					if (localLogItems != null)
					{
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
		}

		/// <summary>
		/// Handles an error while reading the log files.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void logFileGroupReader_Error(object sender, ErrorEventArgs e)
		{
			if (!dispatcher.CheckAccess())
			{
				dispatcher.BeginInvoke(
					new ErrorEventHandler(logFileGroupReader_Error),
					sender,
					e);
			}
			else
			{
				TaskDialogResult result = TaskDialog.Show(
					owner: MainWindow.Instance,
					allowDialogCancellation: true,
					title: "FieldLogViewer",
					mainInstruction: "An error occured while reading the log files.",
					content: "For details, including the exact problem and the offending file name and position, please open FieldLogViewer's log file from " +
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

			// LastLogStartItem, IndentLevel and UtcOffset source are only supported for FieldLog items
			FieldLogItemViewModel flItem = item as FieldLogItemViewModel;
			if (flItem != null)
			{
				// Check for new IndentLevel value
				int tryIndentLevel;
				if (flItem.TryGetIndentLevelData(out tryIndentLevel))
				{
					flItem.IndentLevel = tryIndentLevel;
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
				}

				// Use LastLogStartItem and UtcOffset of the previous item from the same session
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
						break;
					}
					prevIndex--;
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
								nextFlItem.IndentLevel = flItem.IndentLevel;
							}
						}

						// All same session also get LastLogStartItem and UtcOffset
						nextFlItem.LastLogStartItem = flItem.LastLogStartItem;

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
					}
				}

				// Publish loaded items to the UI
				using (FL.NewScope("Copying localLogItems to logItems"))
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
			f.DisplayName = "Relevant exceptions";
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
			f.DisplayName = "No trace";
			fcg = new FilterConditionGroupViewModel(f);
			fc = new FilterConditionViewModel(fcg);
			fc.Column = FilterColumn.Priority;
			fc.Comparison = FilterComparison.GreaterOrEqual;
			fc.Value = FieldLogPriority.Checkpoint.ToString();
			fcg.Conditions.Add(fc);
			f.ConditionGroups.Add(fcg);
			Filters.Add(f);
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

		#endregion Other methods

		#region IViewCommandSource members

		private ViewCommandManager viewCommandManager = new ViewCommandManager();
		public ViewCommandManager ViewCommandManager { get { return this.viewCommandManager; } }

		#endregion IViewCommandSource members
	}
}
