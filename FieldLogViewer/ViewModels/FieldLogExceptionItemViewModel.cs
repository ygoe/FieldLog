using System;
using System.Linq;
using System.Windows;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.ViewModels
{
	internal class FieldLogExceptionItemViewModel : FieldLogItemViewModel
	{
		public FieldLogExceptionItemViewModel(FieldLogExceptionItem item)
		{
			this.Item = item;
			base.Item = item;

			this.ExceptionVM = new FieldLogExceptionViewModel(item.Exception);
			this.EnvironmentVM = new FieldLogEnvironmentViewModel(item.EnvironmentData, this);
		}

		public new FieldLogExceptionItem Item { get; private set; }
		public FieldLogException Exception { get { return this.Item.Exception; } }

		public FieldLogExceptionViewModel ExceptionVM { get; private set; }
		public string Context { get { return this.Item.Context; } }
		public FieldLogEnvironmentViewModel EnvironmentVM { get; private set; }

		public string SimpleMessage
		{
			get
			{
				if (Context == FL.StackTraceOnlyExceptionContext ||
					Context == FL.StackTraceEnvOnlyExceptionContext)
				{
					return "Stack trace: " +
						(this.Exception.Message != null ? this.Exception.Message.Trim().Replace("\r", "").Replace("\n", "↲") : "");
				}

				return MakeSimpleExMessage(Exception);
			}
		}

		public string TypeImageSource { get { return "/Images/ExceptionItem_14.png"; } }

		public Visibility EnvVisibility
		{
			get
			{
				// There's no hard indicator to know whether environment data is available in an
				// exception item. We just check for some values that should always be set.
				return Item.EnvironmentData.ClrType != null ||
					Item.EnvironmentData.CurrentDirectory != null ||
					Item.EnvironmentData.ExecutablePath != null ?
					Visibility.Visible : Visibility.Collapsed;
			}
		}

		public override string ToString()
		{
			return GetType().Name + ": [" + PrioTitle + "] " + SimpleMessage;
		}

		public override void Refresh()
		{
			ExceptionVM.Refresh();
		}

		private string MakeSimpleExMessage(FieldLogException ex)
		{
			// Resolve the single inner exception of an AggregateException
			if (ex.Type == "System.AggregateException" &&
				ex.InnerExceptions != null &&
				ex.InnerExceptions.Length == 1)
			{
				return "AggEx: " + MakeSimpleExMessage(ex.InnerExceptions[0]);
			}

			// Remove namespace, abbreviate "Exception" to "Ex", make common names even shorter
			string exType = ex.Type;
			if (exType == "System.ArgumentException")
			{
				exType = "ArgEx";
			}
			else if (exType == "System.ArgumentOutOfRangeException")
			{
				exType = "ArgOutOfRangeEx";
			}
			else if (exType == "System.ApplicationException")
			{
				exType = "ApplEx";
			}
			else if (exType == "System.InvalidOperationException")
			{
				exType = "InvalidOpEx";
			}
			else if (exType == "System.IO.DirectoryNotFoundException")
			{
				exType = "DirNotFoundEx";
			}
			else if (exType == "System.NotImplementedException")
			{
				exType = "NotImplEx";
			}
			else if (exType == "System.NullReferenceException")
			{
				exType = "NullRefEx";
			}
			else if (exType == "System.OperationCanceledException")
			{
				exType = "OpCanceledEx";
			}
			else
			{
				int dotIndex = exType.LastIndexOf('.');
				if (dotIndex > -1)
					exType = exType.Substring(dotIndex + 1);
				if (exType.EndsWith("Exception", StringComparison.Ordinal))
					exType = exType.Substring(0, exType.Length - "Exception".Length + 2);
			}

			// Return shortened exception type and exception message in a single line
			return exType + ": " +
				(ex.Message != null ? ex.Message.Trim().Replace("\r", "").Replace("\n", "↲") : "");
		}
	}
}
