using System;
using System.Linq;
using System.Windows.Controls;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.Views
{
	public partial class FieldLogWebRequestDataView : UserControl
	{
		#region Static constructor

		static FieldLogWebRequestDataView()
		{
			ViewCommandManager.SetupMetadata<FieldLogWebRequestDataView>();
		}

		#endregion Static constructor

		#region Constructors

		public FieldLogWebRequestDataView()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region View commands

		#endregion View commands
	}
}
