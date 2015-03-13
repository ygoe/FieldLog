using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unclassified.LogSubmit.Transports;

namespace Unclassified.LogSubmit.Views
{
	public partial class MailTransportView : UserControl, IView
	{
		#region Private data

		private MailTransport transport = new MailTransport();

		#endregion Private data

		#region Constructors

		public MailTransportView()
		{
			InitializeComponent();

			Dock = DockStyle.Fill;

			transport.RecipientAddress = SharedData.Instance.MailTransportRecipientAddress;
		}

		#endregion Constructors

		#region Public properties

		internal MailTransport Transport { get { return transport; } }

		#endregion Public properties

		#region Control event handlers

		private void InteractiveCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			DirectInfoLabel.Visible = !InteractiveCheckBox.Checked;
		}

		#endregion Control event handlers

		#region Public methods

		public void Activate(bool forward)
		{
			long maxMB = 19;
			maxMB *= 1024 * 1024;   // Convert to MB
			maxMB = maxMB * 3 / 4;   // Adjust for 1/3 base64 overhead
			SizeWarningLabel.Visible = SharedData.Instance.ArchiveFileSize > maxMB;

			UpdateButtons();
		}

		public void Deactivate(bool forward)
		{
			SharedData.Instance.InteractiveEMail = InteractiveCheckBox.Checked;
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
