using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Unclassified.FieldLogViewer.ViewModel;
using Unclassified;
using Unclassified.UI;

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

			newItemMediaPlayer.Open(new Uri(@"..\..\Sounds\Simple_Low.mp3", UriKind.Relative));

			logItemsScrollPixelDc = DelayedCall.Create(() => { logItemsHostPanel.ScrollToPixel = true; }, 600);
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
					Grid.SetRow(ItemDetails, 0);
					Grid.SetColumn(ItemDetails, 1);
				}
				else if (ratio < threshold && prevRatio >= threshold)
				{
					// Window is now taller, move list and details in a column
					MainLayout.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
					MainLayout.ColumnDefinitions[1].Width = new GridLength(0);
					Grid.SetRow(ItemDetails, 1);
					Grid.SetColumn(ItemDetails, 0);
				}

				prevRatio = ratio;
			}
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
					viewModel.LinkProperty(
						vm => vm.FilteredLogItems,
						c => { if (c != null) c.CollectionChanged += LogItems_CollectionChanged; });
				}
			}
		}

		private void SmoothVirtualizingPanel_Loaded(object sender, RoutedEventArgs e)
		{
			logItemsHostPanel = sender as SmoothVirtualizingPanel;
		}

		private void LogItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				DateTime now = DateTime.UtcNow;

				// OLD: Used before we had the FinishedReadingFiles ViewCommand
				//if (prevItemTime != DateTime.MinValue && now > prevItemTime.AddSeconds(1))
				//{
				//    logItemsSmoothScrollActive = true;
				//}

				FindLogItemsScroll();
				if (logItemsScroll != null)
				{
					// Only scroll to the end if we're already near it and if the option is enabled
					MainViewModel vm = DataContext as MainViewModel;
					if (logItemsScrolledNearEnd && vm != null && vm.IsLiveScrollingEnabled)
					{
						if (logItemsSmoothScrollActive)
						{
							// Start the animation later when the layout has been updated and we
							// know the maximum height to scroll to
							Dispatcher.BeginInvoke(
								(Action) delegate
								{
									logItemsScrollPixelDc.Reset();
									logItemsHostPanel.ScrollToPixel = false;

									logItemsScrollMediator.AnimateEaseOut(
										ScrollViewerOffsetMediator.VerticalOffsetProperty,
										logItemsScroll.VerticalOffset,
										logItemsScroll.ScrollableHeight,
										TimeSpan.FromMilliseconds(500));
								},
								System.Windows.Threading.DispatcherPriority.Background);
						}
						else
						{
							logItemsScroll.ScrollToEnd();
						}
					}
				}

				// Play sound on new item, with rate limiting
				if (now > prevItemTime.AddSeconds(5))
				{
					newItemMediaPlayer.Position = TimeSpan.Zero;
					//newItemMediaPlayer.Play();
				}
				
				prevItemTime = DateTime.UtcNow;
			}
		}

		private void FindLogItemsScroll()
		{
			if (logItemsScroll == null)
			{
				// Try to find a reference on the scrollbar
				if (VisualTreeHelper.GetChildrenCount(LogItemsList) == 0)
					return;

				var border = VisualTreeHelper.GetChild(LogItemsList, 0) as Decorator;
				if (border != null)
				{
					logItemsScroll = border.Child as ScrollViewer;
					if (logItemsScroll != null)
					{
						// Enable smooth scrolling of partial item heights
						//logItemsScroll.CanContentScroll = false;

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
			if (e.VerticalChange >= 0)
			{
				// Scrolled down, can only set flag if in range
				logItemsScrolledNearEnd |= cond;
			}
			else
			{
				// Scrolled up, can only clear flag if out of range
				logItemsScrolledNearEnd &= cond;
			}
		}

		#endregion Window event handlers

		#region View commands

		[ViewCommand]
		public void StartedReadingFiles()
		{
			logItemsSmoothScrollActive = false;
		}

		[ViewCommand]
		public void FinishedReadingFiles()
		{
			System.Diagnostics.Debug.WriteLine("ViewCommand: FinishedReadingFiles");
			logItemsSmoothScrollActive = true;
			ScrollToEnd();
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

		#endregion View commands
	}
}
