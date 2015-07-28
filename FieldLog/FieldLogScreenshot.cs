using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
#if !NET20
using System.Windows;
using System.Windows.Interop;
#endif

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Provides screenshot methods for FieldLog.
	/// </summary>
	public static class FieldLogScreenshot
	{
		#region Static constructor

		static FieldLogScreenshot()
		{
			KeepTime = TimeSpan.FromDays(1);
			MaxTotalSize = 50 * 1024 * 1024;
		}

		#endregion Static constructor

		#region Properties

		/// <summary>
		/// Gets or sets the time that screenshots are kept before purging.
		/// </summary>
		public static TimeSpan KeepTime { get; set; }

		/// <summary>
		/// Gets or sets the maximum total data size of screenshot files before purging them.
		/// </summary>
		public static long MaxTotalSize { get; set; }

		#endregion Properties

		#region Public create methods

		/// <summary>
		/// Creates a screenshot of all screens.
		/// </summary>
		public static void CreateForAllScreens()
		{
			Rectangle rect = new Rectangle();
			foreach (var screen in Screen.AllScreens)
			{
				rect = Rectangle.Union(rect, screen.Bounds);
			}
			CreateForRectangle(rect);
		}

		/// <summary>
		/// Creates a screenshot of the entire primary screen.
		/// </summary>
		public static void CreateForPrimaryScreen()
		{
			Rectangle rect = Screen.PrimaryScreen.Bounds;
			CreateForRectangle(rect);
		}

		/// <summary>
		/// Creates a screenshot of the screen that contains the specified window.
		/// </summary>
		/// <param name="window"></param>
		public static void CreateForWindowScreen(Form window)
		{
			Screen windowScreen = Screen.FromControl(window);
			Rectangle rect = windowScreen.Bounds;
			CreateForRectangle(rect);
		}

#if !NET20

		/// <summary>
		/// Creates a screenshot of the screen that contains the specified window.
		/// </summary>
		/// <param name="window"></param>
		/// <remarks>
		/// This method is only available in the ASPNET build.
		/// </remarks>
		public static void CreateForWindowScreen(Window window)
		{
			Screen windowScreen = Screen.FromHandle(new WindowInteropHelper(window).Handle);
			Rectangle rect = windowScreen.Bounds;
			CreateForRectangle(rect);
		}

#endif

		/// <summary>
		/// Creates a screenshot of the screen that contains the application's main window.
		/// </summary>
		public static void CreateForMainWindowScreen()
		{
#if !NET20
			var wpfApp = System.Windows.Application.Current;
			if (wpfApp != null)
			{
				CreateForWindowScreen(wpfApp.MainWindow);
				return;
			}
#endif
			var winForms = System.Windows.Forms.Application.OpenForms;
			if (winForms != null && winForms.Count > 0)
			{
				CreateForWindowScreen(winForms[0]);
				return;
			}
		}

		/// <summary>
		/// Creates a screenshot of the specified window.
		/// </summary>
		/// <param name="window"></param>
		public static void CreateForWindow(Form window)
		{
			Rectangle rect = window.Bounds;
			CreateForRectangle(rect);
		}

#if !NET20

		/// <summary>
		/// Creates a screenshot of the specified window.
		/// </summary>
		/// <param name="window"></param>
		/// <remarks>
		/// This method is only available in the ASPNET build.
		/// </remarks>
		public static void CreateForWindow(Window window)
		{
			Rectangle rect = new Rectangle((int) window.Left, (int) window.Top, (int) window.Width, (int) window.Height);
			CreateForRectangle(rect);
		}

#endif

		/// <summary>
		/// Creates a screenshot of the application's main window.
		/// </summary>
		public static void CreateForMainWindow()
		{
#if !NET20
			var wpfApp = System.Windows.Application.Current;
			if (wpfApp != null)
			{
				CreateForWindow(wpfApp.MainWindow);
				return;
			}
#endif
			var winForms = System.Windows.Forms.Application.OpenForms;
			if (winForms != null && winForms.Count > 0)
			{
				CreateForWindow(winForms[0]);
				return;
			}
		}

		#endregion Public create methods

		#region Public purge method

		/// <summary>
		/// Purges screenshot files.
		/// </summary>
		public static void Purge()
		{
			try
			{
				string basePath = FL.LogFileBasePath;
				if (basePath == null)
					return;   // Nothing to do
				string logDir = Path.GetDirectoryName(basePath);
				string logFile = Path.GetFileName(basePath);

				DateTime purgeTime = FL.UtcNow.Subtract(KeepTime);
				foreach (string fileName in Directory.GetFiles(logDir, logFile + "-scr-*.*"))
				{
					FileInfo fi = new FileInfo(fileName);
					if (fi.LastWriteTimeUtc < purgeTime)
					{
						// File is old enough to be deleted
						try
						{
							File.Delete(fileName);
						}
						catch
						{
							// Retry next time (might be locked by a log viewer reading the file)
						}
					}
				}

				// Keep maximum data size
				string[] fileNames = Directory.GetFiles(logDir, logFile + "-scr-*.*");
				DateTime[] fileTimes = new DateTime[fileNames.Length];
				long[] fileSizes = new long[fileNames.Length];
				long totalUsedSize = 0;
				for (int i = 0; i < fileNames.Length; i++)
				{
					FileInfo fi = new FileInfo(fileNames[i]);
					fileTimes[i] = fi.LastWriteTimeUtc;
					fileSizes[i] = fi.Length;
					totalUsedSize += fileSizes[i];
				}
				while (totalUsedSize > MaxTotalSize)
				{
					// Find oldest file
					int oldestIndex = -1;
					DateTime oldestTime = DateTime.MaxValue;
					for (int i = 0; i < fileTimes.Length; i++)
					{
						if (fileTimes[i] < oldestTime)
						{
							oldestTime = fileTimes[i];
							oldestIndex = i;
						}
					}
					if (oldestIndex == -1) break;   // Nothing more to delete

					// Delete the file and reduce the total size
					try
					{
						File.Delete(fileNames[oldestIndex]);
						totalUsedSize -= fileSizes[oldestIndex];
					}
					catch
					{
						// Try the next file
					}
					fileTimes[oldestIndex] = DateTime.MaxValue;   // Don't consider this file again
				}
			}
			catch (Exception ex)
			{
				FL.Error(ex, "Purging screenshots");
			}
		}

		#endregion Public purge method

		#region Private methods

		private static void CreateForRectangle(Rectangle rect)
		{
			if (KeepTime <= TimeSpan.Zero || MaxTotalSize <= 0)
				return;   // Nothing to do

			try
			{
				string fileName = GetFileNameWithoutExtension();
				if (fileName != null)
				{
					// Source: http://stackoverflow.com/a/5049138/143684
					// Source: http://stackoverflow.com/a/1163770/143684
					using (Bitmap bitmap = new Bitmap(rect.Width, rect.Height))
					{
						using (Graphics graphics = Graphics.FromImage(bitmap))
						{
							graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
						}

						// Save bitmap as PNG and JPEG
						bitmap.Save(fileName + ".png", ImageFormat.Png);
						SaveJpegImage(bitmap, fileName + ".jpg", 75);

						// Evaluate both file sizes and decide which to keep
						long pngSize = new FileInfo(fileName + ".png").Length;
						long jpgSize = new FileInfo(fileName + ".jpg").Length;
						if (pngSize > jpgSize * 2)
						{
							File.Delete(fileName + ".png");
						}
						else
						{
							File.Delete(fileName + ".jpg");
						}
					}

					Purge();
				}
			}
			catch (Exception ex)
			{
				FL.Error(ex, "Creating screenshot");
			}
		}

		private static string GetFileNameWithoutExtension()
		{
			string basePath = FL.LogFileBasePath;
			if (basePath == null)
				return null;
			return basePath + "-scr-" + FL.UtcNow.Ticks;
		}

		private static void SaveJpegImage(Image img, string filename, int quality)
		{
			ImageCodecInfo codec = null;
			foreach (ImageCodecInfo i in ImageCodecInfo.GetImageDecoders())
			{
				if (i.MimeType == "image/jpeg")
				{
					codec = i;
					break;
				}
			}
			if (codec == null)
				throw new Exception("JPEG codec not found");

			EncoderParameters ep = new EncoderParameters(1);
			ep.Param[0] = new EncoderParameter(Encoder.Quality, (long) quality);

			img.Save(filename, codec, ep);
		}

		#endregion Private methods
	}
}
