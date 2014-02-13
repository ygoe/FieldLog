// SmoothVirtualizingPanel
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
// Bugs fixed and lessons learned along the way:
// * Don't call UpdateScrollInfo in MeasureOverride. The size in MeasureOverride and
//   ArrangeOverride may differ. UpdateScrollInfo causes an InvalidateMeasure on each size change.
//   This leads to an infinite layout loop, causing all sorts of GUI weirdness, at unlucky window
//   sizes (this time every 4th pixel width).
//   See also http://stackoverflow.com/q/21740995/143684
//
// Resources used to build this:
// * Dr. WPF's GREAT guide to how WPF layout works
//   http://drwpf.com/blog/itemscontrol-a-to-z/
// * Dan Creiver's Set of articles on making a virtualized panel
//   http://blogs.msdn.com/b/dancre/archive/tags/virtualizingtilepanel/
// * BenCon's IScrollInfo Articles
//   http://blogs.msdn.com/search/searchresults.aspx?q=iscrollinfo&sections=3253

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Collections.Specialized;

namespace Unclassified.UI
{
	/// <summary>
	/// Implements a virtualizing panel that supports smooth pixel-based scrolling.
	/// </summary>
	/// <remarks>
	/// This class is designed to be used as an ItemsPanelTemplate for a ListBox control.
	/// </remarks>
	public class SmoothVirtualizingPanel : VirtualizingPanel, IScrollInfo
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

		private bool canHorizontallyScroll;
		private bool canVerticallyScroll;
		private Size extent = new Size();
		private Point offset = new Point();
		private ScrollViewer scrollOwner;
		private Size viewport = new Size();

		#endregion Private fields

		#region Constructor

		public SmoothVirtualizingPanel()
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

		private bool scrollToPixel = true;
		public bool ScrollToPixel
		{
			get { return scrollToPixel; }
			set
			{
				if (value != scrollToPixel)
				{
					scrollToPixel = value;
					SetVerticalOffset(offset.Y);
				}
			}
		}

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
			itemsPerPage = (int) Math.Floor(ViewportHeight / ItemHeight);
		}

		private int GetItemsCount()
		{
			ItemsControl itemControl = ItemsControl.GetItemsOwner(this);
			return itemControl.Items.Count;
		}

		/// <summary>
		/// Gets the first visible item given the current offset.
		/// </summary>
		/// <returns></returns>
		private int GetFirstVisibleIndex()
		{
			return (int) Math.Floor(offset.Y / ItemHeight);
		}

		/// <summary>
		/// Gets the last visible item given the current offset.
		/// </summary>
		/// <returns></returns>
		private int GetLastVisibleIndex()
		{
			return Math.Min((int) Math.Ceiling((offset.Y + viewport.Height) / ItemHeight) - 1, GetItemsCount() - 1);
		}

		/// <summary>
		/// Updates the extents and other data.
		/// </summary>
		/// <param name="availableSize"></param>
		private void UpdateScrollInfo(Size availableSize)
		{
			// See how many items there are
			ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
			int itemsCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

			Size childSize = GetChildSize(availableSize);
			Size newExtent = new Size(availableSize.Width, childSize.Height * itemsCount);

			// Update extent
			if (extent != newExtent)
			{
				extent = newExtent;
				if (offset.Y > extent.Height - viewport.Height)
				{
					SetVerticalOffset(extent.Height - viewport.Height);
				}
				if (scrollOwner != null)
				{
					scrollOwner.InvalidateScrollInfo();
				}
			}
			// Update viewport
			if (availableSize != viewport)
			{
				viewport = availableSize;
				UpdateViewportParameters();
				if (scrollOwner != null)
				{
					scrollOwner.InvalidateScrollInfo();
				}
			}
		}

		/// <summary>
		/// Revirtualizes the items that are no longer visible.
		/// </summary>
		/// <param name="minDesiredGenerated">The first item index that should be visible.</param>
		/// <param name="maxDesiredGenerated">The last item index that should be visible.</param>
		private void CleanUpItems(int minDesiredGenerated, int maxDesiredGenerated)
		{
			for (int i = InternalChildren.Count - 1; i >= 0; i--)
			{
				GeneratorPosition childPos = new GeneratorPosition(i, 0);
				int itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(childPos);
				if (itemIndex < minDesiredGenerated || itemIndex > maxDesiredGenerated)
				{
					ItemContainerGenerator.Remove(childPos, 1);
					RemoveInternalChildRange(i, 1);
				}
			}
		}

		#endregion Private methods

		#region Overridden methods

		/// <summary>
		/// Measures the child items in the panel.
		/// </summary>
		/// <param name="availableSize">The available size.</param>
		/// <returns>The desired size.</returns>
		protected override Size MeasureOverride(Size availableSize)
		{
			if (ItemContainerGenerator == null) return availableSize;   // Nothing to do
			
			// Our children should be as wide as we are but as tall as item height.

			Size childSize = GetChildSize(availableSize);

			ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
			int itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

			// Figure out range that's visible based on layout algorithm
			int firstVisibleIndex = GetFirstVisibleIndex();
			int lastVisibleIndex = GetLastVisibleIndex();
			// Generate a few more items at either end to support keyboard navigation
			// TODO: Test whether this is really required and how many additional items we really need
			int firstGeneratedIndex = Math.Max(0, firstVisibleIndex - 3);
			int lastGeneratedIndex = Math.Min(itemCount - 1, lastVisibleIndex + 3);
			//Debug.WriteLine("itemCount = " + itemCount + "; firstGeneratedIndex = " + firstGeneratedIndex + "; lastGeneratedIndex = " + lastGeneratedIndex);

			// TODO: For the Home and End keys to work, the first and last item must be realized. Do that every time and don't clean them up?
			
			// Get the generator position of the first visible data item
			GeneratorPosition startPos = ItemContainerGenerator.GeneratorPositionFromIndex(firstGeneratedIndex);

			// Get index where we'd insert the child for this position. If the item is realized
			// (position.Offset == 0), it's just position.Index, otherwise we have to add one to
			// insert after the corresponding child
			int childIndex = startPos.Offset == 0 ? startPos.Index : startPos.Index + 1;

			using (ItemContainerGenerator.StartAt(startPos, GeneratorDirection.Forward, true))
			{
				for (int itemIndex = firstGeneratedIndex; itemIndex <= lastGeneratedIndex; itemIndex++, childIndex++)
				{
					bool isNewlyRealized;
					// Get or create the child
					UIElement child = ItemContainerGenerator.GenerateNext(out isNewlyRealized) as UIElement;
					if (isNewlyRealized)
					{
						// Figure out if we need to insert the child at the end or somewhere in the middle
						if (childIndex >= InternalChildren.Count)
						{
							base.AddInternalChild(child);
						}
						else
						{
							base.InsertInternalChild(childIndex, child);
						}
						ItemContainerGenerator.PrepareItemContainer(child);
					}
					else
					{
						// The child has already been created, let's be sure it's in the right spot
						Debug.Assert(child == InternalChildren[childIndex], "Wrong child generated");
					}

					// Measurements will depend on layout algorithm
					child.Measure(childSize);
				}
			}

			// TODO: This could be deferred to idle time for efficiency
			CleanUpItems(firstGeneratedIndex, lastGeneratedIndex);
			return availableSize;
		}

		/// <summary>
		/// Arranges the child items in the panel.
		/// </summary>
		/// <param name="finalSize">The available size.</param>
		/// <returns>The used size.</returns>
		protected override Size ArrangeOverride(Size finalSize)
		{
			Size childSize = GetChildSize(finalSize);
			UpdateScrollInfo(finalSize);

			for (int i = 0; i < Children.Count; i++)
			{
				UIElement child = Children[i];
				int itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));
				child.Arrange(new Rect(0, ItemHeight * itemIndex, childSize.Width, ItemHeight));
			}
			//DumpGeneratorContent();
			return finalSize;
		}

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
			InvalidateMeasure();
			InvalidateArrange();
		}

		#endregion Overridden methods

		#region IScrollInfo members

		public bool CanHorizontallyScroll
		{
			get { return canHorizontallyScroll; }
			set { canHorizontallyScroll = value; }
		}

		public bool CanVerticallyScroll
		{
			get { return canVerticallyScroll; }
			set { canVerticallyScroll = value; }
		}

		public double ExtentHeight
		{
			get { return extent.Height; }
		}

		public double ExtentWidth
		{
			get { return extent.Width; }
		}

		public double HorizontalOffset
		{
			get { return offset.X; }
		}

		public double VerticalOffset
		{
			get { return offset.Y; }
		}

		public void LineDown()
		{
			SetVerticalOffset(VerticalOffset + ItemHeight);
		}

		public void LineLeft()
		{
		}

		public void LineRight()
		{
		}

		public void LineUp()
		{
			SetVerticalOffset(VerticalOffset - ItemHeight);
		}

		public Rect MakeVisible(Visual visual, Rect rectangle)
		{
			if (rectangle.IsEmpty || visual == null || visual == this || !base.IsAncestorOf(visual))
			{
				return Rect.Empty;
			}

			FrameworkElement fe = visual as FrameworkElement;
			var transform = visual.TransformToAncestor(this);
			Point p = transform.Transform(new Point(0, 0));
			p.Offset(0, trans.Y);
			Rect rect = new Rect(p, fe.RenderSize);

			if (rect.Y < 0)
			{
				SetVerticalOffset(VerticalOffset + rect.Y);
			}
			else if (rect.Y + rect.Height > viewport.Height)
			{
				double verticalOffset = rect.Y + rect.Height + VerticalOffset - viewport.Height;
				SetVerticalOffset(verticalOffset);
			}

			return new Rect(HorizontalOffset, VerticalOffset, ViewportWidth, ViewportHeight);
		}

		public void MouseWheelDown()
		{
			SetVerticalOffset(VerticalOffset + ItemHeight * SystemParameters.WheelScrollLines);
		}

		public void MouseWheelLeft()
		{
		}

		public void MouseWheelRight()
		{
		}

		public void MouseWheelUp()
		{
			SetVerticalOffset(VerticalOffset - ItemHeight * SystemParameters.WheelScrollLines);
		}

		public void PageDown()
		{
			// TODO: Only scroll so far that the selected item is at the very bottom of the viewport
			// This method is only called when the PageDn key is pressed AND the view must be scrolled.
			// Currently, it's scrolled by a multiple of an integral item height.
			// So the gap between the last selected item and the bottom of the viewport remains the same, but should become 0.
			SetVerticalOffset(VerticalOffset + itemsPerPage * ItemHeight);
		}

		public void PageLeft()
		{
		}

		public void PageRight()
		{
		}

		public void PageUp()
		{
			// TODO: See notes about scrolling in PageDown().
			SetVerticalOffset(VerticalOffset - itemsPerPage * ItemHeight);
		}

		public ScrollViewer ScrollOwner
		{
			get { return scrollOwner; }
			set { scrollOwner = value; }
		}

		public void SetHorizontalOffset(double horizontalOffset)
		{
		}

		public void SetVerticalOffset(double verticalOffset)
		{
			if (verticalOffset < 0 || viewport.Height >= extent.Height)
			{
				verticalOffset = 0;
			}
			else if (verticalOffset + viewport.Height >= extent.Height)
			{
				verticalOffset = extent.Height - viewport.Height;
			}

			if (ScrollToPixel)
			{
				verticalOffset = Math.Round(verticalOffset);
			}
			
			offset.Y = verticalOffset;
			if (scrollOwner != null)
			{
				scrollOwner.InvalidateScrollInfo();
			}

			trans.Y = -verticalOffset;
			InvalidateMeasure();
		}

		public double ViewportHeight
		{
			get { return viewport.Height; }
		}

		public double ViewportWidth
		{
			get { return viewport.Width; }
		}

		#endregion IScrollInfo members
	}
}
