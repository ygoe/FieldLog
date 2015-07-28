using System;
using System.Linq;

namespace Unclassified.FieldLogViewer.SourceInfo
{
	/// <summary>
	/// Dispatches deobfuscation calls to a deobfuscator implementation for a specific obfuscator.
	/// </summary>
	internal class Deobfuscator
	{
		#region Private data

		private IDeobfuscator deobfuscatorImpl;

		#endregion Private data

		#region File management

		/// <summary>
		/// Gets all loaded map file names, or an empty array if no map file is loaded.
		/// </summary>
		public string[] FileNames
		{
			get
			{
				if (deobfuscatorImpl != null)
				{
					return deobfuscatorImpl.FileNames;
				}
				else
				{
					return new string[0];
				}
			}
		}

		/// <summary>
		/// Adds a file to the deobfuscator and loads it.
		/// </summary>
		/// <param name="fileName">The name of the obfuscation map file to load.</param>
		public void AddFile(string fileName)
		{
			if (deobfuscatorImpl == null)
			{
				// Find out what system we need for this file
				if (SeeUnsharpDeobfuscator.SupportsFile(fileName))
				{
					deobfuscatorImpl = new SeeUnsharpDeobfuscator();
				}
				else if (DotfuscatorDeobfuscator.SupportsFile(fileName))
				{
					deobfuscatorImpl = new DotfuscatorDeobfuscator();
				}
				else
				{
					throw new NotSupportedException("The obfuscation map file is not supported.");
				}
			}

			deobfuscatorImpl.AddFile(fileName);
		}

		/// <summary>
		/// Removes all loaded map files from the deobfuscator.
		/// </summary>
		public void Clear()
		{
			deobfuscatorImpl = null;
		}

		#endregion File management

		#region Deobfuscation

		/// <summary>
		/// Gets a value indicating whether it might be worth it to call the <see cref="Deobfuscate"/>
		/// method.
		/// </summary>
		public bool IsLoaded
		{
			get { return deobfuscatorImpl != null; }
		}

		/// <summary>
		/// Deobfuscates a stack frame.
		/// </summary>
		/// <param name="module">The name of the module to look up.</param>
		/// <param name="typeName">The full name of the type of the method to look up.</param>
		/// <param name="methodName">The name of the method to look up.</param>
		/// <param name="signature">The signature of the method.</param>
		/// <param name="token">The metadata token of the method.</param>
		/// <param name="originalName">The deobfuscated method name.</param>
		/// <param name="originalNameWithSignature">The deobfuscated method name with signature.</param>
		/// <param name="originalToken">The original metadata token of the method.</param>
		/// <returns>true if the item was found; otherwise, false.</returns>
		public bool Deobfuscate(
			string module,
			string typeName,
			string methodName,
			string signature,
			int token,
			out string originalName,
			out string originalNameWithSignature,
			out int originalToken)
		{
			if (deobfuscatorImpl != null)
			{
				return deobfuscatorImpl.Deobfuscate(
					module,
					typeName,
					methodName,
					signature,
					token,
					out originalName,
					out originalNameWithSignature,
					out originalToken);
			}

			// Haven't checked the IsLoaded property before?
			// No deobfuscator loaded
			originalName = null;
			originalNameWithSignature = null;
			originalToken = 0;
			return false;
		}

		#endregion Deobfuscation
	}

	/// <summary>
	/// A deobfuscator implementation for a specific obfuscator.
	/// </summary>
	internal interface IDeobfuscator
	{
		#region File management

		/// <summary>
		/// Gets all loaded map file names, or an empty array if no map file is loaded.
		/// </summary>
		string[] FileNames { get; }

		/// <summary>
		/// Adds a file to the deobfuscator and loads it.
		/// </summary>
		/// <param name="fileName">The name of the obfuscation map file to load.</param>
		void AddFile(string fileName);

		#endregion File management

		#region Deobfuscation

		/// <summary>
		/// Deobfuscates a stack frame.
		/// </summary>
		/// <param name="module">The name of the module to look up.</param>
		/// <param name="typeName">The full name of the type of the method to look up.</param>
		/// <param name="methodName">The name of the method to look up.</param>
		/// <param name="signature">The signature of the method.</param>
		/// <param name="token">The metadata token of the method.</param>
		/// <param name="originalName">The deobfuscated method name.</param>
		/// <param name="originalNameWithSignature">The deobfuscated method name with signature.</param>
		/// <param name="originalToken">The original metadata token of the method.</param>
		/// <returns>true if the item was found; otherwise, false.</returns>
		bool Deobfuscate(
			string module,
			string typeName,
			string methodName,
			string signature,
			int token,
			out string originalName,
			out string originalNameWithSignature,
			out int originalToken);

		#endregion Deobfuscation
	}
}
