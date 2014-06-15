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
		public string Details
		{
			get
			{
				// Don't show internal data in the GUI
				if (this.Item.Details != null && this.Item.Details.StartsWith("\u0001"))
					return null;
				return this.Item.Details;
			}
		}

		public string SimpleText
		{
			get
			{
				return this.Text == null ? "(null)" : this.Text.Trim().Replace("\r", "").Replace("\n", "↲");
			}
		}

		public string TypeImageSource { get { return "/Images/TextItem_14.png"; } }

		public override string ToString()
		{
			return GetType().Name + ": [" + PrioTitle + "] " + SimpleText;
		}

		/// <summary>
		/// Gets a new UtcOffset value from the log item.
		/// </summary>
		/// <param name="utcOffset">The new UtcOffset value from this log item instance.</param>
		/// <returns>true if a new UtcOffset value was set; otherwise, false.</returns>
		public override bool TryGetUtcOffsetData(out int utcOffset)
		{
			if (Item.Details != null && Item.Details.StartsWith("\u0001UtcOffset="))
			{
				return int.TryParse(Item.Details.Substring(11), out utcOffset);
			}
			utcOffset = 0;
			return false;
		}
	}
}
