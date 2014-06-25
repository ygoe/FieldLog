using System;
using System.Linq;
using System.Windows.Controls;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.View
{
	public partial class FieldLogExceptionItemView : UserControl
	{
		#region Static constructor

		static FieldLogExceptionItemView()
		{
			ViewCommandManager.SetupMetadata<FieldLogExceptionItemView>();
		}

		#endregion Static constructor

		#region Constructors

		public FieldLogExceptionItemView()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region View commands

		#endregion View commands
	}
}
