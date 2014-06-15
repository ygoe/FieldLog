// FieldLog – .NET logging solution
// © Yves Goergen, Made in Germany
// Website: http://dev.unclassified.de/source/fieldlog
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
			foreach (var kvp in sources)
			{
				FieldLogTraceListener listener;
				if (!listeners.TryGetValue(kvp.Key, out listener))
				{
					listener = new FieldLogTraceListener(kvp.Value);
					listeners.Add(kvp.Key, listener);
					kvp.Key.Listeners.Add(listener);
				}
				PresentationTraceSources.DataBindingSource.Switch.Level = level;
			}

			if (diagTraceListener == null)
			{
				diagTraceListener = new FieldLogTraceListener("Diagnostics.Trace");
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

		#endregion Static listener management

		#region Private data

		private string sourceName;
		private StringBuilder msgSb = new StringBuilder();

		#endregion Private data

		#region Constructor

		private FieldLogTraceListener(string sourceName)
		{
			this.sourceName = sourceName;
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

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
		{
			TraceEvent(eventCache, source, eventType, id, "");
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			WriteToFieldLog(source, eventType, id, string.Format(CultureInfo.InvariantCulture, format, args));
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
		{
			WriteToFieldLog(source, eventType, id, message);
		}

		// Also support plain text messages. Collect all Write(), write item on WriteLine().
		public override void Write(object o)
		{
			msgSb.Append(o);
		}

		public override void Write(string message)
		{
			msgSb.Append(message);
		}

		public override void WriteLine(object o)
		{
			msgSb.Append(o);
			WriteLine("");
		}

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
			string shortMsg = "ID " + id;

			if (source == PresentationTraceSources.DataBindingSource.Name)
			{
				int i = msg.IndexOf(" BindingExpression:");
				if (i > 0)
				{
					msg = msg.Substring(0, i) + "\n\n" + msg.Substring(i + 1).Replace("; ", ";\n");
				}

				// From [PresentationFramework]MS.Internal.TraceData properties and methods.
				// There are other classes in this namespace (also in assembly [WindowsBase]) for
				// other trace sources as well.
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
			}

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
			}

			if (!FL.IsShutdown)
			{
				FL.Text(prio, sourceName + ": " + shortMsg, msg + "\n\nEvent ID: " + id);
			}
		}

		#endregion FieldLog writing
	}
}
