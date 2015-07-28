using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Unclassified.FieldLog;
using Unclassified.UI;

namespace Unclassified.FieldLogViewer.ViewModels
{
	internal class FieldLogEnvironmentViewModel : ViewModelBase
	{
		public FieldLogEnvironmentViewModel(FieldLogEventEnvironment environment, FieldLogItemViewModel itemVM)
		{
			this.Environment = environment;
			this.ItemVM = itemVM;
		}

		public FieldLogEventEnvironment Environment { get; private set; }
		public FieldLogItemViewModel ItemVM { get; private set; }

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

		public string AppVersionAndConfig
		{
			get
			{
				if (Environment != null)
				{
					return Environment.AppVersion + (!string.IsNullOrEmpty(Environment.AppAsmConfiguration) ? " (" + Environment.AppAsmConfiguration + ")" : "");
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
					if (!string.IsNullOrEmpty(cn))
					{
						try
						{
							cn += ", " + new CultureInfo(Environment.CultureName).DisplayName;
						}
						catch
						{
						}
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

		public string ProcessUptime
		{
			get
			{
				if (Environment != null && ItemVM.LastLogStartItem != null)
				{
					string startTime = "";
					switch (App.Settings.ItemTimeMode)
					{
						case ItemTimeType.Utc:
							startTime = ItemVM.LastLogStartItem.Time.ToString("yyyy-MM-dd, HH:mm:ss") + " UTC";
							break;
						case ItemTimeType.Local:
							startTime = ItemVM.LastLogStartItem.Time.ToLocalTime().ToString("yyyy-MM-dd, HH:mm:ss");
							break;
						case ItemTimeType.Remote:
							int hours = ItemVM.LastLogStartItem.UtcOffset / 60;
							int mins = Math.Abs(ItemVM.LastLogStartItem.UtcOffset) % 60;
							startTime = ItemVM.LastLogStartItem.Time.AddMinutes(ItemVM.LastLogStartItem.UtcOffset).ToString("yyyy-MM-dd, HH:mm:ss") + " " +
								hours.ToString("+00;-00;+00") + ":" + mins.ToString("00");
							break;
					}

					return (ItemVM.Time - ItemVM.LastLogStartItem.Time).ToString(@"d\.hh\:mm\:ss") +
						", started " + startTime;
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
					if (!string.IsNullOrEmpty(cn))
					{
						try
						{
							cn += ", " + new CultureInfo(Environment.OSLanguage).DisplayName;
						}
						catch
						{
						}
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
					string startTime = "";
					switch (App.Settings.ItemTimeMode)
					{
						case ItemTimeType.Utc:
							startTime = Environment.OSLastBootTime.ToString("yyyy-MM-dd, HH:mm:ss") + " UTC";
							break;
						case ItemTimeType.Local:
							startTime = Environment.OSLastBootTime.ToLocalTime().ToString("yyyy-MM-dd, HH:mm:ss");
							break;
						case ItemTimeType.Remote:
							int hours = ItemVM.UtcOffset / 60;
							int mins = Math.Abs(ItemVM.UtcOffset) % 60;
							startTime = Environment.OSLastBootTime.AddMinutes(ItemVM.UtcOffset).ToString("yyyy-MM-dd, HH:mm:ss") + " " +
								hours.ToString("+00;-00;+00") + ":" + mins.ToString("00");
							break;
					}

					return (ItemVM.Time - Environment.OSLastBootTime).ToString(@"d\.hh\:mm\:ss") +
						", started " + startTime +
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
					int utcOffset = (int)Environment.LocalTimeZoneOffset.TotalMinutes;
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
