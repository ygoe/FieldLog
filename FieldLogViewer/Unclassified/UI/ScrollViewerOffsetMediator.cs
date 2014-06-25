using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Unclassified.UI
{
	// Source: http://blogs.msdn.com/b/delay/archive/2009/08/04/scrolling-so-smooth-like-the-butter-on-a-muffin-how-to-animate-the-horizontal-verticaloffset-properties-of-a-scrollviewer.aspx
	/// <summary>
	/// Mediator that forwards Offset property changes on to a ScrollViewer
	/// instance to enable the animation of Horizontal/VerticalOffset.
	/// </summary>
	public class ScrollViewerOffsetMediator : FrameworkElement
	{
		/// <summary>
		/// ScrollViewer instance to forward Offset changes on to.
		/// </summary>
		public ScrollViewer ScrollViewer
		{
			get { return (ScrollViewer) GetValue(ScrollViewerProperty); }
			set { SetValue(ScrollViewerProperty, value); }
		}

		public static readonly DependencyProperty ScrollViewerProperty = DependencyProperty.Register(
			"ScrollViewer",
			typeof(ScrollViewer),
			typeof(ScrollViewerOffsetMediator),
			new PropertyMetadata(OnScrollViewerChanged));

		private static void OnScrollViewerChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var mediator = (ScrollViewerOffsetMediator) o;
			var scrollViewer = (ScrollViewer) e.NewValue;
			if (scrollViewer != null)
			{
				scrollViewer.ScrollToVerticalOffset(mediator.VerticalOffset);
			}
		}

		/// <summary>
		/// VerticalOffset property to forward to the ScrollViewer.
		/// </summary>
		public double VerticalOffset
		{
			get { return (double) GetValue(VerticalOffsetProperty); }
			set { SetValue(VerticalOffsetProperty, value); }
		}

		public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.Register(
			"VerticalOffset",
			typeof(double),
			typeof(ScrollViewerOffsetMediator),
			new PropertyMetadata(OnVerticalOffsetChanged));

		public static void OnVerticalOffsetChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var mediator = (ScrollViewerOffsetMediator) o;
			if (mediator.ScrollViewer != null)
			{
				mediator.ScrollViewer.ScrollToVerticalOffset((double) e.NewValue);
			}
		}

		/// <summary>
		/// Multiplier for ScrollableHeight property to forward to the ScrollViewer.
		/// </summary>
		/// <remarks>
		/// 0.0 means "scrolled to top"; 1.0 means "scrolled to bottom".
		/// </remarks>
		public double ScrollableHeightMultiplier
		{
			get { return (double) GetValue(ScrollableHeightMultiplierProperty); }
			set { SetValue(ScrollableHeightMultiplierProperty, value); }
		}

		public static readonly DependencyProperty ScrollableHeightMultiplierProperty = DependencyProperty.Register(
			"ScrollableHeightMultiplier",
			typeof(double),
			typeof(ScrollViewerOffsetMediator),
			new PropertyMetadata(OnScrollableHeightMultiplierChanged));

		public static void OnScrollableHeightMultiplierChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var mediator = (ScrollViewerOffsetMediator) o;
			var scrollViewer = mediator.ScrollViewer;
			if (scrollViewer != null)
			{
				scrollViewer.ScrollToVerticalOffset((double) e.NewValue * scrollViewer.ScrollableHeight);
			}
		}
	}
}
