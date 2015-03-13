// Copyright (c) 2009, Yves Goergen, http://unclassified.software/source/animation
//
// Copying and distribution of this file, with or without modification, are permitted provided the
// copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

using System;
using System.Drawing;
using System.Windows.Forms;
using Unclassified.Util;

// Benötigt die Klasse DelayedCall, ebenfalls verfügbar auf unclassified.software.
//
// Anwendung:
//
// Ausblenden und anschließendes Schließen des Fensters:
// (Close ist Methode des Fensters this)
//
//   new Animation(AnimationTypes.FadeOut, this, 0, Close);
//
// Sanftes Ändern der Höhe des Fensters auf 500 Pixel:
// (SomeTaskWhenResized ist die aufzurufende Funktion nach Abschluss der Größenänderung)
//
//   new Animation(AnimationTypes.ResizeVert, this, 500 - Height, SomeTaskWhenResized);
//
// .NET-Kompatibilität: 2.0, 3.5
//
// Hinweise für .NET 1.1: Als EventHandler muss ein 'new delegate...' übergeben werden,
// der Funktionsname allein ist eine Abkürzung, die seit .NET 2.0 möglich ist.

namespace Unclassified.UI
{
	public enum AnimationTypes
	{
		None,
		ResizeHoriz,
		ResizeVert,
		FadeIn,
		FadeOut,
		Callback
	}

	public delegate void AnimationFinishedHandler(object target);

	public delegate void AnimationCallback(object target, int value);

	/// <seealso cref="http://unclassified.software/source/animation"/>
	public class Animation
	{
		AnimationTypes type;
		object target;
		int offset;
		AnimationFinishedHandler handler;
		int start;
		int end;
		int interval;
		int duration;
		int timePassed;
		bool cancellationPending = false;
		AnimationCallback callback;

		public Animation(AnimationTypes type, object target, int offset, AnimationFinishedHandler handler)
			: this(type, target, offset, handler, 0, null, 0)
		{
		}

		public Animation(AnimationTypes type, object target, int offset, AnimationFinishedHandler handler, int duration)
			: this(type, target, offset, handler, 0, null, 0)
		{
		}

		public Animation(AnimationTypes type, object target, int offset, AnimationFinishedHandler handler, int duration, AnimationCallback callback, int startValue)
		{
			this.type = type;
			this.target = target;
			this.offset = offset;
			this.handler = handler;
			this.duration = duration;

			// timings in ms
			interval = 10;
			timePassed = 0;

			Control c;
			Form f;
			switch (type)
			{
				case AnimationTypes.ResizeHoriz:
					c = target as Control;
					if (c == null) return;
					start = c.Width;
					end = start + offset;
					if (this.duration == 0) this.duration = 150;
					break;
				case AnimationTypes.ResizeVert:
					c = target as Control;
					if (c == null) return;
					start = c.Height;
					end = start + offset;
					if (this.duration == 0) this.duration = 150;
					break;
				case AnimationTypes.FadeIn:
					f = target as Form;
					if (f == null) return;
					start = (int) (f.Opacity * 100);
					end = start + offset;
					if (this.duration == 0) this.duration = 250;
					break;
				case AnimationTypes.FadeOut:
					f = target as Form;
					if (f == null) return;
					start = (int) (f.Opacity * 100);
					end = start + offset;
					if (this.duration == 0) this.duration = 2000;
					break;
				case AnimationTypes.Callback:
					if (callback == null) return;
					start = startValue;
					end = start + offset;
					if (this.duration == 0) this.duration = 1000;
					this.callback = callback;
					break;
				default:
					return;
			}

			Next();
		}

		public void Cancel()
		{
			cancellationPending = true;
		}

		private double MakeCurve()
		{
			double timePercent = (double) timePassed / (double) duration;

			// we use the sine function from 3pi/2 to 5pi/2
			// scale down linear time percentage from 0...1 to 3pi/2 to 5pi/2
			double curve = Math.Sin(1.5 * Math.PI + timePercent * Math.PI);
			// translate sine output from -1...1 to 0...1
			curve = (curve + 1) / 2;
			// DEBUG: don't use curve but linear progress instead
			//curve = timePercent;

			return curve;
		}

		public void Next()
		{
			if (cancellationPending) return;   // and don't come back

			timePassed += interval;
			if (timePassed > duration)
			{
				if (handler != null) handler(target);
				return;
			}

			try
			{
				Control c;
				Form f;
				Rectangle wa;
				switch (type)
				{
					case AnimationTypes.ResizeVert:
						c = target as Control;
						c.Height = start + (int) ((end - start) * MakeCurve());
						wa = Screen.FromControl(c).WorkingArea;
						if (c is Form && c.Bottom > wa.Bottom)
						{
							c.Top -= c.Bottom - wa.Bottom;
							if (c.Top < wa.Top)
							{
								c.Top = wa.Top;
							}
						}
						break;
					case AnimationTypes.ResizeHoriz:
						c = target as Control;
						c.Width = start + (int) ((end - start) * MakeCurve());
						wa = Screen.FromControl(c).WorkingArea;
						if (c is Form && c.Right > wa.Right)
						{
							c.Left -= c.Right - wa.Right;
							if (c.Left < wa.Left)
							{
								c.Left = wa.Left;
							}
						}
						break;
					case AnimationTypes.FadeIn:
						f = target as Form;
						f.Opacity = (double) (start + ((end - start) * MakeCurve())) / 100;
						break;
					case AnimationTypes.FadeOut:
						f = target as Form;
						f.Opacity = (double) (start + ((end - start) * MakeCurve())) / 100;
						break;
					case AnimationTypes.Callback:
						callback(target, start + (int) ((end - start) * MakeCurve()));
						break;
				}

				DelayedCall.Start(Next, interval);
			}
			catch (ObjectDisposedException)
			{
				// Control is gone, stop here
			}
		}
	}
}
