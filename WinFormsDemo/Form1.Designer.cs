namespace WinFormsDemo
{
	partial class Form1
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
			this.throwExceptionButton = new System.Windows.Forms.Button();
			this.throwThreadExceptionButton = new System.Windows.Forms.Button();
			this.showErrorButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// throwExceptionButton
			// 
			this.throwExceptionButton.Location = new System.Drawing.Point(12, 41);
			this.throwExceptionButton.Name = "throwExceptionButton";
			this.throwExceptionButton.Size = new System.Drawing.Size(208, 23);
			this.throwExceptionButton.TabIndex = 1;
			this.throwExceptionButton.Text = "Throw Exception in UI thread";
			this.throwExceptionButton.UseVisualStyleBackColor = true;
			this.throwExceptionButton.Click += new System.EventHandler(this.throwExceptionButton_Click);
			// 
			// throwThreadExceptionButton
			// 
			this.throwThreadExceptionButton.Location = new System.Drawing.Point(12, 70);
			this.throwThreadExceptionButton.Name = "throwThreadExceptionButton";
			this.throwThreadExceptionButton.Size = new System.Drawing.Size(208, 23);
			this.throwThreadExceptionButton.TabIndex = 2;
			this.throwThreadExceptionButton.Text = "Throw Exception in different thread";
			this.throwThreadExceptionButton.UseVisualStyleBackColor = true;
			this.throwThreadExceptionButton.Click += new System.EventHandler(this.throwThreadExceptionButton_Click);
			// 
			// showErrorButton
			// 
			this.showErrorButton.Location = new System.Drawing.Point(12, 12);
			this.showErrorButton.Name = "showErrorButton";
			this.showErrorButton.Size = new System.Drawing.Size(208, 23);
			this.showErrorButton.TabIndex = 0;
			this.showErrorButton.Text = "Show error dialog";
			this.showErrorButton.UseVisualStyleBackColor = true;
			this.showErrorButton.Click += new System.EventHandler(this.showErrorButton_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(337, 158);
			this.Controls.Add(this.throwThreadExceptionButton);
			this.Controls.Add(this.showErrorButton);
			this.Controls.Add(this.throwExceptionButton);
			this.Name = "Form1";
			this.Text = "FieldLog Windows Forms Demo";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button throwExceptionButton;
		private System.Windows.Forms.Button throwThreadExceptionButton;
		private System.Windows.Forms.Button showErrorButton;
	}
}

