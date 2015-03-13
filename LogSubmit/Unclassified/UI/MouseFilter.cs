// Copyright (c) 2012, Yves Goergen
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted
// provided that the following conditions are met:
//
// * Redistributions of source code must retain the above copyright notice, this list of conditions
//   and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of
//   conditions and the following disclaimer in the documentation and/or other materials provided
//   with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
// OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Unclassified.Automation;
using Unclassified.Util;

namespace Unclassified
{
	/// <summary>
	/// Provides mouse filtering features like hiding the cursor after a timeout of inactivity or dispatching
	/// mouse wheel input the the control below the mouse cursor.
	/// </summary>
	class MouseFilter : Component, IMessageFilter
	{
		// AutoHideCursor variables
		private bool autoHideCursor = false;
		private bool cursorHidden = false;
		private DelayedCall hideCursorTimer;
		private Point lastMousePoint = new Point(-1, -1);   // Invalid point, so any first seen mouse position is different

		// DispatchMouseWheel variables
		private bool dispatchMouseWheel = false;

		/// <summary>
		/// Fired when the mouse cursor was hidden.
		/// </summary>
		[Description("Fired when the mouse cursor was hidden.")]
		public event EventHandler MouseHidden;
		/// <summary>
		/// Fired when the mouse cursor was shown.
		/// </summary>
		[Description("Fired when the mouse cursor was shown.")]
		public event EventHandler MouseShown;

		public MouseFilter()
		{
			InitializeComponent();
		}

		public MouseFilter(IContainer container)
		{
			container.Add(this);
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			hideCursorTimer = DelayedCall.Create(HideCursor, 2000);
			if (!DesignMode)
			{
				Application.AddMessageFilter(this);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (hideCursorTimer != null) hideCursorTimer.Dispose();
			if (!DesignMode)
			{
				Application.RemoveMessageFilter(this);   // Removing it more than once is okay
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the mouse cursor should be hidden after it has not been moved for a while.
		/// </summary>
		[DefaultValue(false)]
		[Description("Hides the mouse cursor after a while of inactivity.")]
		public bool AutoHideCursor
		{
			get { return autoHideCursor; }
			set
			{
				autoHideCursor = value;
				if (!DesignMode)
				{
					if (value)
					{
						HideCursor();
						DelayedCall.Start(HideCursor, 100);
					}
					else
					{
						hideCursorTimer.Cancel();
						ShowCursor();
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether mouse wheel input should be dispatched to the control below the mouse cursor.
		/// </summary>
		/// <remarks>Normally, mouse wheel input always goes to the control which holds the keyboard
		/// input focus. But since the wheel is mounted to the mouse and not the keyboard, it should
		/// respect the mouse cursor's position more than the keyboard focus. Due to the way how Windows
		/// sends the corresponding messages however, you cannot scroll in an inactive window even with
		/// the cursor over it.</remarks>
		[DefaultValue(false)]
		[Description("Enables dispatching of mouse wheel input to the control below the mouse cursor.")]
		public bool DispatchMouseWheel
		{
			get { return dispatchMouseWheel; }
			set { dispatchMouseWheel = value; }
		}

		public bool PreFilterMessage(ref Message m)
		{
			if (m.Msg == WinApi.WM_MOUSEMOVE && autoHideCursor)
			{
				Point p = new Point(m.LParam.ToInt32());
				WinApi.ClientToScreen(m.HWnd, ref p);
				if (p != lastMousePoint)
				{
					lastMousePoint = p;
					ShowCursor();
					hideCursorTimer.Reset();
				}
				return false;   // Don't block this message
			}
			else if (m.Msg == WinApi.WM_MOUSEWHEEL && dispatchMouseWheel)
			{
				Point p = new Point(m.LParam.ToInt32());

				// First, see whether there's an open combobox list popup that must be handled differently
				WindowProperties prop = new WindowProperties();
				prop.ProcessId = new Window(m.HWnd).ProcessId;
				prop.ClassName = "ComboLBox";
				prop.Visible = true;
				Window popup;
				if (Window.TryFind(prop, out popup))
				{
					// Found a combobox popup window, check coordinates
					if (popup.Rectangle.Contains(p))
					{
						// Mouse cursor is over visible combo listbox popup, let the message pass
						return false;
					}
					// Mouse cursor is outside the popup, block the message
					// (Do not scroll the popup because the mouse is not in it;
					// but also do not scroll anything else outside the popup)
					return true;
				}
				
				// Regular window surface: find the deepest control below the mouse cursor
				Control control = Form.ActiveForm;
				while (control != null)
				{
					Point clientPoint = control.PointToClient(p);
					Control subControl = control.GetChildAtPoint(clientPoint, GetChildAtPointSkip.Invisible);
					if (subControl == null || subControl is ScrollBar || subControl is ComboBox) break;
					//System.Diagnostics.Debug.WriteLine("Found control in " + control.Name + " at " + clientPoint + ": " + subControl.Name);
					control = subControl;
				}
				if (control != null)
				{
					if (control.Handle == m.HWnd) return false;   // The message already arrived for this control, let it pass

					// Search up the parents for a scrollable panel
					Control c = control;
					while (!(c is Form))
					{
						Panel panel = c as Panel;
						if (panel != null && panel.AutoScroll)
						{
							Point scrollPos = panel.AutoScrollPosition;
							// Position report is always negative, but new values must be set positive...
							scrollPos.X = -scrollPos.X;
							scrollPos.Y = -scrollPos.Y;
							int delta = m.WParam.ToInt32() >> 16;
							delta /= 120;   // Convert to number of wheel notches
							scrollPos.Y += -delta * 20;   // Delta value is "negative" (neg is downwards, pos is upwards)
							panel.AutoScrollPosition = scrollPos;

							return true;   // Block this message
						}
						
						c = c.Parent;
					}
					
					// NOTE: Possible performance improvement: Flag the re-posted message so that we can
					//       recognise it faster when it re-arrives here and we can let it pass.
					//       Use either a very high delta value (HIWORD(wParam)) or one of the modifier
					//       keys MK_* (LOWORD(wParam)) or an unassigned bit of lParam.

					//System.Diagnostics.Debug.WriteLine("Re-posting message to " + control.Name);
					WinApi.PostMessage(control.Handle, WinApi.WM_MOUSEWHEEL, m.WParam, m.LParam);

					// If IMessageModifyAndFilter wasn't System.Windows.Form's internal, we could just
					// implement that interface, modify the message and let it pass changed. But so we
					// need to block the message and send a new one that we're going to let pass then.
				}
				return true;   // Block this message
			}
			return false;   // Don't block this message
		}

		/// <summary>
		/// Hides the cursor if it's currently shown.
		/// </summary>
		/// <remarks>Since calls to <c>Cursor.Hide</c> and <c>Cursor.Show</c> must be symmetrical,
		/// these functions keep their own counter to make sure they're not called too often.</remarks>
		private void HideCursor()
		{
			if (!cursorHidden)
			{
				Cursor.Hide();
				cursorHidden = true;
				if (MouseHidden != null) MouseHidden(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Shows the cursor if it's currently hidden.
		/// </summary>
		/// <remarks>Since calls to <c>Cursor.Hide</c> and <c>Cursor.Show</c> must be symmetrical,
		/// these functions keep their own counter to make sure they're not called too often.</remarks>
		private void ShowCursor()
		{
			if (cursorHidden)
			{
				Cursor.Show();
				cursorHidden = false;
				if (MouseShown != null) MouseShown(this, EventArgs.Empty);
			}
		}
	}
}
