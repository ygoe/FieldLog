namespace Unclassified.LogSubmit.Views
{
	partial class TimeSelectionView
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TimeSelectionView));
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.TitleLabel = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.LastUpdateTimeLabel = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.LogMinTimeLabel = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.TimeTrackBar = new System.Windows.Forms.TrackBar();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.TimeSpanLabel = new System.Windows.Forms.Label();
			this.WebLinkLabel = new Unclassified.UI.ULinkLabel();
			this.tableLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TimeTrackBar)).BeginInit();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.TitleLabel, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.label2, 0, 5);
			this.tableLayoutPanel1.Controls.Add(this.LastUpdateTimeLabel, 1, 5);
			this.tableLayoutPanel1.Controls.Add(this.label3, 0, 6);
			this.tableLayoutPanel1.Controls.Add(this.LogMinTimeLabel, 1, 6);
			this.tableLayoutPanel1.Controls.Add(this.label4, 0, 7);
			this.tableLayoutPanel1.Controls.Add(this.TimeTrackBar, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.label5, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.label6, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.TimeSpanLabel, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this.WebLinkLabel, 0, 8);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 9;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(480, 283);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// TitleLabel
			// 
			this.TitleLabel.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.TitleLabel, 2);
			this.TitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TitleLabel.Location = new System.Drawing.Point(0, 0);
			this.TitleLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
			this.TitleLabel.Name = "TitleLabel";
			this.TitleLabel.Size = new System.Drawing.Size(206, 18);
			this.TitleLabel.TabIndex = 0;
			this.TitleLabel.Text = "Select the time span to submit";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.label1, 2);
			this.label1.Location = new System.Drawing.Point(0, 28);
			this.label1.Margin = new System.Windows.Forms.Padding(0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(469, 52);
			this.label1.TabIndex = 1;
			this.label1.Text = resources.GetString("label1.Text");
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(0, 158);
			this.label2.Margin = new System.Windows.Forms.Padding(0, 10, 0, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(199, 13);
			this.label2.TabIndex = 6;
			this.label2.Text = "Last update time of the selected log files:";
			// 
			// LastUpdateTimeLabel
			// 
			this.LastUpdateTimeLabel.AutoSize = true;
			this.LastUpdateTimeLabel.Location = new System.Drawing.Point(203, 158);
			this.LastUpdateTimeLabel.Margin = new System.Windows.Forms.Padding(4, 10, 0, 0);
			this.LastUpdateTimeLabel.Name = "LastUpdateTimeLabel";
			this.LastUpdateTimeLabel.Size = new System.Drawing.Size(111, 13);
			this.LastUpdateTimeLabel.TabIndex = 7;
			this.LastUpdateTimeLabel.Text = "LastUpdateTimeLabel";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(0, 177);
			this.label3.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(163, 13);
			this.label3.TabIndex = 8;
			this.label3.Text = "Latest submitted log starting time:";
			// 
			// LogMinTimeLabel
			// 
			this.LogMinTimeLabel.AutoSize = true;
			this.LogMinTimeLabel.Location = new System.Drawing.Point(203, 177);
			this.LogMinTimeLabel.Margin = new System.Windows.Forms.Padding(4, 6, 0, 0);
			this.LogMinTimeLabel.Name = "LogMinTimeLabel";
			this.LogMinTimeLabel.Size = new System.Drawing.Size(91, 13);
			this.LogMinTimeLabel.TabIndex = 9;
			this.LogMinTimeLabel.Text = "LogMinTimeLabel";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.label4, 2);
			this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.ForeColor = System.Drawing.SystemColors.GrayText;
			this.label4.Location = new System.Drawing.Point(0, 210);
			this.label4.Margin = new System.Windows.Forms.Padding(0, 20, 0, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(480, 26);
			this.label4.TabIndex = 10;
			this.label4.Text = "Note: The submitted log data can begin before the displayed starting time because" +
    " only complete log files can be submitted.";
			// 
			// TimeTrackBar
			// 
			this.TimeTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TimeTrackBar.AutoSize = false;
			this.tableLayoutPanel1.SetColumnSpan(this.TimeTrackBar, 2);
			this.TimeTrackBar.LargeChange = 1;
			this.TimeTrackBar.Location = new System.Drawing.Point(0, 109);
			this.TimeTrackBar.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.TimeTrackBar.Maximum = 14;
			this.TimeTrackBar.Name = "TimeTrackBar";
			this.TimeTrackBar.Size = new System.Drawing.Size(480, 26);
			this.TimeTrackBar.TabIndex = 4;
			this.TimeTrackBar.ValueChanged += new System.EventHandler(this.TimeTrackBar_ValueChanged);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(0, 90);
			this.label5.Margin = new System.Windows.Forms.Padding(0, 10, 0, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(40, 13);
			this.label5.TabIndex = 2;
			this.label5.Text = "Longer";
			// 
			// label6
			// 
			this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(439, 90);
			this.label6.Margin = new System.Windows.Forms.Padding(0, 10, 0, 0);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(41, 13);
			this.label6.TabIndex = 3;
			this.label6.Text = "Shorter";
			// 
			// TimeSpanLabel
			// 
			this.TimeSpanLabel.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.TimeSpanLabel, 2);
			this.TimeSpanLabel.Location = new System.Drawing.Point(0, 135);
			this.TimeSpanLabel.Margin = new System.Windows.Forms.Padding(0);
			this.TimeSpanLabel.Name = "TimeSpanLabel";
			this.TimeSpanLabel.Size = new System.Drawing.Size(81, 13);
			this.TimeSpanLabel.TabIndex = 5;
			this.TimeSpanLabel.Text = "TimeSpanLabel";
			// 
			// WebLinkLabel
			// 
			this.WebLinkLabel.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.WebLinkLabel, 2);
			this.WebLinkLabel.Location = new System.Drawing.Point(0, 242);
			this.WebLinkLabel.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.WebLinkLabel.Name = "WebLinkLabel";
			this.WebLinkLabel.Size = new System.Drawing.Size(240, 13);
			this.WebLinkLabel.TabIndex = 11;
			this.WebLinkLabel.TabStop = true;
			this.WebLinkLabel.Text = "Learn more about how FieldLog manages log files";
			this.WebLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.WebLinkLabel_LinkClicked);
			// 
			// TimeSelectionView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.tableLayoutPanel1);
			this.DoubleBuffered = true;
			this.Name = "TimeSelectionView";
			this.Size = new System.Drawing.Size(480, 283);
			this.SizeChanged += new System.EventHandler(this.TimeSelectionView_SizeChanged);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TimeTrackBar)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label TitleLabel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label LastUpdateTimeLabel;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label LogMinTimeLabel;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TrackBar TimeTrackBar;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label TimeSpanLabel;
		private Unclassified.UI.ULinkLabel WebLinkLabel;
	}
}
