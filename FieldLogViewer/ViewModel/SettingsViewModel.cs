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
			if (MainViewModel.Instance.SelectedFilter != null)
			{
				var filterToDelete = MainViewModel.Instance.SelectedFilter;
				MainViewModel.Instance.SelectedFilter = MainViewModel.Instance.Filters[0];
				MainViewModel.Instance.Filters.Remove(filterToDelete);
				if (MainViewModel.Instance.Filters.Count == 0)
				{
					OnCreateFilter();
				}
			}
		}

		#endregion Commands

		#region Data properties

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

		#endregion Data properties
	}
}
