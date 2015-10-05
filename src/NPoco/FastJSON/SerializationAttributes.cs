using System;
using System.Reflection;

namespace NPoco.FastJSON
{
	/// <summary>
	/// Indicates whether non-public classes, structs, fields or properties could be serialized and deserialized.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public sealed class JsonSerializableAttribute : Attribute
	{
	}

	/// <summary>
	/// Indicates whether a field or property should be included in serialization.
	/// To control whether a field or property should be deserialized, use the <see cref="System.ComponentModel.ReadOnlyAttribute"/>.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class JsonIncludeAttribute : Attribute
	{
		/// <summary>
		/// Gets whether the annotated field or property should be included in serialization disregarding whether it is read-only or not. The default value is true.
		/// </summary>
		public bool Include { get; private set; }
		/// <summary>
		/// Indicates a member should be included in serialization.
		/// </summary>
		public JsonIncludeAttribute () { Include = true; }
		/// <summary>
		/// Indicates whether a member should be included in serialization.
		/// </summary>
		/// <param name="include">Indicates whether a member should be included in serialization.</param>
		public JsonIncludeAttribute (bool include) {
			Include = include;
		}
	}

	/// <summary>
	/// Indicates the name and data type of a field or property.
	/// The same field or property with multiple <see cref="JsonFieldAttribute"/> can have various names mapped to various types.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public sealed class JsonFieldAttribute : Attribute
	{
		/// <summary>
		/// Gets the name of the serialized field or property.
		/// The case of the serialized name defined in this attribute will not be changed by <see cref="JSONParameters.NamingConvention"/> setting in <see cref="JSONParameters"/>.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the type of the field or property.
		/// </summary>
		public Type DataType { get; private set; }

		/// <summary>
		/// Specifies the name of the serialized field or property.
		/// </summary>
		/// <param name="name">The name of the serialized field or property.</param>
		public JsonFieldAttribute (string name) {
			Name = name;
		}

		/// <summary>
		/// Specifies the name of the serialized field or property which has a associated type.
		/// </summary>
		/// <param name="name">The name of the serialized field or property.</param>
		/// <param name="dataType">The name is only used when the value is of this data type.</param>
		public JsonFieldAttribute (string name, Type dataType) {
			Name = name;
			DataType = dataType;
		}
	}

	/// <summary>
	/// Specifies a value of the annotated member which is hidden from being serialized.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public sealed class JsonNonSerializedValueAttribute : Attribute
	{
		/// <summary>
		/// Gets the non-serialized value.
		/// </summary>
		public object Value { get; private set; }

		/// <summary>
		/// Specifies a value of the annotated member which is hidden from being serialized.
		/// </summary>
		/// <param name="value">The non-serialized value.</param>
		public JsonNonSerializedValueAttribute (object value) {
			Value = value;
		}
	}

	/// <summary>
	/// Indicates the value format of the annotated enum type.
	/// </summary>
	[AttributeUsage (AttributeTargets.Enum)]
	public sealed class JsonEnumFormatAttribute : Attribute
	{
		readonly EnumValueFormat _format;

		/// <summary>
		/// Specifies the format of an enum type.
		/// </summary>
		/// <param name="valueFormat">The format of the serialized enum type.</param>
		public JsonEnumFormatAttribute (EnumValueFormat valueFormat) {
			_format = valueFormat;
		}

		/// <summary>
		/// Gets the format of the annotated enum type.
		/// </summary>
		public EnumValueFormat Format {
			get { return _format; }
		}
	}

	/// <summary>
	/// Controls the serialized name of an Enum value.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field)]
	public sealed class JsonEnumValueAttribute : Attribute
	{
		/// <summary>
		/// Gets the literal name of the Enum value.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Specifies the serialized name of the annotated Enum value.
		/// </summary>
		/// <param name="name"></param>
		public JsonEnumValueAttribute (string name) {
			Name = name;
		}
	}

	/// <summary>
	/// Controls the object being serialized or deserialized.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class JsonInterceptorAttribute : Attribute
	{
		/// <summary>
		/// The type of interceptor. The instance of the type should implement <see cref="IJsonInterceptor"/>.
		/// During serialization and deserialization, an instance of <see cref="IJsonInterceptor"/> will be created to process values of the object being serialized or deserialized.
		/// </summary>
		public Type InterceptorType {
			get { return Interceptor == null ? null : Interceptor.GetType (); }
		}

		internal IJsonInterceptor Interceptor { get; private set; }

		/// <summary>
		/// Marks a class or a struct to be processed by an <see cref="IJsonInterceptor"/>.
		/// </summary>
		/// <param name="interceptorType">The type of <see cref="IJsonInterceptor"/></param>
		/// <exception cref="JsonSerializationException">The exception will be thrown if the type does not implements <see cref="IJsonInterceptor"/>.</exception>
		public JsonInterceptorAttribute (Type interceptorType) {
			if (interceptorType == null) {
				throw new ArgumentNullException ("interceptorType");
			}
			if (interceptorType.IsInterface || typeof (IJsonInterceptor).IsAssignableFrom (interceptorType) == false) {
				throw new JsonSerializationException (String.Concat ("The type ", interceptorType.FullName, " defined in ", typeof (JsonInterceptorAttribute).FullName, " does not implement interface ", typeof (IJsonInterceptor).FullName));
			}
			Interceptor = Activator.CreateInstance (interceptorType) as IJsonInterceptor;
		}
	}

	/// <summary>
	/// Controls data conversion in serialization and deserialization.
	/// </summary>
	/// <remarks>
	/// <para>This attribute can be applied to types or type members.</para>
	/// <para>If it is applied to types, the converter will be used in all instances of the type, each property or field that has that data type will use the converter prior to serialization or deserialization.</para>
	/// <para>If both the type member and the type has applied this attribute, the attribute on the type member will have a higher precedence.</para>
	/// </remarks>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class JsonConverterAttribute : Attribute
	{
		/// <summary>
		/// <para>The type of converter to convert string to object. The type should implement <see cref="IJsonConverter"/>.</para>
		/// <para>During serialization and deserialization, an instance of <see cref="IJsonConverter"/> will be used to convert values between their original type and target type.</para>
		/// </summary>
		public Type ConverterType {
			get { return Converter == null ? null : Converter.GetType (); }
		}

		internal IJsonConverter Converter { get; private set; }

		/// <summary>
		/// Marks the value of a field or a property to be converted by an <see cref="IJsonConverter"/>.
		/// </summary>
		/// <param name="converterType">The type of the <see cref="IJsonConverter"/>.</param>
		/// <exception cref="JsonSerializationException">Exception can be thrown if the type does not implements <see cref="IJsonConverter"/>.</exception>
		public JsonConverterAttribute (Type converterType) {
			if (converterType == null) {
				throw new ArgumentNullException ("converterType");
			}
			if (converterType.IsInterface || typeof (IJsonConverter).IsAssignableFrom (converterType) == false) {
				throw new JsonSerializationException (String.Concat ("The type ", converterType.FullName, " defined in ", typeof (JsonConverterAttribute).FullName, " does not implement interface ", typeof (IJsonConverter).FullName));
			}
			Converter = Activator.CreateInstance (converterType) as IJsonConverter;
		}
	}

	/// <summary>
	/// Controls data conversion of <see cref="System.Collections.IEnumerable"/> items in serialization and deserialization.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class JsonItemConverterAttribute : Attribute
	{
		/// <summary>
		/// <para>The type of converter to convert string to object. The type should implement <see cref="IJsonConverter"/>.</para>
		/// <para>During serialization and deserialization, an instance of <see cref="IJsonConverter"/> will be used to convert values between their original type and target type.</para>
		/// </summary>
		public Type ConverterType {
			get { return Converter == null ? null : Converter.GetType (); }
		}

		internal IJsonConverter Converter { get; private set; }

		/// <summary>
		/// Marks the item value of a field or a property to be converted by an <see cref="IJsonConverter"/>.
		/// </summary>
		/// <param name="converterType">The type of the <see cref="IJsonConverter"/>.</param>
		/// <exception cref="JsonSerializationException">Exception can be thrown if the type does not implements <see cref="IJsonConverter"/>.</exception>
		public JsonItemConverterAttribute (Type converterType) {
			if (converterType == null) {
				throw new ArgumentNullException ("converterType");
			}
			if (converterType.IsInterface || typeof(IJsonConverter).IsAssignableFrom (converterType) == false) {
				throw new JsonSerializationException (String.Concat ("The type ", converterType.FullName, " defined in ", typeof(JsonConverterAttribute).FullName, " does not implement interface ", typeof(IJsonConverter).FullName));
			}
			Converter = Activator.CreateInstance (converterType) as IJsonConverter;

		}
	}

	/// <summary>
	/// Denotes a type which implements <see cref="System.Collections.IEnumerable"/> should be serialized with its members and items being placed to a field named by this attribute.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class JsonCollectionAttribute : Attribute
	{
		/// <summary>
		/// Gets the name of the container.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonCollectionAttribute"/> class.
		/// </summary>
		/// <param name="name">The name of the container.</param>
		public JsonCollectionAttribute (string name) {
			Name = name;
		}
	}

	static class AttributeHelper
	{
		public static T[] GetAttributes<T> (MemberInfo member, bool inherit) where T : Attribute {
			return member.GetCustomAttributes (typeof (T), inherit) as T[];
		}
		public static T GetAttribute<T> (MemberInfo member, bool inherit) where T : Attribute {
			return Attribute.GetCustomAttribute (member, typeof (T), inherit) as T;
		}
		public static bool HasAttribute<T> (MemberInfo member, bool inherit) where T : Attribute {
			return Attribute.GetCustomAttribute (member, typeof (T), inherit) is T;
		}
	}
}
