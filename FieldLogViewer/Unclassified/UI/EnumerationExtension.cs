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
	/// <summary>
	/// Provides enumeration values in XAML.
	/// </summary>
	public class EnumerationExtension : MarkupExtension
	{
		private bool generateStringValue;

		/// <summary>
		/// Initialises a new instance of the EnumerationExtension class.
		/// </summary>
		public EnumerationExtension()
		{
		}

		/// <summary>
		/// Initialises a new instance of the EnumerationExtension class.
		/// </summary>
		/// <param name="enumType">The enumeration type.</param>
		public EnumerationExtension(Type enumType)
		{
			EnumType = enumType;
		}

		/// <summary>
		/// Initialises a new instance of the EnumerationExtension class.
		/// </summary>
		/// <param name="enumType">The enumeration type.</param>
		/// <param name="requiredAttribute">The type of the attribute that the enumeration values must have set.</param>
		public EnumerationExtension(Type enumType, Type requiredAttribute)
		{
			EnumType = enumType;
			RequiredAttribute = requiredAttribute;
		}

		/// <summary>
		/// Initialises a new instance of the EnumerationExtension class.
		/// </summary>
		/// <param name="enumType">The enumeration type.</param>
		/// <param name="generateStringValue">true to generate string values of the enum members, false to generate typed enum values.</param>
		public EnumerationExtension(Type enumType, bool generateStringValue)
		{
			EnumType = enumType;
			this.generateStringValue = generateStringValue;
		}

		/// <summary>
		/// Initialises a new instance of the EnumerationExtension class.
		/// </summary>
		/// <param name="enumType">The enumeration type.</param>
		/// <param name="requiredAttribute">The type of the attribute that the enumeration values must have set.</param>
		/// <param name="generateStringValue">true to generate string values of the enum members, false to generate typed enum values.</param>
		public EnumerationExtension(Type enumType, Type requiredAttribute, bool generateStringValue)
		{
			EnumType = enumType;
			RequiredAttribute = requiredAttribute;
			this.generateStringValue = generateStringValue;
		}

		private Type enumType;

		/// <summary>
		/// Gets or sets the enumeration type.
		/// </summary>
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

		/// <summary>
		/// Gets or sets the type of the attribute that the enumeration values must have set.
		/// </summary>
		[ConstructorArgument("requiredAttribute")]
		public Type RequiredAttribute { get; set; }

		/// <summary>
		/// Returns an object that is provided as the value of the target property for this markup extension.
		/// </summary>
		/// <param name="serviceProvider">Unused.</param>
		/// <returns></returns>
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

		/// <summary>
		/// Returns a sequence of typed values for this markup extension.
		/// </summary>
		/// <returns></returns>
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

		/// <summary>
		/// Defines a wrapper for an enumeration value and its description string.
		/// </summary>
		public class EnumerationMember
		{
			/// <summary>
			/// Gets or sets the description for the enumeration value.
			/// </summary>
			public string Description { get; set; }

			/// <summary>
			/// Gets or sets the enumeration value.
			/// </summary>
			public object Value { get; set; }

			/// <summary>
			/// Determines whether the specified object is equal to the current object.
			/// </summary>
			/// <param name="obj">The object to compare with the current object.</param>
			/// <returns></returns>
			public override bool Equals(object obj)
			{
				EnumerationMember em = obj as EnumerationMember;
				if (em != null)
				{
					return Value.Equals(em.Value);
				}
				return false;
			}

			/// <summary>
			/// Returns a hash value for the current object.
			/// </summary>
			/// <returns></returns>
			public override int GetHashCode()
			{
				return Value.GetHashCode();
			}
		}
	}

	#endregion Non-generic class for use in XAML

	#region Generic class for use in code

	/// <summary>
	/// Provides typed enumeration values in XAML.
	/// </summary>
	/// <typeparam name="T">The enumeration type.</typeparam>
	public class EnumerationExtension<T> : MarkupExtension
	{
		/// <summary>
		/// Initialises a new instance of the EnumerationExtension class.
		/// </summary>
		public EnumerationExtension()
		{
			EnumType = typeof(T);
		}

		/// <summary>
		/// Initialises a new instance of the EnumerationExtension class.
		/// </summary>
		/// <param name="requiredAttribute">The type of the attribute that the enumeration values must have set.</param>
		public EnumerationExtension(Type requiredAttribute)
		{
			EnumType = typeof(T);
			RequiredAttribute = requiredAttribute;
		}

		private Type enumType;

		/// <summary>
		/// Gets or sets the enumeration type.
		/// </summary>
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

		/// <summary>
		/// Gets or sets the type of the attribute that the enumeration values must have set.
		/// </summary>
		[ConstructorArgument("requiredAttribute")]
		public Type RequiredAttribute { get; set; }

		/// <summary>
		/// Returns an object that is provided as the value of the target property for this markup extension.
		/// </summary>
		/// <param name="serviceProvider">Unused.</param>
		/// <returns></returns>
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
					Description = GetDescription(enumValue)
				}).ToArray();
		}

		/// <summary>
		/// Returns a sequence of typed values for this markup extension.
		/// </summary>
		/// <returns></returns>
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
		/// <param name="enumValue">Value of the enum type to get the description text for.</param>
		/// <returns></returns>
		public static string GetDescription(T enumValue)
		{
			var descriptionAttribute = typeof(T)
				.GetField(enumValue.ToString())
				.GetCustomAttributes(typeof(DescriptionAttribute), false)
				.FirstOrDefault() as DescriptionAttribute;
			return descriptionAttribute != null ? descriptionAttribute.Description : enumValue.ToString();
		}

		/// <summary>
		/// Defines a wrapper for an enumeration value and its description string.
		/// </summary>
		public class EnumerationMember
		{
			/// <summary>
			/// Gets or sets the description for the enumeration value.
			/// </summary>
			public string Description { get; set; }

			/// <summary>
			/// Gets or sets the enumeration value.
			/// </summary>
			public T Value { get; set; }

			/// <summary>
			/// Determines whether the specified object is equal to the current object.
			/// </summary>
			/// <param name="obj">The object to compare with the current object.</param>
			/// <returns></returns>
			public override bool Equals(object obj)
			{
				EnumerationMember em = obj as EnumerationMember;
				if (em != null)
				{
					return Value.Equals(em.Value);
				}
				return false;
			}

			/// <summary>
			/// Returns a hash value for the current object.
			/// </summary>
			/// <returns></returns>
			public override int GetHashCode()
			{
				return Value.GetHashCode();
			}
		}
	}

	#endregion Generic class for use in code
}
