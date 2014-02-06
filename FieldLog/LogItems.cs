using System;
using System.Threading;

namespace Unclassified.FieldLog
{
	#region Log item base class

	/// <summary>
	/// Abstract base class that defines a log item.
	/// </summary>
	public abstract class FieldLogItem
	{
		/// <summary>Gets the approximated data size of this log item. Used for buffer size estimation.</summary>
		public int Size { get; protected set; }

		/// <summary>Gets the log item counter. Used for correct ordering of log items with the exact same time value. May wrap around.</summary>
		public int EventCounter { get; internal set; }
		/// <summary>Gets the exact time when the log item was generated.</summary>
		public DateTime Time { get; private set; }
		/// <summary>Gets the priority of the log item.</summary>
		public FieldLogPriority Priority { get; private set; }
		/// <summary>Gets the current unique process execution ID of the log item.</summary>
		public Guid SessionId { get; private set; }
		/// <summary>Gets the current thread ID of the log item.</summary>
		public int ThreadId { get; private set; }

		/// <summary>
		/// Gets the name of the file from which this log item was read, if any.
		/// </summary>
		public string LogItemSourceFileName { get; private set; }

		/// <summary>
		/// Initialises a new instance of the FieldLogItem class with Trace priority.
		/// </summary>
		public FieldLogItem()
			: this(FieldLogPriority.Trace)
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogItem class.
		/// </summary>
		/// <param name="priority">The priority of the new log item.</param>
		public FieldLogItem(FieldLogPriority priority)
		{
			Time = FL.UtcNow;
			Priority = priority;
			SessionId = FL.SessionId;
			if (FL.ThreadId == 0)
			{
				FL.ThreadId = Thread.CurrentThread.ManagedThreadId;
			}
			ThreadId = FL.ThreadId;

			Size = 4 + 4 + 8 + 4 + 16 + 4;
		}

		/// <summary>
		/// Converts the data of the current FieldLogItem object to its equivalent string
		/// representation.
		/// </summary>
		/// <returns>A string representation of the value of the current FieldLogItem object.</returns>
		public override string ToString()
		{
			return "FieldLogItem(EventCounter=" + EventCounter + ", Time=" + Time.ToString("yyyyMMdd'T'HHmmss") + ", Priority=" + Priority + ")";
		}

		/// <summary>
		/// Writes the common log item fields to the log file writer.
		/// </summary>
		/// <param name="writer">The log file writer to write to.</param>
		internal virtual void Write(FieldLogFileWriter writer)
		{
			writer.AddBuffer(Time.Ticks);
			writer.AddBuffer(EventCounter);
			writer.AddBuffer((byte) Priority);
			writer.AddBuffer(SessionId.ToByteArray());
			writer.AddBuffer(ThreadId);
		}

		internal static FieldLogItem Read(FieldLogFileReader reader, FieldLogItemType type)
		{
			switch (type)
			{
				case FieldLogItemType.Text:
					return FieldLogTextItem.Read(reader);
				case FieldLogItemType.Data:
					return FieldLogDataItem.Read(reader);
				case FieldLogItemType.Exception:
					return FieldLogExceptionItem.Read(reader);
				case FieldLogItemType.Scope:
				case FieldLogItemType.RepeatedScope:
					return FieldLogScopeItem.Read(reader, type == FieldLogItemType.RepeatedScope);
			}
			throw new ArgumentException("Unsupported log item type (" + (int) type + ")");
		}

		/// <summary>
		/// Reads the common log item fields from the specified log file reader.
		/// </summary>
		/// <param name="reader">The log file reader to read from.</param>
		protected void ReadBaseData(FieldLogFileReader reader)
		{
			LogItemSourceFileName = reader.FileName;
			Time = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);
			EventCounter = reader.ReadInt32();
			Priority = (FieldLogPriority) reader.ReadByte();
			SessionId = new Guid(reader.ReadBytes(16));
			ThreadId = reader.ReadInt32();
		}
	}

	#endregion Log item base class

	#region Text log item class

	/// <summary>
	/// Defines a log item that contains a simple text message.
	/// </summary>
	public class FieldLogTextItem : FieldLogItem
	{
		/// <summary>Gets the text message.</summary>
		public string Text { get; private set; }
		/// <summary>Gets additional details of the log event.</summary>
		public string Details { get; private set; }

		private FieldLogTextItem()
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogTextItem class with Trace priority.
		/// </summary>
		/// <param name="text">The text message.</param>
		public FieldLogTextItem(string text)
			: this(FieldLogPriority.Trace, text, null)
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogTextItem class with Trace priority.
		/// </summary>
		/// <param name="text">The text message.</param>
		/// <param name="details">Additional details of the log event.</param>
		public FieldLogTextItem(string text, string details)
			: this(FieldLogPriority.Trace, text, details)
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogTextItem class.
		/// </summary>
		/// <param name="priority">The priority of the new log item.</param>
		/// <param name="text">The text message.</param>
		public FieldLogTextItem(FieldLogPriority priority, string text)
			: this(priority, text, null)
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogTextItem class.
		/// </summary>
		/// <param name="priority">The priority of the new log item.</param>
		/// <param name="text">The text message.</param>
		/// <param name="details">Additional details of the log event.</param>
		public FieldLogTextItem(FieldLogPriority priority, string text, string details)
			: base(priority)
		{
			Text = text;
			Details = details;

			Size += (Text != null ? Text.Length * 2 : 0) +
				(Details != null ? Details.Length * 2 : 0);
		}

		/// <summary>
		/// Converts the data of the current FieldLogTextItem object to its equivalent string
		/// representation.
		/// </summary>
		/// <returns>A string representation of the value of the current FieldLogTextItem object.</returns>
		public override string ToString()
		{
			return "FieldLogTextItem(EventCounter=" + EventCounter + ", Time=" + Time.ToString("yyyyMMdd'T'HHmmss") + ", Priority=" + Priority + ", Text=" + Text + ")";
		}

		/// <summary>
		/// Writes the log item fields to the log file writer.
		/// </summary>
		/// <param name="writer">The log file writer to write to.</param>
		internal override void Write(FieldLogFileWriter writer)
		{
			writer.SetItemType(FieldLogItemType.Text);
			base.Write(writer);
			writer.AddBuffer(Text);
			writer.AddBuffer(Details);
			writer.WriteBuffer();
		}

		/// <summary>
		/// Reads the log item fields from the specified log file reader.
		/// </summary>
		/// <param name="reader">The log file reader to read from.</param>
		internal static FieldLogTextItem Read(FieldLogFileReader reader)
		{
			FieldLogTextItem item = new FieldLogTextItem();
			item.ReadBaseData(reader);
			item.Text = reader.ReadString();
			item.Details = reader.ReadString();
			return item;
		}
	}

	#endregion Text log item class

	#region Data log item class

	/// <summary>
	/// Defines a log item that contains a variable name and value.
	/// </summary>
	public class FieldLogDataItem : FieldLogItem
	{
		/// <summary>Gets the name of the data item.</summary>
		public string Name { get; private set; }
		/// <summary>Gets the value of the data item.</summary>
		public string Value { get; private set; }

		// TODO: Add helper methods to serialise objects or other data types, e.g. to JSON

		private FieldLogDataItem()
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogDataItem class with Trace priority.
		/// </summary>
		/// <param name="name">The name of the data item. Can be an arbitrary string that is useful for the logging purpose.</param>
		/// <param name="value">The value of the data item. Must be converted to a string, line breaks are allowed for structuring.</param>
		public FieldLogDataItem(string name, string value)
			: this(FieldLogPriority.Trace, name, value)
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogDataItem class.
		/// </summary>
		/// <param name="priority">The priority of the new log item.</param>
		/// <param name="name">The name of the data item. Can be an arbitrary string that is useful for the logging purpose.</param>
		/// <param name="value">The value of the data item. Must be converted to a string, line breaks are allowed for structuring.</param>
		public FieldLogDataItem(FieldLogPriority priority, string name, string value)
			: base(priority)
		{
			Name = name;
			Value = value;

			Size += (Name != null ? Name.Length * 2 : 0) +
				(Value != null ? Value.Length * 2 : 0);
		}

		/// <summary>
		/// Converts the data of the current FieldLogDataItem object to its equivalent string
		/// representation.
		/// </summary>
		/// <returns>A string representation of the value of the current FieldLogDataItem object.</returns>
		public override string ToString()
		{
			return "FieldLogDataItem(EventCounter=" + EventCounter + ", Time=" + Time.ToString("yyyyMMdd'T'HHmmss") + ", Priority=" + Priority + ", Name=" + Name + ")";
		}

		/// <summary>
		/// Writes the log item fields to the log file writer.
		/// </summary>
		/// <param name="writer">The log file writer to write to.</param>
		internal override void Write(FieldLogFileWriter writer)
		{
			writer.SetItemType(FieldLogItemType.Data);
			base.Write(writer);
			writer.AddBuffer(Name);
			writer.AddBuffer(Value);
			writer.WriteBuffer();
		}

		/// <summary>
		/// Reads the log item fields from the specified log file reader.
		/// </summary>
		/// <param name="reader">The log file reader to read from.</param>
		internal static FieldLogDataItem Read(FieldLogFileReader reader)
		{
			FieldLogDataItem item = new FieldLogDataItem();
			item.ReadBaseData(reader);
			item.Name = reader.ReadString();
			item.Value = reader.ReadString();
			return item;
		}
	}

	#endregion Data log item class

	#region Exception log item class

	/// <summary>
	/// Defines a log item that contains exception and environment information.
	/// </summary>
	public class FieldLogExceptionItem : FieldLogItem
	{
		/// <summary>Gets the exception instance.</summary>
		public FieldLogException Exception { get; private set; }
		/// <summary>Gets the context in which the exception has been thrown.</summary>
		public string Context { get; private set; }
		/// <summary>Gets the environment at the time of creating the log item.</summary>
		public FieldLogEventEnvironment EnvironmentData { get; private set; }

		private FieldLogExceptionItem()
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogExceptionItem class with Trace priority.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		public FieldLogExceptionItem(Exception ex)
			: this(FieldLogPriority.Critical, ex, null)
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogExceptionItem class with Trace priority.
		/// </summary>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown. Can be an
		/// arbitrary string that is useful for the logging purpose.</param>
		public FieldLogExceptionItem(Exception ex, string context)
			: this(FieldLogPriority.Critical, ex, context)
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogExceptionItem class.
		/// </summary>
		/// <param name="priority">The priority of the new log item.</param>
		/// <param name="ex">The exception instance.</param>
		public FieldLogExceptionItem(FieldLogPriority priority, Exception ex)
			: this(priority, ex, null)
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogExceptionItem class.
		/// </summary>
		/// <param name="priority">The priority of the new log item.</param>
		/// <param name="ex">The exception instance.</param>
		/// <param name="context">The context in which the exception has been thrown. Can be an
		/// arbitrary string that is useful for the logging purpose.</param>
		public FieldLogExceptionItem(FieldLogPriority priority, Exception ex, string context)
			: base(priority)
		{
			Exception = new FieldLogException(ex);
			Context = context;
			EnvironmentData = FieldLogEventEnvironment.Current();

			Size += Exception.Size +
				(Context != null ? Context.Length * 2 : 0) +
				EnvironmentData.Size;
		}

		/// <summary>
		/// Converts the data of the current FieldLogExceptionItem object to its equivalent string
		/// representation.
		/// </summary>
		/// <returns>A string representation of the value of the current FieldLogExceptionItem object.</returns>
		public override string ToString()
		{
			return "FieldLogExceptionItem(EventCounter=" + EventCounter + ", Time=" + Time.ToString("yyyyMMdd'T'HHmmss") + ", Priority=" + Priority + ", Message=" + Exception.Message + ")";
		}

		/// <summary>
		/// Writes the log item fields to the log file writer.
		/// </summary>
		/// <param name="writer">The log file writer to write to.</param>
		internal override void Write(FieldLogFileWriter writer)
		{
			writer.SetItemType(FieldLogItemType.Exception);
			base.Write(writer);
			Exception.Write(writer);
			writer.AddBuffer(Context);
			EnvironmentData.Write(writer);
			writer.WriteBuffer();
		}

		/// <summary>
		/// Reads the log item fields from the specified log file reader.
		/// </summary>
		/// <param name="reader">The log file reader to read from.</param>
		internal static FieldLogExceptionItem Read(FieldLogFileReader reader)
		{
			FieldLogExceptionItem item = new FieldLogExceptionItem();
			item.ReadBaseData(reader);
			item.Exception = FieldLogException.Read(reader);
			item.Context = reader.ReadString();
			item.EnvironmentData = FieldLogEventEnvironment.Read(reader);
			return item;
		}
	}

	#endregion Exception log item class

	#region Scope log item class

	/// <summary>
	/// Defines a log item that contains code scope information.
	/// </summary>
	public class FieldLogScopeItem : FieldLogItem
	{
		/// <summary>Gets the scope type.</summary>
		public FieldLogScopeType Type { get; private set; }
		/// <summary>Gets the new scope nesting level after the log item. The last scope item in a thread should be 0.</summary>
		public int Level { get; private set; }
		/// <summary>Gets the scope name. Should be application-unique and hierarchical for easier analysis.</summary>
		public string Name { get; private set; }
		/// <summary>Gets a value indicating whether this is a background thread. (Only valid when entering a thread scope.)</summary>
		public bool IsBackgroundThread { get; private set; }
		/// <summary>Gets a value indicating whether this is a pool thread. (Only valid when entering a thread scope.)</summary>
		public bool IsPoolThread { get; private set; }
		/// <summary>Gets the process static environment data. (Only valid when entering a process scope.)</summary>
		public FieldLogEventEnvironment EnvironmentData { get; private set; }

		/// <summary>
		/// Gets or sets a value whether this item has already been written to a log file. Items
		/// that have not yet been written will not be repeated upon creating the next new log file.
		/// </summary>
		public bool WasWritten { get; set; }
		/// <summary>
		/// Gets or sets a value whether this item is repeated in a new log file.
		/// </summary>
		public bool IsRepeated { get; set; }
		/// <summary>
		/// Gets the thread object for a ThreadStart-type log item.
		/// </summary>
		public Thread Thread { get; private set; }

		private FieldLogScopeItem()
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogExceptionItem class with Trace priority.
		/// </summary>
		/// <param name="type">The scope type.</param>
		/// <param name="name">The scope name.</param>
		public FieldLogScopeItem(FieldLogScopeType type, string name)
			: this(FieldLogPriority.Trace, type, name)
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogExceptionItem class.
		/// </summary>
		/// <param name="priority">The priority of the new log item.</param>
		/// <param name="type">The scope type.</param>
		/// <param name="name">The scope name.</param>
		public FieldLogScopeItem(FieldLogPriority priority, FieldLogScopeType type, string name)
			: base(priority)
		{
			Type = type;
			Level = FL.ScopeLevel;
			Name = name;

			if (Type == FieldLogScopeType.ThreadStart)
			{
				IsBackgroundThread = Thread.CurrentThread.IsBackground;
				IsPoolThread = Thread.CurrentThread.IsThreadPoolThread;

				Thread = Thread.CurrentThread;
			}
			if (Type == FieldLogScopeType.LogStart)
			{
				EnvironmentData = FieldLogEventEnvironment.Current();
				Size += EnvironmentData.Size;
			}

			Size += 4 + 4 +
				(Name != null ? Name.Length * 2 : 0) +
				4 + 4 + 4 + 4 + 4;
		}

		/// <summary>
		/// Converts the data of the current FieldLogScopeItem object to its equivalent string
		/// representation.
		/// </summary>
		/// <returns>A string representation of the value of the current FieldLogScopeItem object.</returns>
		public override string ToString()
		{
			return "FieldLogScopeItem(EventCounter=" + EventCounter + ", Time=" + Time.ToString("yyyyMMdd'T'HHmmss") + ", Priority=" + Priority + ", Scope=" + Type + ")";
		}

		/// <summary>
		/// Writes the log item fields to the log file writer.
		/// </summary>
		/// <param name="writer">The log file writer to write to.</param>
		internal override void Write(FieldLogFileWriter writer)
		{
			writer.SetItemType(IsRepeated ? FieldLogItemType.RepeatedScope : FieldLogItemType.Scope);
			base.Write(writer);
			writer.AddBuffer((byte) Type);
			writer.AddBuffer(Level);
			writer.AddBuffer(Name);

			if (Type == FieldLogScopeType.ThreadStart)
			{
				byte flags = 0;
				if (IsBackgroundThread) flags |= 1;
				if (IsPoolThread) flags |= 2;
				writer.AddBuffer(flags);
			}
			if (Type == FieldLogScopeType.LogStart)
			{
				EnvironmentData.Write(writer);
			}

			writer.WriteBuffer();
		}

		/// <summary>
		/// Reads the log item fields from the specified log file reader.
		/// </summary>
		/// <param name="reader">The log file reader to read from.</param>
		/// <param name="isRepeated">true if the log item is repeated from a previous file, false if it is written for the first time.</param>
		internal static FieldLogScopeItem Read(FieldLogFileReader reader, bool isRepeated)
		{
			FieldLogScopeItem item = new FieldLogScopeItem();
			item.ReadBaseData(reader);
			item.IsRepeated = isRepeated;
			item.Type = (FieldLogScopeType) reader.ReadByte();
			item.Level = reader.ReadInt32();
			item.Name = reader.ReadString();
			
			if (item.Type == FieldLogScopeType.ThreadStart)
			{
				byte flags = reader.ReadByte();
				item.IsBackgroundThread = (flags & 1) != 0;
				item.IsPoolThread = (flags & 2) != 0;
			}
			if (item.Type == FieldLogScopeType.LogStart)
			{
				item.EnvironmentData = FieldLogEventEnvironment.Read(reader);
			}
			return item;
		}
	}

	#endregion Scope log item class

	// TODO: Prepare time measurement by log items (not only temporarily in the log viewer) - with multiple intermediate times, with multiple timers

	#region Scope helpers

	/// <summary>
	/// Provides an IDisposable implementation to help in general scope logging.
	/// </summary>
	public class FieldLogScope : IDisposable
	{
		private string name;

		/// <summary>
		/// Initialises a new instance of the FieldLogScope class.
		/// </summary>
		/// <param name="name">The scope name.</param>
		public FieldLogScope(string name)
		{
			this.name = name;
			FL.Enter(name);
		}

		/// <summary>
		/// Logs the end of the current scope.
		/// </summary>
		public void Dispose()
		{
			FL.Leave(name);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Finalises the FieldLogScope instance. This generates an Error log item.
		/// </summary>
		~FieldLogScope()
		{
			FL.Error("FieldLogScope.Dispose was not called! Scope levels before this item may be wrong.", "Name = " + name);
			Dispose();
		}
	}

	/// <summary>
	/// Provides an IDisposable implementation to help in thread scope logging.
	/// </summary>
	public class FieldLogThreadScope : IDisposable
	{
		private string name;

		/// <summary>
		/// Initialises a new instance of the FieldLogThreadScope class.
		/// </summary>
		/// <param name="name">The thread scope name.</param>
		public FieldLogThreadScope(string name)
		{
			this.name = name;
			FL.LogScope(FieldLogScopeType.ThreadStart, name);
		}

		/// <summary>
		/// Logs the end of the current scope.
		/// </summary>
		public void Dispose()
		{
			FL.LogScope(FieldLogScopeType.ThreadEnd, name);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Finalises the FieldLogThreadScope instance. This generates an Error log item.
		/// </summary>
		~FieldLogThreadScope()
		{
			FL.Error("FieldLogThreadScope.Dispose was not called! Scope levels before this item may be wrong.", "Name = " + name);
			Dispose();
		}
	}

	#endregion Scope helpers
}
