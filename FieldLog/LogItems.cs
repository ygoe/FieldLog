using System;
using System.Threading;

namespace Unclassified.FieldLog
{
	#region Log item data structures

	public abstract class FieldLogItem
	{
		/// <summary>Approximate data size of this log item. Used for buffer size estimation.</summary>
		public int Size { get; protected set; }

		/// <summary>Log items counter. Used for correct ordering of log items with the exact same time value. May wrap around.</summary>
		public int EventCounter { get; internal set; }
		/// <summary>Exact time when the log item was generated.</summary>
		public DateTime Time { get; private set; }
		/// <summary>Priority of the log item.</summary>
		public FieldLogPriority Priority { get; private set; }
		/// <summary>Current unique process execution ID.</summary>
		public Guid SessionId { get; private set; }
		/// <summary>Current thread ID.</summary>
		public int ThreadId { get; private set; }

		/// <summary>
		/// Gets the name of the file from which this log item was read, if any.
		/// </summary>
		public string LogItemSourceFileName { get; private set; }

		public FieldLogItem()
			: this(FieldLogPriority.Trace)
		{
		}

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

		public override string ToString()
		{
			return "FieldLogItem(EventCounter=" + EventCounter + ", Time=" + Time.ToString("yyyyMMdd'T'HHmmss") + ", Priority=" + Priority + ")";
		}

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

	public class FieldLogTextItem : FieldLogItem
	{
		/// <summary>Text message.</summary>
		public string Text { get; private set; }
		/// <summary>Additional details of the log event.</summary>
		public string Details { get; private set; }

		private FieldLogTextItem()
		{
		}

		public FieldLogTextItem(string text)
			: this(FieldLogPriority.Trace, text, null)
		{
		}

		public FieldLogTextItem(string text, string details)
			: this(FieldLogPriority.Trace, text, details)
		{
		}

		public FieldLogTextItem(FieldLogPriority priority, string text)
			: this(priority, text, null)
		{
		}

		public FieldLogTextItem(FieldLogPriority priority, string text, string details)
			: base(priority)
		{
			Text = text;
			Details = details;

			Size += (Text != null ? Text.Length * 2 : 0) +
				(Details != null ? Details.Length * 2 : 0);
		}

		public override string ToString()
		{
			return "FieldLogTextItem(EventCounter=" + EventCounter + ", Time=" + Time.ToString("yyyyMMdd'T'HHmmss") + ", Priority=" + Priority + ", Text=" + Text + ")";
		}

		internal override void Write(FieldLogFileWriter writer)
		{
			writer.SetItemType(FieldLogItemType.Text);
			base.Write(writer);
			writer.AddBuffer(Text);
			writer.AddBuffer(Details);
			writer.WriteBuffer();
		}

		internal static FieldLogTextItem Read(FieldLogFileReader reader)
		{
			FieldLogTextItem item = new FieldLogTextItem();
			item.ReadBaseData(reader);
			item.Text = reader.ReadString();
			item.Details = reader.ReadString();
			return item;
		}
	}

	public class FieldLogDataItem : FieldLogItem
	{
		/// <summary>Name of the data item.</summary>
		public string Name { get; private set; }
		/// <summary>Value of the data item.</summary>
		public string Value { get; private set; }

		// TODO: Add helper methods to serialise objects or other data types, e.g. to JSON

		private FieldLogDataItem()
		{
		}

		public FieldLogDataItem(string name, string value)
			: this(FieldLogPriority.Trace, name, value)
		{
		}

		public FieldLogDataItem(FieldLogPriority priority, string name, string value)
			: base(priority)
		{
			Name = name;
			Value = value;

			Size += (Name != null ? Name.Length * 2 : 0) +
				(Value != null ? Value.Length * 2 : 0);
		}

		public override string ToString()
		{
			return "FieldLogDataItem(EventCounter=" + EventCounter + ", Time=" + Time.ToString("yyyyMMdd'T'HHmmss") + ", Priority=" + Priority + ", Name=" + Name + ")";
		}

		internal override void Write(FieldLogFileWriter writer)
		{
			writer.SetItemType(FieldLogItemType.Data);
			base.Write(writer);
			writer.AddBuffer(Name);
			writer.AddBuffer(Value);
			writer.WriteBuffer();
		}

		internal static FieldLogDataItem Read(FieldLogFileReader reader)
		{
			FieldLogDataItem item = new FieldLogDataItem();
			item.ReadBaseData(reader);
			item.Name = reader.ReadString();
			item.Value = reader.ReadString();
			return item;
		}
	}

	public class FieldLogExceptionItem : FieldLogItem
	{
		public FieldLogException Exception { get; private set; }
		public string Context { get; private set; }
		public FieldLogEventEnvironment EnvironmentData { get; private set; }

		private FieldLogExceptionItem()
		{
		}

		public FieldLogExceptionItem(Exception ex)
			: this(FieldLogPriority.Critical, ex, null)
		{
		}

		public FieldLogExceptionItem(Exception ex, string context)
			: this(FieldLogPriority.Critical, ex, context)
		{
		}

		public FieldLogExceptionItem(FieldLogPriority priority, Exception ex)
			: this(priority, ex, null)
		{
		}

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

		public override string ToString()
		{
			return "FieldLogExceptionItem(EventCounter=" + EventCounter + ", Time=" + Time.ToString("yyyyMMdd'T'HHmmss") + ", Priority=" + Priority + ", Message=" + Exception.Message + ")";
		}

		internal override void Write(FieldLogFileWriter writer)
		{
			writer.SetItemType(FieldLogItemType.Exception);
			base.Write(writer);
			Exception.Write(writer);
			writer.AddBuffer(Context);
			EnvironmentData.Write(writer);
			writer.WriteBuffer();
		}

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

	public class FieldLogScopeItem : FieldLogItem
	{
		/// <summary>Type of the scope event.</summary>
		public FieldLogScopeType Type { get; private set; }
		/// <summary>New scope nesting level after the log item. The last scope item in a thread should be 0.</summary>
		public int Level { get; private set; }
		/// <summary>Scope name. Should be application-unique and hierarchical for easier analysis.</summary>
		public string Name { get; private set; }
		/// <summary>Indicates whether this is a background thread. (Only valid when entering a thread scope.)</summary>
		public bool IsBackgroundThread { get; private set; }
		/// <summary>Indicates whether this is a pool thread. (Only valid when entering a thread scope.)</summary>
		public bool IsPoolThread { get; private set; }
		/// <summary>Process static environment data. (Only valid when entering a process scope.)</summary>
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

		public FieldLogScopeItem(FieldLogScopeType type, string name)
			: this(FieldLogPriority.Trace, type, name)
		{
		}

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

		public override string ToString()
		{
			return "FieldLogScopeItem(EventCounter=" + EventCounter + ", Time=" + Time.ToString("yyyyMMdd'T'HHmmss") + ", Priority=" + Priority + ", Scope=" + Type + ")";
		}

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

	// TODO: Prepare time measurement by log items (not only temporarily in the log viewer) - with multiple intermediate times, with multiple timers

	#endregion Log item data structures

	#region Scope helpers

	public class FieldLogScope : IDisposable
	{
		private string name;

		public FieldLogScope(string name)
		{
			this.name = name;
			FL.Enter(name);
		}

		public void Dispose()
		{
			FL.Leave(name);
			GC.SuppressFinalize(this);
		}

		~FieldLogScope()
		{
			FL.Error("FieldLogScope.Dispose was not called! Scope levels before this item may be wrong.", "Name = " + name);
			Dispose();
		}
	}

	public class FieldLogThreadScope : IDisposable
	{
		private string name;

		public FieldLogThreadScope(string name)
		{
			this.name = name;
			FL.LogScope(FieldLogScopeType.ThreadStart, name);
		}

		public void Dispose()
		{
			FL.LogScope(FieldLogScopeType.ThreadEnd, name);
			GC.SuppressFinalize(this);
		}

		~FieldLogThreadScope()
		{
			FL.Error("FieldLogThreadScope.Dispose was not called! Scope levels before this item may be wrong.", "Name = " + name);
			Dispose();
		}
	}

	#endregion Scope helpers
}
