using System;
using System.Linq;
using System.Windows.Controls;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.View
{
	public partial class FieldLogTextItemView : UserControl
	{
		#region Static constructor

		static FieldLogTextItemView()
		{
			ViewCommandManager.SetupMetadata<FieldLogTextItemView>();
		}

		#endregion Static constructor

		#region Constructors

		public FieldLogTextItemView()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region View commands

		#endregion View commands
	}
}
