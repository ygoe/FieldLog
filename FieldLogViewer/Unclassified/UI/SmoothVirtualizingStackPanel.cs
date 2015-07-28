// SmoothVirtualizingStackPanel
// A copy of SmoothVirtualizingPanel... Work in progress
// A WPF layout panel suitable for use in a listbox.
//
// Originally written in VB.NET by Marauderz (http://www.marauderzstuff.com)
// Source: http://www.marauderzstuff.com/PermaLink,guid,b31f2278-9103-4c84-8520-8adf2aef7fbc.aspx
// Converted to C# and extended by Yves Goergen (http://unclassified.de)
// With inspiration from Dan Crevier's VirtualizingTilePanel
// Source: http://blogs.msdn.com/b/dancre/archive/2006/02/16/implementing-a-virtualizingpanel-part-4-the-goods.aspx
// With inspiration from Goroll's VirtualizingTreePanel
// Source: https://treeviewex.codeplex.com/
//
// Known bugs in the original VB.NET version:
// * Changing the ListBox' width with a few thousand items locks the UI for about a second
// * Once, changing the ListBox' width with a few thousand items kept the UI locked and only
//   reacting on user input once a second (not reproducible)
//
// Features:
// * It's a virtualizing panel, therefore it's usable for large collections
// * It implements IScrollInfo, therefore it can smoothly scroll instead of per item
// * On a touch enabled system, you get touch panning for free [to be tested]
//
// Issues:
// * Very rudimentary code, likely to be unstable [to be tested]
// * Only supports FIXED height items, take a close look at how the panel is created and how
//   ItemHeight must be specified
//
// Resources used to build this:
// * Dr. WPF's GREAT guide to how WPF layout works
//   http://drwpf.com/blog/itemscontrol-a-to-z/
// * Dan Creiver's Set of articles on making a virtualized panel
//   http://blogs.msdn.com/b/dancre/archive/tags/virtualizingtilepanel/
// * BenCon's IScrollInfo Articles
//   http://blogs.msdn.com/search/searchresults.aspx?q=iscrollinfo&sections=3253

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Unclassified.UI
{
	/// <summary>
	/// Implements a virtualizing panel that supports smooth pixel-based scrolling.
	/// </summary>
	/// <remarks>
	/// This class is designed to be used as an ItemsPanelTemplate for a ListBox control.
	/// </remarks>
	public class SmoothVirtualizingStackPanel : VirtualizingStackPanel, IScrollInfo
	{
		#region Private fields

		/// <summary>
		/// Transform for use in scrolling.
		/// </summary>
		private TranslateTransform trans = new TranslateTransform();

		/// <summary>
		/// The number of items that are visible in the current viewport.
		/// </summary>
		private int itemsPerPage;

		#endregion Private fields

		#region Constructor

		public SmoothVirtualizingStackPanel()
		{
			ItemHeight = 50;
			RenderTransform = trans;
		}

		#endregion Constructor

		#region Public properties

		/// <summary>
		/// Gets or sets the height of each item in the panel in pixels.
		/// </summary>
		/// <remarks>
		/// For virtualization to work, every item in the panel must have the same height. This
		/// height cannot be automatically determined and must be explicitly set through this
		/// property.
		/// </remarks>
		public int ItemHeight { get; set; }

		#endregion Public properties

		#region Private methods

		/// <summary>
		/// Gets the size of the children based on the constrain area.
		/// </summary>
		/// <param name="constrainArea"></param>
		/// <returns></returns>
		private Size GetChildSize(Size constrainArea)
		{
			return new Size(constrainArea.Width, ItemHeight);
		}

		private void DumpGeneratorContent()
		{
			IItemContainerGenerator generator = ItemContainerGenerator;
			ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
			Debug.WriteLine("Generator positions");
			for (int i = 0; i < itemsControl.Items.Count; i++)
			{
				GeneratorPosition pos = generator.GeneratorPositionFromIndex(i);
				Debug.WriteLine("Item index: {0}, Gen pos: Index: {1} Offset: {2}", i, pos.Index, pos.Offset);
			}
		}

		/// <summary>
		/// Updates the viewport parameters when the viewport size was changed.
		/// </summary>
		private void UpdateViewportParameters()
		{
			itemsPerPage = (int)Math.Floor(ViewportHeight / ItemHeight);
		}

		private int GetItemsCount()
		{
			ItemsControl itemControl = ItemsControl.GetItemsOwner(this);
			return itemControl.Items.Count;
		}

		#endregion Private methods

		#region Overridden methods

		/// <summary>
		/// When items are removed, remove the corresponding UI if necessary.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
		{
			switch (args.Action)
			{
				case NotifyCollectionChangedAction.Remove:
				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Move:
					RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
					break;
			}
		}

		#endregion Overridden methods
	}
}
