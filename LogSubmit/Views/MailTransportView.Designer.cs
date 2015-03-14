namespace Unclassified.LogSubmit.Views
{
	partial class MailTransportView
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
			this.label2 = new System.Windows.Forms.Label();
			this.InteractiveCheckBox = new System.Windows.Forms.CheckBox();
			this.DirectInfoLabel = new System.Windows.Forms.Label();
			this.SizeWarningLabel = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.InteractiveCheckBox, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.DirectInfoLabel, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this.SizeWarningLabel, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 5);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 6;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(439, 344);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(0, 0);
			this.label2.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(123, 13);
			this.label2.TabIndex = 0;
			this.label2.Text = "[mail transport view.intro]";
			// 
			// InteractiveCheckBox
			// 
			this.InteractiveCheckBox.AutoSize = true;
			this.InteractiveCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.InteractiveCheckBox.Location = new System.Drawing.Point(3, 50);
			this.InteractiveCheckBox.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.InteractiveCheckBox.Name = "InteractiveCheckBox";
			this.InteractiveCheckBox.Size = new System.Drawing.Size(191, 18);
			this.InteractiveCheckBox.TabIndex = 2;
			this.InteractiveCheckBox.Text = "[mail transport view.review option]";
			this.InteractiveCheckBox.UseVisualStyleBackColor = true;
			this.InteractiveCheckBox.CheckedChanged += new System.EventHandler(this.InteractiveCheckBox_CheckedChanged);
			// 
			// DirectInfoLabel
			// 
			this.DirectInfoLabel.AutoSize = true;
			this.DirectInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.DirectInfoLabel.ForeColor = System.Drawing.SystemColors.GrayText;
			this.DirectInfoLabel.Location = new System.Drawing.Point(0, 72);
			this.DirectInfoLabel.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.DirectInfoLabel.Name = "DirectInfoLabel";
			this.DirectInfoLabel.Size = new System.Drawing.Size(166, 13);
			this.DirectInfoLabel.TabIndex = 3;
			this.DirectInfoLabel.Text = "[mail transport view.approve note]";
			// 
			// SizeWarningLabel
			// 
			this.SizeWarningLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SizeWarningLabel.AutoSize = true;
			this.SizeWarningLabel.BackColor = System.Drawing.Color.LemonChiffon;
			this.SizeWarningLabel.Location = new System.Drawing.Point(0, 23);
			this.SizeWarningLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
			this.SizeWarningLabel.Name = "SizeWarningLabel";
			this.SizeWarningLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			this.SizeWarningLabel.Size = new System.Drawing.Size(439, 17);
			this.SizeWarningLabel.TabIndex = 1;
			this.SizeWarningLabel.Text = "[mail transport view.mail size warning]";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.ForeColor = System.Drawing.SystemColors.GrayText;
			this.label1.Location = new System.Drawing.Point(0, 91);
			this.label1.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(137, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "[mail transport view.privacy]";
			// 
			// MailTransportView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.tableLayoutPanel1);
			this.DoubleBuffered = true;
			this.Name = "MailTransportView";
			this.Size = new System.Drawing.Size(439, 344);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox InteractiveCheckBox;
		private System.Windows.Forms.Label DirectInfoLabel;
		private System.Windows.Forms.Label SizeWarningLabel;
		private System.Windows.Forms.Label label1;
	}
}
