using System;
using System.ComponentModel;
using System.Linq;
using Unclassified.TxLib;
using Unclassified.Util;

namespace Unclassified.LogSubmit.Transports
{
	internal class MailTransport : TransportBase
	{
		private const int olMailItem = 0;

		private dynamic outlookApp;
		private string body;

		public string RecipientAddress { get; set; }
		public string Subject { get; set; }

		public MailTransport()
		{
			Subject = "Log archive submitted by FieldLog";
		}

		public override bool CanExecute()
		{
			return !string.IsNullOrWhiteSpace(RecipientAddress);
		}

		protected override void OnExecute(BackgroundWorker backgroundWorker)
		{
			body = "This is a log archive submitted by the FieldLog submit tool.";

			MessageApi mapi = new MessageApi();
			mapi.AddRecipientTo(RecipientAddress);
			mapi.AddAttachment(SharedData.Instance.ArchiveFileName);

			bool result;
			if (SharedData.Instance.InteractiveEMail)
			{
				result = mapi.SendMailInteractive(Subject, body);
			}
			else
			{
				result = mapi.SendMailDirect(Subject, body);
			}
			if (backgroundWorker.CancellationPending)
			{
				return;
			}
			if (!result)
			{
				string x64Note = "";
				if (IntPtr.Size == 8)
				{
					x64Note = " " + Tx.T("msg.error.mapi x64");
				}
				string mapiError = "MAPI: " + mapi.LastError + "." + x64Note;
				if (mapi.LastError == MessageApi.MessageApiError.GeneralFailure &&
					IsOutlookAvailable())
				{
					try
					{
						SendWithOutlookDynamic();
					}
					catch (Exception ex)
					{
						throw new Exception(mapiError + " " + Tx.T("msg.error.outlook fallback failed") + " " + ex.Message);
					}
				}
				else
				{
					throw new Exception(mapiError);
				}
			}
		}

		private bool IsOutlookAvailable()
		{
			Type outlookAppType = Type.GetTypeFromProgID("Outlook.Application");
			if (outlookAppType == null)
				return false;
			outlookApp = Activator.CreateInstance(outlookAppType);
			return outlookApp != null;
		}

		private void SendWithOutlookDynamic()
		{
			var outlookMsg = outlookApp.CreateItem(olMailItem);
			outlookMsg.Recipients.Add(RecipientAddress);
			outlookMsg.Subject = Subject;
			outlookMsg.Body = body;
			outlookMsg.Attachments.Add(SharedData.Instance.ArchiveFileName);
			if (SharedData.Instance.InteractiveEMail)
			{
				outlookMsg.Display();
			}
			else
			{
				outlookMsg.Save();
				outlookMsg.Send();
			}
		}
	}
}
