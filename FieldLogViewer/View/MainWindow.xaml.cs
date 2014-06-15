using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Unclassified.FieldLog;
using Unclassified.FieldLogViewer.ViewModel;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.FieldLogViewer.View
{
	public partial class MainWindow : Window
	{
		#region Static constructor

		static MainWindow()
		{
			ViewCommandManager.SetupMetadata<MainWindow>();
		}

		#endregion Static constructor

		#region Static data

		public static MainWindow Instance { get; private set; }

		#endregion Static data

		#region Private data

		private bool logItemsSmoothScrollActive;
		private ScrollViewer logItemsScroll;
		private ScrollViewerOffsetMediator logItemsScrollMediator;
		private bool logItemsScrolledNearEnd = true;
		private double prevRatio = 10;
		private DateTime prevItemTime;
		private MediaPlayer newItemMediaPlayer = new MediaPlayer();
		private SmoothVirtualizingPanel logItemsHostPanel;
		private DelayedCall logItemsScrollPixelDc;
		private bool isFlashing;
		private bool isScrollAnimationPosted;
		private DelayedCall updateScrollmapDc;
		private bool scrollmapUpdatePending;

		#endregion Private data

		#region Constructors

		public MainWindow()
		{
			Instance = this;
			
			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.Manual;
			Left = AppSettings.Instance.Window.MainLeft;
			Top = AppSettings.Instance.Window.MainTop;
			Width = AppSettings.Instance.Window.MainWidth;
			Height = AppSettings.Instance.Window.MainHeight;
			WindowState = AppSettings.Instance.Window.MainIsMaximized ? WindowState.Maximized : WindowState.Normal;

			newItemMediaPlayer.Open(new Uri(@"Sounds\ting.mp3", UriKind.Relative));

			logItemsScrollPixelDc = DelayedCall.Create(() => { logItemsHostPanel.ScrollToPixel = true; }, 600);
			updateScrollmapDc = DelayedCall.Create(UpdateScrollmap, 100);

			AppSettings.Instance.OnPropertyChanged(s => s.ShowWarningsErrorsInScrollBar, () => InvalidateScrollmap(false));
			AppSettings.Instance.OnPropertyChanged(s => s.ShowSelectionInScrollBar, () => InvalidateScrollmap(false));
		}

		#endregion Constructors

		#region Window event handlers

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			DebugMonitor.Stop();
		}

		private void Window_LocationChanged(object sender, EventArgs e)
		{
			if (AppSettings.Instance != null)
			{
				AppSettings.Instance.Window.MainLeft = (int) RestoreBounds.Left;
				AppSettings.Instance.Window.MainTop = (int) RestoreBounds.Top;
				AppSettings.Instance.Window.MainWidth = (int) RestoreBounds.Width;
				AppSettings.Instance.Window.MainHeight = (int) RestoreBounds.Height;
				AppSettings.Instance.Window.MainIsMaximized = WindowState == WindowState.Maximized;
			}
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Window_LocationChanged(this, EventArgs.Empty);

			if (ActualHeight > 0)
			{
				double ratio = ActualWidth / ActualHeight;
				const double threshold = 1.5;
				if (ratio >= threshold && prevRatio < threshold)
				{
					// Window is now wider, move list and details in a row
					MainLayout.RowDefinitions[1].Height = new GridLength(0);
					MainLayout.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
					Grid.SetRow(ItemDetailsBorder, 0);
					Grid.SetColumn(ItemDetailsBorder, 1);
					ItemDetailsBorder.BorderThickness = new Thickness(1, 0, 0, 0);
				}
				else if (ratio < threshold && prevRatio >= threshold)
				{
					// Window is now taller, move list and details in a column
					MainLayout.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
					MainLayout.ColumnDefinitions[1].Width = new GridLength(0);
					Grid.SetRow(ItemDetailsBorder, 1);
					Grid.SetColumn(ItemDetailsBorder, 0);
					ItemDetailsBorder.BorderThickness = new Thickness(0, 1, 0, 0);
				}

				prevRatio = ratio;
			}

			InvalidateScrollmap();
		}

		private void Window_Activated(object sender, EventArgs e)
		{
			if (isFlashing)
			{
				this.StopFlashing();
				isFlashing = false;
			}
		}

		private void Window_Deactivated(object sender, EventArgs e)
		{
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.Property == DataContextProperty)
			{
				var viewModel = DataContext as MainViewModel;
				if (viewModel != null)
				{
					// Add event handler for every new ObservableCollection<LogItemViewModelBase>
					// that is assigned to FilteredLogItems.Source in the MainViewModel
					viewModel.OnPropertyChanged(
						vm => vm.FilteredLogItemsView,
						c =>
						{
							if (c != null)
							{
								c.CollectionChanged += LogItems_CollectionChanged;
								InvalidateScrollmap();
							}
						});
				}
			}
		}

		#endregion Window event handlers

		#region Control event handlers

		private void SmoothVirtualizingPanel_Loaded(object sender, RoutedEventArgs e)
		{
			logItemsHostPanel = sender as SmoothVirtualizingPanel;
			if (scrollmapUpdatePending)
			{
				scrollmapUpdatePending = false;
				UpdateScrollmap();
			}
		}

		private void LogItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			InvalidateScrollmap(e.Action != NotifyCollectionChangedAction.Add);

			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				DateTime now = DateTime.UtcNow;

				CheckScrollToEnd();

				// Flash window on new item, if window is inactive and not yet flashing
				if (AppSettings.Instance.IsFlashingEnabled)
				{
					if (!this.IsActive)
					{
						if (!isFlashing)
						{
							this.Flash();
							isFlashing = true;
						}
					}
					else
					{
						isFlashing = false;
					}
				}

				// Play sound on new item, with rate limiting
				if (AppSettings.Instance.IsSoundEnabled)
				{
					if (now > prevItemTime.AddSeconds(1))
					{
						newItemMediaPlayer.Position = TimeSpan.Zero;
						newItemMediaPlayer.Play();
					}
				}
				prevItemTime = DateTime.UtcNow;
			}
		}

		private void CheckScrollToEnd()
		{
			FindLogItemsScroll();
			if (logItemsScroll != null)
			{
				// Only scroll to the end if we're already near it and if the option is enabled
				if (logItemsScrolledNearEnd && AppSettings.Instance.IsLiveScrollingEnabled)
				{
					if (logItemsSmoothScrollActive)
					{
						// Start the animation later when the layout has been updated and we
						// know the maximum height to scroll to
						if (!isScrollAnimationPosted)
						{
							Dispatcher.BeginInvoke(
								(Action) delegate
								{
									isScrollAnimationPosted = false;
									logItemsScrollPixelDc.Reset();
									logItemsHostPanel.ScrollToPixel = false;

									logItemsScrollMediator.AnimateEaseOut(
										ScrollViewerOffsetMediator.VerticalOffsetProperty,
										logItemsScroll.VerticalOffset,
										logItemsScroll.ScrollableHeight,
										TimeSpan.FromMilliseconds(500));
								},
								System.Windows.Threading.DispatcherPriority.Input);
							isScrollAnimationPosted = true;
						}
					}
					else
					{
						logItemsScroll.ScrollToEnd();
					}
				}
			}
		}

		private void FindLogItemsScroll()
		{
			if (logItemsScroll == null)
			{
				// Try to find a reference on the scrollbar
				if (VisualTreeHelper.GetChildrenCount(LogItemsList) == 0)
					return;

				logItemsScroll = null;
				logItemsScrollMediator = null;

				var border = VisualTreeHelper.GetChild(LogItemsList, 0) as Decorator;
				if (border != null)
				{
					logItemsScroll = border.Child as ScrollViewer;
					if (logItemsScroll != null)
					{
						logItemsScroll.ScrollChanged += logItemsScroll_ScrollChanged;

						// Initialise scrolling mediator
						logItemsScrollMediator = new ScrollViewerOffsetMediator();
						logItemsScrollMediator.ScrollViewer = logItemsScroll;
					}
				}
			}
		}

		private void logItemsScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			bool cond = logItemsScroll.VerticalOffset >= logItemsScroll.ScrollableHeight - 50;
			// e.VerticalChange can actually be 0, so test for positive and negative values explicitly
			if (e.VerticalChange > 0)
			{
				// Scrolled down, can only set flag if in range
				logItemsScrolledNearEnd |= cond;
			}
			else if (e.VerticalChange < 0)
			{
				// Scrolled up, can only clear flag if out of range
				logItemsScrolledNearEnd &= cond;

				// Stop the scroll animation immediately when scrolling up
				if (DependencyPropertyHelper.GetValueSource(logItemsScrollMediator, ScrollViewerOffsetMediator.VerticalOffsetProperty).IsAnimated)
				{
					if (logItemsScrollMediator != null)   // Should always be true here
					{
						logItemsScrollMediator.StopDoubleAnimation(ScrollViewerOffsetMediator.VerticalOffsetProperty);
						logItemsScrollPixelDc.Fire();
					}
				}
			}
		}

		private void LogItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			MainViewModel.Instance.SelectedItems = LogItemsList.SelectedItems.OfType<LogItemViewModelBase>().ToList();
			InvalidateScrollmap(false);
		}

		#endregion Control event handlers

		#region Scrollmap

		private void InvalidateScrollmap(bool clear = true)
		{
			if (clear)
				ClearScrollmap();
			updateScrollmapDc.Reset();
		}
		
		private void ClearScrollmap()
		{
			WarningMap.Data = null;
			ErrorMap.Data = null;
			CriticalMap.Data = null;
			SelectionMap.Data = null;
		}

		private void UpdateScrollmap()
		{
			if (logItemsHostPanel == null)
			{
				// SmoothVirtualizingPanel_Loaded wasn't called yet. Remember to update the scroll
				// map when it will be called.
				scrollmapUpdatePending = true;
				return;
			}
			if (logItemsScroll == null) return;

			bool showWarningsErrors = AppSettings.Instance.ShowWarningsErrorsInScrollBar;
			bool showSelection = AppSettings.Instance.ShowSelectionInScrollBar;

			if (!showWarningsErrors && !showSelection) return;   // Nothing to do
			
			double itemOffset = 0;
			double itemHeight = logItemsHostPanel.ItemHeight;

			bool scrollBarVisible = logItemsScroll.ComputedVerticalScrollBarVisibility == Visibility.Visible;
			if (scrollBarVisible)
			{
				ScrollBar sb = logItemsScroll.FindVisualChild<ScrollBar>(d => AutomationProperties.GetAutomationId(d) == "VerticalScrollBar");
				if (sb != null)
				{
					Grid g = sb.FindVisualChild<Grid>();
					if (g != null && g.RowDefinitions.Count == 3)
					{
						itemOffset = g.RowDefinitions[0].ActualHeight + 2;
						itemHeight = (g.RowDefinitions[1].ActualHeight - 4) / LogItemsList.Items.Count;
					}
				}
			}
			else
			{
				return;
			}

			StreamGeometry warningGeometry = new StreamGeometry();
			warningGeometry.FillRule = FillRule.Nonzero;
			StreamGeometry errorGeometry = new StreamGeometry();
			errorGeometry.FillRule = FillRule.Nonzero;
			StreamGeometry criticalGeometry = new StreamGeometry();
			criticalGeometry.FillRule = FillRule.Nonzero;
			StreamGeometry selectionGeometry = new StreamGeometry();
			selectionGeometry.FillRule = FillRule.Nonzero;

			StreamGeometryContext warningCtx = warningGeometry.Open();
			StreamGeometryContext errorCtx = errorGeometry.Open();
			StreamGeometryContext criticalCtx = criticalGeometry.Open();
			StreamGeometryContext selectionCtx = selectionGeometry.Open();

			HashSet<object> selectedItems = new HashSet<object>(LogItemsList.SelectedItems.OfType<object>());

			for (int index = 0; index < LogItemsList.Items.Count; index++)
			{
				if (showWarningsErrors)
				{
					FieldLogItemViewModel flItem = LogItemsList.Items[index] as FieldLogItemViewModel;
					if (flItem != null)
					{
						if (flItem.Priority >= FieldLogPriority.Warning)
						{
							double y = Math.Round(itemOffset + (index + 0.5) * itemHeight);

							StreamGeometryContext ctx;
							if (flItem.Priority == FieldLogPriority.Warning) ctx = warningCtx;
							else if (flItem.Priority == FieldLogPriority.Error) ctx = errorCtx;
							else if (flItem.Priority == FieldLogPriority.Critical) ctx = criticalCtx;
							else continue;

							ctx.BeginFigure(new Point(0, y - 2), true, true);
							ctx.PolyLineTo(new Point[]
								{
									new Point(3, y - 2),
									new Point(3, y + 2),
									new Point(0, y + 2)
								}, false, false);
						}
					}
				}

				if (showSelection)
				{
					if (selectedItems.Contains(LogItemsList.Items[index]))
					{
						double y = Math.Round(itemOffset + (index + 0.5) * itemHeight);
						selectionCtx.BeginFigure(new Point(4, y - 1), true, true);
						selectionCtx.PolyLineTo(new Point[]
							{
								new Point(7, y - 1),
								new Point(7, y + 1),
								new Point(4, y + 1)
							}, false, false);
					}
				}
			}

			warningCtx.Close();
			warningGeometry.Freeze();
			errorCtx.Close();
			errorGeometry.Freeze();
			criticalCtx.Close();
			criticalGeometry.Freeze();
			selectionCtx.Close();
			selectionGeometry.Freeze();

			WarningMap.Data = warningGeometry;
			ErrorMap.Data = errorGeometry;
			CriticalMap.Data = criticalGeometry;
			SelectionMap.Data = selectionGeometry;
		}

		#endregion Scrollmap

		#region View commands

		[ViewCommand]
		public void StartedReadingFiles()
		{
			logItemsSmoothScrollActive = false;
		}

		[ViewCommand]
		public void FinishedReadingFiles()
		{
			if (logItemsHostPanel != null)
			{
				logItemsHostPanel.UpdateLayout();
			}
			CheckScrollToEnd();
			logItemsSmoothScrollActive = true;
		}

		[ViewCommand]
		public void FinishedReadingFilesAgain()
		{
			logItemsSmoothScrollActive = true;
			CheckScrollToEnd();
		}

		[ViewCommand]
		public void ScrollToEnd()
		{
			FindLogItemsScroll();
			if (logItemsScroll != null)
			{
				logItemsScroll.ScrollToEnd();
			}
		}

		private object savedScrollItem;
		private int savedScrollOffset;
		private object prevSelectedItem;
		private int prevSelectedOffset;
		private int prevOffset;

		[ViewCommand]
		public void SaveScrolling()
		{
			if (logItemsScroll != null &&
				LogItemsList.ActualHeight > 0 &&
				logItemsHostPanel.ItemHeight > 0)
			{
				bool selectionIsVisible;
				int topVisibleIndex = (int) (logItemsScroll.VerticalOffset / logItemsHostPanel.ItemHeight);
				int bottomVisibleIndex = (int) ((logItemsScroll.VerticalOffset + LogItemsList.ActualHeight) / logItemsHostPanel.ItemHeight);
				if (LogItemsList.SelectedIndex > -1 &&
					LogItemsList.SelectedIndex >= topVisibleIndex &&
					LogItemsList.SelectedIndex <= bottomVisibleIndex)
				{
					// Selection is visible. Save selection index and offset.
					savedScrollItem = LogItemsList.SelectedItem;
					savedScrollOffset = LogItemsList.SelectedIndex * logItemsHostPanel.ItemHeight - (int) logItemsScroll.VerticalOffset;
					selectionIsVisible = true;

					// Remember selection in case it gets lost in the next filtered view
					prevSelectedItem = savedScrollItem;
					prevSelectedOffset = savedScrollOffset;
				}
				else
				{
					// Selection not visible. Save center visible item index and offset.
					int centerVisibleIndex = (int) ((logItemsScroll.VerticalOffset + LogItemsList.ActualHeight / 2) / logItemsHostPanel.ItemHeight);
					if (centerVisibleIndex < 0)
						centerVisibleIndex = 0;
					if (centerVisibleIndex >= LogItemsList.Items.Count)
						centerVisibleIndex = LogItemsList.Items.Count - 1;
					if (centerVisibleIndex != -1)
					{
						savedScrollItem = LogItemsList.Items[centerVisibleIndex];
						savedScrollOffset = centerVisibleIndex * logItemsHostPanel.ItemHeight - (int) logItemsScroll.VerticalOffset;
					}
					else
					{
						// No item available, nothing to save
						savedScrollItem = null;
						savedScrollOffset = 0;
					}
					selectionIsVisible = false;
				}

				// If nothing was selected now:
				// Compare current scrolling with when it was last restored
				if (!selectionIsVisible &&
					(int) logItemsScroll.VerticalOffset != prevOffset)
				{
					// Scroll was changed, forget previous selection
					prevSelectedItem = null;
				}
			}
			else
			{
				savedScrollItem = null;
				savedScrollOffset = 0;
			}
		}

		[ViewCommand]
		public void RestoreScrolling()
		{
			int itemIndex;

			// We need the new scroll info anyway
			logItemsHostPanel.UpdateLayout();

			// First, try to bring back the previously selected item
			if (prevSelectedItem != null)
			{
				itemIndex = LogItemsList.Items.IndexOf(prevSelectedItem);
				if (itemIndex > -1)
				{
					// Bring the item exactly where it was before on the screen
					int itemOffset = itemIndex * logItemsHostPanel.ItemHeight;
					logItemsHostPanel.SetVerticalOffset(itemOffset - prevSelectedOffset);
					LogItemsList.SelectedIndex = itemIndex;
					return;
				}
				// else: The previously selected item is still not visible

				if (savedScrollItem == null)
				{
					// No previous items were visible, at least try with the last valid selected item
					savedScrollItem = prevSelectedItem;
					savedScrollOffset = prevSelectedOffset;
				}
			}

			if (savedScrollItem != null)
			{
				itemIndex = LogItemsList.Items.IndexOf(savedScrollItem);
				if (itemIndex > -1)
				{
					// Bring the item exactly where it was before on the screen
					int itemOffset = itemIndex * logItemsHostPanel.ItemHeight;
					logItemsHostPanel.SetVerticalOffset(itemOffset - savedScrollOffset);
				}
				else
				{
					// The saved item is no longer visible. Search for item with nearest time.
					DateTime targetTime = GetTimeOfItem(savedScrollItem);
					object nearestItem = null;
					long minTickDiff = long.MaxValue;
					for (int i = 0; i < LogItemsList.Items.Count; i++)
					{
						object item = LogItemsList.Items[i];
						DateTime itemTime = GetTimeOfItem(item);
						long tickDiff = Math.Abs(itemTime.Ticks - targetTime.Ticks);
						if (tickDiff < minTickDiff)
						{
							minTickDiff = tickDiff;
							nearestItem = item;
						}
					}
					if (nearestItem != null)
					{
						itemIndex = LogItemsList.Items.IndexOf(nearestItem);
						if (itemIndex > -1)
						{
							int itemOffset = itemIndex * logItemsHostPanel.ItemHeight;
							logItemsHostPanel.SetVerticalOffset(itemOffset - savedScrollOffset);
						}
					}

					// Remember the current scoll offset. If it wasn't changed and the item is
					// visible again, we can scroll directly to it and select it.
					// Read from logItemsHostPanel instead of the ScrollViewer logItemsScroll
					// because we've only just set the value there and need that current value.
					prevOffset = (int) logItemsHostPanel.VerticalOffset;
				}
			}
		}

		private DateTime GetTimeOfItem(object item)
		{
			FieldLogItemViewModel flItem = item as FieldLogItemViewModel;
			if (flItem != null)
			{
				return flItem.Time;
			}
			DebugMessageViewModel dbgItem = item as DebugMessageViewModel;
			if (dbgItem != null)
			{
				return dbgItem.Time;
			}
			throw new ArgumentException("Unsupported item type.");   // Should never happen
		}

		#endregion View commands
	}
}
