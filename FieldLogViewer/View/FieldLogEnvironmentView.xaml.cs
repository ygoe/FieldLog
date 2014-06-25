using System;
using System.Linq;
using System.Windows.Controls;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.View
{
	public partial class FieldLogEnvironmentView : UserControl
	{
		#region Static constructor

		static FieldLogEnvironmentView()
		{
			ViewCommandManager.SetupMetadata<FieldLogEnvironmentView>();
		}

		#endregion Static constructor

		#region Constructors

		public FieldLogEnvironmentView()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region View commands

		#endregion View commands
	}
}
