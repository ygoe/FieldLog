using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Unclassified.UI
{
	/// <summary>
	/// Represents a combination of a standard button on the left and a drop-down button on the right.
	/// </summary>
	[TemplatePartAttribute(Name = "PART_Popup", Type = typeof(Popup))]
	[TemplatePartAttribute(Name = "PART_Button", Type = typeof(Button))]
	public class SplitButton : MenuItem
	{
		#region Static members

		/// <summary>
		/// Identifies the CornerRadius dependency property.
		/// </summary>
		public static readonly DependencyProperty CornerRadiusProperty;

		private static readonly RoutedEvent ButtonClickEvent;

		/// <summary>
		/// A reference to the single SplitButton that has an opened submenu.
		/// </summary>
		private static SplitButton openSplitButton;

		static SplitButton()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(typeof(SplitButton)));

			CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner(typeof(SplitButton));

			IsSubmenuOpenProperty.OverrideMetadata(typeof(SplitButton),
				new FrameworkPropertyMetadata(
					BooleanBoxes.FalseBox,
					new PropertyChangedCallback(OnIsSubmenuOpenChanged),
					new CoerceValueCallback(CoerceIsSubmenuOpen)));

			ButtonClickEvent = EventManager.RegisterRoutedEvent("ButtonClick", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(SplitButton));
			KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
			KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(KeyboardNavigationMode.None));
			KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(KeyboardNavigationMode.None));

			EventManager.RegisterClassHandler(typeof(SplitButton), MenuItem.ClickEvent, new RoutedEventHandler(OnMenuItemClick));
			EventManager.RegisterClassHandler(typeof(SplitButton), Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseButtonDown), true);
		}

		private static void OnIsSubmenuOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			SplitButton splitButton = sender as SplitButton;
			if ((bool)args.NewValue)
			{
				openSplitButton = splitButton;

				// Handle keyboard events as long as the popup is displayed
				var window = Window.GetWindow(splitButton);
				window.PreviewKeyDown += OnPreviewKeyDown;

				if (Mouse.Captured != splitButton)
				{
					Mouse.Capture(splitButton, CaptureMode.SubTree);
					Mouse.AddLostMouseCaptureHandler(splitButton, OnLostMouseCapture);
				}
			}
			else
			{
				// Remove popup keyboard handler
				var window = Window.GetWindow(splitButton);
				window.PreviewKeyDown -= OnPreviewKeyDown;

				openSplitButton = null;

				if (Mouse.Captured == splitButton)
				{
					Mouse.Capture(null);
				}

				if (splitButton.IsKeyboardFocused)
				{
					splitButton.Focus();
				}
			}
		}

		private static void OnLostMouseCapture(object sender, MouseEventArgs args)
		{
			SplitButton splitButton = sender as SplitButton;
			Mouse.RemoveLostMouseCaptureHandler(splitButton, OnLostMouseCapture);
			splitButton.Dispatcher.BeginInvoke(
				new Action(splitButton.CloseSubmenu),
				System.Windows.Threading.DispatcherPriority.Input);
		}

		private static void OnPreviewKeyDown(object sender, KeyEventArgs args)
		{
			if (openSplitButton != null)
			{
				if (args.Key == Key.Escape ||
					args.Key == Key.System)
				{
					openSplitButton.CloseSubmenu();
					args.Handled = true;
				}
			}
		}

		/// <summary>
		/// Set the IsSubmenuOpen property value at the right time.
		/// </summary>
		private static object CoerceIsSubmenuOpen(DependencyObject element, object value)
		{
			SplitButton splitButton = element as SplitButton;
			if ((bool)value)
			{
				if (!splitButton.IsLoaded)
				{
					splitButton.Loaded += delegate (object sender, RoutedEventArgs args)
					{
						splitButton.CoerceValue(IsSubmenuOpenProperty);
					};

					return BooleanBoxes.FalseBox;
				}
			}

			return (bool)value && splitButton.HasItems;
		}

		private static void OnMenuItemClick(object sender, RoutedEventArgs args)
		{
			SplitButton splitButton = sender as SplitButton;
			MenuItem menuItem = args.OriginalSource as MenuItem;

			// To make the ButtonClickEvent get fired as we expected, you should mark the ClickEvent
			// as handled to prevent the event from popping up to the button portion of the SplitButton.
			if (menuItem != null &&
				menuItem.Parent != null &&
				!typeof(MenuItem).IsAssignableFrom(menuItem.Parent.GetType()))
			{
				args.Handled = true;
			}
		}

		private static void OnMouseButtonDown(object sender, MouseButtonEventArgs args)
		{
			SplitButton splitButton = sender as SplitButton;
			//if (!splitButton.IsKeyboardFocusWithin)
			//{
			//    splitButton.Focus();
			//    return;
			//}

			if (Mouse.Captured == splitButton && args.OriginalSource == splitButton)
			{
				splitButton.CloseSubmenu();
				return;
			}

			if (args.Source is MenuItem)
			{
				MenuItem menuItem = (MenuItem)args.Source;
				if (!menuItem.HasItems)
				{
					splitButton.CloseSubmenu();
					menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, menuItem));
				}
			}
		}

		#endregion Static members

		#region Private data

		private Button splitButtonHeaderSite;

		#endregion Private data

		#region Properties

		/// <summary>
		/// Gets or sets a value that represents the degree to which the corners of a <see cref="SplitButton"/> are rounded.
		/// </summary>
		public CornerRadius CornerRadius
		{
			get { return (CornerRadius)GetValue(CornerRadiusProperty); }
			set { SetValue(CornerRadiusProperty, value); }
		}

		#endregion Properties

		/// <summary>
		/// Occurs when the button portion of a <see cref="SplitButton"/> is clicked.
		/// </summary>
		public event RoutedEventHandler ButtonClick
		{
			add { base.AddHandler(ButtonClickEvent, value); }
			remove { base.RemoveHandler(ButtonClickEvent, value); }
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			splitButtonHeaderSite = this.GetTemplateChild("PART_Button") as Button;
			if (splitButtonHeaderSite != null)
			{
				splitButtonHeaderSite.PreviewMouseDown += OnHeaderButtonMouseDown;
				splitButtonHeaderSite.Click += OnHeaderButtonClick;
			}
		}

		private void OnHeaderButtonMouseDown(object sender, RoutedEventArgs args)
		{
			// Close the popup as soon as the mouse is pressed on the button part
			CloseSubmenu();
		}

		private void OnHeaderButtonClick(object sender, RoutedEventArgs args)
		{
			// Close the popup in case the Click event was invoked somehow without pressing the mouse
			CloseSubmenu();
			OnButtonClick();
		}

		protected virtual void OnButtonClick()
		{
			base.RaiseEvent(new RoutedEventArgs(ButtonClickEvent, this));
			var cmd = Command;
			if (cmd != null && cmd.CanExecute(CommandParameter))
			{
				cmd.Execute(CommandParameter);
			}
		}

		#region Helper methods

		private void CloseSubmenu()
		{
			if (this.IsSubmenuOpen)
			{
				ClearValue(SplitButton.IsSubmenuOpenProperty);
				if (this.IsSubmenuOpen)
				{
					this.IsSubmenuOpen = false;
				}
			}
		}

		#endregion Helper methods
	}
}
