using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Specialized;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unclassified
{
	/// <summary>
	/// Provides extension methods for class property handling.
	/// </summary>
	public static class PropertyExtensions
	{
		#region INotifyPropertyChanged helpers

		//public static void OnPropertyChanged(this INotifyPropertyChanged sender, string propertyName, Action handler)
		//{
		//    sender.PropertyChanged += (s, e) =>
		//    {
		//        if (e.PropertyName == propertyName) handler();
		//    };
		//}

		/// <summary>
		/// Links the value of a property of a source object to an action method. Whenever the
		/// source property is changed, the setter action is invoked and can update the value in
		/// another property or method.
		/// </summary>
		/// <typeparam name="TSource">Type that defines the property.</typeparam>
		/// <typeparam name="TProperty">Value type of the property.</typeparam>
		/// <param name="source">Instance of the type that defines the property. Must implement INotifyPropertyChanged.</param>
		/// <param name="expr">Lambda expression of the property.</param>
		/// <param name="handler">Action that handles the changed value.</param>
		/// <example>
		/// Link a source property to a local property of the same type:
		/// <code>
		/// source.LinkProperty(s => s.SourceProperty, v => this.MyProperty = v);
		/// </code>
		/// Link a source property to a local method that accepts the source property's value type
		/// as its only parameter:
		/// <code>
		/// source.LinkProperty(s => s.SourceProperty, OnUpdate);
		/// </code>
		/// </example>
		public static void LinkProperty<TSource, TProperty>(
			this TSource source,
			System.Linq.Expressions.Expression<Func<TSource, TProperty>> expr,
			Action<TProperty> handler)
			where TSource : INotifyPropertyChanged
		{
			var memberExpr = expr.Body as System.Linq.Expressions.MemberExpression;
			if (memberExpr != null)
			{
				PropertyInfo property = memberExpr.Member as PropertyInfo;
				if (property != null)
				{
					source.PropertyChanged += (s, e) =>
					{
						if (e.PropertyName == property.Name)
						{
							handler((TProperty) property.GetValue(source, null));
						}
					};

					// Set value immediately
					handler((TProperty) property.GetValue(source, null));
					return;
				}
			}
			throw new ArgumentException("Unsupported expression type.");
		}

		/// <summary>
		/// Binds the value of a property of a source object to a property of a target object.
		/// Whenever either property is changed, the other side of the binding is set to the same
		/// value. To avoid endless loops, the PropertyChanged event must only be raised when the
		/// value actually changed, not already on assignment. The target property is updated
		/// immediately after setting up the binding.
		/// </summary>
		/// <typeparam name="TTarget">Type that defines the target property.</typeparam>
		/// <typeparam name="TSource">Type that defines the source property.</typeparam>
		/// <typeparam name="TProperty">Value type of both properties.</typeparam>
		/// <param name="target">Instance of the type that defines the target property. Must implement INotifyPropertyChanged.</param>
		/// <param name="targetExpr">Lambda expression of the target property.</param>
		/// <param name="source">Instance of the type that defines the source property. Must implement INotifyPropertyChanged.</param>
		/// <param name="sourceExpr">Lambda expression of the source property.</param>
		/// <example>
		/// Bind a source property to a local property of the same type:
		/// <code>
		/// source.BindProperty(me => me.TargetProperty, sourceObj, src => src.SourceProperty);
		/// </code>
		/// </example>
		public static void BindProperty<TTarget, TSource, TProperty>(
			this TTarget target,
			System.Linq.Expressions.Expression<Func<TTarget, TProperty>> targetExpr,
			TSource source,
			System.Linq.Expressions.Expression<Func<TSource, TProperty>> sourceExpr)
			where TTarget : INotifyPropertyChanged
			where TSource : INotifyPropertyChanged
		{
			// Initialise all expression parts and reflected properties
			var targetMemberExpr = targetExpr.Body as System.Linq.Expressions.MemberExpression;
			if (targetMemberExpr == null)
				throw new ArgumentException("Unsupported target expression type.");
			PropertyInfo targetProperty = targetMemberExpr.Member as PropertyInfo;
			if (targetProperty == null)
				throw new ArgumentException("Unsupported target expression type.");

			var sourceMemberExpr = sourceExpr.Body as System.Linq.Expressions.MemberExpression;
			if (sourceMemberExpr == null)
				throw new ArgumentException("Unsupported source expression type.");
			PropertyInfo sourceProperty = sourceMemberExpr.Member as PropertyInfo;
			if (sourceProperty == null)
				throw new ArgumentException("Unsupported source expression type.");

			// When the source changes, update the target
			source.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == sourceProperty.Name)
				{
					targetProperty.SetValue(
						target, 
						sourceProperty.GetValue(source, null),
						null);
				}
			};
			// When the target changes, update the source
			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == targetProperty.Name)
				{
					sourceProperty.SetValue(
						source,
						targetProperty.GetValue(target, null),
						null);
				}
			};

			// Update the target immediately
			targetProperty.SetValue(
				target,
				sourceProperty.GetValue(source, null),
				null);
		}

		/// <summary>
		/// Returns the property name of a lambda expression.
		/// </summary>
		/// <typeparam name="TSource">Type that defines the property.</typeparam>
		/// <typeparam name="TProperty">Value type of the property.</typeparam>
		/// <param name="source">Instance of the type that defines the property.</param>
		/// <param name="expr">Lambda expression of the property.</param>
		/// <returns></returns>
		/// <example>
		/// <code>
		/// string name = this.ExprName(x => x.MyProperty);
		/// </code>
		/// The value of name is set to "MyProperty".
		/// </example>
		public static string ExprName<TSource, TProperty>(
			this TSource source,
			System.Linq.Expressions.Expression<Func<TSource, TProperty>> expr)
		{
			var memberExpr = expr.Body as System.Linq.Expressions.MemberExpression;
			if (memberExpr != null)
			{
				PropertyInfo property = memberExpr.Member as PropertyInfo;
				if (property != null)
				{
					return property.Name;
				}
			}
			throw new ArgumentException("Unsupported expression type.");
		}

		#endregion INotifyPropertyChanged helpers
	}
}
