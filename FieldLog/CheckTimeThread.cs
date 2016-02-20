// FieldLog – .NET logging solution
// © Yves Goergen, Made in Germany
// Website: http://unclassified.software/source/fieldlog
//
// This library is free software: you can redistribute it and/or modify it under the terms of
// the GNU Lesser General Public License as published by the Free Software Foundation, version 3.
//
// This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along with this
// library. If not, see <http://www.gnu.org/licenses/>.

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
		private static double prevLoggedOffset;

		/// <summary>
		/// Starts the time checking thread.
		/// </summary>
		public static void Start()
		{
			checkThread = new Thread(ThreadProc);
			checkThread.Name = "FieldLog.CheckTimeThread";
			checkThread.IsBackground = true;
			checkThread.Start();
		}

		/// <summary>
		/// Stops the time checking thread.
		/// </summary>
		public static void Stop()
		{
			cancelPending = true;
			checkThread.Interrupt();
			if (!checkThread.Join(50))
			{
				checkThread.Abort();
			}
		}

		private static void ThreadProc()
		{
#if NET20
			localOffset = (int)TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
#else
			localOffset = (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
#endif
			// Only check for offset changes every new minute (plus 1 second safety margin) to save
			// resources for clearing the TimeZoneInfo cache
			nextOffsetCheck = DateTime.UtcNow.Ticks / 600000000 * 600000000 + 610000000;

			do
			{
				try
				{
					Thread.Sleep(CheckInterval);
				}
				catch (ThreadInterruptedException)
				{
					// That's fine.
				}

				// Check for UTC time changes
				TimeSpan offset = DateTime.UtcNow - FL.UtcNow;
				if (Math.Abs(offset.TotalMilliseconds) >= FL.CheckTimeThreshold)
				{
					FL.RebaseTime();

					string msg = "System time changed by " + offset.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture) + " ms";
					var item = new FieldLogTextItem(FieldLogPriority.Notice, msg, "Changes less than " + FL.CheckTimeThreshold + " ms are not reported.");
					// Discard this log item if there was no other item logged within the last
					// 10 seconds because it wouldn't be interesting anyway. If no other log items
					// may have discontinuous timestamps, the time recalibration can occur silently.
					FL.LogInternal(item, TimeSpan.FromSeconds(10));
					Debug.WriteLine(msg);
					prevLoggedOffset = 0;
				}
				else if (FL.LogTimeThreshold >= 0 &&
					Math.Abs(offset.TotalMilliseconds - prevLoggedOffset) > FL.LogTimeThreshold)
				{
					FL.TraceData("System time offset", offset.TotalMilliseconds);
					prevLoggedOffset = offset.TotalMilliseconds;
				}

				// Check for local time zone changes
				if (DateTime.UtcNow.Ticks >= nextOffsetCheck)
				{
					// Clear the cache to get the real current setting
#if NET20
					int newLocalOffset = (int)TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
#else
					TimeZoneInfo.ClearCachedData();
					int newLocalOffset = (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
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
