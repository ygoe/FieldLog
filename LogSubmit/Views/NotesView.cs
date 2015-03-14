using System;
using System.Linq;
using System.Windows.Forms;
using Unclassified.TxLib;

namespace Unclassified.LogSubmit.Views
{
	public partial class NotesView : UserControl, IView
	{
		#region Constructors

		public NotesView()
		{
			InitializeComponent();

			TxDictionaryBinding.AddTextBindings(this);

			Dock = DockStyle.Fill;
		}

		#endregion Constructors

		#region Public methods

		public void Activate(bool forward)
		{
			UpdateButtons();
		}

		public void Deactivate(bool forward)
		{
			SharedData.Instance.Notes = NotesTextBox.Text;
			SharedData.Instance.EMailAddress = EMailTextBox.Text;
		}

		#endregion Public methods

		#region Private methods

		private void UpdateButtons()
		{
			MainForm.Instance.BackEnabled = true;
			MainForm.Instance.NextEnabled = true;
		}

		#endregion Private methods
	}
}
