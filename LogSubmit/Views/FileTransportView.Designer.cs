namespace Unclassified.LogSubmit.Views
{
	partial class FileTransportView
	{
		/// <summary> 
		/// Erforderliche Designervariable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Verwendete Ressourcen bereinigen.
		/// </summary>
		/// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Vom Komponenten-Designer generierter Code

		/// <summary> 
		/// Erforderliche Methode für die Designerunterstützung. 
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent()
		{
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.label1 = new System.Windows.Forms.Label();
			this.FileNameTextBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.BrowseButton = new System.Windows.Forms.Button();
			this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			this.FileDragPictureBox = new System.Windows.Forms.PictureBox();
			this.DragInfoLabel = new System.Windows.Forms.Label();
			this.tableLayoutPanel1.SuspendLayout();
			this.tableLayoutPanel2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.FileDragPictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.FileNameTextBox, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.BrowseButton, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 3);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 5;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(498, 225);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(0, 36);
			this.label1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(108, 18);
			this.label1.TabIndex = 1;
			this.label1.Text = "&Destination file name:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// FileNameTextBox
			// 
			this.FileNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.FileNameTextBox.Location = new System.Drawing.Point(112, 36);
			this.FileNameTextBox.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
			this.FileNameTextBox.Name = "FileNameTextBox";
			this.FileNameTextBox.Size = new System.Drawing.Size(386, 20);
			this.FileNameTextBox.TabIndex = 2;
			this.FileNameTextBox.TextChanged += new System.EventHandler(this.FileNameTextBox_TextChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.label2, 2);
			this.label2.Location = new System.Drawing.Point(0, 0);
			this.label2.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(484, 26);
			this.label2.TabIndex = 0;
			this.label2.Text = "Copies the log archive to a local disk on your computer. This may also be on a po" +
    "rtable or USB drive. You need to carry the file to us yourself then, or transfer" +
    " it from another computer.";
			// 
			// BrowseButton
			// 
			this.BrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BrowseButton.AutoSize = true;
			this.BrowseButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.BrowseButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.BrowseButton.Location = new System.Drawing.Point(425, 60);
			this.BrowseButton.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
			this.BrowseButton.Name = "BrowseButton";
			this.BrowseButton.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.BrowseButton.Size = new System.Drawing.Size(73, 22);
			this.BrowseButton.TabIndex = 3;
			this.BrowseButton.Text = "B&rowse...";
			this.BrowseButton.UseVisualStyleBackColor = true;
			this.BrowseButton.Click += new System.EventHandler(this.BrowseButton_Click);
			// 
			// tableLayoutPanel2
			// 
			this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel2.AutoSize = true;
			this.tableLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanel2.ColumnCount = 2;
			this.tableLayoutPanel1.SetColumnSpan(this.tableLayoutPanel2, 2);
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel2.Controls.Add(this.FileDragPictureBox, 0, 0);
			this.tableLayoutPanel2.Controls.Add(this.DragInfoLabel, 1, 0);
			this.tableLayoutPanel2.Location = new System.Drawing.Point(2, 102);
			this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(2, 20, 0, 0);
			this.tableLayoutPanel2.Name = "tableLayoutPanel2";
			this.tableLayoutPanel2.RowCount = 1;
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel2.Size = new System.Drawing.Size(496, 54);
			this.tableLayoutPanel2.TabIndex = 5;
			// 
			// FileDragPictureBox
			// 
			this.FileDragPictureBox.Image = global::Unclassified.LogSubmit.Properties.Resources.FLDocument_48;
			this.FileDragPictureBox.Location = new System.Drawing.Point(0, 0);
			this.FileDragPictureBox.Margin = new System.Windows.Forms.Padding(0);
			this.FileDragPictureBox.Name = "FileDragPictureBox";
			this.FileDragPictureBox.Size = new System.Drawing.Size(72, 54);
			this.FileDragPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.FileDragPictureBox.TabIndex = 4;
			this.FileDragPictureBox.TabStop = false;
			this.FileDragPictureBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FileDragPictureBox_MouseDown);
			// 
			// DragInfoLabel
			// 
			this.DragInfoLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.DragInfoLabel.AutoSize = true;
			this.DragInfoLabel.Location = new System.Drawing.Point(72, 0);
			this.DragInfoLabel.Margin = new System.Windows.Forms.Padding(0);
			this.DragInfoLabel.Name = "DragInfoLabel";
			this.DragInfoLabel.Size = new System.Drawing.Size(383, 54);
			this.DragInfoLabel.TabIndex = 0;
			this.DragInfoLabel.Text = "Tip:\r\nAlternatively, you can drag the archive from this icon and drop it directly" +
    " into the destination directory or application, then click the “Finish” button.";
			this.DragInfoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.DragInfoLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.DragInfoLabel_MouseDown);
			// 
			// FileTransportView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.tableLayoutPanel1);
			this.DoubleBuffered = true;
			this.Name = "FileTransportView";
			this.Size = new System.Drawing.Size(498, 225);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.tableLayoutPanel2.ResumeLayout(false);
			this.tableLayoutPanel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.FileDragPictureBox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox FileNameTextBox;
		private System.Windows.Forms.Button BrowseButton;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label DragInfoLabel;
		private System.Windows.Forms.PictureBox FileDragPictureBox;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
	}
}
