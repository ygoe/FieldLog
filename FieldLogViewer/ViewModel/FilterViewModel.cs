using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class FilterViewModel : ViewModelBase
	{
		#region Constructors

		public FilterViewModel()
		{
			this.ConditionGroups = new ObservableCollection<FilterConditionGroupViewModel>();
			this.ConditionGroups.CollectionChanged += ConditionGroups_CollectionChanged;
			this.ConditionGroups.Add(new FilterConditionGroupViewModel(this));

			InitializeCommands();
		}

		public FilterViewModel(bool acceptAll)
			: this()
		{
			AcceptAll = acceptAll;
		}

		#endregion Constructors

		#region Event handlers

		private bool isLoading;

		private void ConditionGroups_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (ConditionGroups.Count > 0)
			{
				bool isFirst = true;
				foreach (var cg in ConditionGroups)
				{
					cg.IsFirst = isFirst;
					isFirst = false;
				}
				OnFilterChanged();
			}
			else if (!isLoading)
			{
				Dispatcher.CurrentDispatcher.BeginInvoke((Action) OnCreateConditionGroup, DispatcherPriority.Normal);
			}
		}

		#endregion Event handlers

		#region Commands

		public DelegateCommand CreateConditionGroupCommand { get; private set; }

		private void InitializeCommands()
		{
			CreateConditionGroupCommand = new DelegateCommand(OnCreateConditionGroup);
		}

		private void OnCreateConditionGroup()
		{
			ConditionGroups.Add(new FilterConditionGroupViewModel(this));
		}

		#endregion Commands

		#region Data properties

		public ObservableCollection<FilterConditionGroupViewModel> ConditionGroups { get; private set; }

		public bool AcceptAll { get; private set; }

		#endregion Data properties

		#region Loading and saving

		public void LoadFromString(string data)
		{
			isLoading = true;
			ConditionGroups.Clear();
			IEnumerable<string> lines = data.Split('\n').Select(s => s.Trim('\r'));
			List<string> lineBuffer = new List<string>();
			bool haveName = false;
			foreach (string line in lines)
			{
				if (!haveName)
				{
					// The first line contains only the filter name
					DisplayName = line;
					haveName = true;
				}
				else
				{
					if (lineBuffer.Count > 0 && line.StartsWith("or,"))
					{
						// Load buffer
						FilterConditionGroupViewModel grp = new FilterConditionGroupViewModel(this);
						grp.LoadFromString(lineBuffer);
						ConditionGroups.Add(grp);
						lineBuffer.Clear();
					}
					// Save line to buffer
					lineBuffer.Add(line);
				}
			}
			// Load buffer
			FilterConditionGroupViewModel grp2 = new FilterConditionGroupViewModel(this);
			grp2.LoadFromString(lineBuffer);
			ConditionGroups.Add(grp2);
			isLoading = false;
		}

		public string SaveToString()
		{
			return DisplayName + Environment.NewLine +
				ConditionGroups.Select(c => c.SaveToString()).Aggregate((a, b) => a + Environment.NewLine + b);
		}

		#endregion Loading and saving

		#region Change notification

		/// <summary>
		/// Raised when the filter definition has changed.
		/// </summary>
		public event Action FilterChanged;

		/// <summary>
		/// Raises the FilterChanged event.
		/// </summary>
		public void OnFilterChanged()
		{
			var handler = FilterChanged;
			if (handler != null)
			{
				handler();
			}
		}

		#endregion Change notification

		#region Filter logic

		/// <summary>
		/// Determines whether the specified log item matches any condition group of this filter.
		/// </summary>
		/// <param name="item">The log item to evaluate.</param>
		/// <returns></returns>
		public bool IsMatch(FieldLogItemViewModel item)
		{
			if (AcceptAll) return true;
			
			return ConditionGroups.Any(c => c.IsMatch(item));
		}

		#endregion Filter logic
	}
}
