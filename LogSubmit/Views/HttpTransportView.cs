using System;
using System.Linq;
using System.Windows.Forms;
using Unclassified.LogSubmit.Transports;
using Unclassified.TxLib;

namespace Unclassified.LogSubmit.Views
{
	public partial class HttpTransportView : UserControl, IView
	{
		#region Private data

		private HttpTransport transport = new HttpTransport();

		#endregion Private data

		#region Constructors

		public HttpTransportView()
		{
			InitializeComponent();

			TxDictionaryBinding.AddTextBindings(this);

			Dock = DockStyle.Fill;

			if (!string.IsNullOrWhiteSpace(SharedData.Instance.HttpTransportUrl))
			{
				transport.Url = SharedData.Instance.HttpTransportUrl;
			}
			transport.Token = SharedData.Instance.HttpTransportToken;
		}

		#endregion Constructors

		#region Public properties

		internal HttpTransport Transport { get { return transport; } }

		#endregion Public properties

		#region Control event handlers

		#endregion Control event handlers

		#region Public methods

		public void Activate(bool forward)
		{
			UpdateButtons();
		}

		public void Deactivate(bool forward)
		{
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
