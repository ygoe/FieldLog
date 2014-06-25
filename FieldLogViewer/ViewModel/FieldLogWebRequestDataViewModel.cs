using System;
using System.Linq;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.ViewModel
{
	internal class FieldLogWebRequestDataViewModel : ViewModelBase
	{
		public FieldLogWebRequestDataViewModel(FieldLogWebRequestData webRequestData, FieldLogItemViewModel itemVM)
		{
			this.WebRequestData = webRequestData;
			this.ItemVM = itemVM;
		}

		public FieldLogWebRequestData WebRequestData { get; private set; }
		public FieldLogItemViewModel ItemVM { get; private set; }

		public TimeSpan RequestDuration { get; set; }
	}
}
