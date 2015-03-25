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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Implements a TraceListener that can be added to several TraceSources and writes their
	/// events to the FieldLog log file.
	/// </summary>
	/// <remarks>
	/// This class is largely specific to WPF and not included in the NET20 project.
	/// </remarks>
	internal class FieldLogTraceListener : TraceListener
	{
		#region Static listener management

		private static Dictionary<TraceSource, string> sources = new Dictionary<TraceSource, string>();
		private static Dictionary<TraceSource, FieldLogTraceListener> listeners = new Dictionary<TraceSource, FieldLogTraceListener>();
		private static FieldLogTraceListener diagTraceListener;

		static FieldLogTraceListener()
		{
			// Collect all available trace sources for easier enumeration
			sources.Add(PresentationTraceSources.AnimationSource, "WPF Animation");
			sources.Add(PresentationTraceSources.DataBindingSource, "WPF DataBinding");
			sources.Add(PresentationTraceSources.DependencyPropertySource, "WPF DependencyProperty");
			sources.Add(PresentationTraceSources.DocumentsSource, "WPF Documents");
			sources.Add(PresentationTraceSources.FreezableSource, "WPF Freezable");
			sources.Add(PresentationTraceSources.HwndHostSource, "WPF HwndHost");
			sources.Add(PresentationTraceSources.MarkupSource, "WPF Markup");
			sources.Add(PresentationTraceSources.NameScopeSource, "WPF NameScope");
			sources.Add(PresentationTraceSources.ResourceDictionarySource, "WPF ResourceDictionary");
			sources.Add(PresentationTraceSources.RoutedEventSource, "WPF RoutedEvent");
			sources.Add(PresentationTraceSources.ShellSource, "WPF Shell");
		}

		/// <summary>
		/// Starts event logging of all events with Warning level and up from all WPF sources and
		/// from System.Diagnostics.Trace.
		/// </summary>
		public static void Start()
		{
			Start(SourceLevels.Warning);
		}

		/// <summary>
		/// Starts event logging of all events from all WPF sources and System.Diagnostics.Trace.
		/// </summary>
		/// <param name="level">The minimum source level to log.</param>
		public static void Start(SourceLevels level)
		{
			// Enable WPF tracing, independently of registry setting
			PresentationTraceSources.Refresh();

			foreach (var kvp in sources)
			{
				FieldLogTraceListener listener;
				if (!listeners.TryGetValue(kvp.Key, out listener))
				{
					listener = new FieldLogTraceListener(kvp.Value, "WPF");
					listeners.Add(kvp.Key, listener);
					kvp.Key.Listeners.Add(listener);
				}
				kvp.Key.Switch.Level = level;
			}

			if (diagTraceListener == null)
			{
				diagTraceListener = new FieldLogTraceListener("Diagnostics.Trace", "Trace");
				Trace.Listeners.Add(diagTraceListener);
			}
		}

		/// <summary>
		/// Stops event logging started with <see cref="Start()"/>.
		/// </summary>
		public static void Stop()
		{
			foreach (var kvp in sources)
			{
				FieldLogTraceListener listener;
				if (listeners.TryGetValue(kvp.Key, out listener))
				{
					listener.Flush();
					listener.Close();
					kvp.Key.Listeners.Remove(listener);
					listeners.Remove(kvp.Key);
				}
			}

			if (diagTraceListener != null)
			{
				diagTraceListener.Flush();
				diagTraceListener.Close();
				Trace.Listeners.Remove(diagTraceListener);
				diagTraceListener = null;
			}
		}

		/// <summary>
		/// Sets the minimum source level to log while the trace logging is active.
		/// </summary>
		/// <param name="level">The minimum source level to log.</param>
		public static void SetLevel(SourceLevels level)
		{
			foreach (var kvp in sources)
			{
				if (listeners.ContainsKey(kvp.Key))
				{
					kvp.Key.Switch.Level = level;
				}
			}
		}

		/// <summary>
		/// Resets the minimum source level to log to its initial Warning value.
		/// </summary>
		public static void ResetLevel()
		{
			SetLevel(SourceLevels.Warning);
		}

		/// <summary>
		/// Sets the minimum source level to log and resets it to its initial Warning value at the
		/// specified Dispatcher priority.
		/// </summary>
		/// <param name="level">The minimum source level to log.</param>
		/// <param name="dispPriority">The Dispatcher priority at which to reset the level.</param>
		public static void SetLevelUntilDispatcherPriority(SourceLevels level, DispatcherPriority dispPriority)
		{
			SetLevel(level);
			Dispatcher.CurrentDispatcher.BeginInvoke(
				new Action(() => SetLevel(SourceLevels.Warning)),
				dispPriority);
		}

		/// <summary>
		/// Sets the minimum source level to log and resets it to its initial Warning value at the
		/// Loaded Dispatcher priority.
		/// </summary>
		/// <param name="level">The minimum source level to log.</param>
		public static void SetLevelUntilLoaded(SourceLevels level)
		{
			SetLevel(level);
			Dispatcher.CurrentDispatcher.BeginInvoke(
				new Action(() => SetLevel(SourceLevels.Warning)),
				DispatcherPriority.Loaded);
		}

		#endregion Static listener management

		#region Private data

		// Shared across all listeners for all WPF trace sources
		// (Actually shared across all listeners like this, but it's good enough for now)
		private static int indentLevel;

		/// <summary>
		/// List of cached pre-generated indentation prefixes. The item index is the indentation
		/// level, 0 is unindented.
		/// </summary>
		private static List<string> indentStrings = new List<string>();

		private string sourceName;
		private string shortName;
		private StringBuilder msgSb = new StringBuilder();

		#endregion Private data

		#region Constructor

		private FieldLogTraceListener(string sourceName, string shortName)
		{
			this.sourceName = sourceName;
			this.shortName = shortName;
		}

		#endregion Constructor

		#region Overridden trace methods

		// These are probably never used?
		//public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
		//{
		//    base.TraceData(eventCache, source, eventType, id, data);
		//}

		//public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
		//{
		//    base.TraceData(eventCache, source, eventType, id, data);
		//}

		/// <inheritdoc/>
		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
		{
			TraceEvent(eventCache, source, eventType, id, "");
		}

		/// <inheritdoc/>
		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			WriteToFieldLog(source, eventType, id, string.Format(CultureInfo.InvariantCulture, format, args));
		}

		/// <inheritdoc/>
		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
		{
			WriteToFieldLog(source, eventType, id, message);
		}

		// Also support plain text messages. Collect all Write(), write item on WriteLine().
		/// <inheritdoc/>
		public override void Write(object o)
		{
			msgSb.Append(o);
		}

		/// <inheritdoc/>
		public override void Write(string message)
		{
			msgSb.Append(message);
		}

		/// <inheritdoc/>
		public override void WriteLine(object o)
		{
			msgSb.Append(o);
			WriteLine("");
		}

		/// <inheritdoc/>
		public override void WriteLine(string message)
		{
			msgSb.Append(message);
			var final = msgSb.ToString();
			msgSb.Length = 0;
			if (!FL.IsShutdown)
			{
				FL.Trace(sourceName + ": " + final);
			}
		}

		#endregion Overridden trace methods

		#region FieldLog writing

		private void WriteToFieldLog(string source, TraceEventType eventType, int id, string msg)
		{
			string shortMsg = null;

			// Name comparisons roughly in a descending order of frequency, to optimise performance
			if (source == PresentationTraceSources.DataBindingSource.Name)
			{
				HandleDataBindingMessage(id, ref msg, ref shortMsg);
			}
			else if (source == PresentationTraceSources.RoutedEventSource.Name)
			{
				if (id == 3)
				{
					if (eventType == TraceEventType.Start)
					{
						eventType = TraceEventType.Verbose;   // Don't indent for this
					}
					else if (eventType == TraceEventType.Stop)
					{
						return;   // Don't log this
					}
				}
				//if (id == 4)
				//{
				//    return;   // Don't log this
				//}
				HandleRoutedEventMessage(id, ref msg, ref shortMsg);
			}
			else if (source == PresentationTraceSources.ResourceDictionarySource.Name)
			{
				HandleResourceDictionaryMessage(id, ref msg, ref shortMsg);
			}
			else if (source == PresentationTraceSources.MarkupSource.Name)
			{
				HandleMarkupMessage(id, ref msg, ref shortMsg);
			}
			else if (source == PresentationTraceSources.AnimationSource.Name)
			{
				HandleAnimationMessage(id, ref msg, ref shortMsg);
			}
			else if (source == PresentationTraceSources.DependencyPropertySource.Name)
			{
				HandleDependencyPropertyMessage(id, ref msg, ref shortMsg);
			}
			else if (source == PresentationTraceSources.FreezableSource.Name)
			{
				HandleFreezableMessage(id, ref msg, ref shortMsg);
			}
			else if (source == PresentationTraceSources.HwndHostSource.Name)
			{
				HandleHwndHostMessage(id, ref msg, ref shortMsg);
			}
			else if (source == PresentationTraceSources.NameScopeSource.Name)
			{
				HandleNameScopeMessage(id, ref msg, ref shortMsg);
			}
			else if (source == PresentationTraceSources.ShellSource.Name)
			{
				HandleShellMessage(id, ref msg, ref shortMsg);
			}
			// DocumentsSource does not have any information and is not augmented here

			// Fallback short message if unknown event ID
			if (shortMsg == null)
			{
				shortMsg = "ID " + id;
			}

			// Select appropriate FieldLog priority or otherwise highlight event type
			FieldLogPriority prio = FieldLogPriority.Trace;
			switch (eventType)
			{
				case TraceEventType.Critical:
				case TraceEventType.Error:
					prio = FieldLogPriority.Error;
					break;
				case TraceEventType.Warning:
					prio = FieldLogPriority.Warning;
					break;
				case TraceEventType.Information:
					prio = FieldLogPriority.Info;
					break;
				case TraceEventType.Start:
					//shortMsg += " [Start]";
					break;
				case TraceEventType.Stop:
					shortMsg += " [Stop]";
					break;
				case TraceEventType.Suspend:
					shortMsg += " [Suspend]";
					break;
				case TraceEventType.Resume:
					shortMsg += " [Resume]";
					break;
				case TraceEventType.Transfer:
					shortMsg += " [Transfer]";
					break;
			}

			// Write message to the log if it's still there
			if (!FL.IsShutdown)
			{
				if (eventType == TraceEventType.Stop)
				{
					indentLevel--;
				}

				string indentPrefix = null;
				if (indentLevel < 0)
				{
					indentPrefix = "«";
					indentLevel = 0;
				}
				else if (indentLevel > 0)
				{
					// Use a cached pre-generated indent prefix for better performance
					while (indentStrings.Count <= indentLevel)
					{
						int newLevel = indentStrings.Count;
						string prefix = "";
						for (int i = 1; i <= newLevel; i++)
						{
							prefix += "-  ";
						}
						indentStrings.Add(prefix);
					}
					indentPrefix = indentStrings[indentLevel];
				}

				FL.Text(
					prio,
					indentPrefix + shortName + ": " + shortMsg,
					(!string.IsNullOrEmpty(msg) ? msg + "\n\n" : "") +
						"Event ID: " + id + "\nEvent type: " + eventType + "\nSource: " + sourceName);

				if (eventType == TraceEventType.Start)
				{
					indentLevel++;
				}
			}
		}

		private static void HandleAnimationMessage(int id, ref string msg, ref string shortMsg)
		{
			int i = msg.IndexOf("; ", StringComparison.Ordinal);
			if (i > 0)
			{
				msg = msg.Substring(0, i) + "\n\n" + msg.Substring(i + 2).Replace("; ", ";\n");
			}

			// From [PresentationCore]MS.Internal.TraceAnimation properties
			switch (id)
			{
				case 1: shortMsg = "Storyboard begin"; break;
				case 2: shortMsg = "Storyboard pause"; break;
				case 3: shortMsg = "Storyboard remove"; break;
				case 4: shortMsg = "Storyboard resume"; break;
				case 5: shortMsg = "Storyboard stop"; break;
				case 6: shortMsg = "Storyboard not applied"; break;
				case 7: shortMsg = "Animate storage validation failed"; break;
				case 8: shortMsg = "Animate storage validation no longer failing"; break;
			}
		}

		private static void HandleDataBindingMessage(int id, ref string msg, ref string shortMsg)
		{
			int i = msg.IndexOf(" BindingExpression:", StringComparison.Ordinal);
			if (i == -1) i = msg.IndexOf(" MultiBindingExpression:", StringComparison.Ordinal);
			if (i == -1) i = msg.IndexOf(" PriorityBindingExpression:", StringComparison.Ordinal);
			if (i == -1) i = msg.IndexOf(" TemplateBindingExpression:", StringComparison.Ordinal);
			if (i > 0)
			{
				msg = msg.Substring(0, i) + "\n\n" + msg.Substring(i + 1).Replace("; ", ";\n");
			}

			// From [PresentationFramework]MS.Internal.TraceData properties and methods
			switch (id)
			{
				case 1: shortMsg = "Cannot create default converter"; break;
				case 2: shortMsg = "Cannot find FrameworkElement for target element"; break;
				case 3: shortMsg = "Cannot find element that provides DataContext"; break;
				case 4: shortMsg = "Cannot find source"; break;
				case 5: shortMsg = "Value is not valid for target property"; break;
				case 6: shortMsg = "Converter failed for transfer"; break;
				case 7: shortMsg = "Converter failed for update"; break;
				case 8: shortMsg = "Cannot save value from target back to source"; break;
				case 9: shortMsg = "Requires explicit culture"; break;
				case 10: shortMsg = "No value to transfer"; break;
				case 11: shortMsg = "Fallback value conversion failed"; break;
				case 17: shortMsg = "Cannot get CLR raw value"; break;
				case 18: shortMsg = "Cannot set CLR raw value"; break;
				case 19: shortMsg = "Missing source data item"; break;
				case 20: shortMsg = "Missing information"; break;
				case 21: shortMsg = "Null data item"; break;
				case 22: shortMsg = "Default value converter failed"; break;
				case 23: shortMsg = "Default value converter failed for culture"; break;
				case 27: shortMsg = "MultiConverter failed for update"; break;
				case 28: shortMsg = "MultiValueConverter missing for transfer"; break;
				case 29: shortMsg = "MultiValueConverter missing for update"; break;
				case 30: shortMsg = "MultiValueConverter count mismatch"; break;
				case 31: shortMsg = "MultiBinding has no converter"; break;
				case 36: shortMsg = "Reference previous not in context"; break;
				case 37: shortMsg = "Reference no wrapper in children"; break;
				case 38: shortMsg = "Reference ancestor type not specified"; break;
				case 39: shortMsg = "Reference ancestor level invalid"; break;
				case 40: shortMsg = "Property not found"; break;
				case 41: shortMsg = "Property not found because data item is null"; break;
				case 43: shortMsg = "Cannot get IDataErrorInfo.Error"; break;
				case 47: shortMsg = "Cannot get XML node collection"; break;
				case 53: shortMsg = "Using CollectionView directly is not fully supported"; break;
				case 55: shortMsg = "Cannot sort"; break;
				case 71: shortMsg = "DataContext is null"; break;
				case 86: shortMsg = "String formatting failed"; break;
				case 87: shortMsg = "Value is not valid for target"; break;
			}

			if (id == 10)
			{
				// Other similar IDs 20, 21, 41 seem always followed by ID 10, so logging this
				// overview info only once.

				// Parsing what's formatted starting from the
				// [PresentationFramework]MS.Internal.TraceData.OnTrace method
				string path = FindText(msg, "BindingExpression:Path=", ";");
				string elementType = FindText(msg, "target element is '", "'");
				string propertyName = FindText(msg, "target property is '", "'");
				if (elementType != null && propertyName != null)
				{
					shortMsg += ": " + elementType +
						(propertyName != "NoTarget" ? "." + propertyName : ".?") +
						(path != null ? " = " + path : "");
				}
			}
		}

		private static void HandleDependencyPropertyMessage(int id, ref string msg, ref string shortMsg)
		{
			int i = msg.IndexOf("; ", StringComparison.Ordinal);
			if (i > 0)
			{
				msg = msg.Substring(0, i) + "\n\n" + msg.Substring(i + 2).Replace("; ", ";\n");
			}

			// From [WindowsBase]MS.Internal.TraceDependencyProperty properties
			switch (id)
			{
				case 1: shortMsg = "Apply template content"; break;
				case 2: shortMsg = "Register DependencyProperty"; break;
				case 3: shortMsg = "Update effective value (Start)"; break;
				case 4: shortMsg = "Update effective value (Stop)"; break;
			}
		}

		private static void HandleFreezableMessage(int id, ref string msg, ref string shortMsg)
		{
			int i = msg.IndexOf("; ", StringComparison.Ordinal);
			if (i > 0)
			{
				msg = msg.Substring(0, i) + "\n\n" + msg.Substring(i + 2).Replace("; ", ";\n");
			}

			// From [WindowsBase]MS.Internal.TraceFreezable properties
			switch (id)
			{
				case 1: shortMsg = "Unable to freeze expression"; break;
				case 2: shortMsg = "Unable to freeze DispatcherObject with thread affinity"; break;
				case 3: shortMsg = "Unable to freeze Freezable sub-property"; break;
				case 4: shortMsg = "Unable to freeze animated properties"; break;
			}
		}

		private static void HandleHwndHostMessage(int id, ref string msg, ref string shortMsg)
		{
			// From [PresentationFramework]MS.Internal.TraceHwndHost properties
			switch (id)
			{
				case 1: shortMsg = "HwndHost in 3D"; break;
			}
		}

		private static void HandleMarkupMessage(int id, ref string msg, ref string shortMsg)
		{
			int i = msg.IndexOf("; ", StringComparison.Ordinal);
			if (i > 0)
			{
				msg = msg.Substring(0, i) + "\n\n" + msg.Substring(i + 2).Replace("; ", ";\n");
			}

			// From [PresentationFramework]MS.Internal.TraceMarkup properties
			switch (id)
			{
				case 1: shortMsg = "Add value to IAddChild"; break;
				case 2: shortMsg = "Add value to array"; break;
				case 3: shortMsg = "Add value to dictionary"; break;
				case 4: shortMsg = "Add value to list"; break;
				case 5: shortMsg = "Start initialization"; break;
				case 6: shortMsg = "Create MarkupExtension"; break;
				case 7: shortMsg = "Create object"; break;
				case 8: shortMsg = "End initialization"; break;
				case 9: shortMsg = "Load XAML/BAML"; break;
				case 10: shortMsg = "Convert constructor parameter"; break;
				case 11: shortMsg = "Provide value"; break;
				case 12: shortMsg = "Set ContentProperty value"; break;
				case 13: shortMsg = "Set property value"; break;
				case 14: shortMsg = "XAML exception thrown"; break;
				case 15: shortMsg = "Type convert"; break;
				case 16: shortMsg = "Type convert fallback"; break;
			}
		}

		private static void HandleNameScopeMessage(int id, ref string msg, ref string shortMsg)
		{
			int i = msg.IndexOf("; ", StringComparison.Ordinal);
			if (i > 0)
			{
				msg = msg.Substring(0, i) + "\n\n" + msg.Substring(i + 2).Replace("; ", ";\n");
			}

			// From [WindowsBase]MS.Internal.TraceNameScope properties
			switch (id)
			{
				case 1: shortMsg = "Register name"; break;
				case 2: shortMsg = "Unregister name"; break;
			}
		}

		private static void HandleResourceDictionaryMessage(int id, ref string msg, ref string shortMsg)
		{
			int i = msg.IndexOf("; ", StringComparison.Ordinal);
			if (i > 0)
			{
				msg = msg.Substring(0, i) + "\n\n" + msg.Substring(i + 2).Replace("; ", ";\n");
			}

			// From [PresentationFramework]MS.Internal.TraceResourceDictionary properties
			switch (id)
			{
				case 1: shortMsg = "Resource added to ResourceDictionary"; break;
				case 2: shortMsg = "Delayed creation of resource"; break;
				case 3: shortMsg = "Found resource on element"; break;
				case 4: shortMsg = "Found resource in style"; break;
				case 5: shortMsg = "Found resource in template"; break;
				case 6: shortMsg = "Found resource in theme style"; break;
				case 7: shortMsg = "Found resource in application"; break;
				case 8: shortMsg = "Found resource in theme"; break;
				case 9: shortMsg = "Resource not found"; break;
				case 10: shortMsg = "New resource dictionary set"; break;
				case 11: shortMsg = "Searching resource"; break;
				case 12: shortMsg = "Deferred resource added to ResourceDictionary"; break;
			}

			if (id == 11)
			{
				string resKey = FindText(msg, "ResourceKey='", "'");
				if (resKey != null)
				{
					shortMsg += ": " + resKey;
				}
			}
		}

		private static void HandleRoutedEventMessage(int id, ref string msg, ref string shortMsg)
		{
			int i = msg.IndexOf("; ", StringComparison.Ordinal);
			if (i > 0)
			{
				msg = msg.Substring(0, i) + "\n\n" + msg.Substring(i + 2).Replace("; ", ";\n");
			}

			// From [PresentationCore]MS.Internal.TraceRoutedEvent properties
			switch (id)
			{
				case 1: shortMsg = "Raise event"; break;
				case 2: shortMsg = "Re-raise event as"; break;
				case 3: shortMsg = "Handle event"; break;
				case 4: shortMsg = "Invoke handlers"; break;
			}

			if (id == 1 || id == 2)
			{
				string eventName = FindText(msg, "RoutedEvent='", "'");
				string elementType = FindText(msg, "Element.Type='", "'");
				if (eventName != null && elementType != null)
				{
					// Take the last part of each element type and event name
					i = elementType.LastIndexOf('.');
					if (i > 0)
					{
						elementType = elementType.Substring(i + 1);
					}
					i = eventName.LastIndexOf('.');
					if (i > 0)
					{
						eventName = eventName.Substring(i + 1);
					}
					shortMsg += ": " + elementType + "." + eventName;
				}
				else if (eventName != null)
				{
					shortMsg += ": " + eventName;
				}
			}
		}

		private static void HandleShellMessage(int id, ref string msg, ref string shortMsg)
		{
			// From [PresentationFramework]MS.Internal.TraceShell properties and methods
			switch (id)
			{
				case 1: shortMsg = "Not on Windows 7"; break;
				case 2: shortMsg = "Explorer taskbar timeout"; break;
				case 3: shortMsg = "Explorer taskbar retrying"; break;
				case 4: shortMsg = "Explorer taskbar not running"; break;
				case 5: shortMsg = "Native ITaskbarList3 interface failed"; break;
				case 6: shortMsg = "Catastrophic JumpList failure"; break;   // That's how the internal property is named!
				case 7: shortMsg = "No registered handler"; break;
			}
		}

		private static string FindText(string str, string before, string after)
		{
			int i = str.IndexOf(before, StringComparison.Ordinal);
			if (i > 0)
			{
				int j = str.IndexOf(after, i + before.Length, StringComparison.Ordinal);
				if (j > 0)
				{
					return str.Substring(i + before.Length, j - (i + before.Length));
				}
			}
			return null;
		}

		#endregion FieldLog writing
	}
}
