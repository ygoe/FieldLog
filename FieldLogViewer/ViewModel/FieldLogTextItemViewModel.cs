using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class FieldLogTextItemViewModel : FieldLogItemViewModel
	{
		public FieldLogTextItemViewModel(FieldLogTextItem item)
		{
			this.Item = item;
			base.Item = item;
		}

		public new FieldLogTextItem Item { get; private set; }
		public string Text { get { return this.Item.Text; } }
		public string Details { get { return this.Item.Details; } }

		public string SimpleText
		{
			get
			{
				return this.Text == null ? "(null)" : this.Text.Trim().Replace("\r", "").Replace("\n", "↲");
			}
		}

		public string TypeImageSource { get { return "/Images/TextItem_14.png"; } }
	}
}
