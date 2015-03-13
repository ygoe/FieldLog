namespace Unclassified.LogSubmit.Views
{
	partial class CompressView
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
			this.TitleLabel = new System.Windows.Forms.Label();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.label1 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.RemainingTimeLabel = new System.Windows.Forms.Label();
			this.CompressedSizeLabel = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.CompressWorker = new System.ComponentModel.BackgroundWorker();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.TitleLabel, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.progressBar1, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.label3, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.label2, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this.RemainingTimeLabel, 1, 3);
			this.tableLayoutPanel1.Controls.Add(this.CompressedSizeLabel, 1, 4);
			this.tableLayoutPanel1.Controls.Add(this.label4, 0, 5);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 6;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(541, 409);
			this.tableLayoutPanel1.TabIndex = 1;
			// 
			// TitleLabel
			// 
			this.TitleLabel.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.TitleLabel, 2);
			this.TitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TitleLabel.Location = new System.Drawing.Point(0, 0);
			this.TitleLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
			this.TitleLabel.Name = "TitleLabel";
			this.TitleLabel.Size = new System.Drawing.Size(129, 18);
			this.TitleLabel.TabIndex = 1;
			this.TitleLabel.Text = "Compressing data";
			// 
			// progressBar1
			// 
			this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.SetColumnSpan(this.progressBar1, 2);
			this.progressBar1.Location = new System.Drawing.Point(2, 61);
			this.progressBar1.Margin = new System.Windows.Forms.Padding(2, 20, 0, 0);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(539, 16);
			this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.progressBar1.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.label1, 2);
			this.label1.Location = new System.Drawing.Point(0, 28);
			this.label1.Margin = new System.Windows.Forms.Padding(0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(371, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "The log files are being compressed to reduce the size of the data to transport.";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(0, 87);
			this.label3.Margin = new System.Windows.Forms.Padding(0, 10, 0, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(82, 13);
			this.label3.TabIndex = 4;
			this.label3.Text = "Remaining time:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(0, 106);
			this.label2.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(89, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Compressed size:";
			// 
			// RemainingTimeLabel
			// 
			this.RemainingTimeLabel.AutoSize = true;
			this.RemainingTimeLabel.Location = new System.Drawing.Point(93, 87);
			this.RemainingTimeLabel.Margin = new System.Windows.Forms.Padding(4, 10, 0, 0);
			this.RemainingTimeLabel.Name = "RemainingTimeLabel";
			this.RemainingTimeLabel.Size = new System.Drawing.Size(28, 13);
			this.RemainingTimeLabel.TabIndex = 4;
			this.RemainingTimeLabel.Text = "0:00";
			// 
			// CompressedSizeLabel
			// 
			this.CompressedSizeLabel.AutoSize = true;
			this.CompressedSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CompressedSizeLabel.Location = new System.Drawing.Point(93, 106);
			this.CompressedSizeLabel.Margin = new System.Windows.Forms.Padding(4, 6, 0, 0);
			this.CompressedSizeLabel.Name = "CompressedSizeLabel";
			this.CompressedSizeLabel.Size = new System.Drawing.Size(36, 13);
			this.CompressedSizeLabel.TabIndex = 4;
			this.CompressedSizeLabel.Text = "0 MB";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.label4, 2);
			this.label4.Location = new System.Drawing.Point(0, 139);
			this.label4.Margin = new System.Windows.Forms.Padding(0, 20, 0, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(524, 26);
			this.label4.TabIndex = 3;
			this.label4.Text = "Please review the compressed data size. If you need to reduce the size you can go" +
    " back and select a shorter time span or fewer log files.";
			// 
			// CompressWorker
			// 
			this.CompressWorker.WorkerReportsProgress = true;
			this.CompressWorker.WorkerSupportsCancellation = true;
			this.CompressWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.CompressWorker_DoWork);
			this.CompressWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.CompressWorker_ProgressChanged);
			this.CompressWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.CompressWorker_RunWorkerCompleted);
			// 
			// CompressView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.tableLayoutPanel1);
			this.DoubleBuffered = true;
			this.Name = "CompressView";
			this.Size = new System.Drawing.Size(541, 409);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label TitleLabel;
		private System.Windows.Forms.ProgressBar progressBar1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label RemainingTimeLabel;
		private System.Windows.Forms.Label CompressedSizeLabel;
		private System.ComponentModel.BackgroundWorker CompressWorker;
		private System.Windows.Forms.Label label4;
	}
}
