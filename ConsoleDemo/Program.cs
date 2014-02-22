using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unclassified.FieldLog;

namespace ConsoleDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			FL.AcceptLogFileBasePath();

			Console.WriteLine("FieldLog writer demo application");
			Console.WriteLine();

			NormalActivity();
			//LoadTest();
			//TestTimerPrecision();
		}

		static void LoadTest()
		{
			Console.WriteLine("Load test pattern...");

			for (int i = 1; i <= 50000; i++)
			{
				FL.Trace("Load test - " + i);
				if ((i % 100) == 0)
					FL.Error("Load test error message");
				
				if ((i % 10000) == 0)
					Console.WriteLine("    now at " + i);
			}
		}

		static void NormalActivity()
		{
			Console.WriteLine("Normal application activity pattern...");
			Random rnd = new Random();
			for (int i = 1; i <= 100; i++)
			{
				if (i > 1)
					Thread.Sleep(rnd.Next(1000) + 500);

				switch (rnd.Next(16))
				{
					case 0:
					case 1:
					case 2:
					case 3:
					case 4:
						FL.Trace("Mass items test " + i + " with just a simple trace message");
						break;
					case 5:
						FL.Checkpoint("Mass items test " + i + " (checkpoint)");
						break;
					case 6:
					case 7:
						FL.Info("Mass items test " + i + " - information level");
						break;
					case 8:
						FL.Warning("Mass items test " + i + " - warning!");
						break;
					case 9:
						FL.Error("Mass items test " + i + " - ERROR CONDITION");
						break;
					case 10:
						try
						{
							ThrowException1();
						}
						catch (Exception ex)
						{
							FL.Exception(FieldLogPriority.Error, ex, null, false);
						}
						break;
					case 11:
						try
						{
							throw new AggregateException(
								new ApplicationException("First exception message"),
								new ApplicationException("Second exception message"));
						}
						catch (Exception ex)
						{
							FL.Exception(FieldLogPriority.Notice, ex, null, false);
						}
						break;
					case 12:
					case 13:
						FL.TraceData("varName", rnd.Next(10000).ToString());
						break;
					case 14:
					case 15:
						Task.Factory.StartNew(DoSomeMaths);
						break;
				}
				if ((i % 100) == 0)
					Console.WriteLine("    now at " + i);

				if ((i % 5) == 0)
					Trace.WriteLine("FL ConsoleDemo now at item " + i);
			}

			Thread.Sleep(1400);
			FL.ClearTimer("DoSomeMaths");
			Thread.Sleep(100);
			using (FL.Timer("TimerTimer"))
			{
				var cti = FL.StartTimer("DoSomeMaths");
				FL.StopTimer("DoSomeMaths");
				//cti.Stop();
			}
			Thread.Sleep(1200);
			
			FL.Info("Information item");
			FL.Notice("Notice item");
			FL.Warning("Warning item");

			//Console.WriteLine("Logging a handled exception...");
			//FL.Exception(FieldLogPriority.Error, new InvalidOperationException("You can't do that!"), null, true);

			Console.WriteLine("Throwing an unhandled exception...");
			throw new ApplicationException("Test exception message",
				new ApplicationException("An inner message 1",
					new ApplicationException("An inner message 2")));
		}

		private static void DoSomeMaths()
		{
			using (FL.NewScope())
			using (FL.Timer("DoSomeMaths"))
			{
				for (int i = 1; i <= 10; i++)
				{
					FL.TraceData("i", i);
					Thread.Sleep(31);
				}
			}
		}

		private static void ThrowException1()
		{
			using (FL.NewScope())
			{
				ThrowException1a();
			}
		}

		private static void ThrowException1a()
		{
			using (FL.NewScope())
			{
				ThrowException1b();
			}
		}

		private static void ThrowException1b()
		{
			using (FL.NewScope())
			{
				throw new InvalidOperationException(
					"You can't do that!",
					new ApplicationException("An inner message 1",
						new ApplicationException("An inner message 2")));
			}
		}

		private static void TestTimerPrecision()
		{
			Console.WriteLine("Timer precision test is running, please wait...");
			Console.WriteLine();

			int[] iterationValues = new int[]
			{
				1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000,
				10000, 20000, 30000, 40000, 50000, 60000, 70000, 80000, 90000,
				100000
			};

			Thread.Sleep(1000);

			foreach (int iterations in iterationValues)
			{
				for (int j = 0; j < 20; j++)
				{
					using (FL.Timer("Loop " + iterations, j == 19))
					{
						for (int i = 0; i < iterations; i++)
						{
						}
					}
				}
			}
		}
	}
}
