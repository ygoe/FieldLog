using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unclassified.FieldLog;
using System.Threading;

namespace ConsoleDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			FL.AcceptLogFileBasePath();

			Console.WriteLine("FieldLog writer demo application");
			Console.WriteLine();

			//for (int i = 1; i <= 3; i++)
			//{
			//    if (i > 1)
			//        Thread.Sleep(400);

			//    Console.WriteLine("Writing a trace text message...");
			//    FL.Checkpoint("Message test " + i);
			//    //FL.Trace("Static text");
			//    //FL.Trace("Static text");
			//}

			Console.WriteLine("Writing lots of messages...");
			Random rnd = new Random();
			for (int i = 1; i <= 50; i++)
			{
				if (i > 1)
					Thread.Sleep(rnd.Next(1000) + 500);
					//Thread.Sleep(rnd.Next(20));

				switch (rnd.Next(14))
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
				}
				if ((i % 100) == 0)
					Console.WriteLine("    now at " + i);
			}

			FL.Info("Information item");
			FL.Notice("Notice item");
			FL.Warning("Warning item");

			//Console.WriteLine("Logging a handled exception...");
			//FL.Exception(FieldLogPriority.Error, new InvalidOperationException("You can't do that!"), null, true);

			//Console.WriteLine("Throwing an unhandled exception...");
			//throw new ApplicationException("Test exception message",
			//    new ApplicationException("An inner message 1",
			//        new ApplicationException("An inner message 2")));
		}

		private static void ThrowException1()
		{
			using (new FieldLogScope("ThrowException1"))
			{
				ThrowException1a();
			}
		}

		private static void ThrowException1a()
		{
			using (new FieldLogScope("ThrowException1a"))
			{
				ThrowException1b();
			}
		}

		private static void ThrowException1b()
		{
			using (new FieldLogScope("ThrowException1b"))
			{
				throw new InvalidOperationException(
					"You can't do that!",
					new ApplicationException("An inner message 1",
						new ApplicationException("An inner message 2")));
			}
		}
	}
}
