﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Threading;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.FieldLogViewer.ViewModels
{
	internal class FilterViewModel : ViewModelBase
	{
		#region Private data

		private string fixedDisplayName;

		#endregion Private data

		#region Constructors

		public FilterViewModel()
		{
			ConditionGroups = new ObservableCollection<FilterConditionGroupViewModel>();
		}

		public FilterViewModel(bool acceptAll)
			: this()
		{
			AcceptAll = acceptAll;

			if (AcceptAll)
			{
				fixedDisplayName = "(Show all)";
				DisplayName = fixedDisplayName;
			}
		}

		#endregion Constructors

		#region Overridden methods

		public override string ToString()
		{
			return GetType().Name + ": " + DisplayName + " (" + ConditionGroups.Count + " condition groups)";
		}

		#endregion Overridden methods

		#region Event handlers

		private bool isLoading;
		private bool isReordering;

		private void ConditionGroups_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			if (ConditionGroups.Count > 0)
			{
				UpdateFirstStatus();
				OnFilterChanged(!isReordering);
			}
			else if (!isLoading && !isReordering)
			{
				// Later add an empty condition group, if none has been added until then
				Dispatcher.CurrentDispatcher.BeginInvoke(
					new Action(() =>
						{
							if (ConditionGroups.Count == 0) OnCreateConditionGroup();
						}),
					DispatcherPriority.Normal);
			}
		}

		private void UpdateFirstStatus()
		{
			bool isFirst = true;
			foreach (var cg in ConditionGroups)
			{
				cg.IsFirst = isFirst;
				isFirst = false;
			}
		}

		#endregion Event handlers

		#region Commands

		public DelegateCommand CreateConditionGroupCommand { get; private set; }
		public DelegateCommand ReorderCommand { get; private set; }

		protected override void InitializeCommands()
		{
			CreateConditionGroupCommand = new DelegateCommand(OnCreateConditionGroup);
			ReorderCommand = new DelegateCommand(OnReorder);
		}

		private void OnCreateConditionGroup()
		{
			FilterConditionGroupViewModel cg = new FilterConditionGroupViewModel(this);
			cg.CreateConditionCommand.Execute();
			ConditionGroups.Add(cg);
		}

		private void OnReorder()
		{
			isReordering = true;

			ConditionGroups.ForEachSafe(grp =>
			{
				if (!grp.IsExclude)
				{
					if (ConditionGroups.Before(grp).Any(cg => cg.IsExclude))
					{
						ConditionGroups.Remove(grp);
						int count = ConditionGroups.Count(cg => !cg.IsExclude);
						ConditionGroups.Insert(count, grp);
					}
				}
				else // grp.IsExclude
				{
					if (ConditionGroups.After(grp).Any(cg => !cg.IsExclude))
					{
						ConditionGroups.Remove(grp);
						ConditionGroups.Add(grp);
					}
				}
			});

			ConditionGroups.ForEach(cg => cg.ReorderCommand.Execute());

			isReordering = false;
		}

		#endregion Commands

		#region Data properties

		public ObservableCollection<FilterConditionGroupViewModel> ConditionGroups
		{
			get
			{
				return GetValue<ObservableCollection<FilterConditionGroupViewModel>>("ConditionGroups");
			}
			private set
			{
				if (SetValue(value, "ConditionGroups"))
				{
					ConditionGroups.CollectionChanged += ConditionGroups_CollectionChanged;
					UpdateFirstStatus();
				}
			}
		}

		public bool AcceptAll { get; private set; }

		/// <summary>
		/// Gets or sets the filter that was selected before this quick filter was created and selected.
		/// </summary>
		public FilterViewModel QuickPreviousFilter { get; set; }

		/// <summary>
		/// Gets or sets the time of the creation or last modification of this quick filter.
		/// </summary>
		public DateTime QuickModifiedTime { get; set; }

		/// <summary>
		/// Gets a value indicating whether this is a quick filter.
		/// </summary>
		public bool IsQuickFilter { get { return QuickModifiedTime != DateTime.MinValue; } }

		/// <summary>
		/// Gets or sets a value indicating whether the filter has a problem and cannot be used.
		/// </summary>
		public bool IsFaulty { get; set; }

		#endregion Data properties

		#region Loading and saving

		public void LoadFromString(string data)
		{
			isLoading = true;

			ConditionGroups.Clear();
			IEnumerable<string> lines = data.Split('\n').Select(s => s.Trim('\r'));
			List<string> lineBuffer = new List<string>();
			bool lineBufferIsExclude = false;
			bool lineBufferIsEnabled = true;
			bool haveName = false;
			foreach (string line in lines)
			{
				if (!haveName)
				{
					// The first line contains the "quick filter" flag and filter name
					string[] chunks = line.Split(new char[] { ',' }, 2);
					if (chunks[0] == "qf")
						QuickModifiedTime = DateTime.UtcNow.AddYears(-1);
					DisplayName = chunks[1];
					haveName = true;
				}
				else
				{
					string[] chunks = line.Split(new char[] { ',' }, 3);
					if (chunks[0] == "or" || chunks[0] == "and not")
					{
						// New condition group starts
						if (lineBuffer.Count > 0)
						{
							// Create condition group from buffer of previous lines
							FilterConditionGroupViewModel grp = new FilterConditionGroupViewModel(this);
							grp.LoadFromString(lineBuffer);
							grp.IsExclude = lineBufferIsExclude;
							grp.IsEnabled = lineBufferIsEnabled;
							ConditionGroups.Add(grp);
							lineBuffer.Clear();
						}
						// Remember type for the upcoming condition group
						lineBufferIsExclude = chunks[0] == "and not";
						lineBufferIsEnabled = chunks[1] == "on";
					}
					// Save line to buffer
					lineBuffer.Add(line);
				}
			}
			// Create last condition group from buffer of previous lines
			FilterConditionGroupViewModel grp2 = new FilterConditionGroupViewModel(this);
			grp2.LoadFromString(lineBuffer);
			grp2.IsExclude = lineBufferIsExclude;
			grp2.IsEnabled = lineBufferIsEnabled;
			ConditionGroups.Add(grp2);

			isLoading = false;
		}

		public string SaveToString()
		{
			if (!ConditionGroups.Any()) return null;   // Intermediate state, should not be saved

			return (IsQuickFilter ? "qf" : "") + "," + DisplayName + Environment.NewLine +
				ConditionGroups.Select(c => c.SaveToString()).Aggregate((a, b) => a + Environment.NewLine + b);
		}

		#endregion Loading and saving

		#region Change notification

		/// <summary>
		/// Raised when the filter definition has changed.
		/// </summary>
		public event Action<bool> FilterChanged;

		/// <summary>
		/// Raises the FilterChanged event.
		/// </summary>
		public void OnFilterChanged(bool affectsItems)
		{
			IsFaulty = false;
			var handler = FilterChanged;
			if (handler != null)
			{
				handler(affectsItems);
			}
		}

		protected override void OnDisplayNameChanged()
		{
			if (AcceptAll)
			{
				// The accept-all filter cannot be renamed. Revert the change.
				TaskHelper.WhenLoaded(() => { DisplayName = fixedDisplayName; });
			}
			else
			{
				OnFilterChanged(false);
			}
		}

		#endregion Change notification

		#region Filter logic

		/// <summary>
		/// Determines whether the specified item matches any condition group of this filter.
		/// </summary>
		/// <param name="item">The item to evaluate.</param>
		/// <returns></returns>
		public bool IsMatch(object item)
		{
			// Don't even start thinking if this is the "Show all" filter.
			if (AcceptAll) return true;

			// Do not consider condition groups that are either disabled entirely or that do not
			// contain any condition that is enabled.
			var ActiveConditionGroups = ConditionGroups.Where(cg => cg.IsEnabled && cg.Conditions.Any(c => c.IsEnabled));

			// The item matches if it matches any non-excluding condition group or there are no
			// non-excluding condition groups at all, and only if it does not match any excluding
			// condition group.
			return ActiveConditionGroups.Where(cg => !cg.IsExclude).AnyOrTrue(c => c.IsMatch(item)) &&
				!ActiveConditionGroups.Where(cg => cg.IsExclude).Any(c => c.IsMatch(item));
		}

		#endregion Filter logic

		#region Create new

		public static FilterViewModel CreateNew()
		{
			FilterViewModel newFilter = new FilterViewModel();
			newFilter.DisplayName = DateTime.Now.ToString();
			FilterConditionGroupViewModel newGroup = new FilterConditionGroupViewModel(newFilter);
			newGroup.Conditions.Add(new FilterConditionViewModel(newGroup));
			newFilter.ConditionGroups.Add(newGroup);
			return newFilter;
		}

		#endregion Create new

		#region Duplicate

		public FilterViewModel GetDuplicate()
		{
			FilterViewModel newFilter = new FilterViewModel();
			newFilter.DisplayName = this.DisplayName + " (copy)";
			newFilter.ConditionGroups = new ObservableCollection<FilterConditionGroupViewModel>(ConditionGroups.Select(cg => cg.GetDuplicate(newFilter)));
			return newFilter;
		}

		#endregion Duplicate
	}

	public class FilterException : Exception
	{
		public FilterException()
		{
		}

		public FilterException(string message)
			: base(message)
		{
		}

		public FilterException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}
