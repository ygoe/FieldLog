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
		static void Main(string[] args)
		{
			new Program().Run();
		}

		long emptyLoopTime;

		void Run()
		{
			FL.AcceptLogFileBasePath();

			// For correct-in-context number formatting, if used
			//Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			Console.WriteLine("Log benchmark");
			Console.WriteLine();

			RunEmptyLoop(100000000);
			RunLock(100000000);

			//Console.WriteLine("Press the Enter key to continue.");
			//Console.ReadLine();
			//Console.CursorTop -= 2;
			//Console.WriteLine("                                    ");
			//Console.WriteLine("                                    ");
			//Console.CursorTop -= 2;

			RunFileAppend(1000000);

			RunFieldLog(1000000);

			RunDebugOutputString(100000);

			Console.WriteLine();
			Console.WriteLine("Press the Enter key to quit.");
			Console.ReadLine();

			// Make sure all log items are written, as long as we have the time to wait for it
			FL.Shutdown();
		}

		void RunEmptyLoop(long iterations)
		{
			Stopwatch stopwatch = new Stopwatch();

			Console.WriteLine("Testing empty loop (for reference)...");

			stopwatch.Start();

			long percent = 0;
			for (long i = 1; i <= iterations; i++)
			{
				long newPercent = i * 100 / iterations;
				if (newPercent > percent)
				{
					percent = newPercent;
					Console.CursorLeft = 0;
					Console.Write("  " + percent.ToString("##0") + " %");
				}

				if (i < 0) break;   // Never happens, hope the optimising compiler doesn't find out
			}
			Console.CursorLeft = 0;
			Console.Write("          ");
			Console.CursorLeft = 0;

			stopwatch.Stop();

			long nanoseconds = (long) Math.Round((decimal) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000000 / iterations);
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(6) + " ns/call");

			emptyLoopTime = nanoseconds;
			Console.WriteLine("Following measurements have this time subtracted.");
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
				long newPercent = i * 100 / iterations;
				if (newPercent > percent)
				{
					percent = newPercent;
					Console.CursorLeft = 0;
					Console.Write("  " + percent.ToString("##0") + " %");
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
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(6) + " ns/call");
		}

		void RunFieldLog(long iterations)
		{
			Stopwatch stopwatch = new Stopwatch();

			Console.WriteLine("Testing FieldLog...");

			stopwatch.Start();

			long percent = 0;
			for (long i = 1; i <= iterations; i++)
			{
				long newPercent = i * 100 / iterations;
				if (newPercent > percent)
				{
					percent = newPercent;
					Console.CursorLeft = 0;
					Console.Write("  " + percent.ToString("##0") + " %");
				}

				FL.Trace("Benchmark message - " + i);
			}
			Console.CursorLeft = 0;
			Console.Write("          ");
			Console.CursorLeft = 0;

			stopwatch.Stop();

			long nanoseconds = (long) Math.Round((decimal) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000000 / iterations);
			nanoseconds -= emptyLoopTime;
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(6) + " ns/call");
		}

		void RunDebugOutputString(long iterations)
		{
			Stopwatch stopwatch = new Stopwatch();

			Console.WriteLine("Testing DebugOutputString (Trace.WriteLine)...");

			stopwatch.Start();

			long percent = 0;
			for (long i = 1; i <= iterations; i++)
			{
				long newPercent = i * 100 / iterations;
				if (newPercent > percent)
				{
					percent = newPercent;
					Console.CursorLeft = 0;
					Console.Write("  " + percent.ToString("##0") + " %");
				}

				Trace.WriteLine("Benchmark message - " + i);
			}
			Console.CursorLeft = 0;
			Console.Write("          ");
			Console.CursorLeft = 0;

			stopwatch.Stop();

			long nanoseconds = (long) Math.Round((decimal) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000000 / iterations);
			nanoseconds -= emptyLoopTime;
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(6) + " ns/call");
		}

		void RunFileAppend(long iterations)
		{
			Stopwatch stopwatch = new Stopwatch();
			object syncLock = new object();

			Console.WriteLine("Testing StreamWriter.WriteLine...");

			string logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "log");
			Directory.CreateDirectory(logPath);
			string logFile = Path.Combine(logPath, "fileappend.txt");
			StreamWriter writer = new StreamWriter(logFile, true);

			stopwatch.Start();

			long percent = 0;
			for (long i = 1; i <= iterations; i++)
			{
				long newPercent = i * 100 / iterations;
				if (newPercent > percent)
				{
					percent = newPercent;
					Console.CursorLeft = 0;
					Console.Write("  " + percent.ToString("##0") + " %");
				}

				lock (syncLock)
				{
					writer.WriteLine("Benchmark message - " + i);
				}
			}
			Console.CursorLeft = 0;
			Console.Write("          ");
			Console.CursorLeft = 0;

			stopwatch.Stop();

			writer.Close();

			long nanoseconds = (long) Math.Round((decimal) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000000 / iterations);
			nanoseconds -= emptyLoopTime;
			Console.WriteLine("  " + nanoseconds.ToString().PadLeft(6) + " ns/call");
		}
	}
}
