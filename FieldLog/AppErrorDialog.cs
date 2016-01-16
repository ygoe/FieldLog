// FieldLog – .NET logging solution
// © Yves Goergen, Made in Germany
// Website: http://unclassified.software/source/fieldlog
//
// This library is free software: you can redistribute it and/or modify it under the terms of
// the GNU Lesser General Public License as published by the Free Software Foundation, version 3.
//
// This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along with this
// library. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Unclassified.FieldLog
{
	/// <summary>
	/// The application error dialog.
	/// </summary>
	public class AppErrorDialog : Form
	{
		#region Delegates and types

		private delegate void AddErrorDelegate(bool canContinue, string errorMsg, object ex, bool terminateTimerEnabled);

		private delegate void VoidMethod();   // = Action in .NET 4

		private class ErrorInfo
		{
			public string ErrorMessage { get; set; }
			public object DetailsObject { get; set; }
		}

		#endregion Delegates and types

		#region Static constructor

		static AppErrorDialog()
		{
			CanShowDetails = true;
		}

		#endregion Static constructor

		#region Private static fields

		private static object syncLock = new object();
		private static bool appErrorInitialized;
		private static AppErrorDialog currentInstance;
		private static Thread uiThread;

		#endregion Private static fields

		#region Public static properties

		/// <summary>
		/// Gets or sets a value indicating whether the exception details object can be shown to the
		/// user. This should be set to false for obfuscated applications because there will be no
		/// or no readable data to display. The default value is true.
		/// </summary>
		public static bool CanShowDetails { get; set; }

		#endregion Public static properties

		#region Static methods

		/// <summary>
		/// Shows the application error dialog. This is the only method that is called to show or
		/// update an error dialog. If a dialog is already open, the error is added to it.
		/// </summary>
		/// <param name="canContinue">Indicates whether the application can continue.</param>
		/// <param name="errorMsg">The error message to display.</param>
		/// <param name="ex">The <see cref="Exception"/> instance to display as details object.</param>
		/// <param name="terminateTimerEnabled">Indicates whether the termination safety timer has been started.</param>
		public static void ShowError(bool canContinue, string errorMsg, object ex, bool terminateTimerEnabled)
		{
			lock (syncLock)
			{
				try
				{
					if (currentInstance == null)
					{
						currentInstance = new AppErrorDialog();
						currentInstance.SetCanContinue(canContinue);
						currentInstance.errorLabel.Text = errorMsg;
						currentInstance.grid.SelectedObject = ex;
						currentInstance.detailsLabel.Enabled = ex != null;
						if (terminateTimerEnabled)
						{
							currentInstance.EnableTerminateTimer();
						}

						// Source: http://stackoverflow.com/a/3992635/143684
						uiThread = new Thread(UiThreadStart);
						uiThread.Name = "FieldLog.AppErrorDialogUIThread";
						uiThread.SetApartmentState(ApartmentState.STA);
						uiThread.Start();
					}
					else
					{
						// Add next error to existing dialog
						// Wait until the window handle is created
						int count = 0;
						while (!currentInstance.IsHandleCreated)
						{
							if (count++ > 500)
								throw new TimeoutException("Application error dialog was not created in reasonable time.");
							Thread.Sleep(10);
						}
						currentInstance.Invoke(new AddErrorDelegate(currentInstance.AddError), canContinue, errorMsg, ex, terminateTimerEnabled);
					}
				}
				catch (Exception ex2)
				{
					FL.Critical(ex2, "FieldLog.Showing AppErrorDialog", false);
					FL.Flush();
					MessageBox.Show(
						"Error showing the application error dialog. Details should be logged.",
						"Error",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);
				}
			}

			// Make sure we won't continue in this thread if it's not possible
			while (!canContinue)
			{
				Thread.Sleep(1000000);
			}

			// Slow down or halt the application as long as there are many pending errors.
			// The error dialog runs in its own thread so it will still respond to user input. :-)
			// (Unless, of course, should an error occur in the error dialog…)
			if (currentInstance != null && currentInstance.GetNextErrorsCount() >= 20)
			{
				Thread.Sleep(1000);
			}
			while (currentInstance != null && currentInstance.GetNextErrorsCount() >= 40)
			{
				Thread.Sleep(1000);
			}
		}

		private static void UiThreadStart()
		{
			// Keep the window in the foreground.
			// Sometimes, when debugging in Visual Studio, the window sits in the background,
			// unnoticed, and prevents the application to shut down because it's not a background
			// thread. Setting TopMost again after a short while helps to bring it in the foreground
			// again.
			System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
			timer.Tick += delegate (object sender, EventArgs args)
				{
					// Just to be sure, also set Visible, it doesn't hurt
					currentInstance.Visible = true;
					currentInstance.TopMost = true;

					// Come back, but not so soon
					timer.Interval *= 2;
				};
			timer.Interval = 100;
			timer.Start();

			Application.Run(currentInstance);

			// The window has been closed and the message loop was left. Should there still be a
			// timer event scheduled, it won't be executed anymore. Now clean up everything.
			timer.Stop();
			timer.Dispose();
			lock (syncLock)
			{
				currentInstance.Dispose();
				currentInstance = null;
			}
		}

		#endregion Static methods

		#region Private data

		// Form controls
		private TableLayoutPanel tablePanel;
		private Label introLabel;
		private Panel errorPanel;
		private Label errorLabel;
		private LinkLabel logLabel;
		private LinkLabel detailsLabel;
		private PropertyGrid grid;
		private TableLayoutPanel buttonsPanel;
		private CheckBox sendCheckBox;
		private Button nextButton;
		private Button terminateButton;
		private Button continueButton;
		private System.Windows.Forms.Timer terminateTimer;

		// Other fields
		private Queue<ErrorInfo> nextErrors = new Queue<ErrorInfo>();

		#endregion Private data

		#region Constructor (Form initialisation)

		private AppErrorDialog()
		{
			if (!appErrorInitialized)
			{
				Application.EnableVisualStyles();
				appErrorInitialized = true;
			}

			string title = FL.AppErrorDialogTitle;
			string appName = FL.AppName;
			if (!string.IsNullOrEmpty(appName))
			{
				title = appName + " – " + title;
			}

			BackColor = SystemColors.Window;
			ControlBox = false;
			MinimizeBox = false;
			MaximizeBox = false;
			Font = SystemFonts.MessageBoxFont;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			ShowInTaskbar = false;
			Size = new Size(550, 300);
			StartPosition = FormStartPosition.CenterScreen;
			Text = title;
			TopMost = true;

			tablePanel = new TableLayoutPanel();
			tablePanel.Dock = DockStyle.Fill;
			tablePanel.RowCount = 6;
			tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
			tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tablePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
			tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tablePanel.ColumnCount = 1;
			tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			Controls.Add(tablePanel);

			introLabel = new Label();
			introLabel.BackColor = Color.FromArgb(221, 74, 59);
			introLabel.ForeColor = Color.White;
			introLabel.Dock = DockStyle.Fill;
			introLabel.AutoSize = true;
			introLabel.Font = new Font(
				SystemFonts.MessageBoxFont.FontFamily,
				SystemFonts.MessageBoxFont.SizeInPoints * 1.3f,
				SystemFonts.MessageBoxFont.Style);
			introLabel.MaximumSize = new Size(ClientSize.Width, 0);
			introLabel.Padding = new Padding(6, 4, 7, 6);
			introLabel.Margin = new Padding();
			introLabel.UseCompatibleTextRendering = false;
			introLabel.UseMnemonic = false;
			tablePanel.Controls.Add(introLabel);
			tablePanel.SetRow(introLabel, 0);
			tablePanel.SetColumn(introLabel, 0);

			errorPanel = new Panel();
			errorPanel.AutoScroll = true;
			errorPanel.Dock = DockStyle.Fill;
			errorPanel.Margin = new Padding(7, 8, 10, 6);
			errorPanel.Padding = new Padding();
			tablePanel.Controls.Add(errorPanel);
			tablePanel.SetRow(errorPanel, 1);
			tablePanel.SetColumn(errorPanel, 0);

			errorLabel = new Label();
			errorLabel.AutoSize = true;
			// Always keep the vertical scrollbar width free because the label wouldn't get smaller
			// when the vertical scrollbar appears and then the horizontal scrollbar kicks in as well.
			errorLabel.MaximumSize = new Size(errorPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2, 0);
			errorLabel.Padding = new Padding();
			errorLabel.Margin = new Padding();
			errorLabel.UseCompatibleTextRendering = false;
			errorLabel.UseMnemonic = false;
			errorPanel.Controls.Add(errorLabel);

			logLabel = new LinkLabel();
			logLabel.AutoSize = true;
			logLabel.MaximumSize = new Size(ClientSize.Width - 20, 0);
			logLabel.Margin = new Padding(8, 6, 10, 0);
			logLabel.Padding = new Padding();
			if (FL.LogFileBasePath != null)
			{
				logLabel.Text = string.Format(FL.AppErrorDialogLogPath, FL.LogFileBasePath.Replace("\\", "\\\u200B") + "*.fl");
				string dir = Path.GetDirectoryName(FL.LogFileBasePath).Replace("\\", "\\\u200B");
				logLabel.LinkArea = new LinkArea(FL.AppErrorDialogLogPath.IndexOf("{0}", StringComparison.Ordinal), dir.Length);
				logLabel.LinkClicked += (s, e) =>
				{
					Process.Start(Path.GetDirectoryName(FL.LogFileBasePath));
				};
			}
			else
			{
				logLabel.Text = FL.AppErrorDialogNoLog;
				logLabel.LinkArea = new LinkArea(0, 0);
			}
			logLabel.UseCompatibleTextRendering = false;
			logLabel.UseMnemonic = false;
			tablePanel.Controls.Add(logLabel);
			tablePanel.SetRow(logLabel, 2);
			tablePanel.SetColumn(logLabel, 0);

			detailsLabel = new LinkLabel();
			detailsLabel.AutoSize = true;
			detailsLabel.Margin = new Padding(7, 6, 10, 10);
			detailsLabel.Padding = new Padding();
			detailsLabel.TabIndex = 11;
			detailsLabel.Text = FL.AppErrorDialogDetails;
			detailsLabel.UseCompatibleTextRendering = false;
			detailsLabel.Visible = CanShowDetails;
			tablePanel.Controls.Add(detailsLabel);
			tablePanel.SetRow(detailsLabel, 3);
			tablePanel.SetColumn(detailsLabel, 0);

			var attr = new TypeConverterAttribute(typeof(ExpandableObjectConverter));
			TypeDescriptor.AddAttributes(typeof(Exception), attr);
			grid = new PropertyGrid();
			grid.Dock = DockStyle.Fill;
			grid.Margin = new Padding(10, 10, 10, 10);
			grid.ToolbarVisible = false;
			grid.HelpVisible = false;
			grid.PropertySort = PropertySort.Alphabetical;
			grid.UseCompatibleTextRendering = false;
			grid.Visible = false;
			tablePanel.Controls.Add(grid);
			tablePanel.SetRow(grid, 4);
			tablePanel.SetColumn(grid, 0);

			bool isGridColumnResized = false;
			grid.Resize += (s, e) =>
			{
				if (!isGridColumnResized)
				{
					isGridColumnResized = true;
					// Source: http://stackoverflow.com/a/14475276/143684
					FieldInfo fi = grid.GetType().GetField("gridView", BindingFlags.Instance | BindingFlags.NonPublic);
					if (fi != null)
					{
						Control view = fi.GetValue(grid) as Control;
						if (view != null)
						{
							MethodInfo mi = view.GetType().GetMethod("MoveSplitterTo", BindingFlags.Instance | BindingFlags.NonPublic);
							if (mi != null)
							{
								mi.Invoke(view, new object[] { 170 });
							}
							mi = view.GetType().GetMethod("set_GrayTextColor", BindingFlags.Instance | BindingFlags.NonPublic);
							if (mi != null)
							{
								mi.Invoke(view, new object[] { Color.Black });
							}
						}
					}
				}
			};

			detailsLabel.LinkClicked += (s, e) =>
			{
				detailsLabel.Hide();
				Height += 300;
				Top -= Math.Min(Top - 4, 150);
				tablePanel.RowStyles[4].Height = 350;
				grid.Visible = true;
			};

			buttonsPanel = new TableLayoutPanel();
			buttonsPanel.AutoSize = true;
			buttonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			buttonsPanel.BackColor = SystemColors.Control;
			buttonsPanel.Dock = DockStyle.Fill;
			buttonsPanel.Margin = new Padding();
			buttonsPanel.Padding = new Padding(10, 10, 10, 10);
			buttonsPanel.ColumnCount = 4;
			buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			tablePanel.Controls.Add(buttonsPanel);
			tablePanel.SetRow(buttonsPanel, 5);
			tablePanel.SetColumn(buttonsPanel, 0);

			sendCheckBox = new CheckBox();
			sendCheckBox.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
			sendCheckBox.AutoSize = true;
			sendCheckBox.Enabled = FL.CanSubmitLog;
			if (sendCheckBox.Enabled)
			{
				sendCheckBox.Checked = true;
			}
			sendCheckBox.FlatStyle = FlatStyle.System;
			sendCheckBox.Margin = new Padding();
			sendCheckBox.Padding = new Padding();
			sendCheckBox.Text = FL.AppErrorDialogSendLogs;
			sendCheckBox.UseCompatibleTextRendering = false;
			buttonsPanel.Controls.Add(sendCheckBox);
			buttonsPanel.SetRow(sendCheckBox, 0);
			buttonsPanel.SetColumn(sendCheckBox, 0);

			nextButton = new Button();
			nextButton.AutoSize = true;
			nextButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			nextButton.FlatStyle = FlatStyle.System;
			nextButton.Margin = new Padding(6, 0, 0, 0);
			nextButton.Padding = new Padding(2, 1, 2, 1);
			nextButton.Text = FL.AppErrorDialogNext;
			nextButton.UseCompatibleTextRendering = false;
			nextButton.UseVisualStyleBackColor = true;
			nextButton.Visible = false;
			nextButton.Click += (s, e) =>
			{
				ShowNextError();
			};
			buttonsPanel.Controls.Add(nextButton);
			buttonsPanel.SetRow(nextButton, 0);
			buttonsPanel.SetColumn(nextButton, 1);

			terminateButton = new Button();
			terminateButton.AutoSize = true;
			terminateButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			terminateButton.FlatStyle = FlatStyle.System;
			terminateButton.Margin = new Padding(6, 0, 0, 0);
			terminateButton.Padding = new Padding(2, 1, 2, 1);
			terminateButton.Text = FL.AppErrorDialogTerminate;
			terminateButton.UseCompatibleTextRendering = false;
			terminateButton.UseVisualStyleBackColor = true;
			terminateButton.Click += (s, e) =>
			{
				StartSubmitTool();
				Close();
				FL.Shutdown();
				Environment.Exit(1);
			};
			buttonsPanel.Controls.Add(terminateButton);
			buttonsPanel.SetRow(terminateButton, 0);
			buttonsPanel.SetColumn(terminateButton, 2);

			continueButton = new Button();
			continueButton.AutoSize = true;
			continueButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			continueButton.FlatStyle = FlatStyle.System;
			continueButton.Margin = new Padding(6, 0, 0, 0);
			continueButton.Padding = new Padding(2, 1, 2, 1);
			continueButton.Text = FL.AppErrorDialogContinue;
			continueButton.UseCompatibleTextRendering = false;
			continueButton.UseVisualStyleBackColor = true;
			continueButton.Click += (s, e) =>
			{
				StartSubmitTool();
				Close();
			};
			buttonsPanel.Controls.Add(continueButton);
			buttonsPanel.SetRow(continueButton, 0);
			buttonsPanel.SetColumn(continueButton, 3);
		}

		#endregion Constructor (Form initialisation)

		#region Private helper methods

		private void EnableTerminateTimer()
		{
			if (terminateTimer == null)
			{
				terminateButton.Text = FL.AppErrorDialogTerminate + " (" + FL.AppErrorTerminateTimeout + ")";

				DateTime shutdownTime = DateTime.UtcNow.AddSeconds(FL.AppErrorTerminateTimeout);
				terminateTimer = new System.Windows.Forms.Timer();
				terminateTimer.Interval = 500;
				terminateTimer.Tick += (s, e) =>
				{
					int secondsToShutdown = (int)Math.Round((shutdownTime - DateTime.UtcNow).TotalSeconds);
					if (secondsToShutdown < 0)
						secondsToShutdown = 0;
					terminateButton.Text = FL.AppErrorDialogTerminate + " (" + secondsToShutdown + ")";
				};

				if (!IsHandleCreated)
				{
					// Called directly after the constructor
					Load += (s, e) =>
					{
						// Start timer in the window's thread, not in the main UI thread (which may be blocked)
						terminateTimer.Start();
					};
				}
				else
				{
					Invoke(new VoidMethod(terminateTimer.Start));
				}
			}
		}

		private void SetCanContinue(bool value)
		{
			continueButton.Enabled = value;
			if (value)
			{
				introLabel.Text = FL.AppErrorDialogContinuable;
			}
			else
			{
				introLabel.Text = FL.AppErrorDialogTerminating;
			}
		}

		private void AddError(bool canContinue, string errorMsg, object ex, bool terminateTimerEnabled)
		{
			try
			{
				if (!canContinue)
				{
					SetCanContinue(false);
				}
				if (terminateTimerEnabled)
				{
					EnableTerminateTimer();
				}

				ErrorInfo ei = new ErrorInfo();
				ei.ErrorMessage = errorMsg;
				ei.DetailsObject = ex;
				nextErrors.Enqueue(ei);
				nextButton.Text = FL.AppErrorDialogNext + " (" + nextErrors.Count + ")";
				nextButton.Visible = true;
			}
			catch (Exception ex2)
			{
				FL.Critical(ex2, "FieldLog.Showing AppErrorDialog", false);
				FL.Flush();
				MessageBox.Show(
					"Error updating the application error dialog. Details should be logged.",
					"Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}

		private int GetNextErrorsCount()
		{
			lock (syncLock)
			{
				return nextErrors.Count;
			}
		}

		private void ShowNextError()
		{
			ErrorInfo ei;
			lock (syncLock)
			{
				ei = nextErrors.Dequeue();
			}
			errorLabel.Text = ei.ErrorMessage;
			grid.SelectedObject = ei.DetailsObject;
			detailsLabel.Enabled = ei.DetailsObject != null;
			if (nextErrors.Count > 0)
			{
				nextButton.Text = FL.AppErrorDialogNext + " (" + nextErrors.Count + ")";
			}
			else
			{
				nextButton.Visible = false;
			}
		}

		private void StartSubmitTool()
		{
			if (sendCheckBox.Checked)
			{
				string exeFile = Application.ExecutablePath;
				if (!string.IsNullOrEmpty(exeFile))
				{
					exeFile = Path.GetDirectoryName(exeFile);
					exeFile = Path.Combine(exeFile, "LogSubmit.exe");
					if (File.Exists(exeFile))
					{
						// Found the log submit tool, now start it
						try
						{
							Process.Start(exeFile, "/errordlg /logpath \"" + FL.LogFileBasePath + "\"");
						}
						catch (Exception ex)
						{
							// Start failed, show an error message
							FL.Critical(ex, "Starting log submit tool");
							MessageBox.Show(
								"The log submit tool could not be started." + " " + ex.Message,
								"Error",
								MessageBoxButtons.OK,
								MessageBoxIcon.Error);
						}
						return;
					}
				}
				// Log submit tool not found but logs should be sent, show an error message
				FL.Error("Could not start log submit tool, path or file not found");
				MessageBox.Show(
					"The log submit tool could not be started. The path or file was not found. Please start the tool manually from the application installation directory.",
					"Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}

		#endregion Private helper methods
	}
}
