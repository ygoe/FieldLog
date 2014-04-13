using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class FieldLogDataItemViewModel : FieldLogItemViewModel
	{
		public FieldLogDataItemViewModel(FieldLogDataItem item)
		{
			this.Item = item;
			base.Item = item;
		}

		public new FieldLogDataItem Item { get; private set; }
		public string Name { get { return Item.Name; } }
		public string Value { get { return Item.Value; } }

		public string SimpleName
		{
			get
			{
				if (this.Value != null && (this.Value.StartsWith("{\n") || this.Value.StartsWith("{\r\n")))
				{
					// This is a formatted structured value. Use different line break formatting here.
					return
						(this.Name == null ? "(null)" : this.Name.Trim().Replace("\r", "").Replace("\n", "↲")) +
						" = " +
						string.Join(" ", this.Value.Replace("\r", "").Split('\n').Select(a => a.Trim()));
				}
				
				return
					(this.Name == null ? "(null)" : this.Name.Trim().Replace("\r", "").Replace("\n", "↲")) +
					" = " +
					(this.Value == null ? "(null)" : this.Value.Trim().Replace("\r", "").Replace("\n", "↲"));
			}
		}

		public string TypeImageSource { get { return "/Images/DataItem_14.png"; } }
	}
}
