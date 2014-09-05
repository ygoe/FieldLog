using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Unclassified.Util
{
	/// <summary>
	/// Provides extension methods for media elements like Color, Brush etc.
	/// </summary>
	public static class MediaExtensions
	{
		#region Colour maths

		// TODO: Compare with ColorMath class

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

		#endregion Colour maths

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
