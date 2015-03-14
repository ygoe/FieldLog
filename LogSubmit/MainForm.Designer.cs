namespace Unclassified.LogSubmit
{
	partial class MainForm
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

		#region Vom Windows Form-Designer generierter Code

		/// <summary>
		/// Erforderliche Methode für die Designerunterstützung.
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.BackButton = new System.Windows.Forms.Button();
			this.NextButton = new System.Windows.Forms.Button();
			this.MyCancelButton = new System.Windows.Forms.Button();
			this.ContentPanel = new System.Windows.Forms.Panel();
			this.progressSpinner1 = new Unclassified.UI.ProgressSpinner();
			this.mouseFilter1 = new Unclassified.MouseFilter(this.components);
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 4;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.BackButton, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.NextButton, 2, 1);
			this.tableLayoutPanel1.Controls.Add(this.MyCancelButton, 3, 1);
			this.tableLayoutPanel1.Controls.Add(this.ContentPanel, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.progressSpinner1, 0, 1);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(8, 11, 10, 10);
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(574, 362);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// BackButton
			// 
			this.BackButton.AutoSize = true;
			this.BackButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.BackButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.BackButton.Location = new System.Drawing.Point(296, 330);
			this.BackButton.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
			this.BackButton.MinimumSize = new System.Drawing.Size(75, 0);
			this.BackButton.Name = "BackButton";
			this.BackButton.Size = new System.Drawing.Size(84, 22);
			this.BackButton.TabIndex = 1;
			this.BackButton.Text = "[button.back]";
			this.BackButton.UseVisualStyleBackColor = true;
			this.BackButton.Click += new System.EventHandler(this.BackButton_Click);
			// 
			// NextButton
			// 
			this.NextButton.AutoSize = true;
			this.NextButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.NextButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.NextButton.Location = new System.Drawing.Point(386, 330);
			this.NextButton.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
			this.NextButton.MinimumSize = new System.Drawing.Size(75, 0);
			this.NextButton.Name = "NextButton";
			this.NextButton.Size = new System.Drawing.Size(80, 22);
			this.NextButton.TabIndex = 2;
			this.NextButton.Text = "[button.next]";
			this.NextButton.UseVisualStyleBackColor = true;
			this.NextButton.Click += new System.EventHandler(this.NextButton_Click);
			// 
			// MyCancelButton
			// 
			this.MyCancelButton.AutoSize = true;
			this.MyCancelButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.MyCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.MyCancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.MyCancelButton.Location = new System.Drawing.Point(472, 330);
			this.MyCancelButton.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
			this.MyCancelButton.MinimumSize = new System.Drawing.Size(75, 0);
			this.MyCancelButton.Name = "MyCancelButton";
			this.MyCancelButton.Size = new System.Drawing.Size(92, 22);
			this.MyCancelButton.TabIndex = 3;
			this.MyCancelButton.Text = "[button.cancel]";
			this.MyCancelButton.UseVisualStyleBackColor = true;
			this.MyCancelButton.Click += new System.EventHandler(this.MyCancelButton_Click);
			// 
			// ContentPanel
			// 
			this.tableLayoutPanel1.SetColumnSpan(this.ContentPanel, 4);
			this.ContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ContentPanel.Location = new System.Drawing.Point(8, 11);
			this.ContentPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
			this.ContentPanel.Name = "ContentPanel";
			this.ContentPanel.Size = new System.Drawing.Size(556, 309);
			this.ContentPanel.TabIndex = 0;
			// 
			// progressSpinner1
			// 
			this.progressSpinner1.ForeColor = System.Drawing.SystemColors.Highlight;
			this.progressSpinner1.Location = new System.Drawing.Point(8, 333);
			this.progressSpinner1.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
			this.progressSpinner1.Maximum = 100;
			this.progressSpinner1.Name = "progressSpinner1";
			this.progressSpinner1.Size = new System.Drawing.Size(16, 16);
			this.progressSpinner1.Speed = 2F;
			this.progressSpinner1.TabIndex = 0;
			this.progressSpinner1.TabStop = false;
			this.progressSpinner1.Visible = false;
			// 
			// mouseFilter1
			// 
			this.mouseFilter1.DispatchMouseWheel = true;
			// 
			// MainForm
			// 
			this.AcceptButton = this.NextButton;
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.CancelButton = this.MyCancelButton;
			this.ClientSize = new System.Drawing.Size(574, 362);
			this.Controls.Add(this.tableLayoutPanel1);
			this.DoubleBuffered = true;
			this.MinimumSize = new System.Drawing.Size(590, 400);
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "[window.title]";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.Shown += new System.EventHandler(this.MainForm_Shown);
			this.FontChanged += new System.EventHandler(this.MainForm_FontChanged);
			this.SizeChanged += new System.EventHandler(this.MainForm_SizeChanged);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Button BackButton;
		private System.Windows.Forms.Button NextButton;
		private System.Windows.Forms.Button MyCancelButton;
		private System.Windows.Forms.Panel ContentPanel;
		private Unclassified.MouseFilter mouseFilter1;
		private Unclassified.UI.ProgressSpinner progressSpinner1;
	}
}

