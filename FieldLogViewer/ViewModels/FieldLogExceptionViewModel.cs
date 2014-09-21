﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Unclassified.FieldLog;
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
			if (InnerExceptionVMs != null) InnerExceptionVMs.ForEach(vm => vm.Refresh());
		}
	}
}