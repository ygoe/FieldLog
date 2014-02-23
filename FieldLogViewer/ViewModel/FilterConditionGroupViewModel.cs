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

			Conditions = new ObservableCollection<FilterConditionViewModel>();

			InitializeCommands();

			isEnabled = true;
		}

		#endregion Constructor

		#region Event handlers

		private bool isReordering;

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
				UpdateFirstStatus();
				if (!isReordering)
				{
					OnFilterChanged(true);
				}
			}
			else if (!isReordering)
			{
				parentFilter.ConditionGroups.Remove(this);
			}
		}

		private void UpdateFirstStatus()
		{
			bool isFirst = true;
			foreach (var c in Conditions)
			{
				c.IsFirst = isFirst;
				isFirst = false;
			}
		}

		private void condition_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			CheckConditionsConsistency();
		}

		#endregion Event handlers

		#region Commands

		public DelegateCommand CreateConditionCommand { get; private set; }
		public DelegateCommand ReorderCommand { get; private set; }

		private void InitializeCommands()
		{
			CreateConditionCommand = new DelegateCommand(OnCreateCondition);
			ReorderCommand = new DelegateCommand(OnReorder);
		}

		private void OnCreateCondition()
		{
			this.Conditions.Add(new FilterConditionViewModel(this));
		}

		private void OnReorder()
		{
			isReordering = true;

			Conditions.Sort(c => c.Column);
			
			isReordering = false;

			OnFilterChanged(false);
		}

		#endregion Commands

		#region Data properties

		private ObservableCollection<FilterConditionViewModel> conditions;
		public ObservableCollection<FilterConditionViewModel> Conditions
		{
			get { return conditions; }
			private set
			{
				if (CheckUpdate(value, ref conditions, "Conditions"))
				{
					Conditions.CollectionChanged += Conditions_CollectionChanged;
					UpdateFirstStatus();
				}
			}
		}

		private bool isFirst;
		public bool IsFirst
		{
			get { return isFirst; }
			set { CheckUpdate(value, ref isFirst, "IsFirst", "Margin"); }
		}

		public Thickness Margin
		{
			get { return IsFirst ? new Thickness() : new Thickness(0, 4, 0, 0); }
		}

		public Visibility OrLabelVisibility
		{
			get { return IsFirst ? Visibility.Hidden : Visibility.Visible; }
		}

		private bool isExclude;
		public bool IsExclude
		{
			get { return isExclude; }
			set
			{
				if (CheckUpdate(value, ref isExclude, "IsExclude"))
				{
					OnFilterChanged(true);
				}
			}
		}

		public IEnumerable<ViewModelBase> GroupTypes
		{
			get
			{
				yield return new ValueViewModel<bool>("Include", false);
				yield return new ValueViewModel<bool>("Exclude", true);
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
			CheckConditionsConsistency();
		}

		public string SaveToString()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < Conditions.Count; i++)
			{
				if (i == 0)
				{
					if (IsExclude)
					{
						sb.Append("and not,");
					}
					else
					{
						sb.Append("or,");
					}
					sb.Append(IsEnabled ? "on" : "off");
					sb.Append(",");
				}
				else
				{
					sb.AppendLine();
					sb.Append("and,,");
				}
				sb.Append(Conditions[i].SaveToString());
			}
			return sb.ToString();
		}

		#endregion Loading and saving

		#region Change notification

		public void OnFilterChanged(bool affectsItems)
		{
			parentFilter.OnFilterChanged(affectsItems);
		}

		#endregion Change notification

		#region Filter logic

		/// <summary>
		/// Determines whether the specified item matches all conditions of this condition
		/// group.
		/// </summary>
		/// <param name="item">The item to evaluate.</param>
		/// <returns></returns>
		public bool IsMatch(object item)
		{
			return Conditions.Where(c => c.IsEnabled).All(c => c.IsMatch(item));
		}

		#endregion Filter logic

		#region Duplicate

		public FilterConditionGroupViewModel GetDuplicate(FilterViewModel newParent)
		{
			FilterConditionGroupViewModel newGroup = new FilterConditionGroupViewModel(newParent);
			newGroup.Conditions = new ObservableCollection<FilterConditionViewModel>(this.Conditions.Select(c => c.GetDuplicate(newGroup)));
			newGroup.IsExclude = this.IsExclude;
			newGroup.IsEnabled = this.IsEnabled;
			return newGroup;
		}

		#endregion Duplicate

		#region Consistency checking

		private void CheckConditionsConsistency()
		{
			IsInconsistent = false;
			FilterItemType itemType = FilterItemType.Any;

			// Check the conditions twice because some columns cannot set an item type but only
			// check against one. This is only one group so it cannot interfer with another
			// check-only group. But the conditions must be checked twice so that these check-only
			// columns are certainly also evaluated after all other columns. A smart ordering of
			// the conditions would do as well, but that's more work and not necessary right now.
			foreach (var c in Conditions.Concat(Conditions))
			{
				switch (c.Column)
				{
					case FilterColumn.Type:
						FilterItemType condItemType;
						if (!Enum.TryParse(c.Value, out condItemType)) return;   // Half updated
						if (itemType != condItemType &&
							itemType != FilterItemType.Any &&
							condItemType != FilterItemType.Any)
						{
							IsInconsistent = true;
							return;
						}
						itemType = condItemType;
						break;
					case FilterColumn.TextText:
						if (itemType != FilterItemType.Text &&
							itemType != FilterItemType.DebugOutput &&
							itemType != FilterItemType.Any)
						{
							IsInconsistent = true;
							return;
						}
						itemType = FilterItemType.Text;   // NOTE: Could be DebugOutput as well, but that's unlikely
						break;
					case FilterColumn.TextDetails:
						if (itemType != FilterItemType.Text &&
							itemType != FilterItemType.Any)
						{
							IsInconsistent = true;
							return;
						}
						itemType = FilterItemType.Text;
						break;
					case FilterColumn.DataName:
					case FilterColumn.DataValue:
						if (itemType != FilterItemType.Data &&
							itemType != FilterItemType.Any)
						{
							IsInconsistent = true;
							return;
						}
						itemType = FilterItemType.Data;
						break;
					case FilterColumn.ExceptionType:
					case FilterColumn.ExceptionMessage:
					case FilterColumn.ExceptionCode:
					case FilterColumn.ExceptionData:
					case FilterColumn.ExceptionContext:
						if (itemType != FilterItemType.Exception /*&&
							itemType != FilterItemType.ExceptionRecursive*/ &&
							itemType != FilterItemType.Any)
						{
							IsInconsistent = true;
							return;
						}
						itemType = FilterItemType.Exception;
						break;
					case FilterColumn.ScopeType:
					case FilterColumn.ScopeLevel:
					case FilterColumn.ScopeName:
					case FilterColumn.ScopeIsBackgroundThread:
					case FilterColumn.ScopeIsPoolThread:
						if (itemType != FilterItemType.Scope &&
							itemType != FilterItemType.Any)
						{
							IsInconsistent = true;
							return;
						}
						itemType = FilterItemType.Scope;
						break;
					case FilterColumn.EnvironmentProcessId:
					case FilterColumn.EnvironmentCommandLine:
					case FilterColumn.EnvironmentAppVersion:
					case FilterColumn.EnvironmentUserName:
					case FilterColumn.EnvironmentIsAdministrator:
					case FilterColumn.EnvironmentIsInteractive:
					case FilterColumn.EnvironmentOSType:
					case FilterColumn.EnvironmentOSVersion:
					case FilterColumn.EnvironmentCpuCount:
					case FilterColumn.EnvironmentHostName:
					case FilterColumn.EnvironmentTotalMemory:
					case FilterColumn.EnvironmentScreenDpi:
						if (itemType != FilterItemType.Text &&
							itemType != FilterItemType.Data &&
							itemType != FilterItemType.Exception /*&&
							itemType != FilterItemType.ExceptionRecursive*/ &&
							itemType != FilterItemType.Scope &&
							itemType != FilterItemType.Any)
						{
							IsInconsistent = true;
							return;
						}
						// Cannot set an item type because multiple types are allowed
						break;
					case FilterColumn.EnvironmentCurrentDirectory:
					case FilterColumn.EnvironmentCultureName:
					case FilterColumn.EnvironmentProcessMemory:
					case FilterColumn.EnvironmentPeakProcessMemory:
					case FilterColumn.EnvironmentAvailableMemory:
					case FilterColumn.EnvironmentEnvironmentVariables:
						if (itemType != FilterItemType.Exception /*&&
							itemType != FilterItemType.ExceptionRecursive*/ &&
							itemType != FilterItemType.Scope &&
							itemType != FilterItemType.Any)
						{
							IsInconsistent = true;
							return;
						}
						// Cannot set an item type because two types are allowed
						break;
				}
			}
		}

		#endregion Consistency checking
	}
}
