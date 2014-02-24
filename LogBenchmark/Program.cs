//#define WITH_NLOG

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Unclassified.FieldLog;

namespace LogBenchmark
{
	class Program
	{
		/// <summary>
		/// Application entry point.
		/// </summary>
		/// <param name="args">Command line arguments</param>
		static void Main(string[] args)
		{
			new Program().Run();
		}

		/// <summary>
		/// Indicates whether the test progress in % is displayed. Takes a considerable amount of
		/// time and doesn't seem to be too deterministic so it influences the test results.
		/// </summary>
		bool showProgress = false;
		/// <summary>
		/// Indicates whether the user must confirm to continue after each test. Doesn't seem to
		/// make any difference on the test results.
		/// </summary>
		bool waitBetweenTests = false;
		/// <summary>
		/// The type of messages to write.
		/// 0: Dynamically concatenated strings.
		/// 1: Prepared concatenated strings.
		/// 2: Constant strings.
		/// </summary>
		int messageType = 1;
		/// <summary>
		/// Indicates whether the fieldLog benchmark writes text messages or scopes.
		/// </summary>
		bool flScope = false;
		/// <summary>
		/// Indicates whether FL.NewScope should determine the current method name through reflection.
		/// </summary>
		bool flScopeReflection = false;
		/// <summary>
		/// The iteration time for the empty loop for reference. Intended to compensate the progress
		/// display, otherwise not necessary.
		/// </summary>
		long emptyLoopTime;

		/// <summary>
		/// Runs the tests.
		/// </summary>
		void Run()
		{
			Console.WriteLine("Log benchmark");
			Console.WriteLine();

			// Delete all files in the log directory
			Console.WriteLine("Deleting old log files...");
			Console.WriteLine();
			string logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "log");
			if (Directory.Exists(logPath))
			{
				foreach (string file in Directory.GetFiles(logPath))
				{
					File.Delete(file);
				}
			}
			
			FL.AcceptLogFileBasePath();

			// Wait a bit for the system to calm down from starting this program
			System.Threading.Thread.Sleep(1000);

			// For correct-in-context number formatting, if used
			//Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			RunEmptyLoop(100000000);
			RunLock(100000000);
			PressEnterToContinue();

			// This test setup can either test text messages or scopes.
			if (!flScope)
			{
				RunFieldLogText(1000000);
				PressEnterToContinue();
			}
			else
			{
				RunFieldLogScope(500000);
				PressEnterToContinue();
			}

			//RunDebugOutputString(100000);
			PressEnterToContinue();

			RunFileAppend(1000000);
			PressEnterToContinue();

			RunFileOpenAppend(2000);

#if WITH_NLOG
			PressEnterToContinue();
			RunNLog(1000000);
#endif

			Console.WriteLine("Benchmark finished. Press the Enter key to quit.");
			Console.ReadLine();
		}

		void PressEnterToContinue()
		{
			if (waitBetweenTests)
			{
				Console.WriteLine("Press the Enter key to continue.");
				Console.ReadLine();
				Console.CursorTop -= 2;
				Console.WriteLine("                                    ");
				Console.WriteLine("                                    ");
				Console.CursorTop -= 2;
			}

			System.Threading.Thread.Sleep(1000);
		}

		void RunEmptyLoop(long iterations)
		{
			Stopwatch stopwatch = new Stopwatch();

			Console.WriteLine("Testing empty loop (for reference)...");

			stopwatch.Start();

			long percent = 0;
			for (long i = 1; i <= iterations; i++)
			{
				if (showProgress)
				{
					long newPercent = i * 100 / iterations;
					if (newPercent > percent)
					{
						percent = newPercent;
						Console.CursorLeft = 0;
						Console.Write("  " + percent.ToString("##0") + " %");
					}
				}

				if (i < 0) break;   // Never happens, hope the optimising compiler doesn't find out
			}
			Console.CursorLeft = 0;
			Console.Write("          ");
			Console.CursorLeft = 0;

			stopwatch.Stop();

			long nanoseconds = (long) Math.Round((decimal) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000000 / iterations);
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(8) + " ns/call");

			emptyLoopTime = nanoseconds;
			Console.WriteLine("Following measurements have this time subtracted.");
			Console.WriteLine();

			GC.Collect();
		}

		void RunLock(long iterations)
		{
			Stopwatch stopwatch = new Stopwatch();
			object syncLock = new object();

			Console.WriteLine("Testing lock synchronisation...");

			stopwatch.Start();

			long percent = 0;
			for (long i = 1; i <= iterations; i++)
			{
				if (showProgress)
				{
					long newPercent = i * 100 / iterations;
					if (newPercent > percent)
					{
						percent = newPercent;
						Console.CursorLeft = 0;
						Console.Write("  " + percent.ToString("##0") + " %");
					}
				}

				lock (syncLock)
				{
					if (i < 0) break;   // Never happens, hope the optimising compiler doesn't find out
				}
			}
			Console.CursorLeft = 0;
			Console.Write("          ");
			Console.CursorLeft = 0;

			stopwatch.Stop();

			long nanoseconds = (long) Math.Round((decimal) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000000 / iterations);
			nanoseconds -= emptyLoopTime;
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(8) + " ns/call");
			Console.WriteLine();

			GC.Collect();
		}

		void RunFieldLogText(long iterations)
		{
			Stopwatch stopwatch = new Stopwatch();

			Console.WriteLine("Testing FieldLog text messages...");

			// Disable backlog waiting for this benchmark, we'll measure the flush time separately
			FL.WaitForItemsBacklog = false;

			// Prepare strings to write because string concatenation also takes some time
			string[] strings = new string[iterations];
			for (long i = 1; i <= iterations; i++)
			{
				strings[i - 1] = "Benchmark message - " + i;
			}

			stopwatch.Start();

			long percent = 0;
			for (long i = 1; i <= iterations; i++)
			{
				if (showProgress)
				{
					long newPercent = i * 100 / iterations;
					if (newPercent > percent)
					{
						percent = newPercent;
						Console.CursorLeft = 0;
						Console.Write("  " + percent.ToString("##0") + " %");
					}
				}

				switch (messageType)
				{
					case 0:
						FL.Trace("Benchmark message - " + i);
						break;
					case 1:
						FL.Trace(strings[i - 1]);
						break;
					case 2:
						FL.Trace("Benchmark message");
						break;
				}
			}
			Console.CursorLeft = 0;
			Console.Write("          ");
			Console.CursorLeft = 0;

			stopwatch.Stop();

			long nanoseconds = (long) Math.Round((decimal) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000000 / iterations);
			nanoseconds -= emptyLoopTime;
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(8) + " ns/call");

			// Make sure all log items are written, as long as we have the time to wait for it
			// (there's only ~3 seconds on process shutdown)
			Stopwatch flFlushStopwatch = new Stopwatch();
			flFlushStopwatch.Start();
			FL.Shutdown();
			flFlushStopwatch.Stop();
			Console.WriteLine("Flushing FieldLog to files took " + flFlushStopwatch.ElapsedMilliseconds + " ms");
			Console.WriteLine();

			GC.Collect();
		}

		void RunFieldLogScope(long iterations)
		{
			Stopwatch stopwatch = new Stopwatch();

			Console.WriteLine("Testing FieldLog scope helper...");

			// Disable backlog waiting for this benchmark, we'll measure the flush time separately
			FL.WaitForItemsBacklog = false;

			stopwatch.Start();

			long percent = 0;
			for (long i = 1; i <= iterations; i++)
			{
				if (showProgress)
				{
					long newPercent = i * 100 / iterations;
					if (newPercent > percent)
					{
						percent = newPercent;
						Console.CursorLeft = 0;
						Console.Write("  " + percent.ToString("##0") + " %");
					}
				}

				if (flScopeReflection)
				{
					using (FL.Scope())
					{
					}
				}
				else
				{
					using (FL.Scope("Scope name"))
					{
					}
				}
			}
			Console.CursorLeft = 0;
			Console.Write("          ");
			Console.CursorLeft = 0;

			stopwatch.Stop();

			long nanoseconds = (long) Math.Round((decimal) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000000 / iterations);
			nanoseconds -= emptyLoopTime;
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(8) + " ns/call");

			// Make sure all log items are written, as long as we have the time to wait for it
			// (there's only ~3 seconds on process shutdown)
			Stopwatch flFlushStopwatch = new Stopwatch();
			flFlushStopwatch.Start();
			FL.Shutdown();
			flFlushStopwatch.Stop();
			Console.WriteLine("Flushing FieldLog to files took " + flFlushStopwatch.ElapsedMilliseconds + " ms");
			Console.WriteLine();

			GC.Collect();
		}

		void RunDebugOutputString(long iterations)
		{
			Stopwatch stopwatch = new Stopwatch();

			Console.WriteLine("Testing DebugOutputString (Trace.WriteLine)...");

			// Prepare strings to write because string concatenation also takes some time
			string[] strings = new string[iterations];
			for (long i = 1; i <= iterations; i++)
			{
				strings[i - 1] = "Benchmark message - " + i;
			}

			stopwatch.Start();

			long percent = 0;
			for (long i = 1; i <= iterations; i++)
			{
				if (showProgress)
				{
					long newPercent = i * 100 / iterations;
					if (newPercent > percent)
					{
						percent = newPercent;
						Console.CursorLeft = 0;
						Console.Write("  " + percent.ToString("##0") + " %");
					}
				}

				switch (messageType)
				{
					case 0:
						Trace.WriteLine("Benchmark message - " + i);
						break;
					case 1:
						Trace.WriteLine(strings[i - 1]);
						break;
					case 2:
						Trace.WriteLine("Benchmark message");
						break;
				}
			}
			Console.CursorLeft = 0;
			Console.Write("          ");
			Console.CursorLeft = 0;

			stopwatch.Stop();

			long nanoseconds = (long) Math.Round((decimal) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000000 / iterations);
			nanoseconds -= emptyLoopTime;
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(8) + " ns/call");
			Console.WriteLine();

			GC.Collect();
		}

		void RunFileAppend(long iterations)
		{
			Stopwatch stopwatch = new Stopwatch();
			object syncLock = new object();

			Console.WriteLine("Testing synchronised StreamWriter.WriteLine...");

			string logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "log");
			Directory.CreateDirectory(logPath);
			string logFile = Path.Combine(logPath, "fileappend.txt");
			StreamWriter writer = new StreamWriter(logFile, true);

			// Prepare strings to write because string concatenation also takes some time
			string[] strings = new string[iterations];
			for (long i = 1; i <= iterations; i++)
			{
				strings[i - 1] = "Benchmark message - " + i;
			}

			stopwatch.Start();

			long percent = 0;
			for (long i = 1; i <= iterations; i++)
			{
				if (showProgress)
				{
					long newPercent = i * 100 / iterations;
					if (newPercent > percent)
					{
						percent = newPercent;
						Console.CursorLeft = 0;
						Console.Write("  " + percent.ToString("##0") + " %");
					}
				}

				lock (syncLock)
				{
					writer.Write(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
					writer.Write(" ");
					writer.Write(System.Threading.Thread.CurrentThread.ManagedThreadId);
					writer.Write(" ");
					switch (messageType)
					{
						case 0:
							writer.WriteLine("Benchmark message - " + i);
							break;
						case 1:
							writer.WriteLine(strings[i - 1]);
							break;
						case 2:
							writer.WriteLine("Benchmark message");
							break;
					}
				}
			}
			Console.CursorLeft = 0;
			Console.Write("          ");
			Console.CursorLeft = 0;

			stopwatch.Stop();

			Stopwatch closeStopwatch = new Stopwatch();
			closeStopwatch.Start();
			writer.Close();
			closeStopwatch.Stop();

			long nanoseconds = (long) Math.Round((decimal) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000000 / iterations);
			nanoseconds -= emptyLoopTime;
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(8) + " ns/call");

			Console.WriteLine("Closing file took " + closeStopwatch.ElapsedMilliseconds + " ms");
			Console.WriteLine();

			GC.Collect();
		}

		void RunFileOpenAppend(long iterations)
		{
			Stopwatch stopwatch = new Stopwatch();
			object syncLock = new object();

			Console.WriteLine("Testing synchronised File.AppendAllText...");

			string logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "log");
			Directory.CreateDirectory(logPath);
			string logFile = Path.Combine(logPath, "fileopenappend.txt");

			// Prepare strings to write because string concatenation also takes some time
			string[] strings = new string[iterations];
			for (long i = 1; i <= iterations; i++)
			{
				strings[i - 1] = "Benchmark message - " + i;
			}

			stopwatch.Start();

			long percent = 0;
			for (long i = 1; i <= iterations; i++)
			{
				if (showProgress)
				{
					long newPercent = i * 100 / iterations;
					if (newPercent > percent)
					{
						percent = newPercent;
						Console.CursorLeft = 0;
						Console.Write("  " + percent.ToString("##0") + " %");
					}
				}

				lock (syncLock)
				{
					switch (messageType)
					{
						case 0:
							File.AppendAllText(logFile, DateTime.UtcNow.ToString() + " " +
								System.Threading.Thread.CurrentThread.ManagedThreadId +
								" Benchmark message - " + i + Environment.NewLine);
							break;
						case 1:
							File.AppendAllText(logFile, DateTime.UtcNow.ToString() + " " +
								System.Threading.Thread.CurrentThread.ManagedThreadId + " " +
								strings[i - 1] + Environment.NewLine);
							break;
						case 2:
							File.AppendAllText(logFile, DateTime.UtcNow.ToString() + " " +
								System.Threading.Thread.CurrentThread.ManagedThreadId +
								" Benchmark message" + Environment.NewLine);
							break;
					}
				}
			}
			Console.CursorLeft = 0;
			Console.Write("          ");
			Console.CursorLeft = 0;

			stopwatch.Stop();

			long nanoseconds = (long) Math.Round((decimal) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000000 / iterations);
			nanoseconds -= emptyLoopTime;
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(8) + " ns/call");
			Console.WriteLine();

			GC.Collect();
		}

#if WITH_NLOG
		void RunNLog(long iterations)
		{
			Stopwatch stopwatch = new Stopwatch();

			Console.WriteLine("Testing NLog messages...");

			var config = new NLog.Config.LoggingConfiguration();
			var fileTarget = new NLog.Targets.FileTarget();
			fileTarget.FileName = @"log\nlog.txt";
			fileTarget.Layout = "${date:format=yyyy-MM-dd.HH.mm.ss.ffffff} ${processid}/${threadid} ${level}: ${message}";
			var asyncWrapper = new NLog.Targets.Wrappers.AsyncTargetWrapper(fileTarget, 1000, NLog.Targets.Wrappers.AsyncTargetWrapperOverflowAction.Grow);
			//config.AddTarget("file", asyncWrapper);
			var rule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, asyncWrapper);
			config.LoggingRules.Add(rule);
			NLog.LogManager.Configuration = config;
			var logger = NLog.LogManager.GetLogger("Benchmark");

			// Prepare strings to write because string concatenation also takes some time
			string[] strings = new string[iterations];
			for (long i = 1; i <= iterations; i++)
			{
				strings[i - 1] = "Benchmark message - " + i;
			}

			stopwatch.Start();

			long percent = 0;
			for (long i = 1; i <= iterations; i++)
			{
				if (showProgress)
				{
					long newPercent = i * 100 / iterations;
					if (newPercent > percent)
					{
						percent = newPercent;
						Console.CursorLeft = 0;
						Console.Write("  " + percent.ToString("##0") + " %");
					}
				}

				switch (messageType)
				{
					case 0:
						logger.Trace("Benchmark message - " + i);
						break;
					case 1:
						logger.Trace(strings[i - 1]);
						break;
					case 2:
						logger.Trace("Benchmark message");
						break;
				}
			}
			Console.CursorLeft = 0;
			Console.Write("          ");
			Console.CursorLeft = 0;

			stopwatch.Stop();

			long nanoseconds = (long) Math.Round((decimal) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000000 / iterations);
			nanoseconds -= emptyLoopTime;
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(8) + " ns/call");

			// Make sure all log items are written, as long as we have the time to wait for it
			Stopwatch flFlushStopwatch = new Stopwatch();
			flFlushStopwatch.Start();
			NLog.LogManager.Flush();
			flFlushStopwatch.Stop();
			Console.WriteLine("Flushing NLog to files took " + flFlushStopwatch.ElapsedMilliseconds + " ms");
			Console.WriteLine();

			GC.Collect();
		}
#endif
	}
}
