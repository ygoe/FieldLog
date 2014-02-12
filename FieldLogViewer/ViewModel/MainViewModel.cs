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

		private ObservableCollection<LogItemViewModelBase> logItems = new ObservableCollection<LogItemViewModelBase>();
		private CollectionViewSource sortedFilters = new CollectionViewSource();
		private CollectionViewSource filteredLogItems = new CollectionViewSource();
		private string loadedBasePath;
		private FieldLogFileGroupReader logFileGroupReader;
		private bool isLiveStopped = true;

		#endregion Private data

		#region Constructors

		public MainViewModel()
		{
			Instance = this;

			InitializeCommands();

			UpdateWindowTitle();

			this.BindProperty(vm => vm.IsDebugMonitorActive, AppSettings.Instance, s => s.IsDebugMonitorActive);
			this.BindProperty(vm => vm.ShowRelativeTime, AppSettings.Instance, s => s.ShowRelativeTime);
			this.BindProperty(vm => vm.IsLiveScrollingEnabled, AppSettings.Instance, s => s.IsLiveScrollingEnabled);
			this.BindProperty(vm => vm.IsSoundEnabled, AppSettings.Instance, s => s.IsSoundEnabled);
			this.BindProperty(vm => vm.IsWindowOnTop, AppSettings.Instance, s => s.IsWindowOnTop);
			this.BindProperty(vm => vm.IndentSize, AppSettings.Instance, s => s.IndentSize);
			
			Filters = new ObservableCollection<FilterViewModel>();
			Filters.ForNewOld(
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

			foreach (string s in AppSettings.Instance.Filters)
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
			FilterViewModel selectedFilterVM = Filters.FirstOrDefault(f => f.DisplayName == AppSettings.Instance.SelectedFilter);
			if (selectedFilterVM != null)
			{
				SelectedFilter = selectedFilterVM;
			}
			else
			{
				SelectedFilter = Filters[0];
			}

			sortedFilters.Source = Filters;
			sortedFilters.SortDescriptions.Add(new SortDescription("AcceptAll", ListSortDirection.Descending));
			sortedFilters.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));

			filteredLogItems.Source = logItems;
			filteredLogItems.Filter += filteredLogItems_Filter;

			Dispatcher disp = Dispatcher.CurrentDispatcher;
			DebugMonitor.MessageReceived += (pid, text) =>
			{
				var itemVM = new DebugMessageViewModel(pid, text);

				disp.BeginInvoke(
					new Action<LogItemViewModelBase>(this.InsertNewLogItem),
					itemVM);
			};
		}

		#endregion Constructors

		#region Public properties

		#endregion Public properties

		#region Commands

		public DelegateCommand LoadLogCommand { get; private set; }
		public DelegateCommand StopLiveCommand { get; private set; }
		public DelegateCommand ClearCommand { get; private set; }
		public DelegateCommand LoadMapCommand { get; private set; }
		public DelegateCommand DecreaseIndentSizeCommand { get; private set; }
		public DelegateCommand IncreaseIndentSizeCommand { get; private set; }
		public DelegateCommand SettingsCommand { get; private set; }

		private void InitializeCommands()
		{
			LoadLogCommand = new DelegateCommand(OnLoadLog, CanLoadLog);
			StopLiveCommand = new DelegateCommand(OnStopLive, CanStopLive);
			ClearCommand = new DelegateCommand(OnClear, CanClear);
			LoadMapCommand = new DelegateCommand(OnLoadMap);
			DecreaseIndentSizeCommand = new DelegateCommand(OnDecreaseIndentSize, CanDecreaseIndentSize);
			IncreaseIndentSizeCommand = new DelegateCommand(OnIncreaseIndentSize, CanIncreaseIndentSize);
			SettingsCommand = new DelegateCommand(OnSettings);
		}

		private void InvalidateCommands()
		{
			LoadLogCommand.RaiseCanExecuteChanged();
			StopLiveCommand.RaiseCanExecuteChanged();
			ClearCommand.RaiseCanExecuteChanged();
			LoadMapCommand.RaiseCanExecuteChanged();
			SettingsCommand.RaiseCanExecuteChanged();
		}

		private bool CanLoadLog()
		{
			return !isLoadingFiles;
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
			return !isLoadingFiles && !isLiveStopped;
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
			return !isLoadingFiles;
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
			return IndentSize > 4;
		}

		private void OnDecreaseIndentSize()
		{
			IndentSize -= 4;
			if (IndentSize < 4)
			{
				IndentSize = 4;
			}
		}

		private bool CanIncreaseIndentSize()
		{
			return IndentSize < 32;
		}

		private void OnIncreaseIndentSize()
		{
			IndentSize += 4;
			if (IndentSize > 32)
			{
				IndentSize = 32;
			}
		}

		private void OnSettings()
		{
			SettingsWindow win = new SettingsWindow();
			SettingsViewModel vm = new SettingsViewModel();
			win.DataContext = vm;
			win.Owner = MainWindow.Instance;
			win.Show();
		}

		#endregion Commands

		#region Data properties

		public bool IsDebugMonitorActive
		{
			get
			{
				return DebugMonitor.IsActive;
			}
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

		private bool isLiveScrollingEnabled;
		public bool IsLiveScrollingEnabled
		{
			get
			{
				return isLiveScrollingEnabled;
			}
			set
			{
				if (CheckUpdate(value, ref isLiveScrollingEnabled, "IsLiveScrollingEnabled"))
				{
					if (isLiveScrollingEnabled)
					{
						ViewCommandManager.Invoke("ScrollToEnd");
					}
				}
			}
		}

		private bool isSoundEnabled;
		public bool IsSoundEnabled
		{
			get
			{
				return isSoundEnabled;
			}
			set
			{
				if (CheckUpdate(value, ref isSoundEnabled, "IsSoundEnabled"))
				{
					// TODO: Save to settings? Load from settings?
				}
			}
		}

		public bool IsWindowOnTop
		{
			get
			{
				return MainWindow.Instance.Topmost;
			}
			set
			{
				if (value != MainWindow.Instance.Topmost)
				{
					MainWindow.Instance.Topmost = value;
					OnPropertyChanged("IsWindowOnTop");
				}
			}
		}

		public ObservableCollection<LogItemViewModelBase> LogItems
		{
			get { return this.logItems; }
		}

		public ICollectionView FilteredLogItems
		{
			get
			{
				return filteredLogItems.View;
			}
		}

		public ObservableCollection<FilterViewModel> Filters { get; private set; }

		public ICollectionView SortedFilters
		{
			get
			{
				return sortedFilters.View;
			}
		}

		private FilterViewModel selectedFilter;
		public FilterViewModel SelectedFilter
		{
			get { return selectedFilter; }
			set
			{
				if (CheckUpdate(value, ref selectedFilter, "SelectedFilter"))
				{
					ViewCommandManager.Invoke("SaveScrolling");
					RefreshLogItemsFilterView();
					ViewCommandManager.Invoke("RestoreScrolling");
					if (selectedFilter != null)
					{
						AppSettings.Instance.SelectedFilter = selectedFilter.DisplayName;
					}
					else
					{
						AppSettings.Instance.SelectedFilter = "";
					}
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
					if (isLoadingFiles)
					{
						filteredLogItems.Source = null;
					}
					else
					{
						filteredLogItems.Source = logItems;
						RefreshLogItemsFilterView();
					}
					OnPropertyChanged("FilteredLogItems");
					OnPropertyChanged("LogItemsVisibility");
					OnPropertyChanged("ItemDetailsVisibility");
					OnPropertyChanged("LoadingMsgVisibility");
					InvalidateCommands();
				}
			}
		}

		public Visibility LogItemsVisibility
		{
			get
			{
				return !isLoadingFiles ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public Visibility ItemDetailsVisibility
		{
			get
			{
				return !isLoadingFiles ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public Visibility LoadingMsgVisibility
		{
			get
			{
				return isLoadingFiles ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		private int indentSize;
		public int IndentSize
		{
			get { return indentSize; }
			set
			{
				if (CheckUpdate(value, ref indentSize, "IndentSize"))
				{
					DecreaseIndentSizeCommand.RaiseCanExecuteChanged();
					IncreaseIndentSizeCommand.RaiseCanExecuteChanged();
				}
			}
		}

		private bool highlightSameThread = true;
		public bool HighlightSameThread
		{
			get { return highlightSameThread; }
			set { CheckUpdate(value, ref highlightSameThread, "HighlightSameThread"); }
		}

		private bool showRelativeTime;
		public bool ShowRelativeTime
		{
			get { return showRelativeTime; }
			set { CheckUpdate(value, ref showRelativeTime, "ShowRelativeTime"); }
		}

		public int SelectionDummy
		{
			get { return 0; }
		}

		#endregion Data properties

		#region Log items filter

		/// <summary>
		/// Filter implementation for the collection view returned by FilteredLogItems.
		/// </summary>
		private void filteredLogItems_Filter(object sender, FilterEventArgs e)
		{
			if (SelectedFilter != null)
			{
				e.Accepted = SelectedFilter.IsMatch(e.Item);
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
			AppSettings.Instance.Filters = Filters
				.Where(f => !f.AcceptAll)
				.Select(f => f.SaveToString())
				.Where(s => !string.IsNullOrEmpty(s))
				.ToArray();
		}

		private void RefreshLogItemsFilterView()
		{
			if (filteredLogItems.View != null)
			{
				filteredLogItems.View.Refresh();
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

		public Task OpenFiles(string basePath)
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
				return null;
			}
			
			ViewCommandManager.InvokeLoaded("StartedReadingFiles");
			IsLoadingFiles = true;

			this.logItems.Clear();
			Dispatcher disp = Dispatcher.CurrentDispatcher;

			isLiveStopped = false;
			StopLiveCommand.RaiseCanExecuteChanged();

			List<LogItemViewModelBase> localLogItems = new List<LogItemViewModelBase>();
			object localLogItemsLock = new object();

			loadedBasePath = basePath;
			UpdateWindowTitle();

			return Task.Factory.StartNew(() =>
			{
				EventWaitHandle readWaitHandle = new AutoResetEvent(false);
				readWaitHandle.WaitAction(() => disp.Invoke((Action) delegate
				{
					// Lock the local list so that no item loaded directly afterwards will get lost
					// while we're still preparing the loaded items list to be pushed to the UI
					lock (localLogItemsLock)
					{
						// Apply scope-based indenting to all items now
						Dictionary<int, int> threadLevels = new Dictionary<int, int>();
						foreach (var item in localLogItems)
						{
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
							}
							else
							{
								FieldLogItemViewModel flItem = item as FieldLogItemViewModel;
								if (flItem != null)
								{
									int level;
									if (threadLevels.TryGetValue(flItem.ThreadId, out level))
									{
										flItem.IndentLevel = level;
									}
								}
							}
						}
						
						// Publish loaded items to the UI
						this.logItems = new ObservableCollection<LogItemViewModelBase>(localLogItems);
						localLogItems = null;
					}
					// Notify the UI to make it show the new list of items.
					// From now on, newly loaded items are added one by one to the collection that
					// is already bound to the UI, so the new items will become visible.
					OnPropertyChanged("LogItems");
					IsLoadingFiles = false;
					UpdateWindowTitle();
					ViewCommandManager.InvokeLoaded("FinishedReadingFiles");
				}));
				
				logFileGroupReader = new FieldLogFileGroupReader(basePath, readWaitHandle);
				List<FieldLogScopeItem> seenScopeItems = new List<FieldLogScopeItem>();
				while (true)
				{
					FieldLogItem item = logFileGroupReader.ReadLogItem();
					if (item == null) break;
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

					lock (localLogItemsLock)
					{
						if (localLogItems != null)
						{
							localLogItems.InsertSorted(itemVM, new Comparison<LogItemViewModelBase>((a, b) => a.CompareTo(b)));
						}
						else
						{
							disp.BeginInvoke(
								new Action<LogItemViewModelBase>(this.InsertNewLogItem),
								itemVM);
						}
					}
				}
			});
		}

		private void InsertNewLogItem(LogItemViewModelBase item)
		{
			int newIndex = logItems.InsertSorted(item, (a, b) => a.CompareTo(b));

			// IndentLevel is only supported for FieldLog items
			FieldLogItemViewModel flItem = item as FieldLogItemViewModel;
			if (flItem != null)
			{
				FieldLogScopeItemViewModel scope = item as FieldLogScopeItemViewModel;
				if (scope != null)
				{
					// Use new IndentLevel from Scope item
					if (scope.Type == FieldLogScopeType.Enter)
					{
						scope.IndentLevel = scope.Level - 1;
					}
					else
					{
						scope.IndentLevel = scope.Level;
					}
				}
				else
				{
					// Use IndentLevel of the previous item in the same session & thread
					int prevIndex = newIndex - 1;
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
			
				// Update all items after the inserted item
				for (int index = newIndex + 1; index < logItems.Count; index++)
				{
					FieldLogItemViewModel nextFlItem = logItems[index] as FieldLogItemViewModel;
					if (nextFlItem != null &&
						nextFlItem.SessionId == flItem.SessionId &&
						nextFlItem.ThreadId == flItem.ThreadId)
					{
						FieldLogScopeItemViewModel nextScope = nextFlItem as FieldLogScopeItemViewModel;
						if (nextScope != null)
						{
							// The next Scope item already had a reference level, stop here
							break;
						}
						nextFlItem.IndentLevel = flItem.IndentLevel;
					}
				}
			}
		}

		#endregion Log file loading

		#region Other methods

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
