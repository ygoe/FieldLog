using System;
using System.Linq;
using System.Windows.Controls;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.View
{
	public partial class FieldLogScopeItemView : UserControl
	{
		#region Static constructor

		static FieldLogScopeItemView()
		{
			ViewCommandManager.SetupMetadata<FieldLogScopeItemView>();
		}

		#endregion Static constructor

		#region Constructors

		public FieldLogScopeItemView()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region View commands

		#endregion View commands
	}
}
