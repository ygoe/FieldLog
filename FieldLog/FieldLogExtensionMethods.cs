using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Provides diverse extension methods for use with FieldLog.
	/// </summary>
	/// <remarks>
	/// This class is not included in the NET20 project.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static class FieldLogExtensionMethods
	{
		/// <summary>
		/// Logs an exception raised in the task. Call this like the ContinueWith method with the
		/// <see cref="TaskContinuationOptions.OnlyOnFaulted"/> option.
		/// </summary>
		/// <param name="task"></param>
		/// <param name="taskName">The name of the task, used for the exception log item context.</param>
		/// <returns>A new continuation <see cref="Task"/>.</returns>
		public static Task LogFaulted(this Task task, string taskName)
		{
			return task.ContinueWith(t => FL.Error(t.Exception, taskName + " Task"), TaskContinuationOptions.OnlyOnFaulted);
		}

		/// <summary>
		/// Logs an exception raised in the task. Call this like the ContinueWith method with the
		/// <see cref="TaskContinuationOptions.OnlyOnFaulted"/> option.
		/// </summary>
		/// <param name="task"></param>
		/// <param name="prio">The priority of the log item.</param>
		/// <param name="taskName">The name of the task, used for the exception log item context.</param>
		/// <returns>A new continuation <see cref="Task"/>.</returns>
		public static Task LogFaulted(this Task task, FieldLogPriority prio, string taskName)
		{
			return task.ContinueWith(t => FL.Exception(prio, t.Exception, taskName + " Task"), TaskContinuationOptions.OnlyOnFaulted);
		}

		/// <summary>
		/// Logs an exception raised in the task. Call this like the ContinueWith method with the
		/// <see cref="TaskContinuationOptions.OnlyOnFaulted"/> option.
		/// </summary>
		/// <typeparam name="TResult">The type of the result produced by this Task.</typeparam>
		/// <param name="task"></param>
		/// <param name="taskName">The name of the task, used for the exception log item context.</param>
		/// <returns>A new continuation <see cref="Task"/>.</returns>
		public static Task LogFaulted<TResult>(this Task<TResult> task, string taskName)
		{
			return task.ContinueWith(t => FL.Error(t.Exception, taskName + " Task"), TaskContinuationOptions.OnlyOnFaulted);
		}

		/// <summary>
		/// Logs an exception raised in the task. Call this like the ContinueWith method with the
		/// <see cref="TaskContinuationOptions.OnlyOnFaulted"/> option.
		/// </summary>
		/// <typeparam name="TResult">The type of the result produced by this Task.</typeparam>
		/// <param name="task"></param>
		/// <param name="prio">The priority of the log item.</param>
		/// <param name="taskName">The name of the task, used for the exception log item context.</param>
		/// <returns>A new continuation <see cref="Task"/>.</returns>
		public static Task LogFaulted<TResult>(this Task<TResult> task, FieldLogPriority prio, string taskName)
		{
			return task.ContinueWith(t => FL.Exception(prio, t.Exception, taskName + " Task"), TaskContinuationOptions.OnlyOnFaulted);
		}
	}
}
