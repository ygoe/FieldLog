using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;

namespace Unclassified.UI
{
	#region Non-generic class for use in XAML

	// Source: http://stackoverflow.com/a/4398752/143684
	// Extended as in: http://10rem.net/blog/2011/03/09/creating-a-custom-markup-extension-in-wpf-and-soon-silverlight
	public class EnumerationExtension : MarkupExtension
	{
		private bool generateStringValue;

		public EnumerationExtension()
		{
		}

		public EnumerationExtension(Type enumType)
		{
			EnumType = enumType;
		}

		public EnumerationExtension(Type enumType, Type requiredAttribute)
		{
			EnumType = enumType;
			RequiredAttribute = requiredAttribute;
		}

		public EnumerationExtension(Type enumType, bool generateStringValue)
		{
			EnumType = enumType;
			this.generateStringValue = generateStringValue;
		}

		public EnumerationExtension(Type enumType, Type requiredAttribute, bool generateStringValue)
		{
			EnumType = enumType;
			RequiredAttribute = requiredAttribute;
			this.generateStringValue = generateStringValue;
		}

		private Type enumType;
		[ConstructorArgument("enumType")]
		public Type EnumType
		{
			get { return enumType; }
			set
			{
				if (value != enumType)
				{
					if (value == null)
						throw new ArgumentNullException("EnumType");

					var v = Nullable.GetUnderlyingType(value) ?? value;
					if (v.IsEnum == false)
						throw new ArgumentException("Type must be an Enum.");

					enumType = value;
				}
			}
		}

		[ConstructorArgument("requiredAttribute")]
		public Type RequiredAttribute { get; set; }

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (EnumType == null)
				throw new ArgumentNullException("EnumType");

			var enumValues = Enum.GetValues(EnumType);

			return (
				from object enumValue in enumValues
				where IsAttributeSet(EnumType, enumValue, RequiredAttribute)
				select new EnumerationMember
				{
					Value = generateStringValue ? enumValue.ToString() : enumValue,
					Description = GetDescription(EnumType, enumValue)
				}).ToArray();
		}

		public IEnumerable<EnumerationExtension.EnumerationMember> ProvideTypedValue()
		{
			return (EnumerationExtension.EnumerationMember[]) ProvideValue(null);
		}

		/// <summary>
		/// Determines whether the specified attribute is set on an enumeration member.
		/// </summary>
		/// <param name="enumType">Enum type that defines the specified value.</param>
		/// <param name="enumValue">Value of the enum type to check.</param>
		/// <param name="attrType">Attribute type to look for on the Enum member.</param>
		/// <returns></returns>
		public static bool IsAttributeSet(Type enumType, object enumValue, Type attrType)
		{
			if (attrType == null)
				return true;

			return enumType
				.GetField(enumValue.ToString())
				.GetCustomAttributes(attrType, false)
				.Any();
		}

		/// <summary>
		/// Returns the value of the Description attribute of an enumeration member.
		/// </summary>
		/// <param name="enumType">Enum type that defines the specified value.</param>
		/// <param name="enumValue">Value of the enum type to get the description text for.</param>
		/// <returns></returns>
		public static string GetDescription(Type enumType, object enumValue)
		{
			var descriptionAttribute = enumType
				.GetField(enumValue.ToString())
				.GetCustomAttributes(typeof(DescriptionAttribute), false)
				.FirstOrDefault() as DescriptionAttribute;
			return descriptionAttribute != null ? descriptionAttribute.Description : enumValue.ToString();
		}

		public class EnumerationMember
		{
			public string Description { get; set; }
			public object Value { get; set; }

			public override bool Equals(object obj)
			{
				EnumerationMember em = obj as EnumerationMember;
				if (em != null)
				{
					return Value.Equals(em.Value);
				}
				return false;
			}

			public override int GetHashCode()
			{
				return Value.GetHashCode();
			}
		}
	}

	#endregion Non-generic class for use in XAML

	#region Generic class for use in code

	public class EnumerationExtension<T> : MarkupExtension
	{
		public EnumerationExtension()
		{
			EnumType = typeof(T);
		}

		public EnumerationExtension(Type requiredAttribute)
		{
			EnumType = typeof(T);
			RequiredAttribute = requiredAttribute;
		}

		private Type enumType;
		[ConstructorArgument("enumType")]
		public Type EnumType
		{
			get { return enumType; }
			private set
			{
				if (value != enumType)
				{
					if (value == null)
						throw new ArgumentNullException("EnumType");

					var v = Nullable.GetUnderlyingType(value) ?? value;
					if (v.IsEnum == false)
						throw new ArgumentException("Type must be an Enum.");

					enumType = value;
				}
			}
		}

		[ConstructorArgument("requiredAttribute")]
		public Type RequiredAttribute { get; set; }

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (EnumType == null)
				throw new ArgumentNullException("EnumType");

			var enumValues = Enum.GetValues(EnumType);

			return (
				from T enumValue in enumValues
				where IsAttributeSet(EnumType, enumValue, RequiredAttribute)
				select new EnumerationMember
				{
					Value = enumValue,
					Description = GetDescription(EnumType, enumValue)
				}).ToArray();
		}

		public IEnumerable<EnumerationExtension<T>.EnumerationMember> ProvideTypedValue()
		{
			return (EnumerationExtension<T>.EnumerationMember[]) ProvideValue(null);
		}

		/// <summary>
		/// Determines whether the specified attribute is set on an enumeration member.
		/// </summary>
		/// <param name="enumType">Enum type that defines the specified value.</param>
		/// <param name="enumValue">Value of the enum type to check.</param>
		/// <param name="attrType">Attribute type to look for on the Enum member.</param>
		/// <returns></returns>
		public static bool IsAttributeSet(Type enumType, T enumValue, Type attrType)
		{
			if (attrType == null)
				return true;

			return enumType
				.GetField(enumValue.ToString())
				.GetCustomAttributes(attrType, false)
				.Any();
		}

		/// <summary>
		/// Returns the value of the Description attribute of an enumeration member.
		/// </summary>
		/// <param name="enumType">Enum type that defines the specified value.</param>
		/// <param name="enumValue">Value of the enum type to get the description text for.</param>
		/// <returns></returns>
		public static string GetDescription(Type enumType, T enumValue)
		{
			var descriptionAttribute = enumType
				.GetField(enumValue.ToString())
				.GetCustomAttributes(typeof(DescriptionAttribute), false)
				.FirstOrDefault() as DescriptionAttribute;
			return descriptionAttribute != null ? descriptionAttribute.Description : enumValue.ToString();
		}

		public class EnumerationMember
		{
			public string Description { get; set; }
			public T Value { get; set; }

			public override bool Equals(object obj)
			{
				EnumerationMember em = obj as EnumerationMember;
				if (em != null)
				{
					return Value.Equals(em.Value);
				}
				return false;
			}

			public override int GetHashCode()
			{
				return Value.GetHashCode();
			}
		}
	}

	#endregion Generic class for use in code
}
