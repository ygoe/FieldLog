using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class FieldLogExceptionItemViewModel : FieldLogItemViewModel
	{
		public FieldLogExceptionItemViewModel(FieldLogExceptionItem item)
		{
			this.Item = item;
			base.Item = item;

			this.ExceptionVM = new FieldLogExceptionViewModel(item.Exception);
		}

		public new FieldLogExceptionItem Item { get; private set; }
		public FieldLogException Exception { get { return this.Item.Exception; } }

		public FieldLogExceptionViewModel ExceptionVM { get; private set; }
		public string Context { get { return this.Item.Context; } }
		public FieldLogEventEnvironment EnvironmentData { get { return this.Item.EnvironmentData; } }

		public string SimpleMessage
		{
			get
			{
				return this.Exception.Message == null ? "(null)" : this.Exception.Message.Trim().Replace("\r", "").Replace("\n", "↲");
			}
		}

		public string TypeImageSource { get { return "/Images/ExceptionItem_14.png"; } }
	}
}
