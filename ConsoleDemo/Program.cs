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
	class NestHost
	{
		public class Nest1
		{
			public class Nest2
			{
			}
		}
	}

	class GenericClass<T>
	{
		public static void Method<U>(T obj, U obj2)
		{
			FL.LogStackTrace();
		}
	}
	
	class Program
	{
		static void Main(string[] args)
		{
			FL.AcceptLogFileBasePath();

			Console.WriteLine("FieldLog writer demo application");
			Console.WriteLine();

			//LoadTest();
			//ConstantFlow();
			NormalActivity();
			//BatchActivity();
			//TestTimerPrecision();
			//TestOutputDebugString();
			//TestTaskLogging();

			//NestTest(null);
			//GenericTest<string>("");
			//GenericClass<int>.Method<string>(0, "");
		}

		static int NestTest(ConsoleDemo.NestHost.Nest1.Nest2 obj)
		{
			FL.LogStackTrace();
			return 0;
		}

		static int GenericTest<T>(T value)
		{
			FL.LogStackTrace();
			return 0;
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

		static void ConstantFlow()
		{
			Console.WriteLine("Constant item flow pattern...");

			Random rnd = new Random();
			for (int i = 1; i <= 100; i++)
			{
				int itemCount = rnd.Next(1, 10);
				while (itemCount-- > 0)
				{
					FL.Trace("Test item - " + i);
				}

				if ((i % 10) == 0)
					Console.WriteLine("    now at " + i);

				Thread.Sleep(rnd.Next(400, 600));
			}
		}

		static void NormalActivity()
		{
			Console.WriteLine("Normal application activity pattern...");

			FL.TimerAction(Delay);
			FL.ScopeAction(Delay2, FL.TimerFunc(GetDelay, 400, "GetDelay"), "Delay2");
			using (FL.Scope("Test", new { i = 0, j = 5, str = "Hello", x = new { a = "A", b = "B" } }))
			{
			}

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

		private static void Delay()
		{
			Thread.Sleep(300);
		}

		private static void Delay2(int ms)
		{
			Thread.Sleep(ms);
		}

		private static int GetDelay(int x)
		{
			return x * 2;
		}

		private static void DoSomeMaths()
		{
			using (FL.Scope())
			using (FL.Timer("DoSomeMaths"))
			{
				for (int i = 1; i <= 10; i++)
				{
					FL.InfoData("i", i);
					Thread.Sleep(31);
				}
			}
		}

		private static void ThrowException1()
		{
			using (FL.Scope())
			{
				ThrowException1a();
			}
		}

		private static void ThrowException1a()
		{
			using (FL.Scope())
			{
				ThrowException1b();
			}
		}

		private static void ThrowException1b()
		{
			using (FL.Scope())
			{
				throw new InvalidOperationException(
					"You can't do that!",
					new ApplicationException("An inner message 1",
						new ApplicationException("An inner message 2")));
			}
		}

		private static void BatchActivity()
		{
			Console.WriteLine("Batch test pattern...");

			for (int i = 1; i <= 200; i++)
			{
				FL.Trace("Batch test - " + i);

				if ((i % 10) == 0)
					Console.WriteLine("    now at " + i);

				Thread.Sleep(8000);
			}
		}

		private static void TestTimerPrecision()
		{
			Console.WriteLine("Timer precision test...");

			int[] iterationValues =
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
					using (FL.Timer("Loop " + iterations, true, j == 19))
					{
						for (int i = 0; i < iterations; i++)
						{
						}
					}
				}
			}
		}

		private static void TestOutputDebugString()
		{
			Console.WriteLine("OutputDebugString only test...");

			for (int i = 1; i <= 200; i++)
			{
				System.Diagnostics.Trace.WriteLine("OutputDebugString test - " + i);

				if ((i % 10) == 0)
					Console.WriteLine("    now at " + i);

				Thread.Sleep(8000);
			}
		}

		private static void TestTaskLogging()
		{
			Task.Factory
				.StartNew(() => { throw new InvalidOperationException("Test exception"); })
				.LogFaulted("Demo")
				.Wait();
		}
	}
}
