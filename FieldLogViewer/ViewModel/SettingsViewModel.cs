using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class SettingsViewModel : ViewModelBase
	{
		public SettingsViewModel()
		{
			InitializeCommands();

			MainViewModel.Instance.LinkProperty(vm => vm.SelectedFilter, v => SelectedFilter = v);
			this.LinkProperty(vm => vm.SelectedFilter, v => MainViewModel.Instance.SelectedFilter = v);
		}

		#region Commands

		public DelegateCommand CreateFilterCommand { get; private set; }
		public DelegateCommand DuplicateFilterCommand { get; private set; }
		public DelegateCommand RenameFilterCommand { get; private set; }
		public DelegateCommand DeleteFilterCommand { get; private set; }

		private void InitializeCommands()
		{
			CreateFilterCommand = new DelegateCommand(OnCreateFilter);
			DuplicateFilterCommand = new DelegateCommand(OnDuplicateFilter);
			RenameFilterCommand = new DelegateCommand(OnRenameFilter);
			DeleteFilterCommand = new DelegateCommand(OnDeleteFilter);
		}

		private void OnCreateFilter()
		{
			MainViewModel.Instance.Filters.Add(new FilterViewModel() { DisplayName = DateTime.Now.ToString() });
		}

		private void OnDuplicateFilter()
		{
			// TODO
		}

		private void OnRenameFilter()
		{
			// TODO: Replace ComboBox and buttons with TextBox and OK/Cancel buttons (?)
		}

		private void OnDeleteFilter()
		{
			if (MainViewModel.Instance.SelectedFilter != null)
			{
				MainViewModel.Instance.Filters.Remove(MainViewModel.Instance.SelectedFilter);
				if (MainViewModel.Instance.Filters.Count == 0)
				{
					OnCreateFilter();
				}
				MainViewModel.Instance.SelectedFilter = MainViewModel.Instance.Filters[0];
			}
		}

		#endregion Commands

		#region Data properties

		public ObservableCollection<FilterViewModel> Filters
		{
			get { return MainViewModel.Instance.Filters; }
		}

		private FilterViewModel selectedFilter;
		public FilterViewModel SelectedFilter
		{
			get { return selectedFilter; }
			set { CheckUpdate(value, ref selectedFilter, "SelectedFilter"); }
		}

		#endregion Data properties

	}
}
