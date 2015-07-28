using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Unclassified.Util
{
	// Based on: http://www.codeproject.com/Articles/17561/Programmatically-adding-attachments-to-emails-in-C

	public class MessageApi
	{
		#region Private data

		private List<MapiRecipDesc> recipients = new List<MapiRecipDesc>();
		private List<MapiFileDesc> attachments = new List<MapiFileDesc>();
		private MessageApiError lastError;

		#endregion Private data

		#region Public methods

		public void AddRecipientTo(string address, string name = null)
		{
			if (address == null) throw new ArgumentNullException("address");

			recipients.Add(new MapiRecipDesc
			{
				recipClass = RecipientClass.To,
				address = address,
				name = name ?? address
			});
		}

		public void AddRecipientCC(string address, string name = null)
		{
			if (address == null) throw new ArgumentNullException("address");

			recipients.Add(new MapiRecipDesc
			{
				recipClass = RecipientClass.CC,
				address = address,
				name = name ?? address
			});
		}

		public void AddRecipientBCC(string address, string name = null)
		{
			if (address == null) throw new ArgumentNullException("address");

			recipients.Add(new MapiRecipDesc
			{
				recipClass = RecipientClass.BCC,
				address = address,
				name = name ?? address
			});
		}

		public void AddAttachment(string path, string fileName = null)
		{
			if (path == null) throw new ArgumentNullException("path");

			attachments.Add(new MapiFileDesc
			{
				path = path,
				fileName = fileName ?? Path.GetFileName(path),
				position = -1
			});
		}

		public bool SendMailInteractive(string subject, string body)
		{
			return SendMail(subject, body, SendFlags.LogonUI | SendFlags.Dialog);
		}

		public bool SendMailDirect(string subject, string body)
		{
			return SendMail(subject, body, SendFlags.LogonUI);
		}

		public MessageApiError LastError
		{
			get
			{
				return lastError;
			}
		}

		#endregion Public methods

		#region Private methods

		private bool SendMail(string subject, string body, SendFlags flags)
		{
			MapiMessage message = new MapiMessage();
			message.subject = subject;
			message.noteText = body;
			message.recips = GetRecipients(out message.recipCount);
			message.files = GetAttachments(out message.fileCount);

			lastError = (MessageApiError)MAPISendMail(IntPtr.Zero, IntPtr.Zero, message, flags, 0);
			Cleanup(message);
			return lastError == MessageApiError.Success;
		}

		private IntPtr GetRecipients(out int recipCount)
		{
			recipCount = 0;
			if (recipients.Count == 0) return IntPtr.Zero;

			int size = Marshal.SizeOf(typeof(MapiRecipDesc));
			IntPtr intPtr = Marshal.AllocHGlobal(recipients.Count * size);
			long ptr = intPtr.ToInt64();
			foreach (MapiRecipDesc mapiRecip in recipients)
			{
				Marshal.StructureToPtr(mapiRecip, new IntPtr(ptr), false);
				ptr += size;
			}

			recipCount = recipients.Count;
			return intPtr;
		}

		private IntPtr GetAttachments(out int fileCount)
		{
			fileCount = 0;
			if (attachments.Count <= 0) return IntPtr.Zero;

			int size = Marshal.SizeOf(typeof(MapiFileDesc));
			IntPtr intPtr = Marshal.AllocHGlobal(attachments.Count * size);
			long ptr = intPtr.ToInt64();
			foreach (MapiFileDesc mapiFile in attachments)
			{
				Marshal.StructureToPtr(mapiFile, new IntPtr(ptr), false);
				ptr += size;
			}

			fileCount = attachments.Count;
			return intPtr;
		}

		private void Cleanup(MapiMessage msg)
		{
			int size = Marshal.SizeOf(typeof(MapiRecipDesc));
			long ptr = 0;

			if (msg.recips != IntPtr.Zero)
			{
				ptr = msg.recips.ToInt64();
				for (int i = 0; i < msg.recipCount; i++)
				{
					Marshal.DestroyStructure(new IntPtr(ptr), typeof(MapiRecipDesc));
					ptr += size;
				}
				Marshal.FreeHGlobal(msg.recips);
			}

			if (msg.files != IntPtr.Zero)
			{
				size = Marshal.SizeOf(typeof(MapiFileDesc));

				ptr = msg.files.ToInt64();
				for (int i = 0; i < msg.fileCount; i++)
				{
					Marshal.DestroyStructure(new IntPtr(ptr), typeof(MapiFileDesc));
					ptr += size;
				}
				Marshal.FreeHGlobal(msg.files);
			}

			recipients.Clear();
			attachments.Clear();
		}

		#endregion Private methods

		#region PInvoke

		[DllImport("MAPI32.DLL")]
		private static extern int MAPISendMail(IntPtr session, IntPtr hWnd, MapiMessage message, SendFlags flags, uint reserved);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		private class MapiMessage
		{
			public uint reserved;
			public string subject;
			public string noteText;
			public string messageType;
			public string dateReceived;
			public string conversationID;
			public int flags;
			public IntPtr originator;
			public int recipCount;
			public IntPtr recips;
			public int fileCount;
			public IntPtr files;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		private class MapiRecipDesc
		{
			public uint reserved;
			public RecipientClass recipClass;
			public string name;
			public string address;
			public uint eIDSize;
			public IntPtr entryID;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		private class MapiFileDesc
		{
			public uint reserved;
			public uint flags;
			public int position;
			public string path;
			public string fileName;
			public IntPtr fileType;
		}

		[Flags]
		private enum SendFlags
		{
			LogonUI = 0x1,
			Dialog = 0x8
		}

		private enum RecipientClass
		{
			OriginalSender = 0,
			To = 1,
			CC = 2,
			BCC = 3
		};

		public enum MessageApiError
		{
			Success = 0,
			UserAbort = 1,
			GeneralFailure = 2,
			LoginFailure = 3,
			DiskFull = 4,
			InsufficientMemory = 5,
			AccessDenied = 6,
			Unknown = 7,
			TooManySessions = 8,
			TooManyFiles = 9,
			TooManyRecipients = 10,
			AttachmentNotFound = 11,
			AttachmentOpenFailure = 12,
			AttachmentWriteFailure = 13,
			UnknownRecipient = 14,
			BadRecipientType = 15,
			NoMessages = 16,
			InvalidMessage = 17,
			TextTooLarge = 18,
			InvalidSession = 19,
			TypeNotSupported = 20,
			AmbiguousRecipient = 21,
			MessageInUse = 22,
			NetworkFailure = 23,
			InvalidEditFields = 24,
			InvalidRecipients = 25,
			NotSupported = 26,
			UnicodeNotSupported = 27
		}

		#endregion PInvoke
	}
}
