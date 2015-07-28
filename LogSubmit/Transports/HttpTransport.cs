using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Unclassified.TxLib;
using Unclassified.Util;

namespace Unclassified.LogSubmit.Transports
{
	internal class HttpTransport : TransportBase
	{
		public string Url { get; set; }
		public string Token { get; set; }

		public HttpTransport()
		{
			// Set default API URL
			Url = "https://unclassified.software/api/fieldlog/logsubmit";
		}

		public override bool CanExecute()
		{
			return !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(Token);
		}

		protected override void OnExecute(BackgroundWorker backgroundWorker)
		{
			// Prepare upload
			var client = new HttpClient();
			client.MultipartFormat = true;
			client.AddPostString("token", Token);
			using (var stream = File.OpenRead(SharedData.Instance.ArchiveFileName))
			{
				client.AddPostFile("logFile", Path.GetFileName(SharedData.Instance.ArchiveFileName), stream);
			}

			// Send file
			client.Request(new Uri(Url), backgroundWorker);

			// Check result
			if (client.LastHttpStatusCode != 200)
			{
				throw new Exception(Tx.T("msg.error.http response status", "code", client.LastHttpStatusCode.ToString(), "msg", client.LastResponseData));
			}
			var match = Regex.Match(client.LastResponseData, @"^OK (\S+) ([0-9]+) ([0-9a-f]+)");
			if (!match.Success)
			{
				throw new Exception(Tx.T("msg.error.http response pattern"));
			}
			if (match.Groups[1].Value != Token)
			{
				throw new Exception(Tx.T("msg.error.http response token"));
			}
			var fileInfo = new FileInfo(SharedData.Instance.ArchiveFileName);
			if (long.Parse(match.Groups[2].Value) != fileInfo.Length)
			{
				throw new Exception(Tx.T("msg.error.http response file size", "actual", match.Groups[2].Value, "expected", fileInfo.Length.ToString()));
			}
			string sha1 = GetFileSha1(SharedData.Instance.ArchiveFileName);
			if (!match.Groups[3].Value.Equals(sha1, StringComparison.OrdinalIgnoreCase))
			{
				throw new Exception(Tx.T("msg.error.http response file hash"));
			}
		}

		private static string GetFileSha1(string fileName)
		{
			using (var fileStream = File.OpenRead(fileName))
			using (var sha1 = new SHA1Managed())
			{
				byte[] hash = sha1.ComputeHash(fileStream);
				var formatted = new StringBuilder(2 * hash.Length);
				foreach (byte b in hash)
				{
					// From BitConverter.ToString and private GetHexValue
					int i = b / 16;
					formatted.Append((char)(i < 10 ? i + 48 : i - 10 + 97));
					i = b % 16;
					formatted.Append((char)(i < 10 ? i + 48 : i - 10 + 97));
				}
				return formatted.ToString();
			}
		}
	}
}
