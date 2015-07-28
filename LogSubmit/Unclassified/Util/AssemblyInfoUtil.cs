using System;
using System.Linq;
using System.Reflection;

namespace Unclassified.Util
{
	public class AssemblyInfoUtil
	{
		/// <summary>
		/// Gets the version string of the current application from the AssemblyFileVersionAttribute
		/// or AssemblyVersionAttribute value, or null if the entry assembly is unknown.
		/// </summary>
		/// <remarks>
		/// This is a regular dotted-numeric version with no additional text.
		/// </remarks>
		public static string AppVersion
		{
			get
			{
				// Differences between version attributes: http://stackoverflow.com/a/65062/143684
				// Win32 file resource version
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyFileVersionAttribute)customAttributes[0]).Version;
				}
				// Assembly identity version, always present.
				// The AssemblyVersionAttribute is accessed like this, the attribute itself is not
				// present in the compiled assembly.
				return Assembly.GetEntryAssembly().GetName().Version.ToString();
			}
		}

		/// <summary>
		/// Gets the descriptive version string of the current application from the
		/// AssemblyInformationalVersionAttribute, AssemblyFileVersionAttribute or
		/// AssemblyVersionAttribute value, or null if the entry assembly is unknown.
		/// </summary>
		/// <remarks>
		/// This can contain text in an arbitrary format or include release names or commit hashes.
		/// It may not be suitable for comparison but rather for displaying to the user or writing
		/// to log files.
		/// </remarks>
		public static string AppLongVersion
		{
			get
			{
				// Differences between version attributes: http://stackoverflow.com/a/65062/143684
				// Descriptive version name, can be any string
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyInformationalVersionAttribute)customAttributes[0]).InformationalVersion;
				}
				// Win32 file resource version
				customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyFileVersionAttribute)customAttributes[0]).Version;
				}
				// Assembly identity version, always present.
				// The AssemblyVersionAttribute is accessed like this, the attribute itself is not
				// present in the compiled assembly.
				return Assembly.GetEntryAssembly().GetName().Version.ToString();
			}
		}

		/// <summary>
		/// Gets the assembly configuration of the current application from the
		/// AssemblyConfigurationAttribute value, or null if none is set or the entry assembly is
		/// unknown.
		/// </summary>
		public static string AppAsmConfiguration
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyConfigurationAttribute)customAttributes[0]).Configuration;
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the name of the current application from the AssemblyProductAttribute or
		/// AssemblyTitleAttribute value, or null if none is set or the entry assembly is unknown.
		/// </summary>
		public static string AppName
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyProductAttribute)customAttributes[0]).Product;
				}
				customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyTitleAttribute)customAttributes[0]).Title;
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the description of the current application from the AssemblyDescriptionAttribute
		/// value, or null if none is set or the entry assembly is unknown.
		/// </summary>
		public static string AppDescription
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyDescriptionAttribute)customAttributes[0]).Description;
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the copyright note of the current application from the AssemblyCopyrightAttribute
		/// value, or null if none is set or the entry assembly is unknown.
		/// </summary>
		public static string AppCopyright
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyCopyrightAttribute)customAttributes[0]).Copyright;
				}
				return null;
			}
		}
	}
}
