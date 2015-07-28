using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Unclassified.Util
{
	/// <summary>
	/// An HTTP client that can upload form data and files.
	/// </summary>
	public class HttpClient
	{
		#region Private data

		private Uri lastRequestedUri;
		private int lastHttpStatusCode;
		private string lastRedirectUrlString;
		private Uri lastRedirectUri;
		private string lastResponseData;
		private CookieContainer cookies = new CookieContainer();
		private bool multipartFormat;
		private string multipartBoundary;
		private StringBuilder postString;
		private MemoryStream postStream;

		#endregion Private data

		#region Properties

		/// <summary>
		/// The last requested URI.
		/// </summary>
		public Uri LastRequestedUri { get { return lastRequestedUri; } }

		/// <summary>
		/// The status code of the last request.
		/// </summary>
		public int LastHttpStatusCode { get { return lastHttpStatusCode; } }

		/// <summary>
		/// The URI string of the last redirection.
		/// </summary>
		public string LastRedirectUrlString { get { return lastRedirectUrlString; } }

		/// <summary>
		/// The fully qualified URI of the last redirection.
		/// </summary>
		public Uri LastRedirectUri { get { return lastRedirectUri; } }

		/// <summary>
		/// The contents of the last response.
		/// </summary>
		public string LastResponseData { get { return lastResponseData; } }

		/// <summary>
		/// Gets or sets a value indicating whether the next HTTP POST request contains multipart form data.
		/// </summary>
		public bool MultipartFormat
		{
			get
			{
				return multipartFormat;
			}
			set
			{
				if (postString != null || postStream != null)
				{
					throw new InvalidOperationException("The MultipartFormat property cannot be set when data has already been added.");
				}
				multipartFormat = value;
			}
		}

		#endregion Properties

		#region HTTP operations

		/// <summary>
		/// Adds a string value to the data to be sent with the next POST request.
		/// </summary>
		/// <param name="name">The field name.</param>
		/// <param name="value">The field value.</param>
		public void AddPostString(string name, string value)
		{
			if (name.Contains("\"") || name.Contains("&") || name.Contains("="))
				throw new FormatException("String name contains invalid characters.");

			if (multipartFormat)
			{
				if (postStream == null)
					postStream = new MemoryStream();
				if (multipartBoundary == null)
					multipartBoundary = "---------------------" + DateTime.UtcNow.Ticks.ToString("x");

				string s = "--" + multipartBoundary + "\r\n" +
					"Content-Disposition: form-data; name=\"" + name + "\"\r\n\r\n" +
					value + "\r\n";
				byte[] b = Encoding.UTF8.GetBytes(s);
				postStream.Write(b, 0, b.Length);
			}
			else
			{
				if (postString == null)
					postString = new StringBuilder();

				if (postString.Length > 0)
					postString.Append("&");
				postString.Append(name);
				postString.Append("=");
				postString.Append(Uri.EscapeUriString(value));
			}
		}

		/// <summary>
		/// Adds a file to the data to be sent with the next POST request.
		/// </summary>
		/// <param name="name">The field name.</param>
		/// <param name="fileName">The declared file name.</param>
		/// <param name="fileStream">The stream to read the file contents from.</param>
		public void AddPostFile(string name, string fileName, Stream fileStream)
		{
			if (name.Contains("\"") || name.Contains("&") || name.Contains("="))
				throw new ArgumentException("String name contains invalid characters.", "name");
			if (!multipartFormat)
				throw new InvalidOperationException("Files can only be added using the multipart format.");

			if (postStream == null)
				postStream = new MemoryStream();
			if (multipartBoundary == null)
				multipartBoundary = "---------------------" + DateTime.UtcNow.Ticks.ToString("x");

			string s = "--" + multipartBoundary + "\r\n" +
				"Content-Disposition: form-data; name=\"" + name + "\"; filename=\"" + fileName + "\"\r\n" +
				"Content-Type: application/octet-stream\r\n\r\n";
			byte[] b = Encoding.UTF8.GetBytes(s);
			postStream.Write(b, 0, b.Length);

			byte[] buffer = new byte[4096];
			int bytesRead;
			while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
			{
				postStream.Write(buffer, 0, bytesRead);
			}

			s = "\r\n";
			b = Encoding.UTF8.GetBytes(s);
			postStream.Write(b, 0, b.Length);
		}

		/// <summary>
		/// Sends an HTTP request and basically processes its response.
		/// </summary>
		/// <param name="uri">The URI to fetch.</param>
		/// <remarks>
		/// If POST data has been added before this request, the POST method will be used,
		/// otherwise the GET method will be used. All added POST data will be reset after this
		/// request.
		/// </remarks>
		public void Request(Uri uri, BackgroundWorker backgroundWorker)
		{
			lastRequestedUri = uri;

			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
			if (backgroundWorker != null)
			{
				request.AllowWriteStreamBuffering = false;
				request.SendChunked = true;
			}
			request.AllowAutoRedirect = false;
			request.CookieContainer = cookies;

			if (postString != null || postStream != null)
			{
				request.Method = "POST";
				if (multipartFormat)
				{
					request.ContentType = "multipart/form-data; boundary=" + multipartBoundary;

					using (var requestStream = request.GetRequestStream())
					{
						if (backgroundWorker != null)
						{
							backgroundWorker.ReportProgress(0);
							postStream.Seek(0, SeekOrigin.Begin);
							byte[] buffer = new byte[4096];
							int bytesRead;
							while ((bytesRead = postStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								if (backgroundWorker.CancellationPending)
								{
									request.Abort();
									return;
								}
								requestStream.Write(buffer, 0, bytesRead);
								int permille = (int)(postStream.Position * 1000 / postStream.Length);
								backgroundWorker.ReportProgress(permille);
							}
						}
						else
						{
							postStream.WriteTo(requestStream);
						}

						string end = "--" + multipartBoundary + "--\r\n";
						byte[] data = Encoding.UTF8.GetBytes(end);
						requestStream.Write(data, 0, data.Length);
					}
				}
				else
				{
					request.ContentType = "application/x-www-form-urlencoded";

					byte[] data = Encoding.Default.GetBytes(postString.ToString());
					using (var requestStream = request.GetRequestStream())
					{
						requestStream.Write(data, 0, data.Length);
					}
				}
			}

			Reset();

			HttpWebResponse response;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException ex)
			{
				response = (HttpWebResponse)ex.Response;
			}
			try
			{
				lastHttpStatusCode = (int)response.StatusCode;
				lastRedirectUrlString = null;
				lastResponseData = null;

				if (lastHttpStatusCode == 301 || lastHttpStatusCode == 302 || lastHttpStatusCode == 303 || lastHttpStatusCode == 307)
				{
					lastRedirectUrlString = response.Headers["Location"];
					lastRedirectUri = new Uri(uri, lastRedirectUrlString);
				}
				else
				{
					using (var responseReader = new StreamReader(response.GetResponseStream()))
					{
						lastResponseData = responseReader.ReadToEnd();
					}

					if (lastHttpStatusCode == 200)
					{
						Match m = Regex.Match(lastResponseData, @"<\s*meta\s+http-equiv\s*=\s*""\s*refresh\s*""\s+content=""\d+\s*;\s*url=(.*?)""\s*>", RegexOptions.IgnoreCase);
						if (m.Success)
						{
							lastRedirectUrlString = m.Groups[1].Value.Trim();
							lastRedirectUri = new Uri(uri, lastRedirectUrlString);
						}
					}
				}
			}
			finally
			{
				response.Close();
			}
		}

		/// <summary>
		/// Resets all added POST data and data format configuration.
		/// </summary>
		public void Reset()
		{
			if (postString != null)
			{
				postString = null;
			}
			if (postStream != null)
			{
				postStream.Dispose();
				postStream = null;
			}
			multipartBoundary = null;
			multipartFormat = false;
		}

		#endregion HTTP operations
	}
}
