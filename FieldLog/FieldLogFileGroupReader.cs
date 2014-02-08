using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Implements a virtual log file reader that reads all log files concurrently and returns the
	/// log items from all files in the order of their time value.
	/// </summary>
	/// <remarks>
	/// This class is full of .NET 4.0 code so it's not included in the NET20 project altogether.
	/// </remarks>
	public class FieldLogFileGroupReader
	{
		private FileSystemWatcher fsw;

		private Dictionary<FieldLogPriority, FieldLogFileEnumerator> readers = new Dictionary<FieldLogPriority, FieldLogFileEnumerator>();
		private Task<bool>[] readTasks;
		private readonly object readerLock = new object();
		private Task<bool> waitForNewFileTask;
		private Task<bool> closeTask;
		private AutoResetEvent newFileEvent = new AutoResetEvent(false);
		private AutoResetEvent closeEvent = new AutoResetEvent(false);
		private Dictionary<FieldLogPriority, EventWaitHandle> prioReadSignals = new Dictionary<FieldLogPriority, EventWaitHandle>();

		/// <summary>
		/// Initialises a new instance of the FieldLogFileGroupReader class. This sets up log
		/// readers for each priority and links additional readers for existing files to them.
		/// A FileSystemWatcher is set up to add new files as they are created.
		/// </summary>
		/// <param name="basePath">The path and file prefix of the log files to read.</param>
		/// <param name="readWaitHandle">The wait handle that will be signalled after all files
		/// have been read to the end and if the last reader is now going to wait for further data
		/// to be appended to the file.</param>
		public FieldLogFileGroupReader(string basePath, EventWaitHandle readWaitHandle = null)
		{
			// Start file system watcher to detect new files
			string logDir = Path.GetDirectoryName(basePath);
			string logFile = Path.GetFileName(basePath);
			fsw = new FileSystemWatcher(logDir, logFile + "-*.fl");
			fsw.NotifyFilter = NotifyFilters.FileName;
			fsw.Created += fsw_Created;
			fsw.EnableRaisingEvents = true;

			// Find all log files for every priority
			var prioValues = Enum.GetValues(typeof(FieldLogPriority));
			readTasks = new Task<bool>[prioValues.Length];
			foreach (FieldLogPriority prio in prioValues)
			{
				FindLogFiles(basePath, prio);
			}

			// Wait for all priorities to be read to the end, then signal one event
			if (readWaitHandle != null)
			{
				Task.Factory.StartNew(() =>
				{
					if (prioReadSignals.Count > 0)
					{
						WaitHandle.WaitAll(prioReadSignals.Values.ToArray());
					}
					readWaitHandle.Set();
				});
			}

			waitForNewFileTask = Task.Factory.StartNew<bool>(WaitForNewFile);
			closeTask = Task.Factory.StartNew<bool>(WaitForClose);
		}

		/// <summary>
		/// Implements the task that waits for a new log file to be created.
		/// </summary>
		/// <returns>The return value is not used.</returns>
		private bool WaitForNewFile()
		{
			newFileEvent.WaitOne();
			return false;
		}

		/// <summary>
		/// Implements the task that waits for the close event.
		/// </summary>
		/// <returns>The return value is not used.</returns>
		private bool WaitForClose()
		{
			closeEvent.WaitOne();
			return false;
		}

		/// <summary>
		/// Called when the FileSystemWatcher found a newly created file of the currently used
		/// log file set.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void fsw_Created(object sender, FileSystemEventArgs e)
		{
			if (closeEvent.WaitOne(0)) return;   // Already closing...

			// Ensure it's a file, not a directory
			if (File.Exists(e.FullPath))
			{
				lock (readerLock)
				{
					Match m = Regex.Match(e.FullPath, @"-([0-9])-[0-9]{18}\.fl");
					if (m.Success)
					{
						FieldLogPriority prio = (FieldLogPriority) int.Parse(m.Groups[1].Value);
						AddNewReader(prio, e.FullPath, true);
					}
				}
			}
		}

		/// <summary>
		/// Finds all currently existing log files and adds a new reader for each of them.
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="prio"></param>
		private void FindLogFiles(string basePath, FieldLogPriority prio)
		{
			lock (readerLock)
			{
				// Remove any files that were added from the FileSystemWatcher event before we
				// started scanning for files. Those files will now be found again anyway.
				readers[prio] = null;
				
				string logDir = Path.GetDirectoryName(basePath);
				string logFile = Path.GetFileName(basePath);
				List<string> fileNames = new List<string>(Directory.GetFiles(logDir, logFile + "-" + (int) prio + "-*.fl"));
				fileNames.Sort();
				foreach (string fileName in fileNames)
				{
					AddNewReader(prio, fileName, false);
				}
			}
		}

		/// <summary>
		/// Creates a new log file reader and adds it to the priority's log file enumerator.
		/// </summary>
		/// <param name="prio"></param>
		/// <param name="fileName"></param>
		/// <param name="fromFsw"></param>
		private void AddNewReader(FieldLogPriority prio, string fileName, bool fromFsw)
		{
			// Must be within a lock(readerLock)!

			// Reject the new file if it's already in the queue (delayed FSW event after active scan)
			if (readers[prio] != null &&
				readers[prio].ContainsFile(fileName))
			{
				// This file is already current or queued
				return;
			}
			
			var reader = new FieldLogFileReader(fileName, true);
			if (!fromFsw)
			{
				EventWaitHandle h;
				if (!prioReadSignals.TryGetValue(prio, out h))
				{
					h = new AutoResetEvent(false);
					prioReadSignals[prio] = h;
				}
				reader.ReadWaitHandle = h;
			}

			if (readers[prio] == null)
			{
				// This is the first file of this priority
				readers[prio] = new FieldLogFileEnumerator(reader);
				readTasks[(int) prio] = Task<bool>.Factory.StartNew(readers[prio].MoveNext);
			}
			else
			{
				// Chain the new reader after the last reader in the queue
				readers[prio].LastReader.NextReader = reader;
			}

			// Signal the blocking ReadLogItem method that there's a new reader now
			newFileEvent.Set();
		}

		/// <summary>
		/// Reads the next log item from the log file group. If all files have been read until the
		/// end, this method blocks until a new log item was written to any file, or until the
		/// close event was set.
		/// </summary>
		/// <returns>The next log item, or null if there are no more log items and the waiting was cancelled.</returns>
		public FieldLogItem ReadLogItem()
		{
			while (true)
			{
				Task<bool>[] availableTasks;
				lock (readerLock)
				{
					availableTasks = readTasks
						.Where(t => t != null)
						.Union(new Task<bool>[] { waitForNewFileTask })
						.Union(new Task<bool>[] { closeTask })
						.ToArray();
				}
				Task.WaitAny(availableTasks);
				// We don't care about which task has finished. It may actually be multiple tasks.
				// We just test them all and use the result of all tasks that have finished.
				// Compare all completed readers' current value, find the smallest time and move
				// that enumerator one further.
				DateTime minTime = DateTime.MaxValue;
				FieldLogItem minTimeItem = null;
				FieldLogPriority minTimePrio = 0;
				foreach (var availableTask in availableTasks)
				{
					if (availableTask.IsCompleted)
					{
						// Search from the lowest priority up, as lower priorities appear more often
						for (int prioInt = 0; prioInt < readTasks.Length; prioInt++)
						{
							if (availableTask == readTasks[prioInt])
							{
								// A reader enumerator task has finished, consider its result
								FieldLogPriority prio = (FieldLogPriority) prioInt;
								if (availableTask.Result)
								{
									// A new item of this priority is available.
									// Fetch the item from the reader.
									FieldLogItem item = readers[prio].Current;
									if (item.Time < minTime)
									{
										// The item's time is before any other item in this run, remember it
										minTime = item.Time;
										minTimeItem = item;
										minTimePrio = prio;
									}
								}
								else
								{
									// This priority's reader has finished, remove it.
									// Lock so that AddNewReader won't mess up if it finds a new file for this priority.
									lock (readerLock)
									{
										readers[prio].Dispose();
										readers[prio] = null;
										readTasks[prioInt] = null;
									}
								}
							}
						}
						if (availableTask == waitForNewFileTask)
						{
							// A new file was added and the WaitAny was interrupted, to restart
							// with the newly added reader.
							// Recreate the signal task and continue.
							waitForNewFileTask = Task<bool>.Factory.StartNew(WaitForNewFile);
						}
						if (availableTask == closeTask)
						{
							// The reader was requested to close.
							// Close all current enumerators, which then close all readers and
							// everything should tidy up itself...
							fsw.EnableRaisingEvents = false;
							fsw.Dispose();
							foreach (var reader in readers.Values)
							{
								if (reader != null)
								{
									reader.Close();
								}
							}
							return null;
						}
					}
				}
				if (minTimeItem != null)
				{
					// We found an item.
					// Create new task for the next item of this priority
					var task = Task<bool>.Factory.StartNew(readers[minTimePrio].MoveNext);
					readTasks[(int) minTimePrio] = task;
					// Now return the next log item in time of all that are currently available
					return minTimeItem;
				}
				// Restart this loop and wait for the next event
			}
		}

		/// <summary>
		/// Starts a new Task that calls the ReadLogItem method.
		/// </summary>
		/// <returns>The new Task instance.</returns>
		public Task<FieldLogItem> ReadLogItemAsync()
		{
			return Task<FieldLogItem>.Factory.StartNew(ReadLogItem);
		}

		/// <summary>
		/// Sets the close signal to stop waiting for and reading log items.
		/// </summary>
		public void Close()
		{
			closeEvent.Set();
		}
	}
}
