using System;
using System.Linq;
using System.Windows.Controls;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.Views
{
	public partial class FieldLogDataItemView : UserControl
	{
		#region Static constructor

		static FieldLogDataItemView()
		{
			ViewCommandManager.SetupMetadata<FieldLogDataItemView>();
		}

		#endregion Static constructor

		#region Constructors

		public FieldLogDataItemView()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region View commands

		#endregion View commands
	}
}
