using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Unclassified.UI
{
	public static class WindowManager
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct MARGINS
		{
			public int cxLeftWidth;      // width of left border that retains its size 
			public int cxRightWidth;     // width of right border that retains its size 
			public int cyTopHeight;      // height of top border that retains its size 
			public int cyBottomHeight;   // height of bottom border that retains its size
		};

		[DllImport("dwmapi.dll")]
		private static extern void DwmIsCompositionEnabled(ref bool pfEnabled);
		[DllImport("dwmapi.dll")]
		private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMargins);

		public static bool IsCompositionEnabled
		{
			get
			{
				if (Environment.OSVersion.Version.Major < 6)
					return false;

				bool compositionEnabled = false;
				DwmIsCompositionEnabled(ref compositionEnabled);
				return compositionEnabled;
			}
		}

		public static bool ExtendFrameIntoClientArea(Window window, int leftMargin, int topMargin, int rightMargin, int bottomMargin)
		{
			try
			{
				// Obtain the window handle for WPF application
				IntPtr windowPtr = new WindowInteropHelper(window).Handle;
				HwndSource windowSrc = HwndSource.FromHwnd(windowPtr);
				windowSrc.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

				// Get system dpi
				System.Drawing.Graphics desktop = System.Drawing.Graphics.FromHwnd(windowPtr);
				float desktopDpiX = desktop.DpiX;
				float desktopDpiY = desktop.DpiY;

				// Set Margins
				MARGINS margins = new MARGINS();

				// Extend frame into client area 
				// (The default desktop dpi is 96. The margins are adjusted for the system dpi.)
				margins.cxLeftWidth = Convert.ToInt32(leftMargin * (desktopDpiX / 96));
				margins.cxRightWidth = Convert.ToInt32(rightMargin * (desktopDpiX / 96));
				margins.cyTopHeight = Convert.ToInt32(topMargin * (desktopDpiX / 96));
				margins.cyBottomHeight = Convert.ToInt32(bottomMargin * (desktopDpiX / 96));

				int hr = DwmExtendFrameIntoClientArea(windowSrc.Handle, ref margins);
				if (hr < 0)
				{
					// DwmExtendFrameIntoClientArea failed
					return false;
				}
				return true;
			}
			catch (DllNotFoundException)
			{
				return false;
			}
		}
	}
}
