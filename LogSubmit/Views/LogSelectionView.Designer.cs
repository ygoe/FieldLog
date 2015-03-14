namespace Unclassified.LogSubmit.Views
{
	partial class LogSelectionView
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
			this.CurrentLabel = new System.Windows.Forms.Label();
			this.FindLogsButton = new System.Windows.Forms.Button();
			this.LogDirsListView = new System.Windows.Forms.ListView();
			this.LogBasePathHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LastUpdateHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SizeHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SelectedLogDirText = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.ConfigErrorLabel = new System.Windows.Forms.Label();
			this.BrowseLogButton = new System.Windows.Forms.Button();
			this.ScanDirectoryWorker = new System.ComponentModel.BackgroundWorker();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.CurrentLabel, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.FindLogsButton, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this.LogDirsListView, 0, 5);
			this.tableLayoutPanel1.Controls.Add(this.SelectedLogDirText, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.ConfigErrorLabel, 0, 6);
			this.tableLayoutPanel1.Controls.Add(this.BrowseLogButton, 0, 4);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 7;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(521, 434);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// CurrentLabel
			// 
			this.CurrentLabel.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.CurrentLabel, 2);
			this.CurrentLabel.Location = new System.Drawing.Point(0, 61);
			this.CurrentLabel.Margin = new System.Windows.Forms.Padding(0);
			this.CurrentLabel.Name = "CurrentLabel";
			this.CurrentLabel.Size = new System.Drawing.Size(139, 13);
			this.CurrentLabel.TabIndex = 2;
			this.CurrentLabel.Text = "[log selection.selected path]";
			// 
			// FindLogsButton
			// 
			this.FindLogsButton.AutoSize = true;
			this.FindLogsButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.FindLogsButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.FindLogsButton.Location = new System.Drawing.Point(110, 106);
			this.FindLogsButton.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
			this.FindLogsButton.Name = "FindLogsButton";
			this.FindLogsButton.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.FindLogsButton.Size = new System.Drawing.Size(136, 22);
			this.FindLogsButton.TabIndex = 5;
			this.FindLogsButton.Text = "[log selection.find logs]";
			this.FindLogsButton.UseVisualStyleBackColor = true;
			this.FindLogsButton.Click += new System.EventHandler(this.FindLogsButton_Click);
			// 
			// LogDirsListView
			// 
			this.LogDirsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.LogBasePathHeader,
            this.LastUpdateHeader,
            this.SizeHeader});
			this.tableLayoutPanel1.SetColumnSpan(this.LogDirsListView, 2);
			this.LogDirsListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.LogDirsListView.FullRowSelect = true;
			this.LogDirsListView.HideSelection = false;
			this.LogDirsListView.Location = new System.Drawing.Point(2, 134);
			this.LogDirsListView.Margin = new System.Windows.Forms.Padding(2, 6, 0, 0);
			this.LogDirsListView.Name = "LogDirsListView";
			this.LogDirsListView.Size = new System.Drawing.Size(519, 273);
			this.LogDirsListView.TabIndex = 6;
			this.LogDirsListView.UseCompatibleStateImageBehavior = false;
			this.LogDirsListView.View = System.Windows.Forms.View.Details;
			this.LogDirsListView.Visible = false;
			this.LogDirsListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.LogDirsListView_ColumnClick);
			this.LogDirsListView.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.LogDirsListView_ColumnWidthChanging);
			this.LogDirsListView.SelectedIndexChanged += new System.EventHandler(this.LogDirsListView_SelectedIndexChanged);
			this.LogDirsListView.ClientSizeChanged += new System.EventHandler(this.LogDirsListView_ClientSizeChanged);
			// 
			// LogBasePathHeader
			// 
			this.LogBasePathHeader.Text = "Log base path";
			this.LogBasePathHeader.Width = 150;
			// 
			// LastUpdateHeader
			// 
			this.LastUpdateHeader.Text = "Last update";
			this.LastUpdateHeader.Width = 90;
			// 
			// SizeHeader
			// 
			this.SizeHeader.Text = "Size";
			this.SizeHeader.Width = 70;
			// 
			// SelectedLogDirText
			// 
			this.SelectedLogDirText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.SetColumnSpan(this.SelectedLogDirText, 2);
			this.SelectedLogDirText.Location = new System.Drawing.Point(2, 80);
			this.SelectedLogDirText.Margin = new System.Windows.Forms.Padding(2, 6, 0, 6);
			this.SelectedLogDirText.Name = "SelectedLogDirText";
			this.SelectedLogDirText.ReadOnly = true;
			this.SelectedLogDirText.Size = new System.Drawing.Size(519, 20);
			this.SelectedLogDirText.TabIndex = 3;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.label1, 2);
			this.label1.Location = new System.Drawing.Point(0, 38);
			this.label1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(95, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "[log selection.intro]";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.label2, 2);
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(0, 0);
			this.label2.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(125, 18);
			this.label2.TabIndex = 0;
			this.label2.Text = "[log selection.title]";
			// 
			// ConfigErrorLabel
			// 
			this.ConfigErrorLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ConfigErrorLabel.AutoSize = true;
			this.ConfigErrorLabel.BackColor = System.Drawing.Color.LemonChiffon;
			this.tableLayoutPanel1.SetColumnSpan(this.ConfigErrorLabel, 2);
			this.ConfigErrorLabel.Location = new System.Drawing.Point(0, 417);
			this.ConfigErrorLabel.Margin = new System.Windows.Forms.Padding(0, 10, 0, 0);
			this.ConfigErrorLabel.Name = "ConfigErrorLabel";
			this.ConfigErrorLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			this.ConfigErrorLabel.Size = new System.Drawing.Size(521, 17);
			this.ConfigErrorLabel.TabIndex = 7;
			this.ConfigErrorLabel.Text = "ConfigErrorLabel";
			this.ConfigErrorLabel.Visible = false;
			// 
			// BrowseLogButton
			// 
			this.BrowseLogButton.AutoSize = true;
			this.BrowseLogButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.BrowseLogButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.BrowseLogButton.Location = new System.Drawing.Point(2, 106);
			this.BrowseLogButton.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
			this.BrowseLogButton.Name = "BrowseLogButton";
			this.BrowseLogButton.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.BrowseLogButton.Size = new System.Drawing.Size(102, 22);
			this.BrowseLogButton.TabIndex = 4;
			this.BrowseLogButton.Text = "[button.browse]";
			this.BrowseLogButton.UseVisualStyleBackColor = true;
			this.BrowseLogButton.Click += new System.EventHandler(this.BrowseLogButton_Click);
			// 
			// ScanDirectoryWorker
			// 
			this.ScanDirectoryWorker.WorkerSupportsCancellation = true;
			this.ScanDirectoryWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.ScanDirectoryWorker_DoWork);
			this.ScanDirectoryWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.ScanDirectoryWorker_RunWorkerCompleted);
			// 
			// LogSelectionView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.tableLayoutPanel1);
			this.DoubleBuffered = true;
			this.Name = "LogSelectionView";
			this.Size = new System.Drawing.Size(521, 434);
			this.FontChanged += new System.EventHandler(this.LogSelectionView_FontChanged);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label CurrentLabel;
		private System.Windows.Forms.TextBox SelectedLogDirText;
		private System.Windows.Forms.Button FindLogsButton;
		private System.Windows.Forms.ListView LogDirsListView;
		private System.Windows.Forms.ColumnHeader LogBasePathHeader;
		private System.Windows.Forms.ColumnHeader LastUpdateHeader;
		private System.ComponentModel.BackgroundWorker ScanDirectoryWorker;
		private System.Windows.Forms.ColumnHeader SizeHeader;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label ConfigErrorLabel;
		private System.Windows.Forms.Button BrowseLogButton;
	}
}
