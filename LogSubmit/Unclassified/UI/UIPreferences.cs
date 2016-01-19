using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

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
						IntPtr cursorHandle = NativeMethods.LoadCursorFromFile((string)o);
						return new Cursor(cursorHandle);
					}
				}
				return Cursors.Hand;
			}
		}

		/// <summary>
		/// Sets the font for a Form or Control and all its child controls, regarding different
		/// font style and size as set in the VS designer.
		/// </summary>
		/// <param name="ctl"></param>
		/// <param name="oldFont"></param>
		/// <param name="newFont"></param>
		public static void SetFont(Control ctl, Font oldFont, Font newFont, int recursion = 0)
		{
			if (oldFont != newFont)
			{
				ctl.SuspendLayout();
				foreach (Control subCtl in ctl.Controls)
				{
					SetFont(subCtl, oldFont, newFont, recursion + 1);
				}

				if (ctl.Font != oldFont)
				{
					if (ctl.Font.FontFamily.Name == oldFont.FontFamily.Name)
					{
						ctl.Font = new Font(newFont.FontFamily, ctl.Font.SizeInPoints * (newFont.SizeInPoints / oldFont.SizeInPoints), ctl.Font.Style);
					}
				}

				if (recursion == 0)
				{
					ctl.Font = newFont;
				}
				ctl.ResumeLayout();
			}
		}

		/// <summary>
		/// Updates the font for a Form and all its child controls after it has been created, i.e.
		/// upon a theme change while the Form was open. See SetFont for details.
		/// </summary>
		/// <param name="form"></param>
		/// <param name="oldFont"></param>
		/// <param name="newFont"></param>
		public static void UpdateFont(Form form, Font oldFont, Font newFont)
		{
			if (!oldFont.Equals(newFont))
			{
				SetFont(form, oldFont, newFont);
			}
		}

		/// <summary>
		/// Updates a Form or Control and all its child controls for theming changes, especially
		/// for Windows Aero.
		/// </summary>
		/// <param name="ctl"></param>
		public static void UpdateControlTheme(Control ctl, bool? theme = null, bool? aero = null)
		{
			if (aero == null)
			{
				aero = false;
				theme = false;
				switch (Application.VisualStyleState)
				{
					case System.Windows.Forms.VisualStyles.VisualStyleState.ClientAndNonClientAreasEnabled:
					case System.Windows.Forms.VisualStyles.VisualStyleState.ClientAreaEnabled:
						theme = true;
						break;
				}
				if (theme.Value && Environment.OSVersion.Version.Major >= 6)
				{
					aero = true;
				}
			}

			ctl.SuspendLayout();
			ListView listview = ctl as ListView;
			TreeView treeview = ctl as TreeView;
			CheckBox checkbox = ctl as CheckBox;
			Button button = ctl as Button;
			Panel panel = ctl as Panel;

			if (listview != null)
			{
				if (aero.Value)
					NativeMethods.EnableExplorerTheme(listview);
			}
			else if (treeview != null)
			{
				if (aero.Value)
					NativeMethods.EnableExplorerTheme(treeview);
				treeview.FullRowSelect = aero.Value;
				treeview.HotTracking = aero.Value;
			}
			else if (checkbox != null)
			{
				checkbox.FlatStyle = aero.Value ? FlatStyle.System : FlatStyle.Standard;
			}
			else if (button != null)
			{
				if (button.Image == null &&
					button.ForeColor == SystemColors.ControlText)
				{
					button.FlatStyle = aero.Value ? FlatStyle.System : FlatStyle.Standard;
				}
			}
			else if (panel != null)
			{
				if (panel.BorderStyle == BorderStyle.Fixed3D && theme.Value)
				{
					panel.BorderStyle = BorderStyle.FixedSingle;
				}
				else if (panel.BorderStyle == BorderStyle.FixedSingle && !theme.Value)
				{
					panel.BorderStyle = BorderStyle.Fixed3D;
				}
			}

			foreach (Control subCtl in ctl.Controls)
			{
				UpdateControlTheme(subCtl, theme.Value, aero.Value);
			}
			ctl.ResumeLayout();
		}

		public static string GetMonospaceFontFamily()
		{
			string consolas = null;
			string andaleMono = null;
			string courierNew = null;
			foreach (FontFamily ff in FontFamily.Families)
			{
				if (ff.Name == "Consolas") consolas = ff.Name;
				else if (ff.Name == "Andale Mono") andaleMono = ff.Name;
				else if (ff.Name == "Courier New") courierNew = ff.Name;
			}
			if (consolas != null) return consolas;
			if (andaleMono != null) return andaleMono;
			if (courierNew != null) return courierNew;
			return FontFamily.GenericMonospace.Name;
		}

		#region Windows theming

		public static bool IsThemeActive
		{
			get
			{
				// NOTE: Application.VisualStyleState always returns active theming which is not true.

				// Source: http://www.codeproject.com/Articles/10564/How-to-accurately-detect-if-an-application-is-them
				try
				{
					return NativeMethods.IsAppThemed() && NativeMethods.IsThemeActive();
				}
				catch
				{
					return false;
				}
			}
		}

		public static bool IsAeroThemeActive
		{
			get
			{
				return Environment.OSVersion.Version.Major >= 6 && IsThemeActive;
			}
		}

		#endregion Windows theming

		private static class NativeMethods
		{
			[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
			public static extern IntPtr LoadCursorFromFile(string path);

			[DllImport("uxtheme.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
			public static extern int SetWindowTheme(IntPtr hWnd, string appName, string partList);

			[DllImport("uxtheme.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public extern static bool IsAppThemed();

			[DllImport("uxtheme.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public extern static bool IsThemeActive();

			/// <summary>
			/// Enables the visual theme for ListView and TreeView controls, best visible with Windows 7 Aero theme.
			/// </summary>
			/// <param name="c">ListView or TreeView control to enable theming for.</param>
			public static void EnableExplorerTheme(Control c)
			{
				try
				{
					SetWindowTheme(c.Handle, "explorer", null);
				}
				catch
				{
					// Ignore DLL not found errors (and anything else)
				}
			}
		}
	}
}
