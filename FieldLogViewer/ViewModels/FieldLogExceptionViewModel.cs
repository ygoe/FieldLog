using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Unclassified.FieldLog;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.FieldLogViewer.ViewModels
{
	internal class FieldLogExceptionViewModel : ViewModelBase
	{
		public FieldLogExceptionViewModel(FieldLogException exception)
		{
			this.Exception = exception;
			if (this.Exception.StackFrames != null)
			{
				this.StackFrameVMs = this.Exception.StackFrames.Select(sf => new FieldLogStackFrameViewModel(sf)).ToList();
				UpdateStackFrames();
			}
			if (this.Exception.InnerExceptions != null)
			{
				this.InnerExceptionVMs = this.Exception.InnerExceptions.Select(ie => new FieldLogExceptionViewModel(ie)).ToList();
			}
		}

		public FieldLogException Exception { get; private set; }

		public string Type { get { return this.Exception.Type; } }
		public string Message { get { return this.Exception.Message; } }
		public int Code { get { return this.Exception.Code; } }
		public string Data { get { return this.Exception.Data; } }
		public IEnumerable<FieldLogStackFrameViewModel> StackFrameVMs { get; private set; }
		public IEnumerable<FieldLogExceptionViewModel> InnerExceptionVMs { get; private set; }

		public string CodeStr
		{
			get
			{
				return Code + " (0x" + Code.ToString("X8") + ")";
			}
		}

		public Visibility InnerExceptionsVisibility
		{
			get
			{
				return this.Exception.InnerExceptions.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public void Refresh()
		{
			if (StackFrameVMs != null) StackFrameVMs.ForEach(vm => vm.Refresh());
			UpdateStackFrames();
			if (InnerExceptionVMs != null) InnerExceptionVMs.ForEach(vm => vm.Refresh());
		}

		private void UpdateStackFrames()
		{
			if (StackFrameVMs == null) return;

			bool haveSource = StackFrameVMs.Any(s => !string.IsNullOrEmpty(s.FullSource));

			foreach (var stackFrameVM in StackFrameVMs)
			{
				if (!haveSource || !string.IsNullOrEmpty(stackFrameVM.FullSource))
				{
					stackFrameVM.MethodBrush = Brushes.Black;
				}
				else
				{
					stackFrameVM.MethodBrush = Brushes.Gray;
				}
			}
		}
	}
}
