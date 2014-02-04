using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unclassified.FieldLog;
using System.Windows;

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

		public string FullSource
		{
			get
			{
				return this.FileName + ":" + this.Line + "," + this.Column;
			}
		}

		public Visibility SourceVisibility
		{
			get
			{
				return !string.IsNullOrEmpty(this.FileName) ? Visibility.Visible : Visibility.Collapsed;
			}
		}
	}
}
