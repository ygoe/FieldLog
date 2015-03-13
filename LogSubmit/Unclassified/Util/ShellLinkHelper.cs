using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Unclassified.Util
{
	public static class ShellLinkHelper
	{
		public static void CreateLink(string linkFileName, string startPath, string arguments)
		{
			IShellLink link = (IShellLink) new ShellLink();

			// Setup shortcut information
			//link.SetDescription("My Description");
			link.SetPath(startPath);
			link.SetArguments(arguments);
			link.SetIconLocation(startPath, 0);

			// Save it
			IPersistFile file = (IPersistFile) link;
			file.Save(linkFileName, false);
		}

		// Source: http://stackoverflow.com/a/14632782/143684
		[ComImport]
		[Guid("00021401-0000-0000-C000-000000000046")]
		internal class ShellLink
		{
		}

		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("000214F9-0000-0000-C000-000000000046")]
		internal interface IShellLink
		{
			void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);

			void GetIDList(out IntPtr ppidl);

			void SetIDList(IntPtr pidl);

			void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

			void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

			void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

			void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

			void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

			void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

			void GetHotkey(out short pwHotkey);

			void SetHotkey(short wHotkey);

			void GetShowCmd(out int piShowCmd);

			void SetShowCmd(int iShowCmd);

			void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

			void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

			void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

			void Resolve(IntPtr hwnd, int fFlags);

			void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
		}
	}
}
