using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Implements a thread that monitors the system time and rebases FieldLog's clock on a change.
	/// </summary>
	internal static class CheckTimeThread
	{
		/// <summary>
		/// Defines the time checking interval in milliseonds.
		/// </summary>
		private const int CheckInterval = 1000;

		private static Thread checkThread;
		private static bool cancelPending;
		private static int localOffset;
		private static long nextOffsetCheck;

		/// <summary>
		/// Starts the time checking thread.
		/// </summary>
		public static void Start()
		{
			checkThread = new Thread(ThreadProc);
			checkThread.IsBackground = true;
			checkThread.Start();
		}

		/// <summary>
		/// Stops the time checking thread.
		/// </summary>
		/// <param name="millisecondsTimeout">Timeout to wait for the thread to stop. Should be longer than <see cref="CheckInterval"/>.</param>
		/// <returns>true if the thread has stopped; otherwise, false.</returns>
		public static bool Stop(int millisecondsTimeout)
		{
			cancelPending = true;
			return checkThread.Join(millisecondsTimeout);
		}

		private static void ThreadProc()
		{
#if NET20
			localOffset = (int) TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
#else
			localOffset = (int) TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
#endif
			// Only check for offset changes every new minute (plus 1 second safety margin) to save
			// resources for clearing the TimeZoneInfo cache
			nextOffsetCheck = DateTime.UtcNow.Ticks / 600000000 * 600000000 + 610000000;

			do
			{
				Thread.Sleep(CheckInterval);

				// Check for UTC time changes
				TimeSpan offset = DateTime.UtcNow - FL.UtcNow;
				if (Math.Abs(offset.TotalMilliseconds) > 100)
				{
					FL.RebaseTime();

					string msg = "System time changed by " + offset.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture) + " ms";
					FL.Notice(msg);
					Debug.WriteLine(msg);
				}
				
				// Check for local time zone changes
				if (DateTime.UtcNow.Ticks >= nextOffsetCheck)
				{
					// Clear the cache to get the real current setting
#if NET20
					int newLocalOffset = (int) TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
#else
					TimeZoneInfo.ClearCachedData();
					int newLocalOffset = (int) TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
#endif
					if (newLocalOffset != localOffset)
					{
						int hours = localOffset / 60;
						int mins = Math.Abs(localOffset) % 60;

						int newHours = newLocalOffset / 60;
						int newMins = Math.Abs(newLocalOffset) % 60;

						localOffset = newLocalOffset;

						string msg = "Local time UTC offset changed from " +
							hours.ToString("+00;-00;+00") + ":" + mins.ToString("00") + " to " +
							newHours.ToString("+00;-00;+00") + ":" + newMins.ToString("00");
						string details = "\u0001UtcOffset=" + newLocalOffset;
						FL.Notice(msg, details);
						Debug.WriteLine(msg);
					}
					nextOffsetCheck = DateTime.UtcNow.Ticks / 600000000 * 600000000 + 610000000;
				}
			}
			while (!cancelPending);
		}
	}
}
