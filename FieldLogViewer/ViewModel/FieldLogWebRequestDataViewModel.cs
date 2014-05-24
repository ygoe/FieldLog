using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class FieldLogWebRequestDataViewModel : ViewModelBase
	{
		public FieldLogWebRequestDataViewModel(FieldLogWebRequestData webRequestData, FieldLogItemViewModel itemVM)
		{
			this.WebRequestData = webRequestData;
			this.ItemVM = itemVM;
		}

		public FieldLogWebRequestData WebRequestData { get; private set; }
		public FieldLogItemViewModel ItemVM { get; private set; }
	}
}
