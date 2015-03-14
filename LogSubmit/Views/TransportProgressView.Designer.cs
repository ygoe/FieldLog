namespace Unclassified.LogSubmit.Views
{
	partial class TransportProgressView
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
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.TitleLabel = new System.Windows.Forms.Label();
			this.RemainingTimeLabel = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.FinishedLabel = new System.Windows.Forms.Label();
			this.TransportWorker = new System.ComponentModel.BackgroundWorker();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.progressBar1, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.TitleLabel, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.RemainingTimeLabel, 1, 3);
			this.tableLayoutPanel1.Controls.Add(this.label3, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.FinishedLabel, 0, 4);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 5;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(421, 314);
			this.tableLayoutPanel1.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.label1, 2);
			this.label1.Location = new System.Drawing.Point(0, 28);
			this.label1.Margin = new System.Windows.Forms.Padding(0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(145, 13);
			this.label1.TabIndex = 7;
			this.label1.Text = "[transport progress view.intro]";
			// 
			// progressBar1
			// 
			this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.SetColumnSpan(this.progressBar1, 2);
			this.progressBar1.Location = new System.Drawing.Point(2, 61);
			this.progressBar1.Margin = new System.Windows.Forms.Padding(2, 20, 0, 0);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(419, 16);
			this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.progressBar1.TabIndex = 3;
			// 
			// TitleLabel
			// 
			this.TitleLabel.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.TitleLabel, 2);
			this.TitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TitleLabel.Location = new System.Drawing.Point(0, 0);
			this.TitleLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
			this.TitleLabel.Name = "TitleLabel";
			this.TitleLabel.Size = new System.Drawing.Size(197, 18);
			this.TitleLabel.TabIndex = 1;
			this.TitleLabel.Text = "[transport progress view.title]";
			// 
			// RemainingTimeLabel
			// 
			this.RemainingTimeLabel.AutoSize = true;
			this.RemainingTimeLabel.Location = new System.Drawing.Point(106, 87);
			this.RemainingTimeLabel.Margin = new System.Windows.Forms.Padding(4, 10, 0, 0);
			this.RemainingTimeLabel.Name = "RemainingTimeLabel";
			this.RemainingTimeLabel.Size = new System.Drawing.Size(28, 13);
			this.RemainingTimeLabel.TabIndex = 6;
			this.RemainingTimeLabel.Text = "0:00";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(0, 87);
			this.label3.Margin = new System.Windows.Forms.Padding(0, 10, 0, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(102, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "[msg.remaining time]";
			// 
			// FinishedLabel
			// 
			this.FinishedLabel.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.FinishedLabel, 2);
			this.FinishedLabel.Location = new System.Drawing.Point(0, 120);
			this.FinishedLabel.Margin = new System.Windows.Forms.Padding(0, 20, 0, 0);
			this.FinishedLabel.Name = "FinishedLabel";
			this.FinishedLabel.Size = new System.Drawing.Size(72, 13);
			this.FinishedLabel.TabIndex = 7;
			this.FinishedLabel.Text = "FinishedLabel";
			this.FinishedLabel.Visible = false;
			// 
			// TransportWorker
			// 
			this.TransportWorker.WorkerReportsProgress = true;
			this.TransportWorker.WorkerSupportsCancellation = true;
			this.TransportWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.TransportWorker_DoWork);
			this.TransportWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.TransportWorker_ProgressChanged);
			this.TransportWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.TransportWorker_RunWorkerCompleted);
			// 
			// TransportProgressView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.tableLayoutPanel1);
			this.DoubleBuffered = true;
			this.Name = "TransportProgressView";
			this.Size = new System.Drawing.Size(421, 314);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label TitleLabel;
		private System.Windows.Forms.ProgressBar progressBar1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label RemainingTimeLabel;
		private System.ComponentModel.BackgroundWorker TransportWorker;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label FinishedLabel;
	}
}
