using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.ViewModels
{
	internal class FieldLogStackFrameViewModel : ViewModelBase
	{
		public FieldLogStackFrameViewModel(FieldLogStackFrame stackFrame)
		{
			this.StackFrame = stackFrame;

			FullMethodName = StackFrame.TypeName + "." + StackFrame.MethodName + "(" + StackFrame.MethodSignature + ")";

			StringBuilder sb = new StringBuilder();
			if (!string.IsNullOrEmpty(StackFrame.Module))
			{
				sb.Append("[").Append(Path.GetFileNameWithoutExtension(StackFrame.Module)).Append("]");
				if (StackFrame.Token != 0)
				{
					sb.Append(" @").Append(StackFrame.Token.ToString("x8"));
					if (StackFrame.ILOffset != System.Diagnostics.StackFrame.OFFSET_UNKNOWN)
					{
						sb.Append("+").Append(StackFrame.ILOffset.ToString("x"));
					}
				}
			}
			FullMeta = sb.ToString();

			// Initially format source string
			Refresh();
		}

		public FieldLogStackFrame StackFrame { get; private set; }

		public string FullMethodName { get; private set; }
		public string FullSource { get; private set; }
		public string FullMeta { get; private set; }

		public Visibility MetaVisibility
		{
			get
			{
				return /*!string.IsNullOrEmpty(StackFrame.Module) ? Visibility.Visible :*/ Visibility.Collapsed;
			}
		}

		public Visibility SourceVisibility
		{
			get
			{
				return !string.IsNullOrEmpty(FullSource) ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public override string ToString()
		{
			return GetType().Name + ": " + FullMethodName;
		}

		public void Refresh()
		{
			string fileName;
			int startLine, startColumn, endLine, endColumn;

			if (MainViewModel.Instance.SourceResolver.Resolve(
				StackFrame.Module,
				StackFrame.Token,
				StackFrame.ILOffset,
				out fileName,
				out startLine,
				out startColumn,
				out endLine,
				out endColumn))
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(fileName);
				if (startLine != 0)
				{
					sb.Append(": ").Append(startLine);
					sb.Append(", ").Append(startColumn);
					sb.Append(" - ").Append(endLine);
					sb.Append(", ").Append(endColumn);
				}
				FullSource = sb.ToString();
				OnPropertyChanged("FullSource", "SourceVisibility");
			}
			else
			{
				StringBuilder sb = new StringBuilder();
				if (!string.IsNullOrEmpty(StackFrame.FileName))
				{
					sb.Append(StackFrame.FileName);
					if (StackFrame.Line != 0)
					{
						sb.Append(": ").Append(StackFrame.Line);
						if (StackFrame.Column != 0)
						{
							sb.Append(", ").Append(StackFrame.Column);
						}
					}
				}
				FullSource = sb.ToString();
				OnPropertyChanged("FullSource", "SourceVisibility");
			}
		}
	}
}
