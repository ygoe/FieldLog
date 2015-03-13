using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Unclassified.Util;

namespace Unclassified.LogSubmit.Transports
{
	internal class MailTransport : TransportBase
	{
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
			string body = "This is a log archive submitted by the FieldLog submit tool.";

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
					x64Note = " Maybe your default e-mail application does not support 64 bit MAPI.";
				}
				throw new Exception("MAPI: " + mapi.LastError + "." + x64Note);
			}
		}
	}
}
