using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Unclassified.Util
{
	/// <summary>
	/// Provides a global mutex for system-wide synchronisation.
	/// </summary>
	public class GlobalMutex : IDisposable
	{
		#region Static members

		private static GlobalMutex instance;

		/// <summary>
		/// Creates a global mutex and keeps a static reference to it. This method can be used to
		/// create a mutex to easily synchronise with a setup.
		/// </summary>
		/// <param name="name">The name of the mutex to create in the Global namespace.</param>
		public static void Create(string name)
		{
			if (instance != null)
				throw new InvalidOperationException("The static global mutex is already created.");

			// Create a new mutex and keep a reference to it so it won't be GC'ed
			instance = new GlobalMutex(name);
			// Release the mutex when the application exits
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
		}

		private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			instance.Dispose();
		}

		/// <summary>
		/// Gets the static instance of the global mutex.
		/// </summary>
		public static GlobalMutex Instance
		{
			get { return instance; }
		}

		#endregion Static members

		#region Private data

		private Mutex mutex;
		private bool hasHandle;
		private bool isDisposed;

		#endregion Private data

		#region Constructors

		/// <summary>
		/// Creates a global mutex and allows everyone access to it.
		/// </summary>
		/// <param name="name">The name of the mutex to create in the Global namespace.</param>
		public GlobalMutex(string name)
		{
			// Allow full control of the mutex for everyone so that other users will be able to
			// create the same mutex and synchronise on it, if required.
			var allowEveryoneRule = new MutexAccessRule(
				new SecurityIdentifier(WellKnownSidType.WorldSid, null),
				MutexRights.FullControl,
				AccessControlType.Allow);
			var securitySettings = new MutexSecurity();
			securitySettings.AddAccessRule(allowEveryoneRule);

			bool createdNew;
			// Use the Global prefix to make it a system-wide object
			mutex = new Mutex(false, @"Global\" + name, out createdNew, securitySettings);
		}

		#endregion Constructors

		#region Synchronisation methods

		/// <summary>
		/// Waits to acquire a lock on the mutex.
		/// </summary>
		/// <param name="timeout">The timeout to wait for the lock.</param>
		/// <returns>true if the mutex was acquired and the lock is now held; otherwise, false.</returns>
		public bool TryWait(TimeSpan timeout)
		{
			try
			{
				hasHandle = mutex.WaitOne(timeout);
			}
			catch (AbandonedMutexException)
			{
				// Not sure what happened now... Let it fail, just not as hard.
				hasHandle = false;
			}
			return hasHandle;
		}

		/// <summary>
		/// Releases the previously acquired lock on the mutex.
		/// </summary>
		public void Release()
		{
			if (hasHandle)
			{
				mutex.ReleaseMutex();
			}
		}

		#endregion Synchronisation methods

		#region Dispose and finalizer

		/// <summary>
		/// Releases the mutex and frees all resources.
		/// </summary>
		public void Dispose()
		{
			if (!isDisposed)
			{
				Release();
				mutex.Dispose();
				isDisposed = true;
				GC.SuppressFinalize(this);
			}
		}

		~GlobalMutex()
		{
			Dispose();
		}

		#endregion Dispose and finalizer
	}
}
