using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Unclassified.LogSubmit.Views;
using Unclassified.TxLib;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.LogSubmit
{
	public partial class MainForm : Form
	{
		#region Static members

		public static MainForm Instance;

		#endregion Static members

		#region Private data

		private LogSelectionView logSelectionView;
		private TimeSelectionView timeSelectionView;
		private NotesView notesView;
		private CompressView compressView;
		private TransportView transportView;
		private TransportProgressView transportProgressView;

		private DateTime appStartTime = DateTime.UtcNow;
		private bool finishEnabled;
		private Panel progressPanel;
		private int prevViewIndex;
		private SystemMenu systemMenu;

		private CommandLineOption fromErrorDlgOption;
		private CommandLineOption fromShortcutOption;
		private CommandLineOption logPathOption;
		private CommandLineOption endTimeOption;

		private List<Control> views = new List<Control>();

		#endregion Private data

		#region Constructors

		public MainForm()
		{
			Instance = this;

			InitializeComponent();

			// Set the XML file's build action to "Embedded Resource" and "Never copy" for this to work.
			Tx.LoadFromEmbeddedResource("Unclassified.LogSubmit.LogSubmit.txd");
			TxDictionaryBinding.AddTextBindings(this);

			// Initialise views
			logSelectionView = new LogSelectionView();
			timeSelectionView = new TimeSelectionView();
			notesView = new NotesView();
			compressView = new CompressView();
			transportView = new TransportView();
			transportProgressView = new TransportProgressView();

			views.Add(logSelectionView);
			views.Add(timeSelectionView);
			views.Add(notesView);
			views.Add(compressView);
			views.Add(transportView);
			views.Add(transportProgressView);

			// Read configuration file
			string configFile = Path.Combine(
				Path.GetDirectoryName(Application.ExecutablePath),
				"submit.conf");
			try
			{
				ConfigReader config = new ConfigReader(configFile);
				config.Read();
			}
			catch (Exception ex)
			{
				logSelectionView.SetConfigError(ex);
			}

			// Other initialisation
			Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
			UIPreferences.UpdateFormFont(this, Font, SystemFonts.MessageBoxFont);
			USizeGrip.AddToForm(this);

			systemMenu = new SystemMenu(this);
			systemMenu.AddCommand(Tx.T("menu.about"), OnSysMenuAbout, true);

			progressPanel = new Panel();
			progressPanel.Left = 0;
			progressPanel.Top = 0;
			progressPanel.Width = 0;
			progressPanel.Height = 2;
			//progressPanel.BackColor = SystemColors.Highlight;
			progressPanel.BackColor = Color.Gray;
			Controls.Add(progressPanel);
			progressPanel.BringToFront();

			// Parse command line arguments
			CommandLineReader cmdLine = new CommandLineReader();
			fromErrorDlgOption = cmdLine.RegisterOption("errordlg");
			fromShortcutOption = cmdLine.RegisterOption("shortcut");
			logPathOption = cmdLine.RegisterOption("logpath", 1);
			endTimeOption = cmdLine.RegisterOption("endtime", 1);

			try
			{
				cmdLine.Parse();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					Tx.T("msg.command line error", "msg", ex.Message),
					Tx.T("msg.title.error"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}

			SharedData.Instance.FromErrorDialog = fromErrorDlgOption.IsSet;
			SharedData.Instance.FromShortcut = fromShortcutOption.IsSet;

			if (logPathOption.IsSet)
			{
				try
				{
					logSelectionView.SetLogBasePath(logPathOption.Value);
				}
				catch
				{
					MessageBox.Show(
						Tx.T("msg.logpath parameter invalid", "value", logPathOption.Value),
						Tx.T("msg.title.error"),
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);
					logSelectionView.ResetLogBasePath();
					logSelectionView.FindLogBasePath();
				}
			}
			else
			{
				logSelectionView.FindLogBasePath();
			}

			if (endTimeOption.IsSet)
			{
				try
				{
					appStartTime = DateTime.Parse(endTimeOption.Value);
					SharedData.Instance.LastLogUpdateTime = appStartTime;
				}
				catch
				{
					MessageBox.Show(
						Tx.T("msg.endtime parameter invalid", "value", endTimeOption.Value),
						Tx.T("msg.title.error"),
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);
				}
			}

			// Set start view
			SetView(logSelectionView, true);
		}

		#endregion Constructors

		#region Form event handlers

		private void MainForm_FontChanged(object sender, EventArgs args)
		{
			foreach (var view in views)
			{
				UIPreferences.UpdateFont(view, view.Font, Font);
			}
		}

		private void MainForm_SizeChanged(object sender, EventArgs args)
		{
			UpdateProgress();
		}

		private void MainForm_Shown(object sender, EventArgs args)
		{
			NextButton.Focus();
			logSelectionView.TakeFocus();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs args)
		{
			if (!FinishEnabled && fromErrorDlgOption.IsSet)
			{
				switch (MessageBox.Show(
					Tx.T("msg.cancel before submit", "cmd", Path.GetFileNameWithoutExtension(Application.ExecutablePath)),
					Text,
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question))
				{
					case DialogResult.Yes:
						// Create the desktop shortcut
						string arguments = "/shortcut";
						if (logPathOption.IsSet)
						{
							arguments += " /logpath \"" + logPathOption.Value + "\"";
						}
						arguments += " /endtime " + appStartTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
						string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
						string linkFile = PathUtil.GetUniqueFileName(Path.Combine(desktopDir, Tx.T("shortcut.name") + ".lnk"));
						ShellLinkHelper.CreateLink(linkFile, Application.ExecutablePath, arguments);
						break;
					case DialogResult.No:
						// Nothing to do, just quit
						break;
					default:
						// Do not quit (unless Windows shutdown)
						args.Cancel = args.CloseReason != CloseReason.WindowsShutDown;
						if (args.Cancel) return;   // Don't clean up if we don't quit
						break;
				}
			}

			Cleanup();
		}

		protected override void WndProc(ref Message msg)
		{
			base.WndProc(ref msg);
			systemMenu.HandleMessage(ref msg);
		}

		private void OnSysMenuAbout()
		{
			string msg =
				AssemblyInfoUtil.AppDescription + "\n" +
				Tx.T("about.version") + " " + AssemblyInfoUtil.AppLongVersion + " (" + AssemblyInfoUtil.AppAsmConfiguration + ")\n" +
				AssemblyInfoUtil.AppCopyright + "\n" +
				"http://unclassified.software/source/fieldlog";

			MessageBox.Show(msg, Tx.T("about.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion Form event handlers

		#region Control event handlers

		private void BackButton_Click(object sender, EventArgs args)
		{
			Control view = GetView();
			int index = views.IndexOf(view);
			if (index > 0)
			{
				SetView(views[index - 1], false);
			}
		}

		private void NextButton_Click(object sender, EventArgs args)
		{
			Control view = GetView();
			int index = views.IndexOf(view);
			if (index < views.Count - 1)
			{
				SetView(views[index + 1], true);
			}
		}

		private void MyCancelButton_Click(object sender, EventArgs args)
		{
			Close();
		}

		#endregion Control event handlers

		#region Public methods

		public void SetProgress(bool visible, int value)
		{
			progressSpinner1.Visible = visible;
			progressSpinner1.Spinning = visible;
			progressSpinner1.Value = value;
		}

		public bool BackEnabled
		{
			get { return BackButton.Enabled; }
			set { BackButton.Enabled = value; }
		}

		public bool NextEnabled
		{
			get { return NextButton.Enabled; }
			set { NextButton.Enabled = value; }
		}

		public bool CancelEnabled
		{
			get { return MyCancelButton.Enabled; }
			set { MyCancelButton.Enabled = value; }
		}

		public bool FinishEnabled
		{
			get
			{
				return finishEnabled;
			}
			set
			{
				finishEnabled = value;
				if (finishEnabled)
				{
					MyCancelButton.Text = Tx.T("button.finish");
				}
				else
				{
					MyCancelButton.Text = Tx.T("button.cancel");
				}
				UpdateProgress();
			}
		}

		#endregion Public methods

		#region Private methods

		private Control GetView()
		{
			if (ContentPanel.Controls.Count == 0) return null;
			return ContentPanel.Controls[0];
		}

		private void SetView(Control view, bool forward)
		{
			Control oldView = GetView();
			if (oldView != null)
			{
				if (view == oldView) return;
				((IView) oldView).Deactivate(forward);
			}
			ContentPanel.Controls.Clear();

			ContentPanel.Controls.Add(view);
			((IView) view).Activate(forward);
			UpdateProgress();
		}

		private void UpdateProgress()
		{
			Control view = GetView();
			int index = views.IndexOf(view);
			if (index >= 0)
			{
				//if (FinishEnabled) index++;
				int newWidth = Width * index / (views.Count - 1);
				if (index != prevViewIndex)
				{
					new Animation(
						AnimationTypes.ResizeHoriz,
						progressPanel,
						newWidth - progressPanel.Width,
						null,
						500);
				}
				else
				{
					// The window might have been resized but the view has not changed.
					// Directly setting the width avoids overlapping animation flickering because
					// the size will be changed many times.
					progressPanel.Width = newWidth;
				}
			}
			prevViewIndex = index;
		}

		private void Cleanup()
		{
			// Close all open files and streams
			while (SharedData.Instance.OpenDisposables.Count > 0)
			{
				IDisposable d = SharedData.Instance.OpenDisposables[SharedData.Instance.OpenDisposables.Count - 1];
				if (d != null)
				{
					d.Dispose();
				}
				SharedData.Instance.OpenDisposables.RemoveAt(SharedData.Instance.OpenDisposables.Count - 1);
			}
			// Delete all temporary files (after closing them)
			while (SharedData.Instance.TempFiles.Count > 0)
			{
				string fileName = SharedData.Instance.TempFiles[SharedData.Instance.TempFiles.Count - 1];
				if (fileName != null)
				{
					try
					{
						File.Delete(fileName);
					}
					catch
					{
						// Not important
					}
				}
				SharedData.Instance.TempFiles.RemoveAt(SharedData.Instance.TempFiles.Count - 1);
			}
		}

		#endregion Private methods
	}
}
