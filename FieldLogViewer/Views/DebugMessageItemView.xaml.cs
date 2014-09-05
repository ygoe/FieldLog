using System;
using System.Linq;
using System.Windows.Controls;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.Views
{
	public partial class DebugMessageItemView : UserControl
	{
		#region Static constructor

		static DebugMessageItemView()
		{
			ViewCommandManager.SetupMetadata<DebugMessageItemView>();
		}

		#endregion Static constructor

		#region Constructors

		public DebugMessageItemView()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region View commands

		#endregion View commands
	}
}
