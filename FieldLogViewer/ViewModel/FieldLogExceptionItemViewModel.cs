using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unclassified.FieldLog;
using System.Windows;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class FieldLogExceptionItemViewModel : FieldLogItemViewModel
	{
		public FieldLogExceptionItemViewModel(FieldLogExceptionItem item)
		{
			this.Item = item;
			base.Item = item;

			this.ExceptionVM = new FieldLogExceptionViewModel(item.Exception);
			this.EnvironmentVM = new FieldLogEnvironmentViewModel(item.EnvironmentData, item.Time);
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
				string exType = this.Exception.Type;
				int dotIndex = exType.LastIndexOf('.');
				if (dotIndex > -1)
					exType = exType.Substring(dotIndex + 1);
				if (exType.EndsWith("Exception", StringComparison.InvariantCultureIgnoreCase) &&
					!exType.Equals("Exception", StringComparison.InvariantCultureIgnoreCase))
					exType = exType.Substring(0, exType.Length - "Exception".Length + 2);
				
				return exType + ": " +
					(this.Exception.Message != null ? this.Exception.Message.Trim().Replace("\r", "").Replace("\n", "↲") : "");
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
	}
}
