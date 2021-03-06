﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.FieldLogViewer.ViewModels
{
	internal class SettingsViewModel : ViewModelBase
	{
		#region Constructor

		public SettingsViewModel()
		{
			this.BindProperty(vm => vm.SelectedFilter, MainViewModel.Instance, vm => vm.SelectedFilter);
		}

		#endregion Constructor

		#region Commands

		public DelegateCommand CreateFilterCommand { get; private set; }
		public DelegateCommand DuplicateFilterCommand { get; private set; }
		public DelegateCommand DeleteFilterCommand { get; private set; }
		public DelegateCommand ResetFiltersCommand { get; private set; }

		protected override void InitializeCommands()
		{
			CreateFilterCommand = new DelegateCommand(OnCreateFilter);
			DuplicateFilterCommand = new DelegateCommand(OnDuplicateFilter, CanDuplicateFilter);
			DeleteFilterCommand = new DelegateCommand(OnDeleteFilter, CanDeleteFilter);
			ResetFiltersCommand = new DelegateCommand(OnResetFilters);
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

		private void OnResetFilters()
		{
			if (App.YesNoQuestion("Do you want to delete all existing filters and recreate the default filters?"))
			{
				MainViewModel.Instance.ResetFilters();
			}
		}

		#endregion Commands

		#region Data properties

		public IAppSettings Settings
		{
			get { return App.Settings; }
		}

		#region General tab

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
					DuplicateFilterCommand.RaiseCanExecuteChanged();
					DeleteFilterCommand.RaiseCanExecuteChanged();
				}
			}
		}

		#endregion Filters tab

		#endregion Data properties
	}
}
