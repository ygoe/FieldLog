using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides extension methods for dependency objects.
	/// </summary>
	public static class DependencyObjectExtensions
	{
		/// <summary>
		/// Finds a child of the current object in the visual tree.
		/// </summary>
		/// <typeparam name="TChild">The type of the requested child object.</typeparam>
		/// <param name="source">The object in which to find the child.</param>
		/// <param name="predicate">A predicate that determines whether to accept a child.</param>
		/// <returns>The found child object, or null.</returns>
		public static TChild FindVisualChild<TChild>(this DependencyObject source, Predicate<DependencyObject> predicate = null)
			where TChild : DependencyObject
		{
			if (source == null)
				throw new ArgumentNullException("source");

			TChild foundChild = null;
			int childrenCount = VisualTreeHelper.GetChildrenCount(source);
			for (int i = 0; i < childrenCount && foundChild == null; i++)
			{
				var child = VisualTreeHelper.GetChild(source, i);
				if (child.GetType() != typeof(TChild))
				{
					// Not the requested type, recursively drill down the tree
					foundChild = FindVisualChild<TChild>(child, predicate);
				}
				else if (predicate != null)
				{
					// Compare the predicate
					if (predicate(child))
					{
						foundChild = (TChild)child;
					}
				}
				else
				{
					// Type matches, no predicate given
					foundChild = (TChild)child;
				}
			}
			return foundChild;
		}

		/// <summary>
		/// Finds a child of the current object in the visual tree.
		/// </summary>
		/// <typeparam name="TChild">The type of the requested child object.</typeparam>
		/// <param name="source">The object in which to find the child.</param>
		/// <param name="childName">The element name of the requested child.</param>
		/// <returns>The found child object, or null.</returns>
		public static TChild FindVisualChild<TChild>(this DependencyObject source, string childName)
			where TChild : DependencyObject
		{
			if (source == null)
				throw new ArgumentNullException("source");

			TChild foundChild = null;
			int childrenCount = VisualTreeHelper.GetChildrenCount(source);
			for (int i = 0; i < childrenCount && foundChild == null; i++)
			{
				var child = VisualTreeHelper.GetChild(source, i);
				if (child.GetType() != typeof(TChild))
				{
					// Not the requested type, recursively drill down the tree
					foundChild = FindVisualChild<TChild>(child, childName);
				}
				else if (!string.IsNullOrEmpty(childName))
				{
					// Compare the requested child name
					var frameworkElement = child as FrameworkElement;
					if (frameworkElement != null && frameworkElement.Name == childName)
					{
						foundChild = (TChild)child;
					}
				}
				else
				{
					// Type matches, no name requested
					foundChild = (TChild)child;
				}
			}
			return foundChild;
		}

		/// <summary>
		/// Finds all children of the current object in the visual tree.
		/// </summary>
		/// <typeparam name="TChild">The type of the requested child object.</typeparam>
		/// <param name="source">The object in which to find the child.</param>
		/// <param name="predicate">A predicate that determines whether to accept a child.</param>
		/// <returns>The found child objects, or an empty list.</returns>
		public static IEnumerable<TChild> FindAllVisualChildren<TChild>(this DependencyObject source, Predicate<DependencyObject> predicate = null)
			where TChild : DependencyObject
		{
			if (source == null)
				throw new ArgumentNullException("source");

			List<TChild> foundChildren = new List<TChild>();
			int childrenCount = VisualTreeHelper.GetChildrenCount(source);
			for (int i = 0; i < childrenCount; i++)
			{
				var child = VisualTreeHelper.GetChild(source, i);
				if (child.GetType() != typeof(TChild))
				{
					// Not the requested type, recursively drill down the tree
					foundChildren.AddRange(FindAllVisualChildren<TChild>(child, predicate));
				}
				else if (predicate != null)
				{
					// Compare the predicate
					if (predicate(child))
					{
						foundChildren.Add((TChild)child);
					}
				}
				else
				{
					// Type matches, no name requested
					foundChildren.Add((TChild)child);
				}
			}
			return foundChildren;
		}

		/// <summary>
		/// Finds a parent of the current object in the visual tree.
		/// </summary>
		/// <typeparam name="TParent">The type of the requested parent object.</typeparam>
		/// <param name="source">The object from which to find the parent.</param>
		/// <param name="predicate">A predicate that determines whether to accept a parent.</param>
		/// <returns>The found parent object, or null.</returns>
		public static TParent FindVisualParent<TParent>(this DependencyObject source, Predicate<DependencyObject> predicate = null)
			where TParent : DependencyObject
		{
			if (source == null)
				throw new ArgumentNullException("source");

			DependencyObject dep = source;
			do
			{
				dep = VisualTreeHelper.GetParent(dep);
				if (dep == null) return null;
			}
			while (!(dep is TParent) || (predicate != null && !predicate(dep)));
			return (TParent)dep;
		}
	}
}
