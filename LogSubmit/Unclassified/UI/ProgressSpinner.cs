// Copyright (c) 2009, Yves Goergen, http://unclassified.software/source/progressspinner
//
// Copying and distribution of this file, with or without modification, are permitted provided the
// copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Unclassified.UI
{
	public class ProgressSpinner : Control
	{
		#region Designer code

		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}

		#endregion Designer code

		private Timer timer;
		private int progress;
		private int minimum = 0;
		private int maximum = 100;
		private float angle = 270;
		private bool ensureVisible = true;
		private float speed;
		private bool backwards;

		/// <summary>
		/// Gets or sets a value indicating whether the progress spinner is spinning.
		/// </summary>
		[DefaultValue(false)]
		[Description("Specifies whether the progress spinner is spinning.")]
		[Category("Behavior")]
		public bool Spinning
		{
			get { return timer.Enabled; }
			set { timer.Enabled = value; }
		}

		/// <summary>
		/// Gets or sets the current progress value. Set -1 to indicate that the current progress is unknown.
		/// </summary>
		[DefaultValue(0)]
		[Description("The current progress value. Set -1 to indicate that the current progress is unknown.")]
		[Category("Appearance")]
		public int Value
		{
			get
			{
				return progress;
			}
			set
			{
				if (value != -1 && (value < minimum || value > maximum))
					throw new ArgumentOutOfRangeException("Progress value must be -1 or between Minimum and Maximum.", (Exception)null);
				progress = value;
				Refresh();
			}
		}

		/// <summary>
		/// Gets or sets the minimum progress value.
		/// </summary>
		[DefaultValue(0)]
		[Description("The minimum progress value.")]
		[Category("Appearance")]
		public int Minimum
		{
			get
			{
				return minimum;
			}
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("Minimum value must be >= 0.", (Exception)null);
				if (value >= maximum)
					throw new ArgumentOutOfRangeException("Minimum value must be < Maximum.", (Exception)null);
				minimum = value;
				if (progress != -1 && progress < minimum)
					progress = minimum;
				Refresh();
			}
		}

		/// <summary>
		/// Gets or sets the maximum progress value.
		/// </summary>
		[DefaultValue(0)]
		[Description("The maximum progress value.")]
		[Category("Appearance")]
		public int Maximum
		{
			get
			{
				return maximum;
			}
			set
			{
				if (value <= minimum)
					throw new ArgumentOutOfRangeException("Maximum value must be > Minimum.", (Exception)null);
				maximum = value;
				if (progress > maximum)
					progress = maximum;
				Refresh();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the progress spinner should be visible at all progress values.
		/// </summary>
		[DefaultValue(true)]
		[Description("Specifies whether the progress spinner should be visible at all progress values.")]
		[Category("Appearance")]
		public bool EnsureVisible
		{
			get { return ensureVisible; }
			set { ensureVisible = value; Refresh(); }
		}

		/// <summary>
		/// Gets or sets the speed factor. 1 is the original speed, less is slower, greater is faster.
		/// </summary>
		[DefaultValue(1f)]
		[Description("The speed factor. 1 is the original speed, less is slower, greater is faster.")]
		[Category("Behavior")]
		public float Speed
		{
			get
			{
				return speed;
			}
			set
			{
				if (value <= 0 || value > 10)
					throw new ArgumentOutOfRangeException("Speed value must be > 0 and <= 10.", (Exception)null);
				speed = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the progress spinner should spin anti-clockwise.
		/// </summary>
		[DefaultValue(false)]
		[Description("Specifies whether the progress spinner should spin anti-clockwise.")]
		[Category("Behavior")]
		public bool Backwards
		{
			get { return backwards; }
			set { backwards = value; Refresh(); }
		}

		#region Hidden properties

		[Browsable(false)]
		public override Font Font
		{
			get { return base.Font; }
			set { base.Font = value; }
		}

		[Browsable(false)]
		public override string Text
		{
			get { return base.Text; }
			set { base.Text = value; }
		}

		[Browsable(false)]
		public new int TabIndex
		{
			get { return 0; }
			set { }
		}

		[Browsable(false)]
		public new bool TabStop
		{
			get { return false; }
			set { }
		}

		#endregion Hidden properties

		public ProgressSpinner()
		{
			InitializeComponent();

			timer = new Timer();
			timer.Interval = 20;
			timer.Tick += timer_Tick;

			Width = 16;
			Height = 16;
			speed = 1;
			DoubleBuffered = true;
			ForeColor = SystemColors.Highlight;
		}

		/// <summary>
		/// Resets the progress spinner's status.
		/// </summary>
		public void Reset()
		{
			progress = minimum;
			angle = 270;
			Refresh();
		}

		private void timer_Tick(object sender, EventArgs args)
		{
			angle += 6f * speed * (backwards ? -1 : 1);
			Refresh();
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			Pen forePen = new Pen(ForeColor, (float)Width / 5);
			int padding = (int)Math.Ceiling((float)Width / 10);

			pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

			// Draw spinner pie
			if (progress != -1)
			{
				// We have a progress value, draw a solid arc line
				// angle is the back end of the line.
				// angle +/- progress is the front end of the line

				float sweepAngle;
				float progFrac = (float)(progress - minimum) / (float)(maximum - minimum);
				if (ensureVisible)
					sweepAngle = 30 + 300f * progFrac;
				else
					sweepAngle = 360f * progFrac;
				if (backwards)
					sweepAngle = -sweepAngle;
				pe.Graphics.DrawArc(
					forePen,
					padding, padding, Width - 2 * padding - 1, Height - 2 * padding - 1,
					angle, sweepAngle);
			}
			else
			{
				// No progress value, draw a gradient arc line
				// angle is the opaque front end of the line
				// angle +/- 180° is the transparent tail end of the line

				const int maxOffset = 180;
				for (int offset = 0; offset <= maxOffset; offset += 15)
				{
					int alpha = 290 - (offset * 290 / maxOffset);
					if (alpha > 255)
						alpha = 255;
					if (alpha < 0)
						alpha = 0;
					Color col = Color.FromArgb(alpha, forePen.Color);
					Pen gradPen = new Pen(col, forePen.Width);
					float startAngle = angle + (offset - (ensureVisible ? 30 : 0)) * (backwards ? 1 : -1);
					float sweepAngle = 15 * (backwards ? 1 : -1);   // draw in reverse direction
					pe.Graphics.DrawArc(
						gradPen,
						padding, padding, Width - 2 * padding - 1, Height - 2 * padding - 1,
						startAngle, sweepAngle);
					gradPen.Dispose();
				}
			}

			forePen.Dispose();
		}
	}
}
