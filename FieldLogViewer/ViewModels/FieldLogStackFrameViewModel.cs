using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.ViewModels
{
	internal class FieldLogStackFrameViewModel : ViewModelBase
	{
		public FieldLogStackFrameViewModel(FieldLogStackFrame stackFrame)
		{
			this.StackFrame = stackFrame;

			// Initially format source string
			Refresh();
		}

		public FieldLogStackFrame StackFrame { get; private set; }

		public string FullMethodName { get; private set; }
		public string FullSource { get; private set; }
		public string FullMeta { get; private set; }
		public SolidColorBrush ListBulletBrush { get; private set; }

		public Visibility MetaVisibility
		{
			get
			{
				return !string.IsNullOrEmpty(StackFrame.Module) && App.Settings.ShowStackFrameMetadata ? Visibility.Visible : Visibility.Collapsed;
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

			// Setting may have changed
			OnPropertyChanged("MetaVisibility");

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
				sb.Clear();
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
				sb.Clear();
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

			string originalName;
			string originalNameWithSignature;
			int originalToken;

			if (MainViewModel.Instance.Deobfuscator.IsLoaded &&
				MainViewModel.Instance.Deobfuscator.Deobfuscate(
				StackFrame.Module,
				StackFrame.TypeName,
				StackFrame.MethodName,
				StackFrame.MethodSignature,
				StackFrame.Token,
				out originalName,
				out originalNameWithSignature,
				out originalToken))
			{
				FullMethodName = originalNameWithSignature;

				if (!string.IsNullOrEmpty(StackFrame.Module) && StackFrame.Token != 0 && originalToken != 0)
				{
					// We already have module and token data, add the original token before obfuscation
					FullMeta += " <- @" + originalToken.ToString("x8");
				}

				ListBulletBrush = Brushes.OrangeRed;
			}
			else
			{
				Match match = Regex.Match(StackFrame.MethodSignature, @"^([^(]+)\(([^)]*)\)$");
				if (match.Success)
				{
					string returnType = match.Groups[1].Value;
					string parameters = match.Groups[2].Value;

					StringBuilder methodNameSb = new StringBuilder();
					//methodNameSb.Append(returnType);
					//methodNameSb.Append(" ");
					methodNameSb.Append(StackFrame.TypeName);
					methodNameSb.Append(".");
					methodNameSb.Append(StackFrame.MethodName);
					methodNameSb.Append("(");
					methodNameSb.Append(parameters);
					methodNameSb.Append(")");
					FullMethodName = methodNameSb.ToString();
				}
				else
				{
					FullMethodName = StackFrame.TypeName + "." + StackFrame.MethodName + "(" + StackFrame.MethodSignature + ")";
				}

				ListBulletBrush = Brushes.Black;
			}

			// Was changed one or two times in this method, notify only once
			OnPropertyChanged("FullMethodName", "ListBulletBrush", "FullMeta");
		}
	}
}
