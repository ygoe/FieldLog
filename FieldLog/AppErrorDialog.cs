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

		private delegate void AddErrorDelegate(bool canContinue, string errorMsg, object ex);

		private class ErrorInfo
		{
			public string ErrorMessage { get; set; }
			public object DetailsObject { get; set; }
		}

		#endregion Delegates and types

		#region Private static fields

		private static object syncLock = new object();
		private static bool appErrorInitialized;
		private static AppErrorDialog currentInstance;
		private static Thread uiThread;

		#endregion Private static fields

		#region Static methods

		/// <summary>
		/// Shows the application error dialog. This is the only method that is called to show or
		/// update an error dialog. If a dialog is already open, the error is added to it.
		/// </summary>
		public static void ShowError(bool canContinue, string errorMsg, object ex)
		{
			lock (syncLock)
			{
				if (currentInstance == null)
				{
					currentInstance = new AppErrorDialog();
					currentInstance.SetCanContinue(canContinue);
					currentInstance.errorLabel.Text = errorMsg;
					currentInstance.grid.SelectedObject = ex;

					// Source: http://stackoverflow.com/a/3992635/143684
					uiThread = new Thread(UiThreadStart);
					uiThread.SetApartmentState(ApartmentState.STA);
					uiThread.Start();
				}
				else
				{
					// Add next error to existing dialog
					currentInstance.Invoke(new AddErrorDelegate(currentInstance.AddError), canContinue, errorMsg, ex);
				}
			}

			// Slow down or halt the application as long as there are many pending errors.
			// The error dialog runs in its own thread so it will still respond to user input. :-)
			// (Unless, of course, should an error occur in the error dialog…)
			if (currentInstance.GetNextErrorsCount() >= 20)
			{
				Thread.Sleep(1000);
			}
			while (currentInstance.GetNextErrorsCount() >= 40)
			{
				Thread.Sleep(1000);
			}
		}

		private static void UiThreadStart()
		{
			Application.Run(currentInstance);

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

		// Other fields
		private Queue<ErrorInfo> nextErrors = new Queue<ErrorInfo>();

		#endregion Private data

		#region Constructor (Form initialisation)

		private AppErrorDialog()
		{
			if (!appErrorInitialized)
			{
				Application.SetCompatibleTextRenderingDefault(false);
				appErrorInitialized = true;
			}

			string title = FL.AppErrorDialogTitle;
			string appName = FL.AppName;
			if (!string.IsNullOrEmpty(appName))
			{
				title = appName + " – " + title;
			}

			this.BackColor = SystemColors.Window;
			this.ControlBox = false;
			this.MinimizeBox = false;
			this.MaximizeBox = false;
			this.Font = SystemFonts.MessageBoxFont;
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.ShowInTaskbar = false;
			this.Size = new Size(550, 300);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.Text = title;
			this.TopMost = true;

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
			this.Controls.Add(tablePanel);

			introLabel = new Label();
			introLabel.BackColor = Color.FromArgb(221, 74, 59);
			introLabel.ForeColor = Color.White;
			introLabel.Dock = DockStyle.Fill;
			introLabel.AutoSize = true;
			introLabel.Font = new Font(
				SystemFonts.MessageBoxFont.FontFamily,
				SystemFonts.MessageBoxFont.SizeInPoints * 1.3f,
				SystemFonts.MessageBoxFont.Style);
			introLabel.MaximumSize = new Size(this.ClientSize.Width, 0);
			introLabel.Padding = new Padding(6, 4, 7, 6);
			introLabel.Margin = new Padding();
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
			errorLabel.MaximumSize = new Size(this.ClientSize.Width - 20, 0);
			errorLabel.Padding = new Padding();
			errorLabel.Margin = new Padding();
			errorLabel.UseMnemonic = false;
			errorPanel.Controls.Add(errorLabel);

			logLabel = new LinkLabel();
			logLabel.AutoSize = true;
			logLabel.MaximumSize = new Size(this.ClientSize.Width - 20, 0);
			logLabel.Margin = new Padding(8, 6, 10, 0);
			logLabel.Padding = new Padding();
			if (FL.LogFileBasePath != null)
			{
				logLabel.Text = string.Format(FL.AppErrorDialogLogPath, FL.LogFileBasePath.Replace("\\", "\\\u200B") + "*.fl");
				//logLabel.Text += " Open directory";
				//logLabel.LinkArea = new LinkArea(logLabel.Text.Length - 14, 14);
				string dir = Path.GetDirectoryName(FL.LogFileBasePath).Replace("\\", "\\\u200B");
				logLabel.LinkArea = new LinkArea(FL.AppErrorDialogLogPath.IndexOf("{0}"), dir.Length);
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
				this.Height += 300;
				this.Top -= Math.Min(this.Top - 4, 150);
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
			sendCheckBox.Enabled = false;
			sendCheckBox.FlatStyle = FlatStyle.System;
			sendCheckBox.Margin = new Padding();
			sendCheckBox.Padding = new Padding();
			sendCheckBox.Text = FL.AppErrorDialogSendLogs;
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
			terminateButton.Text = FL.AppErrorDialogTerminate + " (" + FL.AppErrorTerminateTimeout + ")";
			terminateButton.UseVisualStyleBackColor = true;
			terminateButton.Click += (s, e) =>
			{
				this.Close();
				FL.Shutdown();
				Environment.Exit(1);
			};
			buttonsPanel.Controls.Add(terminateButton);
			buttonsPanel.SetRow(terminateButton, 0);
			buttonsPanel.SetColumn(terminateButton, 2);

			DateTime shutdownTime = DateTime.UtcNow.AddSeconds(FL.AppErrorTerminateTimeout);
			System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
			timer.Interval = 1000;
			timer.Tick += (s, e) =>
			{
				int secondsToShutdown = (int) Math.Round((shutdownTime - DateTime.UtcNow).TotalSeconds);
				if (secondsToShutdown < 0)
					secondsToShutdown = 0;
				terminateButton.Text = FL.AppErrorDialogTerminate + " (" + secondsToShutdown + ")";
			};
			timer.Start();

			continueButton = new Button();
			continueButton.AutoSize = true;
			continueButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			continueButton.FlatStyle = FlatStyle.System;
			continueButton.Margin = new Padding(6, 0, 0, 0);
			continueButton.Padding = new Padding(2, 1, 2, 1);
			continueButton.Text = FL.AppErrorDialogContinue;
			continueButton.UseVisualStyleBackColor = true;
			continueButton.Click += (s, e) =>
			{
				this.Close();
			};
			buttonsPanel.Controls.Add(continueButton);
			buttonsPanel.SetRow(continueButton, 0);
			buttonsPanel.SetColumn(continueButton, 3);
		}

		#endregion Constructor (Form initialisation)

		#region Private helper methods

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

		private void AddError(bool canContinue, string errorMsg, object ex)
		{
			if (!canContinue)
			{
				SetCanContinue(false);
			}

			ErrorInfo ei = new ErrorInfo();
			ei.ErrorMessage = errorMsg;
			ei.DetailsObject = ex;
			nextErrors.Enqueue(ei);
			nextButton.Text = FL.AppErrorDialogNext + " (" + nextErrors.Count + ")";
			nextButton.Visible = true;
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
			if (nextErrors.Count > 0)
			{
				nextButton.Text = FL.AppErrorDialogNext + " (" + nextErrors.Count + ")";
			}
			else
			{
				nextButton.Visible = false;
			}
		}

		#endregion Private helper methods
	}
}
