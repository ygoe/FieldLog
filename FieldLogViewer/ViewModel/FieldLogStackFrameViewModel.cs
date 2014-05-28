using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unclassified.FieldLog;
using System.Windows;
using System.IO;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class FieldLogStackFrameViewModel : ViewModelBase
	{
		public FieldLogStackFrameViewModel(FieldLogStackFrame stackFrame)
		{
			this.StackFrame = stackFrame;
		}

		public FieldLogStackFrame StackFrame { get; private set; }

		public string Module { get { return this.StackFrame.Module; } }
		public string TypeName { get { return this.StackFrame.TypeName; } }
		public string MethodName { get { return this.StackFrame.MethodName; } }
		public string MethodSignature { get { return this.StackFrame.MethodSignature; } }
		public string FileName { get { return this.StackFrame.FileName; } }
		public int Line { get { return this.StackFrame.Line; } }
		public int Column { get { return this.StackFrame.Column; } }

		public string FullMethodName
		{
			get
			{
				return this.TypeName + "." + this.MethodName + "(" + this.MethodSignature + ")";
			}
		}

		public string FullMeta
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				if (!string.IsNullOrEmpty(Module))
				{
					sb.Append("[").Append(Path.GetFileNameWithoutExtension(Module)).Append("]");
					if (StackFrame.Token != 0)
					{
						sb.Append(" @").Append(StackFrame.Token.ToString("x8"));
						if (StackFrame.ILOffset != System.Diagnostics.StackFrame.OFFSET_UNKNOWN)
						{
							sb.Append("+").Append(StackFrame.ILOffset.ToString("x"));
						}
					}
				}
				return sb.ToString();
			}
		}

		public Visibility MetaVisibility
		{
			get
			{
				return !string.IsNullOrEmpty(Module) ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public string FullSource
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				if (!string.IsNullOrEmpty(FileName))
				{
					sb.Append(FileName);
					if (Line != 0)
					{
						sb.Append(":").Append(Line);
						if (Column != 0)
						{
							sb.Append(",").Append(Column);
						}
					}
				}
				return sb.ToString();
			}
		}

		public Visibility SourceVisibility
		{
			get
			{
				return !string.IsNullOrEmpty(FileName) ? Visibility.Visible : Visibility.Collapsed;
			}
		}
	}
}
