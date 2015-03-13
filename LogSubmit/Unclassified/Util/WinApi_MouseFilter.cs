// Copyright (c) 2009, Yves Goergen
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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace Unclassified.Util
{
	public partial class WinApi
	{
		// From WinApi_Windows.cs
		// Windows messages
		public const uint WM_MOUSEMOVE = 0x200;
		public const uint WM_MOUSEWHEEL = 0x20A;

		// GetWindowLong constants
		public const int GWL_STYLE = -16;
		public const int GWL_EXSTYLE = -20;

		[DllImport("user32")]
		public static extern bool IsWindow(IntPtr hWnd);
		[DllImport("user32")]
		public static extern bool IsWindowVisible(IntPtr hWnd);
		[DllImport("user32")]
		public static extern int GetClassName(IntPtr hWnd, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buf, int nMaxCount);
		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);
		[DllImport("user32")]
		public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
		[DllImport("user32")]
		public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll")]
		public static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);
		[DllImport("user32.dll")]
		public static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

		//[DllImport("user32")]
		//public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll", EntryPoint = "GetWindowLong")]
		private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
		private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

		// This static method is required because Win32 does not support GetWindowLongPtr directly
		public static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
		{
			if (IntPtr.Size == 8)
				return GetWindowLongPtr64(hWnd, nIndex);
			else
				return GetWindowLongPtr32(hWnd, nIndex);
		}

		// From WinApi_Input.cs
		[DllImport("user32")]
		public static extern bool AttachThreadInput(int idAttach, int idAttachTo, bool fAttach);
	}
}
