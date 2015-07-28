using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Unclassified.LogSubmit.Views;

namespace Unclassified.LogSubmit
{
	public class ListViewColumnSorter : IComparer
	{
		// Based on: http://support.microsoft.com/kb/319401

		#region Private data

		private ListView listView;

		#endregion Private data

		#region Constructors

		public ListViewColumnSorter(ListView listView)
		{
			this.listView = listView;
			SortColumn = 0;
			Order = SortOrder.None;
		}

		#endregion Constructors

		#region Public properties

		/// <summary>
		/// Gets or sets the index of the column to which to apply the sorting operation.
		/// </summary>
		public int SortColumn { get; set; }

		/// <summary>
		/// Gets or sets the order of sorting to apply.
		/// </summary>
		public SortOrder Order { get; set; }

		#endregion Public properties

		#region IComparer members

		/// <summary>
		/// This method is inherited from the IComparer interface.  It compares the two objects passed using a case insensitive comparison.
		/// </summary>
		/// <param name="obj1">First object to be compared</param>
		/// <param name="obj2">Second object to be compared</param>
		/// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
		public int Compare(object obj1, object obj2)
		{
			ListViewItem item1 = (ListViewItem)obj1;
			ListViewItem item2 = (ListViewItem)obj2;

			var info1 = (LogSelectionView.LogBasePathInfo)item1.Tag;
			var info2 = (LogSelectionView.LogBasePathInfo)item2.Tag;

			string textX = item1.SubItems[SortColumn].Text;
			string textY = item2.SubItems[SortColumn].Text;

			int compareResult;

			if (SortColumn == 0)
			{
				// Special handling for first column: Directory separator has highest sort priority
				textX = textX.Replace('\\', '\0');
				textY = textY.Replace('\\', '\0');
				compareResult = StringComparer.OrdinalIgnoreCase.Compare(textX, textY);
			}
			else if (SortColumn == 1)
			{
				// Sort by raw value, not formatted display text
				compareResult = info1.UpdatedTime.CompareTo(info2.UpdatedTime);
			}
			else if (SortColumn == 2)
			{
				// Sort by raw value, not formatted display text
				compareResult = info1.Size.CompareTo(info2.Size);
			}
			else
			{
				// Unsupported column
				return 0;
			}

			if (Order == SortOrder.Ascending)
			{
				return compareResult;
			}
			else if (Order == SortOrder.Descending)
			{
				return -compareResult;
			}
			else
			{
				return 0;   // Don't sort anything, it's already good ("equal")
			}
		}

		#endregion IComparer members

		#region Public methods

		public void HandleColumnClick(int columnIndex)
		{
			if (columnIndex == SortColumn)
			{
				// Reverse the current sort direction for this column.
				if (Order == SortOrder.Ascending)
				{
					Order = SortOrder.Descending;
				}
				else
				{
					Order = SortOrder.Ascending;
				}
			}
			else
			{
				// Set the column number that is to be sorted; default to ascending.
				SortColumn = columnIndex;
				Order = SortOrder.Ascending;

				// Special handling of second column
				if (columnIndex == 1)
				{
					Order = SortOrder.Descending;
				}
			}

			// Perform the sort with these new sort options.
			Update();
		}

		public void Update()
		{
			listView.Sort();
			SetSortIcon(listView, SortColumn, Order);
		}

		#endregion Public methods

		#region ListView column sort arrow

		// Source: http://www.codeproject.com/Tips/734463/Sort-listview-Columns-and-Set-Sort-Arrow-Icon-on-C

		[StructLayout(LayoutKind.Sequential)]
		private struct LVCOLUMN
		{
			public int mask;
			public int cx;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string pszText;
			public IntPtr hbm;
			public int cchTextMax;
			public int fmt;
			public int iSubItem;
			public int iImage;
			public int iOrder;
		}

		private const int HDI_FORMAT = 0x0004;

		private const int HDF_LEFT = 0x0000;
		private const int HDF_BITMAP_ON_RIGHT = 0x1000;
		private const int HDF_SORTUP = 0x0400;
		private const int HDF_SORTDOWN = 0x0200;

		private const int LVM_FIRST = 0x1000;         // List messages
		private const int LVM_GETHEADER = LVM_FIRST + 31;
		private const int HDM_FIRST = 0x1200;         // Header messages
		private const int HDM_GETITEM = HDM_FIRST + 11;
		private const int HDM_SETITEM = HDM_FIRST + 12;

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref LVCOLUMN lPLVCOLUMN);

		private static void SetSortIcon(ListView listView, int columnIndex, SortOrder order)
		{
			IntPtr columnHeader = SendMessage(listView.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);

			for (int columnNumber = 0; columnNumber <= listView.Columns.Count - 1; columnNumber++)
			{
				IntPtr columnPtr = new IntPtr(columnNumber);
				LVCOLUMN lvColumn = new LVCOLUMN();
				lvColumn.mask = HDI_FORMAT;

				SendMessage(columnHeader, HDM_GETITEM, columnPtr, ref lvColumn);

				if (!(order == SortOrder.None) && columnNumber == columnIndex)
				{
					switch (order)
					{
						case System.Windows.Forms.SortOrder.Ascending:
							lvColumn.fmt &= ~HDF_SORTDOWN;
							lvColumn.fmt |= HDF_SORTUP;
							break;
						case System.Windows.Forms.SortOrder.Descending:
							lvColumn.fmt &= ~HDF_SORTUP;
							lvColumn.fmt |= HDF_SORTDOWN;
							break;
					}
					lvColumn.fmt |= (HDF_LEFT | HDF_BITMAP_ON_RIGHT);
				}
				else
				{
					lvColumn.fmt &= ~HDF_SORTDOWN & ~HDF_SORTUP & ~HDF_BITMAP_ON_RIGHT;
				}

				SendMessage(columnHeader, HDM_SETITEM, columnPtr, ref lvColumn);
			}
		}

		#endregion ListView column sort arrow
	}
}
