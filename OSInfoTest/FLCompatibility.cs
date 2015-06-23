using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Contains the required parts from the full FL class to make <see cref="OSInfo"/> work without
	/// the FieldLog assembly.
	/// </summary>
	internal static class FL
	{
		/// <summary>
		/// The entry assembly's Location value. This is determined by other means for ASP.NET
		/// applications.
		/// </summary>
		public static string EntryAssemblyLocation { get; internal set; }

		static FL()
		{
			var entryAssembly = Assembly.GetEntryAssembly();
			if (entryAssembly != null)
			{
				EntryAssemblyLocation = entryAssembly.Location;
			}
		}

		/// <summary>
		/// Compares two dotted-numeric versions. Anything after numbers and dots is ignored.
		/// </summary>
		/// <param name="firstVersion">The first version.</param>
		/// <param name="secondVersion">The second version.</param>
		/// <returns>
		/// A signed number indicating the relative values of <paramref name="firstVersion"/> and <paramref name="secondVersion"/>.
		/// <list type="table">
		///   <listheader>
		///     <term>Return value</term>
		///     <description>Description</description>
		///   </listheader>
		///   <item>
		///     <term>Less than zero</term>
		///     <description><paramref name="firstVersion"/> is less than <paramref name="secondVersion"/>.</description>
		///   </item>
		///   <item>
		///     <term>Zero</term>
		///     <description><paramref name="firstVersion"/> is equal to <paramref name="secondVersion"/>.</description>
		///   </item>
		///   <item>
		///     <term>Greater than zero</term>
		///     <description><paramref name="firstVersion"/> is greater than <paramref name="secondVersion"/>.</description>
		///   </item>
		/// </list>
		/// </returns>
		/// <remarks>
		/// In contrast to <see cref="System.Version.CompareTo(Version)"/>, this method interprets
		/// missing segments as zero. So "1.0" and "1.0.0" are the same version. This is relevant
		/// because the AssemblyVersion attribute always contains all four segments but this is not
		/// how we want to display simpler versions to the user.
		/// </remarks>
		public static int CompareVersions(string firstVersion, string secondVersion)
		{
			// Cut off anything that's not numbers and dots
			firstVersion = Regex.Replace(firstVersion, @"[^0-9.].*$", "");
			secondVersion = Regex.Replace(secondVersion, @"[^0-9.].*$", "");

			string[] firstStrings = firstVersion.Split('.');
			string[] secondStrings = secondVersion.Split('.');
			int length = Math.Max(firstStrings.Length, secondStrings.Length);
			for (int i = 0; i < length; i++)
			{
				string firstStr = i < firstStrings.Length ? firstStrings[i] : "0";
				string secondStr = i < secondStrings.Length ? secondStrings[i] : "0";
				int firstNum = int.Parse(firstStr, System.Globalization.CultureInfo.InvariantCulture);
				int secondNum = int.Parse(secondStr, System.Globalization.CultureInfo.InvariantCulture);
				if (firstNum < secondNum) return -1;
				if (firstNum > secondNum) return 1;
			}
			return 0;
		}
	}
}
