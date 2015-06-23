using System;
using System.Linq;
using Unclassified.FieldLog;

namespace OSInfoTest
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			Console.WriteLine("ProductName     : " + OSInfo.ProductName);
			Console.WriteLine();

			Console.WriteLine("Type            : " + OSInfo.Type);
			Console.WriteLine("Version         : " + OSInfo.Version);
			Console.WriteLine("Edition         : " + OSInfo.Edition);
			Console.WriteLine("Build           : " + OSInfo.Build);
			Console.WriteLine("ServicePack     : " + OSInfo.ServicePack);
			Console.WriteLine("ServicePackBuild: " + OSInfo.ServicePackBuild);
			Console.WriteLine();

			Console.WriteLine("Language        : " + OSInfo.Language);
			Console.WriteLine("IsAppServer     : " + OSInfo.IsAppServer);
			Console.WriteLine("MaxTouchPoints  : " + OSInfo.MaxTouchPoints);
			Console.WriteLine("MouseButtons    : " + OSInfo.MouseButtons);
			Console.WriteLine("ScreenDpi       : " + OSInfo.ScreenDpi);
			Console.WriteLine("AppCompatLayer  : " + OSInfo.AppCompatLayer);
		}
	}
}
