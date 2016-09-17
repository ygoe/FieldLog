using System;
using System.Linq;
using System.Text.RegularExpressions;
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

		#region Control event handlers

		private void EMailTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs args)
		{
			string email = EMailTextBox.Text.Trim();
			if (email != "" && !IsValidEMailAddress(email))
			{
				args.Cancel = true;
				MessageBox.Show(
					FindForm(),
					Tx.T("msg.invalid e-mail address"),
					Tx.T("msg.title.error"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
		}

		#endregion Control event handlers

		#region Public methods

		public void Activate(bool forward)
		{
			UpdateButtons();
		}

		public void Deactivate(bool forward)
		{
			SharedData.Instance.Notes = NotesTextBox.Text;
			SharedData.Instance.EMailAddress = EMailTextBox.Text.Trim();
		}

		#endregion Public methods

		#region Private methods

		private void UpdateButtons()
		{
			MainForm.Instance.BackEnabled = true;
			MainForm.Instance.NextEnabled = true;
		}

		private bool IsValidEMailAddress(string email)
		{
			// Simplified syntax rules, see https://en.wikipedia.org/wiki/Email_address#Syntax
			string pattern = @"^[a-z0-9!#$%&'*+\-/=?^_`{|}~.]+@[a-z0-9\-.]+\.[a-z]{2,}$";
			return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		}

		#endregion Private methods
	}
}
