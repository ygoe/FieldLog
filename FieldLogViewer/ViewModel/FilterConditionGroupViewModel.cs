using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class FilterConditionGroupViewModel : ViewModelBase
	{
		#region Private data

		private FilterViewModel parentFilter;

		#endregion Private data

		#region Constructor

		public FilterConditionGroupViewModel(FilterViewModel parentFilter)
		{
			this.parentFilter = parentFilter;

			this.Conditions = new ObservableCollection<FilterConditionViewModel>();
			this.Conditions.CollectionChanged += Conditions_CollectionChanged;
			this.Conditions.Add(new FilterConditionViewModel(this));

			InitializeCommands();
		}

		#endregion Constructor

		#region Event handlers

		private void Conditions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (FilterConditionViewModel c in e.OldItems)
				{
					c.PropertyChanged -= condition_PropertyChanged;
				}
			}
			if (e.NewItems != null)
			{
				foreach (FilterConditionViewModel c in e.NewItems)
				{
					c.PropertyChanged += condition_PropertyChanged;
				}
			}
			
			if (Conditions.Count > 0)
			{
				bool isFirst = true;
				foreach (var c in Conditions)
				{
					c.IsFirst = isFirst;
					isFirst = false;
				}
				OnFilterChanged();
			}
			else
			{
				parentFilter.ConditionGroups.Remove(this);
			}
		}

		private void condition_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			IsInconsistent = false;

			FilterItemType itemType = FilterItemType.Any;
			bool itemTypeSet = false;
			foreach (var c in Conditions.Where(c => c.Column == FilterColumn.Type))
			{
				if (itemTypeSet)
				{
					IsInconsistent = true;
					return;
				}
				if (!Enum.TryParse(c.Value, out itemType)) return;   // Half updated
			}

			foreach (var c in Conditions.Where(c => c.Column != FilterColumn.Type))
			{
				switch (c.Column)
				{
					case FilterColumn.TextText:
					case FilterColumn.TextDetails:
						if (itemType != FilterItemType.Text)
						{
							IsInconsistent = true;
							return;
						}
						break;
					case FilterColumn.DataName:
					case FilterColumn.DataValue:
						if (itemType != FilterItemType.Data)
						{
							IsInconsistent = true;
							return;
						}
						break;
					case FilterColumn.ExceptionType:
					case FilterColumn.ExceptionMessage:
					case FilterColumn.ExceptionCode:
					case FilterColumn.ExceptionData:
					case FilterColumn.ExceptionContext:
						if (itemType != FilterItemType.Exception /*&&
							itemType != FilterItemType.ExceptionRecursive*/)
						{
							IsInconsistent = true;
							return;
						}
						break;
					case FilterColumn.ScopeType:
					case FilterColumn.ScopeLevel:
					case FilterColumn.ScopeName:
					case FilterColumn.ScopeIsBackgroundThread:
					case FilterColumn.ScopeIsPoolThread:
						if (itemType != FilterItemType.Scope)
						{
							IsInconsistent = true;
							return;
						}
						break;
					case FilterColumn.EnvironmentCultureName:
					case FilterColumn.EnvironmentIsShuttingDown:
					case FilterColumn.EnvironmentCurrentDirectory:
					case FilterColumn.EnvironmentEnvironmentVariables:
					case FilterColumn.EnvironmentUserName:
					case FilterColumn.EnvironmentIsInteractive:
					case FilterColumn.EnvironmentCommandLine:
					case FilterColumn.EnvironmentAppVersion:
					case FilterColumn.EnvironmentProcessMemory:
					case FilterColumn.EnvironmentPeakProcessMemory:
					case FilterColumn.EnvironmentTotalMemory:
					case FilterColumn.EnvironmentAvailableMemory:
						if (itemType != FilterItemType.Exception /*&&
							itemType != FilterItemType.ExceptionRecursive*/ &&
							itemType != FilterItemType.Scope)
						{
							IsInconsistent = true;
							return;
						}
						break;
				}
			}
		}

		#endregion Event handlers

		#region Commands

		public DelegateCommand CreateConditionCommand { get; private set; }

		private void InitializeCommands()
		{
			CreateConditionCommand = new DelegateCommand(OnCreateCondition);
		}

		private void OnCreateCondition()
		{
			this.Conditions.Add(new FilterConditionViewModel(this));
		}

		#endregion Commands

		#region Data properties

		public ObservableCollection<FilterConditionViewModel> Conditions { get; private set; }

		private bool isFirst;
		public bool IsFirst
		{
			get { return isFirst; }
			set { CheckUpdate(value, ref isFirst, "IsFirst", "Margin", "OrLabelVisibility"); }
		}

		public Thickness Margin
		{
			get { return IsFirst ? new Thickness() : new Thickness(0, 4, 0, 0); }
		}

		public Visibility OrLabelVisibility
		{
			get { return IsFirst ? Visibility.Hidden : Visibility.Visible; }
		}

		private bool isInconsistent;
		public bool IsInconsistent
		{
			get { return isInconsistent; }
			set { CheckUpdate(value, ref isInconsistent, "IsInconsistent", "Background"); }
		}

		public Brush Background
		{
			get { return new SolidColorBrush(IsInconsistent ? Color.FromArgb(32, 220, 20, 0) : Color.FromArgb(16, 0, 0, 0)); }
		}

		#endregion Data properties

		#region Loading and saving

		public void LoadFromString(IEnumerable<string> lines)
		{
			Conditions.Clear();
			foreach (string line in lines)
			{
				FilterConditionViewModel cond = new FilterConditionViewModel(this);
				cond.LoadFromString(line);
				Conditions.Add(cond);
			}
		}

		public string SaveToString()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < Conditions.Count; i++)
			{
				if (i == 0)
				{
					sb.Append("or,");
				}
				else
				{
					sb.AppendLine();
					sb.Append("and,");
				}
				sb.Append(Conditions[i].SaveToString());
			}
			return sb.ToString();
		}

		#endregion Loading and saving

		#region Change notification

		public void OnFilterChanged()
		{
			parentFilter.OnFilterChanged();
		}

		#endregion Change notification

		#region Filter logic

		/// <summary>
		/// Determines whether the specified log item matches all conditions of this condition
		/// group.
		/// </summary>
		/// <param name="item">The log item to evaluate.</param>
		/// <returns></returns>
		public bool IsMatch(FieldLogItemViewModel item)
		{
			return Conditions.All(c => c.IsMatch(item));
		}

		#endregion Filter logic
	}
}
