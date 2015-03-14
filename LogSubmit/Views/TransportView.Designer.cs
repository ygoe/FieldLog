namespace Unclassified.LogSubmit.Views
{
	partial class TransportView
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
			this.label1 = new System.Windows.Forms.Label();
			this.TransportComboBox = new System.Windows.Forms.ComboBox();
			this.ContentPanel = new System.Windows.Forms.Panel();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.TitleLabel, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.TransportComboBox, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.ContentPanel, 0, 3);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 4;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(472, 296);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// TitleLabel
			// 
			this.TitleLabel.AutoSize = true;
			this.TitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TitleLabel.Location = new System.Drawing.Point(0, 0);
			this.TitleLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
			this.TitleLabel.Name = "TitleLabel";
			this.TitleLabel.Size = new System.Drawing.Size(134, 18);
			this.TitleLabel.TabIndex = 0;
			this.TitleLabel.Text = "[transport view.title]";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(0, 28);
			this.label1.Margin = new System.Windows.Forms.Padding(0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(102, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "[transport view.intro]";
			// 
			// TransportComboBox
			// 
			this.TransportComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.TransportComboBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.TransportComboBox.FormattingEnabled = true;
			this.TransportComboBox.Location = new System.Drawing.Point(2, 47);
			this.TransportComboBox.Margin = new System.Windows.Forms.Padding(2, 6, 0, 0);
			this.TransportComboBox.Name = "TransportComboBox";
			this.TransportComboBox.Size = new System.Drawing.Size(163, 21);
			this.TransportComboBox.TabIndex = 2;
			this.TransportComboBox.SelectedIndexChanged += new System.EventHandler(this.TransportComboBox_SelectedIndexChanged);
			// 
			// ContentPanel
			// 
			this.ContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ContentPanel.Location = new System.Drawing.Point(0, 74);
			this.ContentPanel.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.ContentPanel.Name = "ContentPanel";
			this.ContentPanel.Size = new System.Drawing.Size(472, 222);
			this.ContentPanel.TabIndex = 3;
			// 
			// TransportView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.tableLayoutPanel1);
			this.DoubleBuffered = true;
			this.Name = "TransportView";
			this.Size = new System.Drawing.Size(472, 296);
			this.FontChanged += new System.EventHandler(this.TransportView_FontChanged);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label TitleLabel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox TransportComboBox;
		private System.Windows.Forms.Panel ContentPanel;
	}
}
