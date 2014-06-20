using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;

namespace Unclassified.Util
{
	/// <summary>
	/// Provides methods for easy console output, input and formatting.
	/// </summary>
	internal static class ConsoleHelper
	{
		#region Encoding

		/// <summary>
		/// Fixes the encoding of the console window for unsupported UI cultures. This method
		/// should be called once at application startup.
		/// </summary>
		public static void FixEncoding()
		{
			// Source: %windir%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe
			Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture.GetConsoleFallbackUICulture();
			if (Console.OutputEncoding.CodePage != 65001 &&
				Console.OutputEncoding.CodePage != Thread.CurrentThread.CurrentUICulture.TextInfo.OEMCodePage &&
				Console.OutputEncoding.CodePage != Thread.CurrentThread.CurrentUICulture.TextInfo.ANSICodePage)
			{
				Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
			}
		}

		#endregion Encoding

		#region Environment

		#region Native interop

		/// <summary>
		/// Defines values returned by the GetFileType function.
		/// </summary>
		private enum FileType : uint
		{
			/// <summary>The specified file is a character file, typically an LPT device or a console.</summary>
			FileTypeChar = 0x0002,
			/// <summary>The specified file is a disk file.</summary>
			FileTypeDisk = 0x0001,
			/// <summary>The specified file is a socket, a named pipe, or an anonymous pipe.</summary>
			FileTypePipe = 0x0003,
			/// <summary>Unused.</summary>
			FileTypeRemote = 0x8000,
			/// <summary>Either the type of the specified file is unknown, or the function failed.</summary>
			FileTypeUnknown = 0x0000,
		}

		/// <summary>
		/// Defines standard device handles for the GetStdHandle function.
		/// </summary>
		private enum StdHandle : int
		{
			/// <summary>The standard input device. Initially, this is the console input buffer, CONIN$.</summary>
			Input = -10,
			/// <summary>The standard output device. Initially, this is the active console screen buffer, CONOUT$.</summary>
			Output = -11,
			/// <summary>The standard error device. Initially, this is the active console screen buffer, CONOUT$.</summary>
			Error = -12,
		}

		/// <summary>
		/// Retrieves the file type of the specified file.
		/// </summary>
		/// <param name="hFile">A handle to the file.</param>
		/// <returns></returns>
		[DllImport("kernel32.dll")]
		private static extern FileType GetFileType(IntPtr hFile);

		/// <summary>
		/// Retrieves a handle to the specified standard device (standard input, standard output,
		/// or standard error).
		/// </summary>
		/// <param name="nStdHandle">The standard device.</param>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetStdHandle(StdHandle nStdHandle);

		/// <summary>
		/// Retrieves the window handle used by the console associated with the calling process.
		/// </summary>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetConsoleWindow();

		/// <summary>
		/// Determines the visibility state of the specified window.
		/// </summary>
		/// <param name="hWnd">A handle to the window to be tested.</param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		#endregion Native interop

		/// <summary>
		/// Gets a value indicating whether the current application has an interactive console and
		/// is able to interact with the user through it.
		/// </summary>
		public static bool IsInteractiveAndVisible
		{
			get
			{
				IntPtr consoleWnd = GetConsoleWindow();
				return Environment.UserInteractive &&
					consoleWnd != IntPtr.Zero &&
					IsWindowVisible(consoleWnd) &&
					!IsInputRedirected &&
					!IsOutputRedirected &&
					!IsErrorRedirected;
			}
		}

		private static bool? isInputRedirected;
		private static bool? isOutputRedirected;
		private static bool? isErrorRedirected;

		/// <summary>
		/// Gets a value that indicates whether input has been redirected from the standard input
		/// stream.
		/// </summary>
		/// <remarks>
		/// The value is cached after the first access.
		/// </remarks>
		public static bool IsInputRedirected
		{
			get
			{
				if (isInputRedirected == null)
				{
					isInputRedirected = GetFileType(GetStdHandle(StdHandle.Input)) != FileType.FileTypeChar;
				}
				return isInputRedirected == true;
			}
		}

		/// <summary>
		/// Gets a value that indicates whether output has been redirected from the standard output
		/// stream.
		/// </summary>
		/// <remarks>
		/// The value is cached after the first access.
		/// </remarks>
		public static bool IsOutputRedirected
		{
			get
			{
				if (isOutputRedirected == null)
				{
					isOutputRedirected = GetFileType(GetStdHandle(StdHandle.Output)) != FileType.FileTypeChar;
				}
				return isOutputRedirected == true;
			}
		}

		/// <summary>
		/// Gets a value that indicates whether the error output stream has been redirected from the
		/// standard error stream.
		/// </summary>
		/// <remarks>
		/// The value is cached after the first access.
		/// </remarks>
		public static bool IsErrorRedirected
		{
			get
			{
				if (isErrorRedirected == null)
				{
					isErrorRedirected = GetFileType(GetStdHandle(StdHandle.Error)) != FileType.FileTypeChar;
				}
				return isErrorRedirected == true;
			}
		}

		#endregion Environment

		#region Cursor

		/// <summary>
		/// Moves the cursor in the current line.
		/// </summary>
		/// <param name="count">The number of characters to move the cursor. Positive values move to the right, negative to the left.</param>
		public static void MoveCursor(int count)
		{
			if (!IsOutputRedirected)
			{
				int x = Console.CursorLeft + count;
				if (x < 0)
				{
					x = 0;
				}
				if (x >= Console.BufferWidth)
				{
					x = Console.BufferWidth - 1;
				}
				Console.CursorLeft = x;
			}
		}

		/// <summary>
		/// Clears the current line and moves the cursor to the first column.
		/// </summary>
		public static void ClearLine()
		{
			if (!IsOutputRedirected)
			{
				Console.CursorLeft = 0;
				Console.Write(new string(' ', Console.BufferWidth - 1));
				Console.CursorLeft = 0;
			}
			else
			{
				Console.WriteLine();
			}
		}

		#endregion Cursor

		#region Color output

		/// <summary>
		/// Writes a text in a different color. The previous color is restored.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="color"></param>
		public static void Write(string text, ConsoleColor color)
		{
			var oldColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.Write(text);
			Console.ForegroundColor = oldColor;
		}

		/// <summary>
		/// Writes a text in a different color. The previous color is restored.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="textColor"></param>
		/// <param name="backColor"></param>
		public static void Write(string text, ConsoleColor textColor, ConsoleColor backColor)
		{
			var oldTextColor = Console.ForegroundColor;
			var oldBackColor = Console.BackgroundColor;
			Console.ForegroundColor = textColor;
			Console.BackgroundColor = backColor;
			Console.Write(text);
			Console.ForegroundColor = oldTextColor;
			Console.BackgroundColor = oldBackColor;
		}

		/// <summary>
		/// Writes a text in a different color. The previous color is restored.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="color"></param>
		public static void WriteLine(string text, ConsoleColor color)
		{
			var oldColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(text);
			Console.ForegroundColor = oldColor;
		}

		/// <summary>
		/// Writes a text in a different color. The previous color is restored.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="textColor"></param>
		/// <param name="backColor"></param>
		public static void WriteLine(string text, ConsoleColor textColor, ConsoleColor backColor)
		{
			var oldTextColor = Console.ForegroundColor;
			var oldBackColor = Console.BackgroundColor;
			Console.ForegroundColor = textColor;
			Console.BackgroundColor = backColor;
			Console.WriteLine(text);
			Console.ForegroundColor = oldTextColor;
			Console.BackgroundColor = oldBackColor;
		}

		#endregion Color output

		#region Progress bar

		private static string progressTitle;
		private static int progressValue;
		private static int progressTotal;
		private static bool progressHasWarning;
		private static bool progressHasError;

		/// <summary>
		/// Gets or sets the progress title, and updates the displayed progress bar accordingly.
		/// </summary>
		/// <remarks>
		/// A progress bar is only displayed if <see cref="ProgressTotal"/> is greater than zero.
		/// </remarks>
		public static string ProgressTitle
		{
			get { return progressTitle; }
			set
			{
				if (value != progressTitle)
				{
					progressTitle = value;
					WriteProgress();
				}
			}
		}

		/// <summary>
		/// Gets or sets the current value of the progress, and updates the displayed progress bar
		/// accordingly.
		/// </summary>
		/// <remarks>
		/// A progress bar is only displayed if <see cref="ProgressTotal"/> is greater than zero.
		/// </remarks>
		public static int ProgressValue
		{
			get { return progressValue; }
			set
			{
				if (value != progressValue)
				{
					progressValue = value;
					WriteProgress();
				}
			}
		}

		/// <summary>
		/// Gets or sets the total value of the progress, and updates the displayed progress bar
		/// accordingly. Setting a value of zero or less clears the progress bar and resets its
		/// state.
		/// </summary>
		/// <remarks>
		/// A progress bar is only displayed if <see cref="ProgressTotal"/> is greater than zero.
		/// </remarks>
		public static int ProgressTotal
		{
			get { return progressTotal; }
			set
			{
				if (value != progressTotal)
				{
					progressTotal = value;
					WriteProgress();
					if (progressTotal <= 0)
					{
						// Reset progress
						progressValue = 0;
						progressHasWarning = false;
						progressHasError = false;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether a warning occured during processing, and
		/// updates the displayed progress bar accordingly.
		/// </summary>
		/// <remarks>
		/// A progress bar is only displayed if <see cref="ProgressTotal"/> is greater than zero.
		/// </remarks>
		public static bool ProgressHasWarning
		{
			get { return progressHasWarning; }
			set
			{
				if (value != progressHasWarning)
				{
					progressHasWarning = value;
					WriteProgress();
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether an error occured during processing, and updates
		/// the displayed progress bar accordingly.
		/// </summary>
		/// <remarks>
		/// A progress bar is only displayed if <see cref="ProgressTotal"/> is greater than zero.
		/// </remarks>
		public static bool ProgressHasError
		{
			get { return progressHasError; }
			set
			{
				if (value != progressHasError)
				{
					progressHasError = value;
					WriteProgress();
				}
			}
		}

		/// <summary>
		/// Writes the progress info in the current line, replacing the current line.
		/// </summary>
		private static void WriteProgress()
		{
			// Replace the current line with the new progress
			ClearLine();
			if (progressTotal > 0)
			{
				// Range checking
				int value = progressValue;
				if (value < 0) value = 0;
				if (value > progressTotal) value = progressTotal;

				Console.Write(progressTitle + " " + value.ToString().PadLeft(progressTotal.ToString().Length) + "/" + progressTotal + " ");

				// Use almost the entire remaining visible space for the progress bar
				int graphLength = 80;
				if (!IsOutputRedirected)
				{
					graphLength = Console.WindowWidth - Console.CursorLeft - 4;
				}
				int graphPart = progressTotal > 0 ? (int) Math.Round((double) value / progressTotal * graphLength) : 0;

				ConsoleColor graphColor;
				if (progressHasError)
					graphColor = ConsoleColor.DarkRed;
				else if (progressHasWarning)
					graphColor = ConsoleColor.DarkYellow;
				else
					graphColor = ConsoleColor.DarkGreen;
				Write(new string('█', graphPart), graphColor);
				Write(new string('░', graphLength - graphPart), ConsoleColor.DarkGray);
			}
		}

		#endregion Progress bar

		#region Line wrapping

		/// <summary>
		/// Writes a string in multiple lines, limited to the console window width, wrapping at
		/// spaces whenever possible and keeping the first line indenting for wrapped lines.
		/// </summary>
		/// <param name="text">The text to write to the console.</param>
		/// <param name="tableMode">Indents to the last occurence of two spaces; otherwise indents to leading spaces.</param>
		public static void WriteWrapped(string text, bool tableMode = false)
		{
			int width = !IsOutputRedirected ? Console.WindowWidth : 80;
			foreach (string line in text.Split('\n'))
			{
				Console.Write(FormatWrapped(line.TrimEnd(), width, tableMode));
			}
		}

		/// <summary>
		/// Formats a string to multiple lines, limited to the specified width, wrapping at spaces
		/// whenever possible and keeping the first line indenting for wrapped lines.
		/// </summary>
		/// <param name="input">The input string to format.</param>
		/// <param name="width">The available width for wrapping.</param>
		/// <param name="tableMode">Indents to the last occurence of two spaces; otherwise indents to leading spaces.</param>
		/// <returns>The formatted string with line breaks and indenting in every line.</returns>
		public static string FormatWrapped(string input, int width, bool tableMode)
		{
			if (input.TrimEnd() == "") return Environment.NewLine;

			// Detect by how many spaces the text is indented. This amount will be used for every
			// following wrapped line.
			int indent = 0;
			if (tableMode)
			{
				indent = input.LastIndexOf("  ");
				if (indent != -1)
				{
					indent += 2;
				}
				else
				{
					indent = 0;
				}
			}
			else
			{
				while (input[indent] == ' ') indent++;
			}
			string indentStr = "";
			if (indent > 0)
			{
				indentStr = new string(' ', indent);
			}

			string output = "";
			do
			{
				int pos = width - 1;
				if (pos > input.Length)
				{
					pos = input.Length;
				}
				else
				{
					while (pos > 0 && input[pos] != ' ') pos--;
					// If the line cannot be wrapped at a space, write it to the full width
					if (pos == 0) pos = width - 1;
				}
				if (output != "")
				{
					// Prepend indenting spaces for the following lines
					output += indentStr;
				}
				output += input.Substring(0, pos) + Environment.NewLine;
				if (pos + 1 < input.Length)
				{
					input = input.Substring(pos + 1);
					// Reduce the available width by the indenting for the following lines
					width -= indent;
				}
				else
				{
					input = "";
				}
			}
			while (input.Length > 0);
			return output;
		}

		#endregion Line wrapping

		#region Interaction

		/// <summary>
		/// Clears the key input buffer. Any keys that have been pressed but not yet processed
		/// before will be dropped.
		/// </summary>
		public static void ClearKeyBuffer()
		{
			if (!IsInputRedirected)
			{
				while (Console.KeyAvailable)
				{
					Console.ReadKey(true);
				}
			}
		}

		/// <summary>
		/// Determines whether the specified key is an input key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static bool IsInputKey(ConsoleKey key)
		{
			int[] ignore = new[]
			{
				16,    // Shift (left or right)
				17,    // Ctrl (left or right)
				18,    // Alt (left or right)
				19,    // Pause
				20,    // Caps lock
				42,    // Print
				44,    // Print screen
				91,    // Windows key (left)
				92,    // Windows key (right)
				93,    // Menu key
				144,   // Num lock
				145,   // Scroll lock
				166,   // Back
				167,   // Forward
				168,   // Refresh
				169,   // Stop
				170,   // Search
				171,   // Favorites
				172,   // Start/Home
				173,   // Mute
				174,   // Volume Down
				175,   // Volume Up
				176,   // Next Track
				177,   // Previous Track
				178,   // Stop Media
				179,   // Play
				180,   // Mail
				181,   // Select Media
				182,   // Application 1
				183    // Application 2
			};
			return !ignore.Contains((int) key);
		}

		/// <summary>
		/// Waits for the user to press any key if in interactive mode and input is not redirected.
		/// </summary>
		/// <param name="message">The message to display. If null, a standard message is displayed.</param>
		/// <param name="timeout">The time in seconds until the method returns even if no key was pressed. If -1, the timeout is infinite.</param>
		/// <param name="showDots">true to show a dot for every second of the timeout, removing one dot each second.</param>
		public static void Wait(string message = null, int timeout = -1, bool showDots = false)
		{
			if (Environment.UserInteractive && !IsInputRedirected)
			{
				if (message == null)
				{
					message = "Press any key to continue...";
				}

				if (message != "")
				{
					ClearLine();
					Console.Write(message);
				}
				if (timeout < 0)
				{
					ClearKeyBuffer();
					// Wait for a real input key
					while (!IsInputKey(Console.ReadKey(true).Key))
					{
					}
				}
				else
				{
					int counter;
					if (showDots)
					{
						counter = timeout;
						while (counter > 0)
						{
							counter--;
							Console.Write(".");
						}
						timeout *= 1000;   // Convert to milliseconds
						counter = 0;
						int step = 100;   // Sleeping duration
						int nextSecond = 1000;
						ClearKeyBuffer();
						while (!(Console.KeyAvailable && IsInputKey(Console.ReadKey(true).Key)) && counter < timeout)
						{
							Thread.Sleep(step);
							counter += step;
							if (showDots && counter > nextSecond)
							{
								nextSecond += 1000;
								MoveCursor(-1);
								Console.Write(" ");
								MoveCursor(-1);
							}
						}
						ClearKeyBuffer();
					}
					if (message != "")
					{
						Console.WriteLine();
					}
				}
			}
		}

		/// <summary>
		/// Waits for the user to press any key if in debugging mode.
		/// </summary>
		/// <remarks>
		/// Visual Studio will wait after the program terminates only if not debugging. When the
		/// program was started with debugging, the console window is closed immediately. This
		/// method can be called at the end of the program to always wait once and be able to
		/// evaluate the last console output.
		/// </remarks>
		public static void WaitIfDebug()
		{
			if (Debugger.IsAttached)
			{
				Wait("Press any key to quit...");
			}
		}

		/// <summary>
		/// Writes an error message in red color, waits for a key and returns the specified exit
		/// code for passing it directly to the return statement.
		/// </summary>
		/// <param name="message">The error message to write.</param>
		/// <param name="exitCode">The exit code to return.</param>
		/// <returns></returns>
		public static int ExitError(string message, int exitCode)
		{
			ClearLine();
			WriteLine(message, ConsoleColor.Red);
			WaitIfDebug();
			return exitCode;
		}

		#endregion Interaction
	}
}
