using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;

namespace Unclassified.FieldLog
{
	public class FieldLogException
	{
		/// <summary>Approximate data size of this log item. Used for buffer size estimation.</summary>
		public int Size { get; protected set; }

		public string Type { get; private set; }
		public string Message { get; private set; }
		public int Code { get; private set; }
		public string Data { get; private set; }
		public FieldLogStackFrame[] StackFrames { get; private set; }
		public FieldLogException[] InnerExceptions { get; private set; }

		public Exception Exception { get; private set; }

		private FieldLogException()
		{
		}

		public FieldLogException(Exception ex)
		{
			Exception = ex;
			
			Type = ex.GetType().FullName;
			Message = ex.Message.TrimEnd();
			StackTrace stackTrace = new StackTrace(ex, true);
			StackFrames = new FieldLogStackFrame[stackTrace.FrameCount];
			for (int i = 0; i < stackTrace.FrameCount; i++)
			{
				StackFrames[i] = new FieldLogStackFrame(stackTrace.GetFrame(i));
			}

			StringBuilder dataSb = new StringBuilder();
			if (ex.Data != null)
				foreach (DictionaryEntry x in ex.Data)
					dataSb.Append("Data." + x.Key + (x.Value != null ? " (" + x.Value.GetType().Name + "): " + x.Value.ToString() : ": null") + "\n");

			// Find more properties through reflection
			PropertyInfo[] props = ex.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (PropertyInfo prop in props)
			{
				// Known properties, already handled
				if (prop.Name == "Message") continue;
				if (prop.Name == "StackTrace") continue;
				if (prop.Name == "ErrorCode") continue;
				if (prop.Name == "Data") continue;
				if (prop.Name == "InnerException") continue;
				if (prop.Name == "InnerExceptions") continue;
				if (prop.Name == "TargetSite") continue;
				if (prop.Name == "HelpLink") continue;
				if (prop.Name == "Source") continue;

				try
				{
					object value = prop.GetValue(ex, null);   // Indexed properties are not supported here!
					dataSb.Append("Property." + prop.Name);
					if (value != null)
					{
						dataSb.Append(" (" + value.GetType().Name + "): " + Convert.ToString(value, CultureInfo.InvariantCulture));
						if (value is byte)
						{
							dataSb.Append(" (0x" + ((byte) value).ToString("X2") + ")");
						}
						if (value is sbyte)
						{
							dataSb.Append(" (0x" + ((sbyte) value).ToString("X2") + ")");
						}
						if (value is ushort)
						{
							dataSb.Append(" (0x" + ((ushort) value).ToString("X4") + ")");
						}
						if (value is short)
						{
							dataSb.Append(" (0x" + ((short) value).ToString("X") + ")");
						}
						if (value is uint)
						{
							dataSb.Append(" (0x" + ((uint) value).ToString("X8") + ")");
						}
						if (value is int)
						{
							dataSb.Append(" (0x" + ((int) value).ToString("X8") + ")");
						}
						if (value is ulong)
						{
							dataSb.Append(" (0x" + ((ulong) value).ToString("X16") + ")");
						}
						if (value is long)
						{
							dataSb.Append(" (0x" + ((long) value).ToString("X16") + ")");
						}
					}
					else
					{
						dataSb.Append(": null");
					}
					dataSb.Append("\n");
				}
				catch (Exception ex2)
				{
					dataSb.Append("Exception property \"" + prop.Name + "\" cannot be retrieved. (" + ex2.GetType().Name + ": " + ex2.Message + ")\n");
				}
			}
			Data = dataSb.ToString().TrimEnd();

#if !NET20
			AggregateException aex = ex as AggregateException;
			if (aex != null)
			{
				InnerExceptions = new FieldLogException[aex.InnerExceptions.Count];
				for (int i = 0; i < aex.InnerExceptions.Count; i++)
				{
					InnerExceptions[i] = new FieldLogException(aex.InnerExceptions[i]);
				}
			}
			else
#endif
			if (ex.InnerException != null)
			{
				InnerExceptions = new FieldLogException[1];
				InnerExceptions[0] = new FieldLogException(ex.InnerException);
			}

			ExternalException eex = ex as ExternalException;   // e.g. COMException
			if (eex != null)
			{
				Code = eex.ErrorCode;
			}

			Size = (Type != null ? Type.Length * 2 : 0) +
				(Message != null ? Message.Length * 2 : 0) +
				4 +
				(Data != null ? Data.Length * 2 : 0);
			foreach (FieldLogStackFrame sf in StackFrames)
			{
				Size += sf.Size;
			}
			if (InnerExceptions != null)
			{
				foreach (FieldLogException ex2 in InnerExceptions)
				{
					Size += ex2.Size;
				}
			}
		}

		internal void Write(FieldLogFileWriter writer)
		{
			writer.AddBuffer(Type);
			writer.AddBuffer(Message);
			writer.AddBuffer(Code);
			writer.AddBuffer(Data);
			writer.AddBuffer(StackFrames.Length);
			foreach (FieldLogStackFrame sf in StackFrames)
			{
				sf.Write(writer);
			}
			if (InnerExceptions != null)
			{
				writer.AddBuffer(InnerExceptions.Length);
				foreach (FieldLogException ex in InnerExceptions)
				{
					ex.Write(writer);
				}
			}
			else
			{
				writer.AddBuffer(0);
			}
		}

		internal static FieldLogException Read(FieldLogFileReader reader)
		{
			FieldLogException ex = new FieldLogException();
			ex.Type = reader.ReadString();
			ex.Message = reader.ReadString();
			ex.Code = reader.ReadInt32();
			ex.Data = reader.ReadString();
			int frameCount = reader.ReadInt32();
			ex.StackFrames = new FieldLogStackFrame[frameCount];
			for (int i = 0; i < frameCount; i++)
			{
				ex.StackFrames[i] = FieldLogStackFrame.Read(reader);
			}
			int innerCount = reader.ReadInt32();
			ex.InnerExceptions = new FieldLogException[innerCount];
			for (int i = 0; i < innerCount; i++)
			{
				ex.InnerExceptions[i] = FieldLogException.Read(reader);
			}
			return ex;
		}
	}

	public class FieldLogStackFrame
	{
		/// <summary>Approximate data size of this log item. Used for buffer size estimation.</summary>
		public int Size { get; protected set; }

		public string Module { get; private set; }
		public string TypeName { get; private set; }
		public string MethodName { get; private set; }
		public string MethodSignature { get; private set; }
		// TODO: Also include method parameter types (and names, if available). Check with Dotfuscator map file about the required format.
		public string FileName { get; private set; }
		public int Line { get; private set; }
		public int Column { get; private set; }

		private FieldLogStackFrame()
		{
		}

		public FieldLogStackFrame(StackFrame stackFrame)
		{
			Module = stackFrame.GetMethod().DeclaringType.Module.FullyQualifiedName;
			TypeName = stackFrame.GetMethod().DeclaringType.FullName;
			MethodName = stackFrame.GetMethod().Name;
			
			StringBuilder sigSb = new StringBuilder();
			ParameterInfo[] parameters = stackFrame.GetMethod().GetParameters();
			for (int i = 0; i < parameters.Length; i++)
			{
				if (i > 0)
				{
					sigSb.Append(", ");
				}
				sigSb.Append(parameters[i].ParameterType.FullName);
			}
			MethodSignature = sigSb.ToString();

			FileName = stackFrame.GetFileName();
			Line = stackFrame.GetFileLineNumber();
			Column = stackFrame.GetFileColumnNumber();

			Size = (Module != null ? Module.Length * 2 : 0) +
				(TypeName != null ? TypeName.Length * 2 : 0) +
				(MethodName != null ? MethodName.Length * 2 : 0) +
				(MethodSignature != null ? MethodSignature.Length * 2 : 0) +
				(FileName != null ? FileName.Length * 2 : 0) +
				4 + 4;
		}

		internal void Write(FieldLogFileWriter writer)
		{
			writer.AddBuffer(Module);
			writer.AddBuffer(TypeName);
			writer.AddBuffer(MethodName);
			writer.AddBuffer(MethodSignature);
			writer.AddBuffer(FileName);
			writer.AddBuffer(Line);
			writer.AddBuffer(Column);
		}

		internal static FieldLogStackFrame Read(FieldLogFileReader reader)
		{
			FieldLogStackFrame frame = new FieldLogStackFrame();
			frame.Module = reader.ReadString();
			frame.TypeName = reader.ReadString();
			frame.MethodName = reader.ReadString();
			frame.MethodSignature = reader.ReadString();
			frame.FileName = reader.ReadString();
			frame.Line = reader.ReadInt32();
			frame.Column = reader.ReadInt32();
			return frame;
		}
	}
}
