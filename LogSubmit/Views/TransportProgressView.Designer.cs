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
			this.FinishedInfoLabel = new System.Windows.Forms.Label();
			this.SuccessPanel = new System.Windows.Forms.TableLayoutPanel();
			this.label4 = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.ErrorPanel = new System.Windows.Forms.TableLayoutPanel();
			this.ErrorLabel = new System.Windows.Forms.Label();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			this.TransportWorker = new System.ComponentModel.BackgroundWorker();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuccessPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.ErrorPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
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
			this.tableLayoutPanel1.Controls.Add(this.FinishedInfoLabel, 0, 7);
			this.tableLayoutPanel1.Controls.Add(this.SuccessPanel, 0, 5);
			this.tableLayoutPanel1.Controls.Add(this.ErrorPanel, 0, 4);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 8;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
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
			this.progressBar1.Maximum = 1000;
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
			// FinishedInfoLabel
			// 
			this.FinishedInfoLabel.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.FinishedInfoLabel, 2);
			this.FinishedInfoLabel.Location = new System.Drawing.Point(0, 194);
			this.FinishedInfoLabel.Margin = new System.Windows.Forms.Padding(0, 20, 0, 0);
			this.FinishedInfoLabel.Name = "FinishedInfoLabel";
			this.FinishedInfoLabel.Size = new System.Drawing.Size(90, 13);
			this.FinishedInfoLabel.TabIndex = 7;
			this.FinishedInfoLabel.Text = "FinishedInfoLabel";
			this.FinishedInfoLabel.Visible = false;
			// 
			// SuccessPanel
			// 
			this.SuccessPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SuccessPanel.AutoSize = true;
			this.SuccessPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.SuccessPanel.ColumnCount = 2;
			this.tableLayoutPanel1.SetColumnSpan(this.SuccessPanel, 2);
			this.SuccessPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.SuccessPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.SuccessPanel.Controls.Add(this.label4, 1, 0);
			this.SuccessPanel.Controls.Add(this.pictureBox1, 0, 0);
			this.SuccessPanel.Location = new System.Drawing.Point(0, 158);
			this.SuccessPanel.Margin = new System.Windows.Forms.Padding(0, 20, 0, 0);
			this.SuccessPanel.Name = "SuccessPanel";
			this.SuccessPanel.RowCount = 1;
			this.SuccessPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.SuccessPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 16F));
			this.SuccessPanel.Size = new System.Drawing.Size(421, 16);
			this.SuccessPanel.TabIndex = 8;
			this.SuccessPanel.Visible = false;
			// 
			// label4
			// 
			this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.label4.AutoSize = true;
			this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(160)))), ((int)(((byte)(0)))));
			this.label4.Location = new System.Drawing.Point(30, 0);
			this.label4.Margin = new System.Windows.Forms.Padding(0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(164, 16);
			this.label4.TabIndex = 7;
			this.label4.Text = "[transport progress view.success]";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::Unclassified.LogSubmit.Properties.Resources.check_green;
			this.pictureBox1.Location = new System.Drawing.Point(2, 0);
			this.pictureBox1.Margin = new System.Windows.Forms.Padding(2, 0, 8, 0);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(20, 16);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.pictureBox1.TabIndex = 8;
			this.pictureBox1.TabStop = false;
			// 
			// ErrorPanel
			// 
			this.ErrorPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ErrorPanel.AutoSize = true;
			this.ErrorPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ErrorPanel.ColumnCount = 2;
			this.tableLayoutPanel1.SetColumnSpan(this.ErrorPanel, 2);
			this.ErrorPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ErrorPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.ErrorPanel.Controls.Add(this.ErrorLabel, 1, 0);
			this.ErrorPanel.Controls.Add(this.pictureBox2, 0, 0);
			this.ErrorPanel.Location = new System.Drawing.Point(0, 120);
			this.ErrorPanel.Margin = new System.Windows.Forms.Padding(0, 20, 0, 0);
			this.ErrorPanel.Name = "ErrorPanel";
			this.ErrorPanel.RowCount = 1;
			this.ErrorPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ErrorPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 18F));
			this.ErrorPanel.Size = new System.Drawing.Size(421, 18);
			this.ErrorPanel.TabIndex = 8;
			this.ErrorPanel.Visible = false;
			// 
			// ErrorLabel
			// 
			this.ErrorLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.ErrorLabel.AutoSize = true;
			this.ErrorLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
			this.ErrorLabel.Location = new System.Drawing.Point(28, 0);
			this.ErrorLabel.Margin = new System.Windows.Forms.Padding(0);
			this.ErrorLabel.Name = "ErrorLabel";
			this.ErrorLabel.Size = new System.Drawing.Size(55, 18);
			this.ErrorLabel.TabIndex = 7;
			this.ErrorLabel.Text = "ErrorLabel";
			this.ErrorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// pictureBox2
			// 
			this.pictureBox2.Image = global::Unclassified.LogSubmit.Properties.Resources.error_red;
			this.pictureBox2.Location = new System.Drawing.Point(2, 0);
			this.pictureBox2.Margin = new System.Windows.Forms.Padding(2, 0, 8, 0);
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.Size = new System.Drawing.Size(18, 18);
			this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.pictureBox2.TabIndex = 8;
			this.pictureBox2.TabStop = false;
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
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.tableLayoutPanel1);
			this.DoubleBuffered = true;
			this.Name = "TransportProgressView";
			this.Size = new System.Drawing.Size(421, 314);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.SuccessPanel.ResumeLayout(false);
			this.SuccessPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ErrorPanel.ResumeLayout(false);
			this.ErrorPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
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
		private System.Windows.Forms.Label FinishedInfoLabel;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TableLayoutPanel SuccessPanel;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.TableLayoutPanel ErrorPanel;
		private System.Windows.Forms.Label ErrorLabel;
		private System.Windows.Forms.PictureBox pictureBox2;
	}
}
