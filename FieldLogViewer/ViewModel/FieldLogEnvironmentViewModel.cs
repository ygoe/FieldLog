using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Unclassified.FieldLog;

namespace Unclassified.FieldLogViewer.ViewModel
{
	class FieldLogEnvironmentViewModel : ViewModelBase
	{
		public FieldLogEnvironmentViewModel(FieldLogEventEnvironment environment, DateTime itemTime)
		{
			this.Environment = environment;
			this.ItemTime = itemTime;
		}

		public FieldLogEventEnvironment Environment { get; private set; }
		public DateTime ItemTime { get; private set; }

		public string ProcessIdArchitecture
		{
			get
			{
				if (Environment != null)
				{
					return Environment.ProcessId + (Environment.IsProcess64Bit ? " (64 bit)" : " (32 bit)");
				}
				return null;
			}
		}

		public string CultureName
		{
			get
			{
				if (Environment != null)
				{
					string cn = Environment.CultureName;
					try
					{
						cn += ", " + new CultureInfo(Environment.CultureName).DisplayName;
					}
					catch
					{
					}
					return cn;
				}
				return null;
			}
		}

		public string UserName
		{
			get
			{
				if (Environment != null)
				{
					return Environment.UserName + (Environment.IsAdministrator ? " (is administrator)" : " (no administrator)");
				}
				return null;
			}
		}

		public string IsInteractive
		{
			get
			{
				if (Environment != null)
				{
					return Environment.IsInteractive ? "Yes" : "No";
				}
				return null;
			}
		}

		public string OSName
		{
			get
			{
				if (Environment != null)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine(Environment.OSProductName);
					sb.Append("(type: ");
					sb.Append(Environment.OSType);
					sb.Append(", version: ");
					sb.Append(Environment.OSVersion);
					sb.Append(", edition: ");
					sb.Append(Environment.OSEdition);
					sb.Append(", SP ");
					sb.Append(Environment.OSServicePack);
					sb.Append(", build: ");
					sb.Append(Environment.OSBuild);
					sb.Append(")");
					return sb.ToString();
				}
				return null;
			}
		}

		public string OSArchitecture
		{
			get
			{
				if (Environment != null)
				{
					return Environment.OSIs64Bit ? "64 bit" : "32 bit";
				}
				return null;
			}
		}

		public string OSLanguage
		{
			get
			{
				if (Environment != null)
				{
					string cn = Environment.OSLanguage;
					try
					{
						cn += ", " + new CultureInfo(Environment.OSLanguage).DisplayName;
					}
					catch
					{
					}
					return cn;
				}
				return null;
			}
		}

		public string OSIsAppServer
		{
			get
			{
				if (Environment != null)
				{
					return Environment.OSIsAppServer ? "Yes" : "No";
				}
				return null;
			}
		}

		public string OSUptime
		{
			get
			{
				if (Environment != null)
				{
					return (ItemTime - Environment.OSLastBootTime).ToString(@"d\.hh\:mm\:ss") +
						(Environment.OSIsFailSafeBoot ? " (fail-safe boot)" : " (normal boot)");
				}
				return null;
			}
		}

		public string LocalTimeZoneOffset
		{
			get
			{
				if (Environment != null)
				{
					int utcOffset = (int) Environment.LocalTimeZoneOffset.TotalMinutes;
					int hours = utcOffset / 60;
					int mins = Math.Abs(utcOffset) % 60;
					return hours.ToString("+00;-00;+00") + ":" + mins.ToString("00");
				}
				return null;
			}
		}

		public string PrimaryScreen
		{
			get
			{
				if (Environment != null)
				{
					string s = Environment.PrimaryScreenWidth + "×" +
						Environment.PrimaryScreenHeight + " pixels, " +
						Environment.PrimaryScreenBitsPerPixel + " bits per pixel";

					string bars = "";
					if (Environment.PrimaryScreenWorkingAreaLeft > 0)
						bars += (bars != "" ? ", " : "") + "left " + Environment.PrimaryScreenWorkingAreaLeft;
					if (Environment.PrimaryScreenHeight - Environment.PrimaryScreenWorkingAreaHeight - Environment.PrimaryScreenWorkingAreaTop > 0)
						bars += (bars != "" ? ", " : "") + "bottom " + (Environment.PrimaryScreenHeight - Environment.PrimaryScreenWorkingAreaHeight - Environment.PrimaryScreenWorkingAreaTop);
					if (Environment.PrimaryScreenWidth - Environment.PrimaryScreenWorkingAreaWidth - Environment.PrimaryScreenWorkingAreaLeft > 0)
						bars += (bars != "" ? ", " : "") + "right " + (Environment.PrimaryScreenWidth - Environment.PrimaryScreenWorkingAreaWidth - Environment.PrimaryScreenWorkingAreaLeft);
					if (Environment.PrimaryScreenWorkingAreaTop > 0)
						bars += (bars != "" ? ", " : "") + "top " + Environment.PrimaryScreenWorkingAreaTop;
					if (bars != "")
						s += ", bars: " + bars;

					return s;
				}
				return null;
			}
		}

		public string ScreenDpi
		{
			get
			{
				if (Environment != null)
				{
					return Environment.ScreenDpi + " dpi (" + (Environment.ScreenDpi * 100 / 96) + " %)";
				}
				return null;
			}
		}
	}
}
