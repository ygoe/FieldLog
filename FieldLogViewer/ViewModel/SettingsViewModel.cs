using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class SettingsViewModel : ViewModelBase
	{
		#region Constructor

		public SettingsViewModel()
		{
			InitializeCommands();

			this.BindProperty(vm => vm.ItemTimeMode, AppSettings.Instance, s => s.ItemTimeMode);
			this.BindProperty(vm => vm.ShowWarningsErrorsInScrollBar, AppSettings.Instance, s => s.ShowWarningsErrorsInScrollBar);
			this.BindProperty(vm => vm.ShowSelectionInScrollBar, AppSettings.Instance, s => s.ShowSelectionInScrollBar);

			this.BindProperty(vm => vm.SelectedFilter, MainViewModel.Instance, vm => vm.SelectedFilter);
		}

		#endregion Constructor

		#region Commands

		public DelegateCommand CreateFilterCommand { get; private set; }
		public DelegateCommand DuplicateFilterCommand { get; private set; }
		public DelegateCommand DeleteFilterCommand { get; private set; }

		private void InitializeCommands()
		{
			CreateFilterCommand = new DelegateCommand(OnCreateFilter);
			DuplicateFilterCommand = new DelegateCommand(OnDuplicateFilter, CanDuplicateFilter);
			DeleteFilterCommand = new DelegateCommand(OnDeleteFilter, CanDeleteFilter);
		}

		private void OnCreateFilter()
		{
			var newFilter = FilterViewModel.CreateNew();
			MainViewModel.Instance.Filters.Add(newFilter);
			MainViewModel.Instance.SelectedFilter = newFilter;
		}

		private bool CanDuplicateFilter()
		{
			return SelectedFilter != null && !SelectedFilter.AcceptAll;
		}

		private void OnDuplicateFilter()
		{
			var newFilter = SelectedFilter.GetDuplicate();
			MainViewModel.Instance.Filters.Add(newFilter);
			MainViewModel.Instance.SelectedFilter = newFilter;
		}

		private bool CanDeleteFilter()
		{
			return SelectedFilter != null && !SelectedFilter.AcceptAll;
		}

		private void OnDeleteFilter()
		{
			if (SelectedFilter != null)
			{
				MainViewModel.Instance.DeleteFilterCommand.Execute();
			}
		}

		#endregion Commands

		#region Data properties

		#region General tab

		private ItemTimeType itemTimeMode;
		public ItemTimeType ItemTimeMode
		{
			get { return itemTimeMode; }
			set { CheckUpdate(value, ref itemTimeMode, "ItemTimeMode"); }
		}

		public IEnumerable<ValueViewModel<ItemTimeType>> AvailableItemTimeModes
		{
			get
			{
				return new ValueViewModel<ItemTimeType>[]
				{
					new ValueViewModel<ItemTimeType>("UTC", ItemTimeType.Utc),
					new ValueViewModel<ItemTimeType>("Local system", ItemTimeType.Local),
					new ValueViewModel<ItemTimeType>("Remote system", ItemTimeType.Remote),
				};
			}
		}

		private bool showWarningsErrorsInScrollBar;
		public bool ShowWarningsErrorsInScrollBar
		{
			get { return showWarningsErrorsInScrollBar; }
			set { CheckUpdate(value, ref showWarningsErrorsInScrollBar, "ShowWarningsErrorsInScrollBar"); }
		}

		private bool showSelectionInScrollBar;
		public bool ShowSelectionInScrollBar
		{
			get { return showSelectionInScrollBar; }
			set { CheckUpdate(value, ref showSelectionInScrollBar, "ShowSelectionInScrollBar"); }
		}

		#endregion General tab

		#region Filters tab

		public ObservableCollection<FilterViewModel> Filters
		{
			get { return MainViewModel.Instance.Filters; }
		}

		public ICollectionView SortedFilters
		{
			get { return MainViewModel.Instance.SortedFilters; }
		}

		private FilterViewModel selectedFilter;
		public FilterViewModel SelectedFilter
		{
			get { return selectedFilter; }
			set
			{
				if (CheckUpdate(value, ref selectedFilter, "SelectedFilter"))
				{
					DuplicateFilterCommand.RaiseCanExecuteChanged();
					DeleteFilterCommand.RaiseCanExecuteChanged();
				}
			}
		}

		#endregion Filters tab

		#endregion Data properties
	}
}
