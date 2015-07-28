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
using Unclassified.Util;

namespace Unclassified.Automation
{
	public class Window
	{
		private delegate bool EnumWindowsDelegate(IntPtr handle, IntPtr lParam);

		[DllImport("user32")]
		private static extern int EnumWindows(EnumWindowsDelegate cb, IntPtr lParam);

		[DllImport("user32")]
		private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

		private IntPtr handle = IntPtr.Zero;
		private WindowProperties searchProperties;

		/// <summary>
		/// Creates a new Window object finding an existing window by its handle.
		/// </summary>
		/// <param name="hWnd">Window handle to match</param>
		public Window(IntPtr hWnd)
		{
			handle = hWnd;
		}

		/// <summary>
		/// Creates a new Window object finding an existing window by search properties.
		/// </summary>
		/// <param name="properties"></param>
		public Window(WindowProperties properties)
		{
			searchProperties = properties;

			Find();
		}

		public IntPtr Handle
		{
			get
			{
				return handle;
			}
		}

		public bool Exists
		{
			get
			{
				return handle != IntPtr.Zero && WinApi.IsWindow(handle);
			}
		}

		public int ProcessId
		{
			get
			{
				int processId;
				GetWindowThreadProcessId(handle, out processId);
				return processId;
			}
		}

		public Rectangle Rectangle
		{
			get
			{
				Rectangle rect;
				WinApi.GetWindowRect(handle, out rect);
				return rect;
			}
		}

		public uint Style
		{
			get
			{
				return (uint)WinApi.GetWindowLong(handle, WinApi.GWL_STYLE).ToInt64();
			}
		}

		public uint ExStyle
		{
			get
			{
				return (uint)WinApi.GetWindowLong(handle, WinApi.GWL_EXSTYLE).ToInt64();
			}
		}

		private void Find()
		{
			EnumWindows(EnumWindowsProcedure, IntPtr.Zero);
			handle = searchProperties.Handle;
		}

		/// <summary>
		/// Compares each found window with the search criteria.
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="lParam"></param>
		/// <returns>true if the window does not match and the next window should be tested. false if the window matches</returns>
		private bool EnumWindowsProcedure(IntPtr handle, IntPtr lParam)
		{
			// Test each criteria
			if (searchProperties.ClassName != null && searchProperties.ClassName != GetClassName(handle)) return true;
			if (searchProperties.ProcessId != 0 && searchProperties.ProcessId != GetProcessId(handle)) return true;
			if (searchProperties.Visible != null && searchProperties.Visible != WinApi.IsWindowVisible(handle)) return true;

			// All required properties match, stop the search and save the window handle
			searchProperties.Handle = handle;
			return false;
		}

		/// <summary>
		/// Determines the class name of a window by its handle.
		/// </summary>
		/// <param name="handle"></param>
		/// <returns></returns>
		public static string GetClassName(IntPtr handle)
		{
			StringBuilder className = new StringBuilder(256);
			WinApi.GetClassName(handle, className, 256);
			return className.ToString();
		}

		/// <summary>
		/// Gets the process ID of the window.
		/// </summary>
		/// <returns></returns>
		public static int GetProcessId(IntPtr handle)
		{
			int processId;
			GetWindowThreadProcessId(handle, out processId);
			return processId;
		}

		public static bool TryFind(WindowProperties prop)
		{
			Window w = new Window(prop);
			return w.Exists;
		}

		public static bool TryFind(WindowProperties prop, out Window window)
		{
			window = new Window(prop);
			if (window.Exists)
				return true;
			window = null;
			return false;
		}
	}
}
