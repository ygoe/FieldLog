using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
