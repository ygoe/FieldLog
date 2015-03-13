using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using Unclassified.Util;

namespace Unclassified.UI
{
	public class UIPreferences
	{
		/// <summary>
		/// Gets the system's hand mouse cursor, used for hyperlinks.
		/// The .NET framework only gives its internal cursor but not the one that the user has set in their profile.
		/// </summary>
		public static Cursor HandCursor
		{
			get
			{
				RegistryKey cursorsKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors");
				if (cursorsKey != null)
				{
					object o = cursorsKey.GetValue("Hand");
					if (o is string)
					{
						IntPtr cursorHandle = WinApi.LoadCursorFromFile((string) o);
						return new Cursor(cursorHandle);
					}
				}
				return Cursors.Hand;
			}
		}

		public static void UpdateFormFont(Form form, Font oldFont, Font newFont)
		{
			if (!oldFont.Equals(newFont))
			{
				UpdateFont(form, oldFont, newFont);

				// Minimise and restore the window to correct any layout errors.

				// Temporarily disable minimise/restore animation because it should not be visible anyway.
				bool animationEnabled = WinApi.IsMinimizeRestoreAnimationEnabled();
				if (animationEnabled)
					WinApi.SetMinimizeRestoreAnimation(false);

				FormWindowState prevState = form.WindowState;
				form.WindowState = FormWindowState.Minimized;
				form.WindowState = prevState;

				if (animationEnabled)
					WinApi.SetMinimizeRestoreAnimation(true);
			}
		}

		public static void UpdateFont(Control ctl, Font oldFont, Font newFont)
		{
			if (!oldFont.Equals(newFont))
			{
				foreach (Control c in ctl.Controls)
				{
					UpdateFont(c, oldFont, newFont);
				}

				if (ctl.Font != newFont)
				{
					if (ctl.Font.FontFamily.Name == oldFont.FontFamily.Name)
					{
						ctl.Font = new Font(newFont.FontFamily, ctl.Font.SizeInPoints * (newFont.SizeInPoints / oldFont.SizeInPoints), ctl.Font.Style);
					}
				}
			}
		}
	}
}
