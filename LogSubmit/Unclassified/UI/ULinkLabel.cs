using System;
using System.Windows.Forms;

namespace Unclassified.UI
{
	public class ULinkLabel : LinkLabel
	{
		private Cursor handCursor;

		protected override void WndProc(ref Message msg)
		{
			if (OverrideCursor == Cursors.Hand)
			{
				// Fetch the real hand cursor from the system settings and cache it
				if (handCursor == null)
				{
					handCursor = UIPreferences.HandCursor;
				}
				// Use the system's hand cursor instead of .NET's internal hand cursor
				OverrideCursor = handCursor;
			}
			else if (handCursor != null && OverrideCursor != handCursor)
			{
				// Forget the cached cursor
				handCursor.Dispose();
				handCursor = null;
			}
			base.WndProc(ref msg);
		}
	}
}
