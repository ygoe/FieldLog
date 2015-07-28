using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Unclassified.TxLib;
using Unclassified.Util;

namespace Unclassified.LogSubmit.Views
{
	public partial class LogSelectionView : UserControl, IView
	{
		#region Private data

		private bool dirListMode;
		private bool fullScanMode;
		private Dictionary<string, LogBasePathInfo> logBasePathData = new Dictionary<string, LogBasePathInfo>();
		private ListViewColumnSorter logDirsColumnSorter;
		private bool isUpdatingColumns;
		private List<string> ignoredDirectories = new List<string>();

		#endregion Private data

		#region Constructors

		public LogSelectionView()
		{
			InitializeComponent();

			TxDictionaryBinding.AddTextBindings(this);
			LogBasePathHeader.Text = Tx.T("log selection view.list.log base path");
			LastUpdateHeader.Text = Tx.T("log selection view.list.last update");
			SizeHeader.Text = Tx.T("log selection view.list.size");

			Dock = DockStyle.Fill;

			// Set up and initialise column sorting
			logDirsColumnSorter = new ListViewColumnSorter(LogDirsListView);
			logDirsColumnSorter.SortColumn = 0;
			logDirsColumnSorter.Order = SortOrder.Ascending;
			logDirsColumnSorter.Update();
			LogDirsListView.ListViewItemSorter = logDirsColumnSorter;

			// Force-enable double buffering in the ListView to prevent flickering
			var prop = LogDirsListView.GetType().GetProperty(
				"DoubleBuffered",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (prop != null)
			{
				prop.SetValue(LogDirsListView, true, null);
			}
		}

		#endregion Constructors

		#region Event handlers

		private void LogSelectionView_FontChanged(object sender, EventArgs args)
		{
			int width;

			isUpdatingColumns = true;

			width = TextRenderer.MeasureText(LastUpdateHeader.Text, Font).Width;
			for (int days = 0; days >= -8; days--)
			{
				width = Math.Max(width, TextRenderer.MeasureText(CommonFormats.DateTimeToShortString(DateTime.Now.AddDays(days)), Font).Width);
			}
			LastUpdateHeader.Width = width + 6;

			width = TextRenderer.MeasureText(SizeHeader.Text, Font).Width;
			width = Math.Max(width, TextRenderer.MeasureText("9999 MB", Font).Width);
			SizeHeader.Width = width + 6;

			isUpdatingColumns = false;
		}

		#endregion Event handlers

		#region Control event handlers

		private void FindLogsButton_Click(object sender, EventArgs args)
		{
			CurrentLabel.Hide();
			SelectedLogDirText.Hide();
			FindLogsButton.Hide();
			ConfigErrorLabel.Hide();
			LogDirsListView.Items.Clear();
			LogDirsListView.Show();
			dirListMode = true;
			UpdateButtons();

			if (ignoredDirectories.Count == 0)
			{
				ignoredDirectories.Add(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\");
				ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Adobe\\"));
				ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Common Files\\"));
				ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google\\"));
				ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft"));
				ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Reference Assemblies\\"));
				ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windows "));
				if (Environment.Is64BitOperatingSystem)
				{
					ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Adobe\\"));
					ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Common Files\\"));
					ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google\\"));
					ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft"));
					ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Reference Assemblies\\"));
					ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windows "));
				}
				ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Adobe\\"));
				ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft\\"));
				ignoredDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Package Cache\\"));
			}

			fullScanMode = true;
			ScanDirectoryWorker.RunWorkerAsync();
			MainForm.Instance.SetProgress(true, -1);
		}

		private void BrowseLogButton_Click(object sender, EventArgs args)
		{
			using (OpenFileDialog dlg = new OpenFileDialog())
			{
				dlg.FileName = SelectedLogDirText.Text;
				dlg.Filter = Tx.T("log selection view.file dialog.filter");
				if (!string.IsNullOrEmpty(SelectedLogDirText.Text))
				{
					dlg.InitialDirectory = Path.GetDirectoryName(SelectedLogDirText.Text);
				}
				dlg.Title = Tx.T("log selection view.file dialog.title");

				if (dlg.ShowDialog() == DialogResult.OK)
				{
					if (ScanDirectoryWorker.IsBusy)
					{
						ScanDirectoryWorker.CancelAsync();
					}

					string basePath = GetBasePath(dlg.FileName);
					SelectedLogDirText.Text = basePath;
					try
					{
						SetLogBasePath(basePath);
					}
					catch
					{
						MessageBox.Show(
							Tx.T("msg.logpath parameter invalid", "value", basePath),
							Tx.T("msg.title.error"),
							MessageBoxButtons.OK,
							MessageBoxIcon.Error);
						ResetLogBasePath();
						FindLogBasePath();
						return;
					}

					CurrentLabel.Show();
					SelectedLogDirText.Show();
					FindLogsButton.Show();
					LogDirsListView.Hide();
					dirListMode = false;
					fullScanMode = false;
					UpdateButtons();
				}
			}
		}

		private void LogDirsListView_ClientSizeChanged(object sender, EventArgs args)
		{
			DelayedCall.Start(UpdateColumnWidths, 1);
		}

		private void LogDirsListView_ColumnClick(object sender, ColumnClickEventArgs args)
		{
			logDirsColumnSorter.HandleColumnClick(args.Column);
		}

		private void LogDirsListView_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs args)
		{
			if (!isUpdatingColumns)
			{
				args.NewWidth = LogDirsListView.Columns[args.ColumnIndex].Width;
				args.Cancel = true;
			}
		}

		private void LogDirsListView_SelectedIndexChanged(object sender, EventArgs args)
		{
			UpdateButtons();
			if (LogDirsListView.SelectedItems.Count > 0)
			{
				SharedData.Instance.LogBasePaths = LogDirsListView.SelectedItems
					.OfType<ListViewItem>()
					.Select(lvi => ((LogBasePathInfo) lvi.Tag).LogBasePath)
					.ToArray();
				SharedData.Instance.LastLogUpdateTime = LogDirsListView.SelectedItems
					.OfType<ListViewItem>()
					.Select(lvi => ((LogBasePathInfo) lvi.Tag).UpdatedTime)
					.Max();
			}
		}

		private void ScanDirectoryWorker_DoWork(object sender, DoWorkEventArgs args)
		{
			foreach (var data in logBasePathData)
			{
				data.Value.Size = 0;
			}

			foreach (var driveInfo in DriveInfo.GetDrives())
			{
				if (driveInfo.IsReady &&
					driveInfo.DriveType == DriveType.Fixed)
				{
					bool result;
					result = ScanDirectory(driveInfo.RootDirectory.Name);
					if (!result)
					{
						args.Cancel = true;
						return;
					}
				}
			}
		}

		private void ScanDirectoryWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
		{
			MainForm.Instance.SetProgress(false, 0);
		}

		#endregion Control event handlers

		#region Public methods

		public void Activate(bool forward)
		{
			if (SharedData.Instance.FromErrorDialog ||
				SharedData.Instance.FromShortcut)
			{
				FindLogsButton.Visible = false;
			}

			UpdateButtons();

			if (!forward && fullScanMode)
			{
				if (!ScanDirectoryWorker.IsBusy)
				{
					ScanDirectoryWorker.RunWorkerAsync();
					MainForm.Instance.SetProgress(true, -1);
				}
			}
		}

		public void Deactivate(bool forward)
		{
			if (ScanDirectoryWorker.IsBusy)
			{
				ScanDirectoryWorker.CancelAsync();
			}
		}

		public void SetLogBasePath(string basePath)
		{
			SelectedLogDirText.Text = basePath;
			UpdateButtons();
			SharedData.Instance.LogBasePaths = new[] { basePath };

			// Find last update time
			DateTime updatedTime = DateTime.MinValue;
			string dir = Path.GetDirectoryName(basePath);
			string baseName = Path.GetFileName(basePath);
			foreach (string logFile in Directory.GetFiles(dir, baseName + "-?-*.fl"))
			{
				FileInfo fi = new FileInfo(logFile);
				if (fi.LastWriteTimeUtc > updatedTime)
				{
					updatedTime = fi.LastWriteTimeUtc;
				}
			}
			foreach (string logFile in Directory.GetFiles(dir, baseName + "-scr-*.*"))
			{
				FileInfo fi = new FileInfo(logFile);
				if (fi.LastWriteTimeUtc > updatedTime)
				{
					updatedTime = fi.LastWriteTimeUtc;
				}
			}
			if (updatedTime == DateTime.MinValue)
			{
				updatedTime = DateTime.UtcNow;
			}
			SharedData.Instance.LastLogUpdateTime = updatedTime;
		}

		public void ResetLogBasePath()
		{
			SelectedLogDirText.Text = "";
			UpdateButtons();
			SharedData.Instance.LogBasePaths = null;
		}

		public void FindLogBasePath()
		{
			string appDir = Path.GetDirectoryName(Application.ExecutablePath);
			Dictionary<string, LogBasePathInfo> logPaths = new Dictionary<string, LogBasePathInfo>();

			// Find *.flconfig in appDir
			foreach (string fileName in Directory.GetFiles(appDir, "*.flconfig"))
			{
				// Read the path setting
				var config = new ConfigReader(fileName);
				string basePath = config.ReadPath();
				// If a log file was found in that path, put it on the list
				if (!string.IsNullOrEmpty(basePath))
				{
					CheckLogBasePath(basePath, logPaths);
				}
			}

			// Find *.exe in appDir
			foreach (string fileName in Directory.GetFiles(appDir, "*.exe"))
			{
				string baseName = Path.GetFileNameWithoutExtension(fileName);
				// Go through all possible default log paths for the exe file
				// If a log file was found in a path, put it on the list
				CheckLogBasePath(Path.Combine(appDir, "log", baseName), logPaths);
				CheckLogBasePath(Path.Combine(appDir, baseName), logPaths);
				CheckLogBasePath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), baseName + "-log", baseName), logPaths);
				CheckLogBasePath(Path.Combine(Path.GetTempPath(), baseName + "-log", baseName), logPaths);
			}

			if (logPaths.Count == 1)
			{
				SetLogBasePath(logPaths.Values.First().LogBasePath);
			}
			else if (logPaths.Count > 1)
			{
				LogDirsListView.Items.Clear();
				foreach (var kvp in logPaths)
				{
					AddDirectory(kvp.Value);
				}
				// Sort by latest update time
				logDirsColumnSorter.SortColumn = 1;
				logDirsColumnSorter.Order = SortOrder.Descending;
				logDirsColumnSorter.Update();

				CurrentLabel.Hide();
				SelectedLogDirText.Hide();
				LogDirsListView.Show();
				dirListMode = true;
				UpdateButtons();
			}
		}

		public void SetConfigError(Exception ex)
		{
			ConfigErrorLabel.Text = Tx.T("log selection view.config error", "msg", ex.Message);
			ConfigErrorLabel.Show();
		}

		public void TakeFocus()
		{
			if (dirListMode)
			{
				LogDirsListView.Focus();
			}
		}

		#endregion Public methods

		#region Private methods

		private void UpdateColumnWidths()
		{
			isUpdatingColumns = true;
			LogBasePathHeader.Width = LogDirsListView.ClientSize.Width - LastUpdateHeader.Width - SizeHeader.Width;
			isUpdatingColumns = false;
		}

		private void UpdateButtons()
		{
			MainForm.Instance.BackEnabled = false;
			MainForm.Instance.NextEnabled =
				!dirListMode && !string.IsNullOrEmpty(SelectedLogDirText.Text) ||
				dirListMode && LogDirsListView.SelectedItems.Count > 0;
		}

		private string GetBasePath(string path)
		{
			Match match = Regex.Match(path, @"^(.+)-(?:[0-9]-[0-9]{18}\.fl|scr-[0-9]{18}\.(?:png|jpg))$");
			if (match.Success)
			{
				return match.Groups[1].Value;
			}
			return path;
		}

		private bool ScanDirectory(string path)
		{
			if (ScanDirectoryWorker.CancellationPending) return false;

			int level = 0;
			string parent = path;
			while ((parent = Path.GetDirectoryName(parent)) != null)
			{
				level++;
			}

			DirectoryInfo di = new DirectoryInfo(path);
			if (level >= 1 &&
				(di.Attributes & FileAttributes.Hidden) != 0 &&
				(di.Attributes & FileAttributes.System) != 0) return true;

			foreach (string dir in ignoredDirectories)
			{
				if (IsPath(path, dir)) return true;
			}
			if (Path.GetFileName(path) == ".git") return true;
			if (Path.GetFileName(path) == ".svn") return true;
			if (level >= 8) return true;

			try
			{
				var interestingFiles =
					Directory.GetFiles(path, "*-?-*.fl")
					.Concat(Directory.GetFiles(path, "*-scr-*.png"))
					.Concat(Directory.GetFiles(path, "*-scr-*.jpg"));
				foreach (string file in interestingFiles)
				{
					if (ScanDirectoryWorker.CancellationPending) return false;

					string basePath = GetBasePath(file);
					if (basePath == null) continue;

					FileInfo fi = new FileInfo(file);
					DateTime updatedTime = fi.LastWriteTimeUtc;
					LogBasePathInfo info;
					if (logBasePathData.TryGetValue(basePath, out info))
					{
						if (updatedTime > info.UpdatedTime)
						{
							info.UpdatedTime = updatedTime;
						}
						info.Size += fi.Length;
						BeginInvoke(new Action<LogBasePathInfo>(UpdateDirectory), info);
					}
					else
					{
						info = new LogBasePathInfo
						{
							LogBasePath = basePath,
							UpdatedTime = updatedTime,
							Size = fi.Length
						};
						logBasePathData.Add(basePath, info);
						BeginInvoke(new Action<LogBasePathInfo>(AddDirectory), info);
					}
				}

				foreach (string subdir in Directory.GetDirectories(path))
				{
					if (ScanDirectoryWorker.CancellationPending) return false;

					bool result = ScanDirectory(subdir);
					if (!result) return false;
				}
			}
			catch (UnauthorizedAccessException)
			{
				// Ignore unauthorised access
			}
			return true;
		}

		private bool IsPath(string path, string path2)
		{
			// Only allow full directory name matches
			path += "\\";
			if (path.StartsWith(path2, StringComparison.OrdinalIgnoreCase)) return true;
			return false;
		}

		private void AddDirectory(LogBasePathInfo info)
		{
			ListViewItem lvi = new ListViewItem(info.LogBasePath);
			lvi.Tag = info;
			lvi.SubItems.Add(CommonFormats.DateTimeToShortString(info.UpdatedTime.ToLocalTime()));
			lvi.SubItems.Add(Tx.DataSize(info.Size));
			LogDirsListView.Items.Add(lvi);
			info.ListViewItem = lvi;
		}

		private void UpdateDirectory(LogBasePathInfo info)
		{
			info.ListViewItem.SubItems[1].Text = CommonFormats.DateTimeToShortString(info.UpdatedTime.ToLocalTime());
			info.ListViewItem.SubItems[2].Text = Tx.DataSize(info.Size);
		}

		private void CheckLogBasePath(string basePath, Dictionary<string, LogBasePathInfo> logPaths)
		{
			string dir = Path.GetDirectoryName(basePath);
			string baseName = Path.GetFileName(basePath);

			try
			{
				foreach (string logFile in Directory.GetFiles(dir, baseName + "-?-*.fl"))
				{
					FileInfo fi = new FileInfo(logFile);
					DateTime updatedTime = fi.LastWriteTimeUtc;
					LogBasePathInfo info;
					if (logPaths.TryGetValue(basePath, out info))
					{
						if (updatedTime > info.UpdatedTime)
						{
							info.UpdatedTime = updatedTime;
						}
						info.Size += fi.Length;
					}
					else
					{
						info = new LogBasePathInfo
						{
							LogBasePath = basePath,
							UpdatedTime = updatedTime,
							Size = fi.Length
						};
						logPaths.Add(basePath, info);
					}
				}
			}
			catch
			{
				// Ignore this base path, we can't use it
			}
		}

		#endregion Private methods

		#region Classes

		internal class LogBasePathInfo
		{
			public DateTime UpdatedTime { get; set; }
			public ListViewItem ListViewItem { get; set; }
			public string LogBasePath { get; set; }
			public long Size { get; set; }
		}

		#endregion Classes
	}
}
