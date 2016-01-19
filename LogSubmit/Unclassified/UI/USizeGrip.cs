using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Unclassified.UI
{
	public class USizeGrip : Control
	{
		private const uint WM_NCHITTEST = 0x84;
		private const int HTBOTTOMRIGHT = 17;

		public static void AddToForm(Form form)
		{
			// Background transparency workaround: Draw USizeGrip control in parts.
			for (int q = 1; q <= 2; q++)
			{
				USizeGrip sizeGrip = new USizeGrip();
				sizeGrip.KeepBottomRight = true;
				sizeGrip.Part = q;
				form.Controls.Add(sizeGrip);
				sizeGrip.BringToFront();
			}
		}

		private System.ComponentModel.IContainer components = null;
		private bool keepBottomRight = false;
		private int part;
		private Form prevParent;

		[Category("Behavior")]
		[DefaultValue(false)]
		public bool KeepBottomRight
		{
			get
			{
				return keepBottomRight;
			}
			set
			{
				keepBottomRight = value;
				if (keepBottomRight)
				{
					Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
					Form form = FindForm();   // Own form, not MDIParent
					if (form != null)
					{
						Top = form.ClientSize.Height - Height;
						Left = form.ClientSize.Width - Width;
					}
				}
			}
		}

		/// <summary>
		/// Background transparency workaround: Split up the entire rectangular control into multiple
		/// parts, 1 is the vertical and 2 is the horizontal part. All controls must touch the
		/// bottom-right window corner for the hit test response to work correctly. So the two parts
		/// are overlapping down to the bottom-right corner but extending more to the top or left
		/// respectively so that they cover the entire diagonal size grip area.
		/// </summary>
		public int Part
		{
			get
			{
				return part;
			}
			set
			{
				part = value;
				if (part == 0)
				{
					Size = new Size(16, 16);
				}
				else if (part == 1)
				{
					Size = new Size(8, 16);
				}
				else if (part == 2)
				{
					Size = new Size(16, 8);
				}
				KeepBottomRight = KeepBottomRight;
			}
		}

		protected override void OnParentChanged(EventArgs args)
		{
			if (prevParent != null)
				prevParent.Resize -= ParentForm_Resize;

			base.OnParentChanged(args);
			// Update this value whenever the parent Form has changed, or in case it wasn't set yet before
			KeepBottomRight = KeepBottomRight;

			prevParent = FindFormEx();
			prevParent.Resize += ParentForm_Resize;
		}

		private void ParentForm_Resize(object sender, EventArgs args)
		{
			if (prevParent != null)
			{
				Visible = prevParent.WindowState != FormWindowState.Maximized;
			}
		}

		public USizeGrip()
		{
			InitializeComponent();

			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.FixedHeight | ControlStyles.FixedWidth | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
			TabStop = false;
			Size = new Size(16, 16);
		}

		/// <summary>
		/// Verwendete Ressourcen bereinigen.
		/// </summary>
		/// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
		protected override void Dispose(bool disposing)
		{
			if (prevParent != null)
			{
				prevParent.Resize -= ParentForm_Resize;
				prevParent = null;
			}

			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Erforderliche Methode für die Designerunterstützung.
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			switch (part)
			{
				case 0:   // Entire size grip (16x16)
					ControlPaint.DrawSizeGrip(pe.Graphics, BackColor, 0, 1, Width, Height);
					break;
				case 1:   // Vertical extent
					ControlPaint.DrawSizeGrip(pe.Graphics, BackColor, 0 - Width, 1, Width * 2, Height);
					break;
				case 2:   // Horizontal extent
					ControlPaint.DrawSizeGrip(pe.Graphics, BackColor, 0, 1 - Height, Width, Height * 2);
					break;
			}
		}

		protected override void WndProc(ref Message m)
		{
			// Responding to the hit test message does all the magic. :-)
			if (m.Msg == WM_NCHITTEST)
			{
				m.Result = new IntPtr(HTBOTTOMRIGHT);
			}
			else
			{
				base.WndProc(ref m);
			}
		}

		private Form FindFormEx()
		{
			Form form = FindForm();
			if (form.MdiParent != null && form.WindowState == FormWindowState.Maximized)
			{
				form = form.MdiParent;
			}
			return form;
		}
	}
}
