using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unclassified.LogSubmit.Tar;
using Unclassified.TxLib;

namespace Unclassified.LogSubmit.Views
{
	public partial class CompressView : UserControl, IView, SevenZip.ICodeProgress
	{
		#region Private data

		private bool dataReady;
		private DateTime startTime;
		private string archiveFileName1;
		private string archiveFileName2;
		private long totalLogSize;

		//private FileStream gzFile;
		//private GZipStream gzStream;
		private FileStream tarFile;
		private TarWriter tarWriter;
		private List<string> createdDirs = new List<string>();
		private FileStream lzmaInput;
		private FileStream lzmaOutput;

		#endregion Private data

		#region Constructors

		public CompressView()
		{
			InitializeComponent();

			TxDictionaryBinding.AddTextBindings(this);

			Dock = DockStyle.Fill;
		}

		#endregion Constructors

		#region Control event handlers

		private void CompressWorker_DoWork(object sender, DoWorkEventArgs args)
		{
			try
			{
				// Quickly set the UI state
				CompressWorker.ReportProgress(0, new ProgressInfo { CompressedSize = 0 });

				// Collect all data
				DateTime minTime = DateTime.MinValue;
				if (SharedData.Instance.LogTimeSpan != TimeSpan.MaxValue)
				{
					minTime = SharedData.Instance.LastLogUpdateTime - SharedData.Instance.LogTimeSpan;
				}
				string notes = SharedData.Instance.Notes;
				string eMailAddress = SharedData.Instance.EMailAddress;
				List<string> logFiles = new List<string>();
				totalLogSize = 0;
				long processedLogSize = 0;
				foreach (string basePath in SharedData.Instance.LogBasePaths)
				{
					string dir = Path.GetDirectoryName(basePath);
					string baseName = Path.GetFileName(basePath);
					foreach (string logFile in Directory.GetFiles(dir, baseName + "-*-*.fl"))
					{
						FileInfo fi = new FileInfo(logFile);
						if (fi.LastWriteTimeUtc >= minTime)
						{
							logFiles.Add(logFile);
							totalLogSize += fi.Length;
						}
					}
				}

				if (logFiles.Count == 0)
					throw new Exception(Tx.T("msg.no log files selected"));

				InitializeArchive();

				if (!string.IsNullOrWhiteSpace(notes))
				{
					using (var notesStream = new MemoryStream())
					using (var writer = new StreamWriter(notesStream, Encoding.UTF8))
					{
						writer.Write(notes);
						writer.Flush();
						notesStream.Seek(0, SeekOrigin.Begin);
						AddFile(notesStream, "notes.txt", DateTime.UtcNow);
					}
				}

				if (!string.IsNullOrWhiteSpace(eMailAddress))
				{
					using (var eMailStream = new MemoryStream())
					using (var writer = new StreamWriter(eMailStream, Encoding.UTF8))
					{
						writer.Write("[InternetShortcut]\r\nURL=mailto:");
						writer.Write(eMailAddress);
						writer.Flush();
						eMailStream.Seek(0, SeekOrigin.Begin);
						AddFile(eMailStream, "email.url", DateTime.UtcNow);
					}
				}

				EnsureDirectory("log/");
				foreach (string logFile in logFiles)
				{
					if (CompressWorker.CancellationPending)
					{
						args.Cancel = true;
						return;
					}

					FileInfo fi = new FileInfo(logFile);

					AddFile(logFile, "log/" + Path.GetFileName(logFile));

					processedLogSize += fi.Length;
					//int percent = (int) (Math.Round(100.0 * processedLogSize / totalLogSize));
					//CompressWorker.ReportProgress(percent, new ProgressInfo { CompressedSize = gzFile.Length });
				}

				if (CompressWorker.CancellationPending)
				{
					args.Cancel = true;
					return;
				}

				CloseArchive();

				CompressArchive();

				FileInfo archiveInfo = new FileInfo(archiveFileName2);
				CompressWorker.ReportProgress(100, new ProgressInfo { CompressedSize = archiveInfo.Length });
			}
			catch (ObjectDisposedException)
			{
				// We're leaving, don't do anything more.
				// Somehow this isn't caught by the BackgroundWorker management.
				return;
			}
		}

		private void CompressWorker_ProgressChanged(object sender, ProgressChangedEventArgs args)
		{
			progressBar1.Value = args.ProgressPercentage;
			if (args.ProgressPercentage == 0)
			{
				startTime = DateTime.UtcNow;
				RemainingTimeLabel.Text = Tx.T("msg.starting");
				RemainingTimeLabel.ForeColor = SystemColors.ControlText;
			}
			else if (args.ProgressPercentage >= 5)
			{
				double elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
				double totalSeconds = elapsedSeconds * 100 / args.ProgressPercentage;
				if (totalSeconds >= 0 && totalSeconds < int.MaxValue)
				{
					TimeSpan remainingTime = TimeSpan.FromSeconds((int) (totalSeconds - elapsedSeconds) + 1);
					RemainingTimeLabel.Text = Tx.TimeSpanRaw(remainingTime, false);
					RemainingTimeLabel.ForeColor = SystemColors.ControlText;
				}
			}

			ProgressInfo pi = args.UserState as ProgressInfo;
			if (pi != null)
			{
				CompressedSizeLabel.Text = Tx.DataSize(pi.CompressedSize);
				SharedData.Instance.ArchiveFileSize = pi.CompressedSize;
			}
		}

		private void CompressWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
		{
			if (args.Cancelled)
			{
				RemainingTimeLabel.Text = Tx.T("msg.cancelled");
				RemainingTimeLabel.ForeColor = SystemColors.ControlText;
				progressBar1.Value = 0;
				DiscardArchive();
			}
			else if (args.Error != null)
			{
				RemainingTimeLabel.Text = Tx.TC("msg.title.error") + " " + args.Error.Message;
				RemainingTimeLabel.ForeColor = Color.FromArgb(240, 0, 0);
				DiscardArchive();
			}
			else
			{
				RemainingTimeLabel.Text = Tx.T("msg.completed");
				RemainingTimeLabel.ForeColor = Color.FromArgb(0, 160, 0);
				dataReady = true;
				UpdateButtons();
				MainForm.Instance.FocusNextButton();
			}
		}

		#endregion Control event handlers

		#region Public methods

		public void Activate(bool forward)
		{
			if (forward)
			{
				dataReady = false;
				if (!CompressWorker.IsBusy)
				{
					CompressWorker.RunWorkerAsync();
				}
				else
				{
					MessageBox.Show(
						Tx.T("msg.previous operation"),
						Tx.T("msg.title.error"),
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);
				}
			}

			UpdateButtons();
		}

		public void Deactivate(bool forward)
		{
			if (!forward)
			{
				if (CompressWorker.IsBusy)
				{
					CompressWorker.CancelAsync();
				}
				DiscardArchive();
			}
		}

		#endregion Public methods

		#region Private methods

		private void UpdateButtons()
		{
			MainForm.Instance.BackEnabled = true;
			MainForm.Instance.NextEnabled = dataReady;
		}

		private void InitializeArchive()
		{
			if (archiveFileName1 == null)
			{
				archiveFileName1 = Path.GetTempFileName();
				SharedData.Instance.ArchiveFileName = archiveFileName1;
				SharedData.Instance.TempFiles.Add(archiveFileName1);
			}
			//gzFile = File.Create(archiveFileName1);
			//gzFile.SetLength(0);
			//SharedData.Instance.OpenDisposables.Add(gzFile);
			//gzStream = new GZipStream(gzFile, CompressionMode.Compress, false);
			//SharedData.Instance.OpenDisposables.Add(gzStream);
			//tarWriter = new TarWriter(gzStream);
			tarFile = File.Create(archiveFileName1);
			tarFile.SetLength(0);
			SharedData.Instance.OpenDisposables.Add(tarFile);
			tarWriter = new TarWriter(tarFile);
			SharedData.Instance.OpenDisposables.Add(tarWriter);
			createdDirs.Clear();
		}

		private void CloseArchive()
		{
			tarWriter.Close();
			SharedData.Instance.OpenDisposables.Remove(tarWriter);
			tarWriter = null;
			//gzStream.Close();
			//gzStream = null;
			tarFile.Close();
			SharedData.Instance.OpenDisposables.Remove(tarFile);
			tarFile = null;
		}

		private void CompressArchive()
		{
			if (archiveFileName2 == null)
			{
				archiveFileName2 = Path.Combine(Path.GetTempPath(), "log-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".tar.lzma");
				SharedData.Instance.ArchiveFileName = archiveFileName2;
				SharedData.Instance.TempFiles.Add(archiveFileName2);
			}

			// Compress the tar file with LZMA
			// Source: http://stackoverflow.com/a/8605828/143684
			SevenZip.Compression.LZMA.Encoder coder = new SevenZip.Compression.LZMA.Encoder();
			using (lzmaInput = new FileStream(archiveFileName1, FileMode.Open))
			using (lzmaOutput = new FileStream(archiveFileName2, FileMode.Create))   // Creates new or truncates existing
			{
				SharedData.Instance.OpenDisposables.Add(lzmaInput);
				SharedData.Instance.OpenDisposables.Add(lzmaOutput);

				coder.WriteCoderProperties(lzmaOutput);
				// Write the decompressed file size
				lzmaOutput.Write(BitConverter.GetBytes(lzmaInput.Length), 0, 8);
				coder.Code(lzmaInput, lzmaOutput, lzmaInput.Length, -1, this);
			}
			SharedData.Instance.OpenDisposables.Remove(lzmaInput);
			SharedData.Instance.OpenDisposables.Remove(lzmaOutput);

			// Clean up the uncompressed tar file, we only keep the name of the compressed lzma file
			// from now on to delete it later.
			File.Delete(archiveFileName1);
			SharedData.Instance.TempFiles.Remove(archiveFileName1);
			archiveFileName1 = null;
		}

		private void DiscardArchive()
		{
			if (tarWriter != null)
			{
				tarWriter.Close();
				SharedData.Instance.OpenDisposables.Remove(tarWriter);
				tarWriter = null;
			}
			//if (gzStream != null)
			//{
			//    gzStream.Close();
			//    SharedData.Instance.OpenDisposables.Remove(gzStream);
			//    gzStream = null;
			//}
			if (tarFile != null)
			{
				tarFile.Close();
				SharedData.Instance.OpenDisposables.Remove(tarFile);
				tarFile = null;
			}
			if (lzmaInput != null)
			{
				lzmaInput.Close();
				SharedData.Instance.OpenDisposables.Remove(lzmaInput);
				lzmaInput = null;
			}
			if (lzmaOutput != null)
			{
				lzmaOutput.Close();
				SharedData.Instance.OpenDisposables.Remove(lzmaOutput);
				lzmaOutput = null;
			}
			if (archiveFileName1 != null)
			{
				File.Delete(archiveFileName1);
				SharedData.Instance.TempFiles.Remove(archiveFileName1);
				archiveFileName1 = null;
			}
			if (archiveFileName2 != null)
			{
				File.Delete(archiveFileName2);
				SharedData.Instance.TempFiles.Remove(archiveFileName2);
				archiveFileName2 = null;
			}
		}

		/// <summary>
		/// Ensures that all path segments are added to the archive.
		/// </summary>
		private void EnsureDirectory(string path, int userId = 0, int groupId = 0, int mode = 493 /* 0755 */)
		{
			string parentDir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(parentDir))
			{
				EnsureDirectory(parentDir, userId, groupId, mode);
				if (!createdDirs.Contains(parentDir))
				{
					AddDirectory(parentDir, userId, groupId, mode, true);
					createdDirs.Add(parentDir);
				}
			}
		}

		/// <summary>
		/// Adds a directory to the currently opened archive.
		/// </summary>
		private void AddDirectory(string path, int userId = 0, int groupId = 0, int mode = 493 /* 0755 */, bool internalCall = false)
		{
			if (!internalCall)
			{
				EnsureDirectory(path, userId, groupId);
			}

			tarWriter.WriteDirectoryEntry(path, userId, groupId, mode);
			createdDirs.Add(path);
		}

		/// <summary>
		/// Adds a file to the currently opened archive, in the root directory.
		/// </summary>
		private void AddFile(string fileName, int userId = 0, int groupId = 0, int mode = 33188 /* 0100644 */)
		{
			FileInfo fi = new FileInfo(fileName);
			using (var stream = File.OpenRead(fileName))
			{
				tarWriter.Write(stream, fi.Length, fi.Name, userId, groupId, mode, fi.LastWriteTimeUtc);
			}
		}

		/// <summary>
		/// Adds a file to the currently opened archive.
		/// </summary>
		private void AddFile(string fileName, string destFileName, int userId = 0, int groupId = 0, int mode = 33188 /* 0100644 */)
		{
			EnsureDirectory(destFileName, userId, groupId);

			FileInfo fi = new FileInfo(fileName);
			using (var stream = File.OpenRead(fileName))
			{
				tarWriter.Write(stream, fi.Length, destFileName, userId, groupId, mode, fi.LastWriteTimeUtc);
			}
		}

		/// <summary>
		/// Adds a file to the currently opened archive.
		/// </summary>
		private void AddFile(Stream stream, string fileName, DateTime utcModifyTime, int userId = 0, int groupId = 0, int mode = 33188 /* 0100644 */)
		{
			tarWriter.Write(stream, stream.Length, fileName, userId, groupId, mode, utcModifyTime);
		}

		#endregion Private methods

		#region ICodeProgress members

		public void SetProgress(long inSize, long outSize)
		{
			// Called every 100 milliseconds
			int percent = (int) (Math.Round(100.0 * inSize / totalLogSize));
			if (percent > 100) percent = 100;
			CompressWorker.ReportProgress(percent, new ProgressInfo { CompressedSize = outSize });
		}

		#endregion ICodeProgress members

		#region Classes

		private class ProgressInfo
		{
			public long CompressedSize { get; set; }
		}

		#endregion Classes
	}
}
