using System;
using System.Diagnostics;
using System.Threading;

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Provides methods for custom time measuring and deferred logging of the results.
	/// </summary>
	/// <remarks>
	/// Tests have shown that measurements are good for time spans from 1 microsecond (10 ticks) on.
	/// </remarks>
	public class CustomTimerInfo
	{
		/// <summary>
		/// Defines the delay for saving custom timers, in milliseconds.
		/// </summary>
		private const int customTimerDelay = 1000;

		private string key;
		private Stopwatch stopwatch;
		private long counter;
		private Timer timer;
		private object syncLock = new object();

		/// <summary>
		/// Initialises a new instance of the CustomTimerInfo class. Does not start measuring.
		/// </summary>
		/// <param name="key">The custom timer key.</param>
		public CustomTimerInfo(string key)
		{
			this.key = key;
			stopwatch = new Stopwatch();
			timer = new Timer(OnCustomTimer, null, Timeout.Infinite, Timeout.Infinite);
		}

		/// <summary>
		/// Handles a custom time measurement timer for saving the time data.
		/// </summary>
		/// <param name="state">Unused.</param>
		private void OnCustomTimer(object state)
		{
			if (key == FL.EnsureJitTimerKey)
			{
				return;
			}
			
			long ticks, ticksPc, localCounter;

			lock (syncLock)
			{
				// Do nothing if the stopwatch was just started again
				if (stopwatch.IsRunning) return;

				// Fetch the data in the lock region
				localCounter = counter;
				// Subtract 4 ticks per measurement, determined by tests
				long correction = localCounter * 4;
				ticks = stopwatch.Elapsed.Ticks - correction;
				if (ticks < 0)
					ticks = 0;
				ticksPc = ticks / localCounter;
			}
				
			// Total time
			long roundTicks = ticks + 5;
			int seconds = (int) (roundTicks / 10000000);
			int ms = (int) ((roundTicks % 10000000) / 10000);
			int us = (int) ((roundTicks % 10000) / 10);

			// Per call time
			long roundTicksPc = ticksPc + 5;
			int secondsPc = (int) (roundTicksPc / 10000000);
			int msPc = (int) ((roundTicksPc % 10000000) / 10000);
			int usPc = (int) ((roundTicksPc % 10000) / 10);

			string text = "Custom timer " + key + " at " + localCounter;
			string details =
				localCounter + " calls\n" +
				secondsPc.ToString() + "." +
					msPc.ToString("000") + "\u2009" +
					usPc.ToString("000") + " seconds per call (" + ticksPc + " ticks)\n" +
				seconds.ToString() + "." +
					ms.ToString("000") + "\u2009" +
					us.ToString("000") + " seconds total (" + ticks + " ticks)";

			FL.Trace(text, details);
		}

		/// <summary>
		/// Starts, or resumes, measuring elapsed time for an interval and increases the call
		/// counter. Does nothing if the measurement is currently started.
		/// </summary>
		/// <param name="incrementCounter">Increment the counter value.</param>
		public void Start(bool incrementCounter = true)
		{
			// Don't start the timer now that we're starting a new iteration
			timer.Change(Timeout.Infinite, Timeout.Infinite);
			lock (syncLock)
			{
				if (stopwatch.IsRunning) return;   // Nothing to do

				if (incrementCounter)
				{
					counter++;
				}
				stopwatch.Start();
			}
		}

		/// <summary>
		/// Stops measuring elapsed time for an interval and schedules saving the measured time to
		/// a log item. Does nothing if the measurement is currently stopped.
		/// </summary>
		/// <param name="writeNow">true to write the timer value immediately, false for the normal delay.</param>
		public void Stop(bool writeNow = false)
		{
			lock (syncLock)
			{
				if (!stopwatch.IsRunning) return;   // Nothing to do

				stopwatch.Stop();
			}
			if (writeNow)
			{
				timer.Change(Timeout.Infinite, Timeout.Infinite);
				OnCustomTimer(null);
			}
			else
			{
				timer.Change(customTimerDelay, Timeout.Infinite);
			}
		}
	}

	/// <summary>
	/// Provides an IDisposable implementation to help in custom time measuring.
	/// </summary>
	public class CustomTimerScope : IDisposable
	{
		private string key;
		private CustomTimerInfo cti;
		private bool writeImmediately;
		private bool isDisposed;

		/// <summary>
		/// Initialises a new instance of the CustomTimerScope class and calls the Start method of
		/// the CustomTimerInfo instance.
		/// </summary>
		/// <param name="key">The custom timer key for a dictionary lookup.</param>
		public CustomTimerScope(string key)
		{
			this.key = key;
			cti = FL.StartTimer(key);
		}

		/// <summary>
		/// Initialises a new instance of the CustomTimerScope class and calls the Start method of
		/// the CustomTimerInfo instance.
		/// </summary>
		/// <param name="key">The custom timer key for a dictionary lookup.</param>
		/// <param name="incrementCounter">Increment the counter value.</param>
		/// <param name="writeImmediately">true to write the timer value immediately when stopping, false for the normal delay.</param>
		public CustomTimerScope(string key, bool incrementCounter, bool writeImmediately)
		{
			this.key = key;
			this.writeImmediately = writeImmediately;
			cti = FL.StartTimer(key, incrementCounter);
		}

		/// <summary>
		/// Initialises a new instance of the CustomTimerScope class and calls the Start method of
		/// the CustomTimerInfo instance.
		/// </summary>
		/// <param name="cti">A CustomTimerInfo instance.</param>
		public CustomTimerScope(CustomTimerInfo cti)
		{
			this.cti = cti;
			cti.Start();
		}

		/// <summary>
		/// Initialises a new instance of the CustomTimerScope class and calls the Start method of
		/// the CustomTimerInfo instance.
		/// </summary>
		/// <param name="cti">A CustomTimerInfo instance.</param>
		/// <param name="incrementCounter">Increment the counter value.</param>
		/// <param name="writeImmediately">true to write the timer value immediately when stopping, false for the normal delay.</param>
		public CustomTimerScope(CustomTimerInfo cti, bool incrementCounter, bool writeImmediately)
		{
			this.cti = cti;
			this.writeImmediately = writeImmediately;
			cti.Start(incrementCounter);
		}

		/// <summary>
		/// Calls the Stop method of the CustomTimerInfo instance.
		/// </summary>
		public void Dispose()
		{
			if (!isDisposed)
			{
				isDisposed = true;
				cti.Stop(writeImmediately);
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Finalises the CustomTimerScope instance. This generates an Error log item.
		/// </summary>
		~CustomTimerScope()
		{
			if (!FL.IsShutdown)
			{
				FL.Error("CustomTimerScope.Dispose was not called! Time measuring data may be missing.", "Key = " + key);
				Dispose();
			}
		}
	}
}
