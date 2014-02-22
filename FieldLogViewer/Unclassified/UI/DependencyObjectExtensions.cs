using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Unclassified.UI
{
	public static class DependencyObjectExtensions
	{
		public static TChild FindVisualChild<TChild>(this DependencyObject source, Func<DependencyObject, bool> predicate = null)
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
						foundChild = (TChild) child;
					}
				}
				else
				{
					// Type matches, no predicate given
					foundChild = (TChild) child;
				}
			}
			return foundChild;
		}

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
						foundChild = (TChild) child;
					}
				}
				else
				{
					// Type matches, no name requested
					foundChild = (TChild) child;
				}
			}
			return foundChild;
		}

		public static IEnumerable<TChild> FindAllVisualChildren<TChild>(this DependencyObject source, Func<DependencyObject, bool> predicate = null)
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
						foundChildren.Add((TChild) child);
					}
				}
				else
				{
					// Type matches, no name requested
					foundChildren.Add((TChild) child);
				}
			}
			return foundChildren;
		}
	}
}
