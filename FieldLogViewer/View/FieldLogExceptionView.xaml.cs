using System;
using System.Linq;
using System.Windows.Controls;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.View
{
	public partial class FieldLogExceptionView : UserControl
	{
		#region Static constructor

		static FieldLogExceptionView()
		{
			ViewCommandManager.SetupMetadata<FieldLogExceptionView>();
		}

		#endregion Static constructor

		#region Constructors

		public FieldLogExceptionView()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region View commands

		#endregion View commands
	}
}
