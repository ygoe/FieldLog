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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Wraps an Exception instance for use in FieldLog logging.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class FieldLogException
	{
		/// <summary>Gets the approximated data size of this log item. Used for buffer size estimation.</summary>
		[Browsable(false)]
		public int Size { get; protected set; }

		/// <summary>Gets the exception type name.</summary>
		public string Type { get; private set; }
		/// <summary>Gets the module that defines the exception type.</summary>
		public string TypeModule { get; private set; }
		/// <summary>Gets the metadata token of the exception type.</summary>
		public int Token { get; private set; }
		/// <summary>Gets the exception message.</summary>
		public string Message { get; private set; }
		/// <summary>Gets the exception code.</summary>
		public int Code { get; private set; }
		/// <summary>Gets additional data provided by the exception.</summary>
		public string Data { get; private set; }
		/// <summary>Gets the stack frames of the exception.</summary>
		public FieldLogStackFrame[] StackFrames { get; private set; }
		/// <summary>Gets the inner exceptions of the exception.</summary>
		public FieldLogException[] InnerExceptions { get; private set; }

		/// <summary>Gets the original Exception instance.</summary>
		public Exception Exception { get; private set; }

		private FieldLogException()
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogException class.
		/// </summary>
		/// <param name="ex">The Exception instance.</param>
		public FieldLogException(Exception ex)
			: this(ex, null)
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogException class.
		/// </summary>
		/// <param name="ex">The Exception instance.</param>
		/// <param name="customStackTrace">A StackTrace that shall be logged instead of the StackTrace from the Exception instance.</param>
		public FieldLogException(Exception ex, StackTrace customStackTrace)
		{
			Exception = ex;
			Type exType = ex.GetType();

			Type = exType.FullName;
			TypeModule = exType.Module.FullyQualifiedName;
			Token = exType.MetadataToken;
			Message = ex.Message.TrimEnd();
			StackTrace stackTrace = customStackTrace;
			if (stackTrace == null)
			{
				stackTrace = new StackTrace(ex, true);
			}
			StackFrames = new FieldLogStackFrame[stackTrace.FrameCount];
			for (int i = 0; i < stackTrace.FrameCount; i++)
			{
				StackFrames[i] = new FieldLogStackFrame(stackTrace.GetFrame(i));
			}

			StringBuilder dataSb = new StringBuilder();
			if (ex.Data != null)
			{
				foreach (DictionaryEntry x in ex.Data)
				{
					dataSb.Append("Data[").Append(x.Key).Append("]");
					if (x.Value != null)
					{
						dataSb.Append(" (")
							.Append(x.Value.GetType().Name)
							.Append("): ")
							.Append(Convert.ToString(x.Value, CultureInfo.InvariantCulture));
					}
					else
					{
						dataSb.Append(": null");
					}
					dataSb.Append("\n");
				}
			}

			// Find more properties through reflection
			PropertyInfo[] props = exType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
					dataSb.Append(prop.Name);
					if (value != null)
					{
						dataSb.Append(" (").Append(value.GetType().Name).Append("): ").Append(Convert.ToString(value, CultureInfo.InvariantCulture));
						if (value is byte)
						{
							dataSb.Append(" (0x").Append(((byte)value).ToString("X2")).Append(")");
						}
						if (value is sbyte)
						{
							dataSb.Append(" (0x").Append(((sbyte)value).ToString("X2")).Append(")");
						}
						if (value is ushort)
						{
							dataSb.Append(" (0x").Append(((ushort)value).ToString("X4")).Append(")");
						}
						if (value is short)
						{
							dataSb.Append(" (0x").Append(((short)value).ToString("X")).Append(")");
						}
						if (value is uint)
						{
							dataSb.Append(" (0x").Append(((uint)value).ToString("X8")).Append(")");
						}
						if (value is int)
						{
							dataSb.Append(" (0x").Append(((int)value).ToString("X8")).Append(")");
						}
						if (value is ulong)
						{
							dataSb.Append(" (0x").Append(((ulong)value).ToString("X16")).Append(")");
						}
						if (value is long)
						{
							dataSb.Append(" (0x").Append(((long)value).ToString("X16")).Append(")");
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
					dataSb.Append("Exception property \"")
						.Append(prop.Name)
						.Append("\" cannot be retrieved. (")
						.Append(ex2.GetType().Name)
						.Append(": ")
						.Append(ex2.Message)
						.Append(")\n");
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
				4 +
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

		/// <summary>
		/// Returns the exception message.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Message;
		}

		/// <summary>
		/// Writes the exception fields to the log file writer.
		/// </summary>
		/// <param name="writer">The log file writer to write to.</param>
		internal void Write(FieldLogFileWriter writer)
		{
			writer.AddBuffer(Type);
			writer.AddBuffer(TypeModule);
			writer.AddBuffer(Token);
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

		/// <summary>
		/// Reads the exception fields from the specified log file reader.
		/// </summary>
		/// <param name="reader">The log file reader to read from.</param>
		/// <returns>The exception data.</returns>
		internal static FieldLogException Read(FieldLogFileReader reader)
		{
			FieldLogException ex = new FieldLogException();
			ex.Type = reader.ReadString();
			if (reader.FormatVersion >= 2)
			{
				ex.TypeModule = reader.ReadString();
				ex.Token = reader.ReadInt32();
			}
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

	/// <summary>
	/// Wraps a StackFrame instance for the FieldLogException class.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class FieldLogStackFrame
	{
		/// <summary>Gets the approximated data size of this log item. Used for buffer size estimation.</summary>
		[Browsable(false)]
		public int Size { get; protected set; }

		/// <summary>Gets the module name.</summary>
		public string Module { get; private set; }
		/// <summary>Gets the metadata token of the method.</summary>
		public int Token { get; private set; }
		/// <summary>Gets the IL code offset of the executed instruction in the method body.</summary>
		public int ILOffset { get; private set; }
		/// <summary>Gets the defining type name.</summary>
		public string TypeName { get; private set; }
		/// <summary>Gets the executed method name.</summary>
		public string MethodName { get; private set; }
		/// <summary>Gets the executed method parameters signature.</summary>
		public string MethodSignature { get; private set; }
		// TODO: Also include method parameter types (and names, if available). Check with Dotfuscator map file about the required format.
		/// <summary>Gets the source code file name.</summary>
		public string FileName { get; private set; }
		/// <summary>Gets the source code line number.</summary>
		public int Line { get; private set; }
		/// <summary>Gets the source code column number.</summary>
		public int Column { get; private set; }

		private FieldLogStackFrame()
		{
		}

		/// <summary>
		/// Initialises a new instance of the FieldLogStackFrame class.
		/// </summary>
		/// <param name="stackFrame">The StackFrame instance.</param>
		public FieldLogStackFrame(StackFrame stackFrame)
		{
			MethodBase method = stackFrame.GetMethod();

			if (method.DeclaringType != null)
			{
				Module = method.DeclaringType.Module.FullyQualifiedName;
				Token = method.MetadataToken;
				ILOffset = stackFrame.GetILOffset();
				TypeName = FormatTypeName(method.DeclaringType);
			}
			MethodName = method.Name;

			// TODO: Include 'extern' indicator from the following tests (needs new file format)
			//bool isPInvoke = (method.Attributes & MethodAttributes.PinvokeImpl) != 0;
			//bool isInternalCall = (method.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0;

			MethodInfo methodInfo = method as MethodInfo;
			StringBuilder sigSb = new StringBuilder();
			if (methodInfo != null)
			{
				sigSb.Append(FormatTypeName(methodInfo.ReturnType));
			}
			else
			{
				sigSb.Append("void");   // Dummy return value for constructors
			}
			ParameterInfo[] parameters = method.GetParameters();
			sigSb.Append("(");
			for (int i = 0; i < parameters.Length; i++)
			{
				if (i > 0)
				{
					sigSb.Append(", ");
				}
				sigSb.Append(FormatTypeName(parameters[i].ParameterType));
			}
			sigSb.Append(")");
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

		/// <summary>
		/// Returns the type and method name.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return TypeName + "." + MethodName;
		}

		private string FormatTypeName(Type t)
		{
			if (t.IsArray)
			{
				return FormatTypeName(t.GetElementType()) + "[]";
			}
			if (t.IsPointer)
			{
				return FormatTypeName(t.GetElementType()) + "*";
			}
			if (t.IsByRef)
			{
				return FormatTypeName(t.GetElementType()) + "&";
			}

			if (t == typeof(void)) return "void";

			if (t == typeof(bool)) return "bool";
			if (t == typeof(byte)) return "byte";
			if (t == typeof(sbyte)) return "sbyte";
			if (t == typeof(char)) return "char";
			if (t == typeof(decimal)) return "decimal";
			if (t == typeof(double)) return "double";
			if (t == typeof(float)) return "float";
			if (t == typeof(int)) return "int";
			if (t == typeof(uint)) return "uint";
			if (t == typeof(long)) return "long";
			if (t == typeof(ulong)) return "ulong";
			if (t == typeof(object)) return "object";
			if (t == typeof(short)) return "short";
			if (t == typeof(ushort)) return "ushort";
			if (t == typeof(string)) return "string";

			if (t == typeof(bool?)) return "bool?";
			if (t == typeof(byte?)) return "byte?";
			if (t == typeof(sbyte?)) return "sbyte?";
			if (t == typeof(char?)) return "char?";
			if (t == typeof(decimal?)) return "decimal?";
			if (t == typeof(double?)) return "double?";
			if (t == typeof(float?)) return "float?";
			if (t == typeof(int?)) return "int?";
			if (t == typeof(uint?)) return "uint?";
			if (t == typeof(long?)) return "long?";
			if (t == typeof(ulong?)) return "ulong?";
			if (t == typeof(short?)) return "short?";
			if (t == typeof(ushort?)) return "ushort?";

			StringBuilder sb = new StringBuilder();
			if (!t.IsGenericParameter)
			{
				if (!string.IsNullOrEmpty(t.Namespace))
				{
					sb.Append(t.Namespace);
					sb.Append(".");
				}
				if (t.IsNested)
				{
					// Collect all nesting parents starting from the inner-most type and add them in
					// reverse order (outer to inner) to the full type name
					Stack<string> nestedNames = new Stack<string>();
					Type parentType = t;
					do
					{
						nestedNames.Push(parentType.DeclaringType.Name);
						parentType = parentType.DeclaringType;
					}
					while (parentType.IsNested);
					while (nestedNames.Count > 0)
					{
						sb.Append(nestedNames.Pop());
						sb.Append("/");
					}
				}
				sb.Append(t.Name);
			}
			else
			{
				// Specify generic parameters by index, not by name
				if (t.DeclaringMethod != null)
					sb.Append("!!");
				else
					sb.Append("!");
				sb.Append(t.GenericParameterPosition);
			}
			if (t.IsGenericType)
			{
				sb.Append(FormatGenericTypeList(t.GetGenericArguments()));
			}
			return sb.ToString();
		}

		private string FormatGenericTypeList(Type[] types)
		{
			if (types == null || types.Length == 0) return "";

			StringBuilder sb = new StringBuilder();
			sb.Append("<");
			int i = 0;
			foreach (Type gt in types)
			{
				if (i++ > 0) sb.Append(", ");
				sb.Append(FormatTypeName(gt));
			}
			sb.Append(">");
			return sb.ToString();
		}

		/// <summary>
		/// Writes the stack frame fields to the log file writer.
		/// </summary>
		/// <param name="writer">The log file writer to write to.</param>
		internal void Write(FieldLogFileWriter writer)
		{
			writer.AddBuffer(Module);
			writer.AddBuffer(Token);
			writer.AddBuffer(ILOffset);
			writer.AddBuffer(TypeName);
			writer.AddBuffer(MethodName);
			writer.AddBuffer(MethodSignature);
			writer.AddBuffer(FileName);
			writer.AddBuffer(Line);
			writer.AddBuffer(Column);
		}

		/// <summary>
		/// Reads the stack frame fields from the specified log file reader.
		/// </summary>
		/// <param name="reader">The log file reader to read from.</param>
		/// <returns>The stack frame data.</returns>
		internal static FieldLogStackFrame Read(FieldLogFileReader reader)
		{
			FieldLogStackFrame frame = new FieldLogStackFrame();
			frame.Module = reader.ReadString();
			if (reader.FormatVersion >= 2)
			{
				frame.Token = reader.ReadInt32();
				frame.ILOffset = reader.ReadInt32();
			}
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
