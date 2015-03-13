using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unclassified.LogSubmit.Transports;
using System.IO;
using Unclassified.Util;

namespace Unclassified.LogSubmit.Views
{
	public partial class FileTransportView : UserControl, IView
	{
		#region Private data

		private FileTransport transport = new FileTransport();

		#endregion Private data

		#region Constructors

		public FileTransportView()
		{
			InitializeComponent();

			Dock = DockStyle.Fill;
		}

		#endregion Constructors

		#region Public properties

		internal FileTransport Transport { get { return transport; } }

		#endregion Public properties

		#region Control event handlers

		private void FileNameTextBox_TextChanged(object sender, EventArgs args)
		{
			UpdateButtons();
		}

		private void BrowseButton_Click(object sender, EventArgs args)
		{
			using (SaveFileDialog dlg = new SaveFileDialog())
			{
				dlg.DefaultExt = "tar.lzma";
				dlg.FileName = FileNameTextBox.Text;
				dlg.Filter = "Log archives|*.tar.lzma|All files|*.*";
				dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
				dlg.SupportMultiDottedExtensions = true;
				dlg.Title = "Select the log archive destination";

				if (dlg.ShowDialog() == DialogResult.OK)
				{
					FileNameTextBox.Text = dlg.FileName;
				}
			}
		}

		private void FileDragPictureBox_MouseDown(object sender, MouseEventArgs args)
		{
			object obj = new FileDrop(SharedData.Instance.ArchiveFileName);
			DoDragDrop(obj, DragDropEffects.Copy);
			MainForm.Instance.FinishEnabled = true;
		}

		private void DragInfoLabel_MouseDown(object sender, MouseEventArgs args)
		{
			object obj = new FileDrop(SharedData.Instance.ArchiveFileName);
			DoDragDrop(obj, DragDropEffects.Copy);
			MainForm.Instance.FinishEnabled = true;
		}

		#endregion Control event handlers

		#region Public methods

		public void Activate(bool forward)
		{
			if (forward)
			{
				if (!string.IsNullOrEmpty(SharedData.Instance.ArchiveFileName))
				{
					FileNameTextBox.Text = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
						Path.GetFileName(SharedData.Instance.ArchiveFileName));
				}
			}

			UpdateButtons();
		}

		public void Deactivate(bool forward)
		{
			transport.FileName = FileNameTextBox.Text;
			MainForm.Instance.FinishEnabled = false;
		}

		#endregion Public methods

		#region Private methods

		private void UpdateButtons()
		{
			MainForm.Instance.BackEnabled = true;
			MainForm.Instance.NextEnabled = !string.IsNullOrWhiteSpace(FileNameTextBox.Text);
		}

		#endregion Private methods
	}
}
