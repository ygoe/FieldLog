using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Unclassified
{
	/// <summary>
	/// Provides extension methods for sequences, lists and collections.
	/// </summary>
	public static class ListExtensions
	{
		#region List iteration

		/// <summary>
		/// Invokes a method for each element in the sequence.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="action">Method to invoke for each sequence element.</param>
		/// <returns>Returns the source sequence to support chaining.</returns>
		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var element in source)
			{
				action(element);
			}
			return source;
		}

		/// <summary>
		/// Invokes a method for each element in the sequence. The sequence is copied in advance so
		/// that the method may change the source sequence while it is being enumerated.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="action">Method to invoke for each sequence element.</param>
		/// <returns>Returns the source sequence to support chaining.</returns>
		public static IEnumerable<T> ForEachSafe<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var element in new List<T>(source))
			{
				action(element);
			}
			return source;
		}

		/// <summary>
		/// Removes all element from a list that do not match the specified condition.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The list to filter.</param>
		/// <param name="predicate">Function that determines whether an element matches the condition.</param>
		public static void Filter<T>(this IList<T> source, Predicate<T> predicate)
		{
			for (int index = 0; index < source.Count; index++)
			{
				if (!predicate(source[index]))
				{
					source.RemoveAt(index);
					index--;
				}
			}
		}

		/// <summary>
		/// Returns a value indicating whether the sequence is empty.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <returns>true if the sequence is empty; otherwise, false.</returns>
		public static bool IsEmpty<T>(this IEnumerable<T> source)
		{
			foreach (T element in source)
			{
				return false;
			}
			return true;
		}

		#endregion List iteration

		#region Default list

		/// <summary>
		/// Returns a default sequence if the source sequence is empty. This is a sequence-keeping
		/// variant of the framework's DefaultIfEmpty method which only returns a single value for
		/// an empty sequence - which cannot be aggregated like non-empty sequences.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="defaultSequence">The sequence to return if <paramref name="source"/> is empty.</param>
		/// <returns></returns>
		public static IEnumerable<T> DefaultIfEmpty<T>(this IEnumerable<T> source, IEnumerable<T> defaultSequence)
		{
			if (source.IsEmpty())
			{
				return defaultSequence;
			}
			else
			{
				return source;
			}
		}

		#endregion Default list

		#region SingleOrDefault replacement

		/// <summary>
		/// Returns the single element of a sequence, or the default value if there are no or
		/// multiple elements in the sequence. The multiple-value case distinguishes this method
		/// from the framework's SingleOrDefault method which throws an exception in this case.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <returns></returns>
		public static T SingleOrDefault2<T>(this IEnumerable<T> source)
		{
			T singleValue = default(T);
			int count = 0;
			foreach (T element in source)
			{
				if (count++ == 0)
				{
					singleValue = element;
				}
				else
				{
					return default(T);
				}
			}
			return singleValue;
		}

		#endregion SingleOrDefault replacement

		#region Sorted collections

		/// <summary>
		/// Inserts an item to a list, sorted by the specified comparison delegate.
		/// </summary>
		/// <typeparam name="T">Type of the list items.</typeparam>
		/// <param name="list">The list to insert the new item to.</param>
		/// <param name="vm">New item to insert into the list.</param>
		public static void InsertSorted<T>(this IList<T> list, T vm, Comparison<T> comparison)
		{
			if (list.Count == 0)
			{
				// Easy...
				list.Add((T) vm);
				return;
			}

			// Do a binary search in the collection to find the best match position
			// (an exact match will likely not exist yet)
			int lower = 0;
			int upper = list.Count - 1;
			int index = (lower + upper) / 2;
			while (lower <= upper)
			{
				// As long as lower <= upper, index is valid and can be used for comparison
				int cmp = comparison(list[index], vm);

				if (cmp == 0)
				{
					// Direct hit, insert after this existing (undefined behaviour for multiple equal items...)
					//index++;
					break;
				}
				else if (cmp < 0)
				{
					// Item at index is less than item to insert, move on to right side
					lower = index + 1;
					index = (lower + upper) / 2;
				}
				else
				{
					// Item at index is greater than item to insert, move on to left side
					upper = index - 1;
					index = (lower + upper) / 2;
				}
			}

			// The resulting index is not equal to the new name because it doesn't exist yet.
			// Because the index was always rounded to the smaller integer, the item at index is
			// always less than the item to insert, if it exists at all (in case index == -1).
			// Use next index to insert new item.
			// Except if upper < 0, because then index was rounded to the higher integer (towards
			// zero).
			if (upper >= 0)
			{
				index++;
			}

			list.Insert(index, (T) vm);
		}

		/// <summary>
		/// Sorts the items in a list by the specified key.
		/// </summary>
		/// <typeparam name="T">Type of the list items.</typeparam>
		/// <typeparam name="TKey">Type of the key to order by.</typeparam>
		/// <param name="list">The list to sort.</param>
		/// <param name="keySelector">Function to extract the key from an element.</param>
		public static void Sort<T, TKey>(this IList<T> list, Func<T, TKey> keySelector)
		{
			var array = list.OrderBy(keySelector).ToArray();
			list.Clear();
			foreach (var item in array)
			{
				list.Add(item);
			}
		}

		/// <summary>
		/// Replaces an item in a list by another item at the same index.
		/// </summary>
		/// <typeparam name="T">Type of the list items.</typeparam>
		/// <param name="list">The list to replace the item in.</param>
		/// <param name="item">Item to find and replace.</param>
		/// <param name="replacement">New item to be set in the list.</param>
		/// <returns>true if the item was replaced, false if it did not exist.</returns>
		public static bool Replace<T>(this IList<T> list, T item, T replacement)
		{
			int index = list.IndexOf(item);
			if (index >= 0)
			{
				list[index] = replacement;
				return true;
			}
			return false;
		}

		#endregion Sorted collections

		#region INotifyCollectionChanged helpers

		// TODO: Find a better name for this method
		/// <summary>
		/// Invokes an action for every element that is added to or removed from the
		/// ObservableCollection. <paramref name="newAction"/> is also invoked immediately for
		/// every element that is currently in the collection.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="newAction">Method to invoke for each added sequence element.</param>
		/// <param name="oldAction">Method to invoke for each removed sequence element.</param>
		public static void ForNewOld<T>(this ObservableCollection<T> source, Action<T> newAction, Action<T> oldAction)
		{
			foreach (T item in source)
			{
				newAction(item);
			}

			source.CollectionChanged += (s, e) =>
			{
				if (e.NewItems != null)
				{
					foreach (T item in e.NewItems)
					{
						newAction(item);
					}
				}
				if (e.OldItems != null)
				{
					foreach (T item in e.OldItems)
					{
						oldAction(item);
					}
				}
			};
		}

		#endregion INotifyCollectionChanged helpers
	}
}
