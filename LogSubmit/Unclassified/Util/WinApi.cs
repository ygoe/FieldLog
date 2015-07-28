using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Unclassified.Util
{
	public partial class WinApi
	{
		#region SystemParameterInfo

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SystemParametersInfo(SPI uiAction, uint uiParam, IntPtr pvParam, SPIF fWinIni);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SystemParametersInfo(SPI uiAction, uint uiParam, String pvParam, SPIF fWinIni);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SystemParametersInfo(SPI uiAction, uint uiParam, ref ANIMATIONINFO pvParam, SPIF fWinIni);

		/// <summary>
		/// SPI_ System-wide parameter - Used in SystemParametersInfo function
		/// </summary>
		[Description("SPI_(System-wide parameter - Used in SystemParametersInfo function )")]
		public enum SPI : uint
		{
			/// <summary>
			/// Retrieves the animation effects associated with user actions. The pvParam parameter must point to an ANIMATIONINFO structure
			/// that receives the information. Set the cbSize member of this structure and the uiParam parameter to sizeof(ANIMATIONINFO).
			/// </summary>
			SPI_GETANIMATION = 0x0048,

			/// <summary>
			/// Sets the animation effects associated with user actions. The pvParam parameter must point to an ANIMATIONINFO structure
			/// that contains the new parameters. Set the cbSize member of this structure and the uiParam parameter to sizeof(ANIMATIONINFO).
			/// </summary>
			SPI_SETANIMATION = 0x0049

			// NOTE: More to be found here: http://pinvoke.net/default.aspx/Enums/SPI.html
		}

		[Flags]
		public enum SPIF
		{
			None = 0x00,
			/// <summary>Writes the new system-wide parameter setting to the user profile.</summary>
			SPIF_UPDATEINIFILE = 0x01,
			/// <summary>Broadcasts the WM_SETTINGCHANGE message after updating the user profile.</summary>
			SPIF_SENDCHANGE = 0x02,
			/// <summary>Same as SPIF_SENDCHANGE.</summary>
			SPIF_SENDWININICHANGE = 0x02
		}

		/// <summary>
		/// ANIMATIONINFO specifies animation effects associated with user actions.
		/// Used with SystemParametersInfo when SPI_GETANIMATION or SPI_SETANIMATION action is specified.
		/// </summary>
		/// <remark>
		/// The uiParam value must be set to (System.UInt32)Marshal.SizeOf(typeof(ANIMATIONINFO)) when using this structure.
		/// </remark>
		[StructLayout(LayoutKind.Sequential)]
		public struct ANIMATIONINFO
		{
			/// <summary>
			/// Creates an AMINMATIONINFO structure. Always use this constructor as it presets the <code>cbSize</code> field.
			/// </summary>
			/// <param name="iMinAnimate">If non-zero and SPI_SETANIMATION is specified, enables minimize/restore animation.</param>
			public ANIMATIONINFO(System.Int32 iMinAnimate)
			{
				this.cbSize = (System.UInt32)Marshal.SizeOf(typeof(ANIMATIONINFO));
				this.iMinAnimate = iMinAnimate;
			}

			/// <summary>
			/// Always must be set to (System.UInt32)Marshal.SizeOf(typeof(ANIMATIONINFO)).
			/// </summary>
			public System.UInt32 cbSize;

			/// <summary>
			/// If non-zero, minimize/restore animation is enabled, otherwise disabled.
			/// </summary>
			public System.Int32 iMinAnimate;
		}

		#endregion SystemParameterInfo

		public static bool IsMinimizeRestoreAnimationEnabled()
		{
			ANIMATIONINFO anim = new ANIMATIONINFO(0);
			SystemParametersInfo(SPI.SPI_GETANIMATION, anim.cbSize, ref anim, SPIF.None);
			return anim.iMinAnimate != 0;
		}

		public static void SetMinimizeRestoreAnimation(bool enable)
		{
			ANIMATIONINFO anim = new ANIMATIONINFO(enable ? 1 : 0);
			SystemParametersInfo(SPI.SPI_SETANIMATION, anim.cbSize, ref anim, SPIF.None);
		}

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern IntPtr LoadCursorFromFile(string path);

		public const uint WM_NCHITTEST = 0x84;

		// From: http://www.pinvoke.net/default.aspx/Enums/WindowHitTestRegions.html
		/// <summary>Options available when a form is tested for mose positions.</summary>
		public enum WindowHitTestRegions
		{
			/// <summary>HTERROR: On the screen background or on a dividing line between windows
			/// (same as HTNOWHERE, except that the DefWindowProc function produces a system
			/// beep to indicate an error).</summary>
			Error = -2,
			/// <summary>HTTRANSPARENT: In a window currently covered by another window in the
			/// same thread (the message will be sent to underlying windows in the same thread
			/// until one of them returns a code that is not HTTRANSPARENT).</summary>
			TransparentOrCovered = -1,
			/// <summary>HTNOWHERE: On the screen background or on a dividing line between
			/// windows.</summary>
			NoWhere = 0,
			/// <summary>HTCLIENT: In a client area.</summary>
			ClientArea = 1,
			/// <summary>HTCAPTION: In a title bar.</summary>
			TitleBar = 2,
			/// <summary>HTSYSMENU: In a window menu or in a Close button in a child window.</summary>
			SystemMenu = 3,
			/// <summary>HTGROWBOX: In a size box (same as HTSIZE).</summary>
			GrowBox = 4,
			/// <summary>HTMENU: In a menu.</summary>
			Menu = 5,
			/// <summary>HTHSCROLL: In a horizontal scroll bar.</summary>
			HorizontalScrollBar = 6,
			/// <summary>HTVSCROLL: In the vertical scroll bar.</summary>
			VerticalScrollBar = 7,
			/// <summary>HTMINBUTTON: In a Minimize button. </summary>
			MinimizeButton = 8,
			/// <summary>HTMAXBUTTON: In a Maximize button.</summary>
			MaximizeButton = 9,
			/// <summary>HTLEFT: In the left border of a resizable window (the user can click
			/// the mouse to resize the window horizontally).</summary>
			LeftSizeableBorder = 10,
			/// <summary>HTRIGHT: In the right border of a resizable window (the user can click
			/// the mouse to resize the window horizontally).</summary>
			RightSizeableBorder = 11,
			/// <summary>HTTOP: In the upper-horizontal border of a window.</summary>
			TopSizeableBorder = 12,
			/// <summary>HTTOPLEFT: In the upper-left corner of a window border.</summary>
			TopLeftSizeableCorner = 13,
			/// <summary>HTTOPRIGHT: In the upper-right corner of a window border.</summary>
			TopRightSizeableCorner = 14,
			/// <summary>HTBOTTOM: In the lower-horizontal border of a resizable window (the
			/// user can click the mouse to resize the window vertically).</summary>
			BottomSizeableBorder = 15,
			/// <summary>HTBOTTOMLEFT: In the lower-left corner of a border of a resizable
			/// window (the user can click the mouse to resize the window diagonally).</summary>
			BottomLeftSizeableCorner = 16,
			/// <summary>HTBOTTOMRIGHT: In the lower-right corner of a border of a resizable
			/// window (the user can click the mouse to resize the window diagonally).</summary>
			BottomRightSizeableCorner = 17,
			/// <summary>HTBORDER: In the border of a window that does not have a sizing
			/// border.</summary>
			NonSizableBorder = 18,
			/// <summary>HTOBJECT: Unknown...No Documentation Found</summary>
			Object = 19,
			/// <summary>HTCLOSE: In a Close button.</summary>
			CloseButton = 20,
			/// <summary>HTHELP: In a Help button.</summary>
			HelpButton = 21,
			/// <summary>HTSIZE: In a size box (same as HTGROWBOX). (Same as GrowBox).</summary>
			SizeBox = GrowBox,
			/// <summary>HTREDUCE: In a Minimize button. (Same as MinimizeButton).</summary>
			ReduceButton = MinimizeButton,
			/// <summary>HTZOOM: In a Maximize button. (Same as MaximizeButton).</summary>
			ZoomButton = MaximizeButton
		}
	}
}
