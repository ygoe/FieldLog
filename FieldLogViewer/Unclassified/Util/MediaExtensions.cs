using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Unclassified.Util
{
	/// <summary>
	/// Provides extension methods for media elements like Color, Brush, Fonts etc.
	/// </summary>
	public static class MediaExtensions
	{
		#region Colour blending

		/// <summary>
		/// Blends two colours.
		/// </summary>
		/// <param name="c1">The first colour.</param>
		/// <param name="c2">The second colour.</param>
		/// <param name="ratio">The ratio between both colours, from 0 for the first to 1 for the second colour.</param>
		/// <returns>The blended colour value.</returns>
		public static Color BlendWith(this Color c1, Color c2, float ratio = 0.5f)
		{
			return Color.Add(Color.Multiply(c1, ratio), Color.Multiply(c2, 1 - ratio));
		}

		/// <summary>
		/// Blends two brushes.
		/// </summary>
		/// <param name="b1">The first brush.</param>
		/// <param name="b2">The second brush.</param>
		/// <param name="ratio">The ratio between both brush colours, from 0 for the first to 1 for the second brush.</param>
		/// <returns>A brush with the blended colour.</returns>
		public static SolidColorBrush BlendWith(this SolidColorBrush b1, SolidColorBrush b2, float ratio = 0.5f)
		{
			return new SolidColorBrush(Color.Add(Color.Multiply(b1.Color, ratio), Color.Multiply(b2.Color, 1 - ratio)));
		}

		/// <summary>
		/// Blends a brush with another colour.
		/// </summary>
		/// <param name="b1">The first brush.</param>
		/// <param name="c2">The second colour.</param>
		/// <param name="ratio">The ratio between both colours, from 0 for the first to 1 for the second colour.</param>
		/// <returns>A brush with the blended colour.</returns>
		public static SolidColorBrush BlendWith(this SolidColorBrush b1, Color c2, float ratio = 0.5f)
		{
			return new SolidColorBrush(Color.Add(Color.Multiply(b1.Color, ratio), Color.Multiply(c2, 1 - ratio)));
		}

		#endregion Colour blending

		#region Colour to grey conversion

		/// <summary>
		/// Converts a colour to its grey representation.
		/// </summary>
		/// <param name="color">The colour to convert.</param>
		/// <returns>The grey colour value.</returns>
		public static Color ToGray(this Color color)
		{
			byte grey = (byte) (color.R * 0.3 + color.G * 0.59 + color.B * 0.11);
			return Color.FromArgb(color.A, grey, grey, grey);
		}

		/// <summary>
		/// Converts a brush to its grey representation.
		/// </summary>
		/// <param name="brush">The brush to convert.</param>
		/// <returns>A brush with the grey colour.</returns>
		public static SolidColorBrush ToGray(this SolidColorBrush brush)
		{
			return new SolidColorBrush(brush.Color.ToGray());
		}

		/// <summary>
		/// Returns a value indicating whether the colour is dark or light.
		/// </summary>
		/// <param name="color">The colour to analyse.</param>
		/// <returns>true if the colour is dark; false otherwise.</returns>
		public static bool IsDark(this Color color)
		{
			return color.ToGray().R < 0x90;
		}

		/// <summary>
		/// Returns a value indicating whether the brush colour is dark or light.
		/// </summary>
		/// <param name="brush">The brush to analyse.</param>
		/// <returns>true if the brush colour is dark; false otherwise.</returns>
		public static bool IsDark(this SolidColorBrush brush)
		{
			return brush.Color.ToGray().R < 0x90;
		}

		#endregion Colour to grey conversion

		#region Fonts

		/// <summary>
		/// Returns the first of the specified font families that is installed on the system.
		/// </summary>
		/// <param name="familyNames">The font family names to test.</param>
		/// <returns>The first available font family or a default font family.</returns>
		public static FontFamily FindAvailableFontFamily(params string[] familyNames)
		{
			foreach (string familyName in familyNames)
			{
				FontFamily ff = Fonts.SystemFontFamilies.FirstOrDefault(f => f.Source == familyName);
				if (ff != null)
				{
					return ff;
				}
			}
			return SystemFonts.MessageFontFamily;
		}

		/// <summary>
		/// Finds the best available monospace font family installed on the system.
		/// </summary>
		/// <returns></returns>
		public static FontFamily FindMonospaceFontFamily()
		{
			return FindAvailableFontFamily("Consolas", "Andale Mono", "Lucida Console", "Courier New");
		}

		#endregion Fonts
	}
}
