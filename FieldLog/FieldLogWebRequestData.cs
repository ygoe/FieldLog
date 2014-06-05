using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Unclassified.FieldLog
{
	/// <summary>
	/// Contains information about the current web request for log items.
	/// </summary>
	public class FieldLogWebRequestData
	{
		#region Static members

		/// <summary>Contains the empty data object.</summary>
		public static readonly FieldLogWebRequestData Empty;

		static FieldLogWebRequestData()
		{
			Empty = new FieldLogWebRequestData();
		}

		/// <summary>
		/// Indicates whether the specified data variable is null or the Empty object.
		/// </summary>
		/// <param name="value">The variable to test.</param>
		/// <returns>true if the value parameter is null or the empty object; otherwise, false.</returns>
		public static bool IsNullOrEmpty(FieldLogWebRequestData value)
		{
			return value == null || value == FieldLogWebRequestData.Empty;
		}

		#endregion Static members

		#region Data properties

		/// <summary>
		/// Gets the approximate data size of this data structure. Used for buffer size estimation.
		/// </summary>
		public int Size
		{
			get
			{
				// Source: http://www.informit.com/guides/content.aspx?g=dotnet&seqNum=682
				int ptrSize = IntPtr.Size;
				int strSize = ptrSize == 4 ? 20 : 32;

				return 4 +
					ptrSize + (RequestUrl != null ? strSize + RequestUrl.Length * 2 : 0) +
					ptrSize + (Method != null ? strSize + RequestUrl.Length * 2 : 0) +
					ptrSize + (ClientAddress != null ? strSize + ClientAddress.Length * 2 : 0) +
					ptrSize + (ClientHostName != null ? strSize + ClientHostName.Length * 2 : 0) +
					ptrSize + (Referrer != null ? strSize + Referrer.Length * 2 : 0) +
					ptrSize + (UserAgent != null ? strSize + UserAgent.Length * 2 : 0) +
					ptrSize + (AcceptLanguages != null ? strSize + AcceptLanguages.Length * 2 : 0) +
					ptrSize + (Accept != null ? strSize + Accept.Length * 2 : 0) +
					ptrSize + (WebSessionId != null ? strSize + WebSessionId.Length * 2 : 0) +
					ptrSize + (AppUserId != null ? strSize + AppUserId.Length * 2 : 0) +
					ptrSize + (AppUserName != null ? strSize + AppUserName.Length * 2 : 0);
			}
		}

		/// <summary>
		/// Gets or sets the URL of the request.
		/// </summary>
		public string RequestUrl { get; set; }
		/// <summary>
		/// Gets or sets the HTTP request method (GET, POST).
		/// </summary>
		public string Method { get; set; }
		/// <summary>
		/// Gets or sets the network (IP) address of the client.
		/// </summary>
		public string ClientAddress { get; set; }
		/// <summary>
		/// Gets or sets the network host name of the client.
		/// </summary>
		public string ClientHostName { get; set; }
		/// <summary>
		/// Gets or sets the Referrer HTTP header of the request.
		/// </summary>
		public string Referrer { get; set; }
		/// <summary>
		/// Gets or sets the User-Agent HTTP header of the request.
		/// </summary>
		public string UserAgent { get; set; }
		/// <summary>
		/// Gets or sets the Accept-Languages HTTP header of the request.
		/// </summary>
		public string AcceptLanguages { get; set; }
		/// <summary>
		/// Gets or sets the Accept HTTP header of the request.
		/// </summary>
		public string Accept { get; set; }
		/// <summary>
		/// Gets or sets the web session ID of the request.
		/// </summary>
		public string WebSessionId { get; set; }
		/// <summary>
		/// Gets or sets the application-defined user ID of the request.
		/// </summary>
		public string AppUserId { get; set; }
		/// <summary>
		/// Gets or sets the application-defined user name of the request.
		/// </summary>
		public string AppUserName { get; set; }

		#endregion Data properties

		#region Constructor

		/// <summary>
		/// Initialises a new instance of the FieldLogWebRequestData class.
		/// </summary>
		public FieldLogWebRequestData()
		{
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="source">The FieldLogWebRequestData instance to copy from.</param>
		public FieldLogWebRequestData(FieldLogWebRequestData source)
		{
			RequestUrl = source.RequestUrl;
			Method = source.Method;
			ClientAddress = source.ClientAddress;
			ClientHostName = source.ClientHostName;
			Referrer = source.Referrer;
			UserAgent = source.UserAgent;
			AcceptLanguages = source.AcceptLanguages;
			Accept = source.Accept;
			WebSessionId = source.WebSessionId;
			AppUserId = source.AppUserId;
			AppUserName = source.AppUserName;
		}

		#endregion Constructor

		#region Copy method

		/// <summary>
		/// Updates the current instance with data from another instance.
		/// </summary>
		/// <param name="source">The FieldLogWebRequestData instance to copy from.</param>
		public void UpdateFrom(FieldLogWebRequestData source)
		{
			RequestUrl = source.RequestUrl;
			Method = source.Method;
			ClientAddress = source.ClientAddress;
			ClientHostName = source.ClientHostName;
			Referrer = source.Referrer;
			UserAgent = source.UserAgent;
			AcceptLanguages = source.AcceptLanguages;
			Accept = source.Accept;
			WebSessionId = source.WebSessionId;
			AppUserId = source.AppUserId;
			AppUserName = source.AppUserName;
		}

		#endregion Copy method

		#region Log file reading/writing

		/// <summary>
		/// Writes the FieldLogWebRequestData data to a log file writer.
		/// </summary>
		/// <param name="writer"></param>
		internal void Write(FieldLogFileWriter writer)
		{
			writer.AddBuffer(RequestUrl);
			writer.AddBuffer(Method);
			writer.AddBuffer(ClientAddress);
			writer.AddBuffer(ClientHostName);
			writer.AddBuffer(Referrer);
			writer.AddBuffer(UserAgent);
			writer.AddBuffer(AcceptLanguages);
			writer.AddBuffer(Accept);
			writer.AddBuffer(WebSessionId);
			writer.AddBuffer(AppUserId);
			writer.AddBuffer(AppUserName);
		}

		/// <summary>
		/// Reads the FieldLogWebRequestData data from a log file reader.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		internal static FieldLogWebRequestData Read(FieldLogFileReader reader)
		{
			FieldLogWebRequestData data = new FieldLogWebRequestData();
			data.RequestUrl = reader.ReadString();
			data.Method = reader.ReadString();
			data.ClientAddress = reader.ReadString();
			data.ClientHostName = reader.ReadString();
			data.Referrer = reader.ReadString();
			data.UserAgent = reader.ReadString();
			data.AcceptLanguages = reader.ReadString();
			data.Accept = reader.ReadString();
			data.WebSessionId = reader.ReadString();
			data.AppUserId = reader.ReadString();
			data.AppUserName = reader.ReadString();

			// Check if the environment is actually empty
			if (string.IsNullOrEmpty(data.RequestUrl))
				return FieldLogWebRequestData.Empty;

			return data;
		}

		#endregion Log file reading/writing
	}
}
