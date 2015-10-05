using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Xml.Serialization;

namespace NPoco.FastJSON
{
	/// <summary>
	/// The general implementation of <see cref="IReflectionController"/>, which takes custom attributes such as <see cref="JsonFieldAttribute"/>, <see cref="JsonConverterAttribute"/>, etc. into consideration.
	/// </summary>
	/// <preliminary />
	public class JsonReflectionController : DefaultReflectionController
	{
#if NET_40_OR_GREATER
		HashSet<Type> _ContractualTypes = new HashSet<Type> ();
		/// <summary>
		/// Gets whether <see cref="DataContractAttribute"/>, <see cref="DataMemberAttribute"/>, etc. attributes in <see cref="System.Runtime.Serialization"/> namespace are supported. (default: true)
		/// </summary>
		public bool UseDataContractAttributes { get; private set; }
#endif
		/// <summary>
		/// Gets a value indicating whether XML serialization attributes should be used to control serialized field names. (default: false)
		/// </summary>
		public bool UseXmlSerializationAttributes { get; private set; }
		/// <summary>
		/// Ignore attributes to check for (default : XmlIgnoreAttribute).
		/// </summary>
		public IList<Type> IgnoreAttributes { get; private set; }

		/// <summary>
		/// Creates an instance of <see cref="JsonReflectionController"/>. For backward compatibility, <see cref="XmlIgnoreAttribute"/> is added into <see cref="IgnoreAttributes"/>.
		/// </summary>
		public JsonReflectionController ()
#if NET_40_OR_GREATER
			: this (true, false)
#endif
		{
		}

		/// <summary>
		/// Creates an instance of <see cref="JsonReflectionController"/> and sets whether <see cref="UseXmlSerializationAttributes"/> option is turned on. For backward compatibility, <see cref="XmlIgnoreAttribute"/> is added into <see cref="IgnoreAttributes"/>.
		/// </summary>
		/// <param name="useXmlSerializationAttributes">Controls whether <see cref="XmlElementAttribute"/> and other serialization attributes should be supported to control serialized field names.</param>
		public JsonReflectionController (bool useXmlSerializationAttributes) {
			IgnoreAttributes = new List<Type> { typeof (XmlIgnoreAttribute) };
			UseXmlSerializationAttributes = useXmlSerializationAttributes;
		}

#if NET_40_OR_GREATER
		/// <summary>
		/// Creates an instance of <see cref="JsonReflectionController"/> and sets whether <see cref="UseDataContractAttributes"/> and <see cref="UseXmlSerializationAttributes"/> options are turned on. For backward compatibility, <see cref="XmlIgnoreAttribute"/> is added into <see cref="IgnoreAttributes"/>.
		/// </summary>
		/// <param name="supportContractualAttributes">Controls whether <see cref="DataContractAttribute"/> should be supported.</param>
		/// <param name="useXmlSerializationAttributes">Controls whether <see cref="XmlElementAttribute"/> and other serialization attributes should be supported to control serialized field names.</param>
		public JsonReflectionController (bool supportContractualAttributes, bool useXmlSerializationAttributes) {
			IgnoreAttributes = new List<Type> { typeof (XmlIgnoreAttribute) };
			UseDataContractAttributes = supportContractualAttributes;
			UseXmlSerializationAttributes = useXmlSerializationAttributes;
		}
#endif
		/// <summary>
		/// This method is called to determine whether the values of the given <see cref="Enum"/> type should be serialized as its numeric form rather than literal form. The override can be set via the <see cref="JsonEnumFormatAttribute"/>.
		/// </summary>
		/// <param name="type">An <see cref="Enum"/> value type.</param>
		/// <returns>If the type should be serialized numerically, returns true, otherwise, false.</returns>
		public override EnumValueFormat GetEnumValueFormat (Type type) {
			var a = AttributeHelper.GetAttribute<JsonEnumFormatAttribute> (type, false);
			return a != null ? a.Format : EnumValueFormat.Default;
		}

		/// <summary>
		/// Gets the overridden name for an enum value. The overridden name can be set via the <see cref="JsonEnumValueAttribute"/>. If null or empty string is returned, the original name of the enum value is used.
		/// </summary>
		/// <param name="member">The enum value member.</param>
		/// <returns>The name of the enum value.</returns>
		public override string GetEnumValueName (MemberInfo member) {
			var a = AttributeHelper.GetAttribute<JsonEnumValueAttribute> (member, false);
			if (a != null) {
				return a.Name;
			}
#if NET_40_OR_GREATER
			if (UseDataContractAttributes) {
				var m = AttributeHelper.GetAttribute<EnumMemberAttribute> (member, true);
				if (m != null && String.IsNullOrEmpty (m.Value) == false) {
					return m.Value;
				}
			}
#endif
			if (UseXmlSerializationAttributes) {
				var m = AttributeHelper.GetAttribute<XmlEnumAttribute> (member, true);
				if (m != null) {
					return m.Name;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets whether the type is always deserializable. The value can be set via <see cref="JsonSerializableAttribute"/>.
		/// </summary>
		/// <param name="type">The type to be deserialized.</param>
		/// <returns>Whether the type can be deserialized even if it is a non-public type.</returns>
		public override bool IsAlwaysDeserializable (Type type) {
			return AttributeHelper.HasAttribute<JsonSerializableAttribute> (type, false);
		}

		/// <summary>
		/// Returns the <see cref="IJsonInterceptor"/> for given type. If no interceptor, null should be returned. The interceptor can be set via <see cref="JsonInterceptorAttribute"/>.
		/// </summary>
		/// <param name="type">The type to be checked.</param>
		/// <returns>The interceptor.</returns>
		public override IJsonInterceptor GetInterceptor (Type type) {
#if NET_40_OR_GREATER
			if (UseDataContractAttributes && AttributeHelper.HasAttribute<DataContractAttribute> (type, false)) {
				_ContractualTypes.Add (type);
			}
#endif
			var ia = AttributeHelper.GetAttribute<JsonInterceptorAttribute> (type, true);
			if (ia != null) {
				return ia.Interceptor;
			}
			return null;
		}

		/// <summary>
		/// This method is called to get the <see cref="IJsonConverter"/> for the type. If no converter, null should be returned. The converter can be set via <see cref="JsonConverterAttribute"/>.
		/// </summary>
		/// <param name="type">The type to be checked for <see cref="IJsonConverter"/>.</param>
		/// <returns>The interceptor.</returns>
		public override IJsonConverter GetConverter (Type type) {
			var ia = AttributeHelper.GetAttribute<JsonConverterAttribute> (type, true);
			if (ia != null) {
				return ia.Converter;
			}
			return null;
		}

		/// <summary>
		/// Gets the name of the collection container for customized types which implement <see cref="IEnumerable"/> and have extra members to be serialized.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> which implements <see cref="IEnumerable"/> and contains extra members to be serialized.</param>
		/// <returns>The name of the collection container. If null is returned, no container will be created.</returns>
		/// <remarks>By default, the serializer serializes types which implement <see cref="IEnumerable"/> interface as JSON arrays.
		/// Members of the type will be ignored. And so to the types which implements <see cref="IDictionary"/>.
		/// To serialize those members, return a non-empty name and the members will be serialized into a field named returned by this method.</remarks>
		public override string GetCollectionContainerName (Type type) {
			var n = AttributeHelper.GetAttribute<JsonCollectionAttribute> (type, true);
			if (n != null && String.IsNullOrEmpty (n.Name) == false) {
				return n.Name;
			}
			return null;
		}
		/// <summary>
		/// Returns whether the specific member is serializable. This value can be set via <see cref="JsonIncludeAttribute"/> and <see cref="IgnoreAttributes"/>.
		/// If true is returned, the member will always get serialized.
		/// If false is returned, the member will be excluded from serialization.
		/// If null is returned, the serialization of the member will be determined by the settings in <see cref="JSONParameters"/>.
		/// </summary>
		/// <param name="member">The member to be serialized.</param>
		/// <param name="info">Reflection information for the member.</param>
		/// <returns>True is returned if the member is serializable, otherwise, false.</returns>
		public override bool? IsMemberSerializable (MemberInfo member, IMemberInfo info) {
			var ic = AttributeHelper.GetAttribute<JsonIncludeAttribute> (member, true);
			if (ic != null) {
				return ic.Include ? true : false;
			}
			if (AttributeHelper.HasAttribute<JsonSerializableAttribute> (member, true)) {
				return true;
			}
#if NET_40_OR_GREATER
			if (UseDataContractAttributes && _ContractualTypes.Contains (member.DeclaringType)) {
				return AttributeHelper.HasAttribute<DataMemberAttribute> (member, true);
			}
			if (UseDataContractAttributes && AttributeHelper.HasAttribute<IgnoreDataMemberAttribute> (member, true)) {
				return false;
			}
#endif
			if (UseXmlSerializationAttributes && AttributeHelper.HasAttribute<XmlIgnoreAttribute> (member, false)) {
				return false;
			}
			if (IgnoreAttributes != null && IgnoreAttributes.Count > 0) {
				foreach (var item in IgnoreAttributes) {
					if (member.IsDefined (item, false)) {
						return false;
					}
				}
			}
			return base.IsMemberSerializable (member, info);
		}

		/// <summary>
		/// Gets whether a field or a property is deserializable. If false is returned, the member will be excluded from deserialization. By default, writable fields or properties are deserializable. The value can be set via <see cref="System.ComponentModel.ReadOnlyAttribute"/>.
		/// </summary>
		/// <param name="member">The member to be serialized.</param>
		/// <param name="info">Reflection information for the member.</param>
		/// <returns>True is returned if the member is serializable, otherwise, false.</returns>
		public override bool IsMemberDeserializable (MemberInfo member, IMemberInfo info) {
			var ro = AttributeHelper.GetAttribute<System.ComponentModel.ReadOnlyAttribute> (member, true);
			if (ro != null) {
				return ro.IsReadOnly == false;
			}
			var s = AttributeHelper.GetAttribute<JsonSerializableAttribute> (member, true);
			if (s != null) {
				return true;
			}
#if NET_40_OR_GREATER
			if (UseDataContractAttributes && _ContractualTypes.Contains (member.DeclaringType)) {
				return AttributeHelper.HasAttribute<DataMemberAttribute> (member, true);
			}
			if (UseDataContractAttributes && AttributeHelper.HasAttribute<IgnoreDataMemberAttribute> (member, true)) {
				return false;
			}
#endif
			if (UseXmlSerializationAttributes && AttributeHelper.HasAttribute<XmlIgnoreAttribute> (member, false)) {
				return false;
			}
			return base.IsMemberDeserializable (member, info);
		}

		/// <summary>
		/// This method returns possible names for corresponding types of a field or a property. This enables polymorphic serialization and deserialization for abstract classes, interfaces, or object types, with predetermined concrete types. If polymorphic serialization is not used, null or an empty <see cref="SerializedNames"/> could be returned. The names can be set via <see cref="JsonFieldAttribute"/>.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The dictionary contains types and their corresponding names.</returns>
		/// <exception cref="InvalidCastException">The <see cref="JsonFieldAttribute.DataType"/> type does not derive from the member type.</exception>
		public override SerializedNames GetSerializedNames (MemberInfo member) {
			var tn = new SerializedNames ();
			var jf = AttributeHelper.GetAttributes<JsonFieldAttribute> (member, true);
			var f = member as FieldInfo;
			var p = member as PropertyInfo;
			var t = p != null ? p.PropertyType : f.FieldType;
			foreach (var item in jf) {
				if (String.IsNullOrEmpty (item.Name)) {
					continue;
				}
				if (item.DataType == null) {
					tn.DefaultName = item.Name;
				}
				else {
					if (t.IsAssignableFrom (item.DataType) == false) {
						throw new InvalidCastException ("The override type (" + item.DataType.FullName + ") does not derive from the member type (" + t.FullName + ")");
					}
					tn.Add (item.DataType, item.Name);
				}
			}
#if NET_40_OR_GREATER
			if (UseDataContractAttributes && jf.Length == 0) {
				var m = AttributeHelper.GetAttribute<DataMemberAttribute> (member, true);
				if (m != null && String.IsNullOrEmpty (m.Name) == false) {
					tn.DefaultName = m.Name;
				}
			}
#endif
			if (UseXmlSerializationAttributes) {
				var ar = AttributeHelper.GetAttribute<XmlArrayAttribute> (member, true);
				if (ar != null) {
					tn.DefaultName = ar.ElementName;
				}
				foreach (var item in AttributeHelper.GetAttributes<XmlElementAttribute> (member, true)) {
					if (String.IsNullOrEmpty (item.ElementName)) {
						continue;
					}
					if (item.DataType == null) {
						tn.DefaultName = item.ElementName;
					}
					else {
						if (t.IsAssignableFrom (item.Type) == false) {
							throw new InvalidCastException ("The override type (" + item.Type.FullName + ") does not derive from the member type (" + t.FullName + ")");
						}
						tn.Add (item.Type, item.ElementName);
					}
				}
				var an = AttributeHelper.GetAttribute<XmlAttributeAttribute> (member, true);
				if (an != null) {
					tn.DefaultName = an.AttributeName;
				}
			}
			return tn;
		}

		/// <summary>
		/// This method returns a series of values that will not be serialized for a field or a property. When the value of the member matches those values, it will not be serialized. If all values can be serialized, null should be returned. The value can be set via <see cref="System.ComponentModel.DefaultValueAttribute"/>.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The values which are not serialized for <paramref name="member"/>.</returns>
		public override IEnumerable GetNonSerializedValues (MemberInfo member) {
			var a = AttributeHelper.GetAttribute<System.ComponentModel.DefaultValueAttribute> (member, true);
			var n = AttributeHelper.GetAttributes<JsonNonSerializedValueAttribute> (member, true);
			if (a == null && n.Length == 0) {
				return null;
			}
			var v = new List<object> ();
			if (a != null) {
				v.Add (a.Value);
			}
			foreach (var item in n) {
				v.Add (item.Value);
			}
			return v;
		}

		/// <summary>
		/// This method returns the <see cref="IJsonConverter"/> to convert values for a field or a property during serialization and deserialization. If no converter is used, null can be returned. The converter can be set via <see cref="JsonConverterAttribute"/>.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The converter.</returns>
		public override IJsonConverter GetMemberConverter (MemberInfo member) {
			var cv = AttributeHelper.GetAttribute<JsonConverterAttribute> (member, true);
			if (cv != null) {
				return cv.Converter;
			}
			return null;
		}

		/// <summary>
		/// This method returns an <see cref="IJsonConverter"/> instance to convert item values for a field or a property which is of <see cref="IEnumerable"/> type during serialization and deserialization. If no converter is used, null can be returned. The converter can be set via <see cref="JsonItemConverterAttribute"/>.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The converter.</returns>
		public override IJsonConverter GetMemberItemConverter (MemberInfo member) {
			var cv = AttributeHelper.GetAttribute<JsonItemConverterAttribute> (member, true);
			if (cv != null) {
				return cv.Converter;
			}
			return null;
		}
	}

	/// <summary>
	/// This is an empty implementation of <see cref="IReflectionController"/> doing nothing but serving as a template class for method-overriding.
	/// </summary>
	/// <preliminary />
	public class DefaultReflectionController : IReflectionController
	{
		/// <summary>
		/// This method is called to determine whether the values of the given <see cref="Enum"/> type should be serialized as its numeric form rather than literal form.
		/// </summary>
		/// <param name="type">An <see cref="Enum"/> value type.</param>
		/// <returns>If the type should be serialized numerically, returns true, otherwise, false.</returns>
		public virtual EnumValueFormat GetEnumValueFormat (Type type) { return EnumValueFormat.Default; }

		/// <summary>
		/// This method is called to override the serialized name of an enum value. If null or empty string is returned, the original name of the enum value is used.
		/// </summary>
		/// <param name="member">The enum value member.</param>
		/// <returns>The name of the enum value.</returns>
		public virtual string GetEnumValueName (MemberInfo member) { return null; }

		/// <summary>
		/// This method is called before the constructor of a type is built for deserialization to detect whether the type is deserializable.
		/// When this method returns true, the type can be deserialized regardless it is a non-public type.
		/// Public types are always deserializable and not affected by the value returned from this method.
		/// If the type contains generic parameters (for generic types) or an element type (for array types), the parameters and element types will be checked first.
		/// </summary>
		/// <param name="type">The type to be deserialized.</param>
		/// <returns>Whether the type can be deserialized even if it is a non-public type.</returns>
		public virtual bool IsAlwaysDeserializable (Type type) { return false; }

		/// <summary>
		/// This method is called to get the <see cref="IJsonInterceptor"/> for the type. If no interceptor, null should be returned.
		/// </summary>
		/// <param name="type">The type to be checked.</param>
		/// <returns>The interceptor.</returns>
		public virtual IJsonInterceptor GetInterceptor (Type type) { return null; }

		/// <summary>
		/// This method is called to get the <see cref="IJsonConverter"/> for the type. If no converter, null should be returned.
		/// </summary>
		/// <param name="type">The type to be checked for <see cref="IJsonConverter"/>.</param>
		/// <returns>The interceptor.</returns>
		public virtual IJsonConverter GetConverter (Type type) { return null; }

		/// <summary>
		/// Gets the name of the collection container for customized types which implement <see cref="IEnumerable"/> and have extra members to be serialized.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> which implements <see cref="IEnumerable"/> and contains extra members to be serialized.</param>
		/// <returns>The name of the collection container. If null is returned, no container will be created.</returns>
		/// <remarks>By default, the serializer serializes types which implement <see cref="IEnumerable"/> interface as JSON arrays.
		/// Members of the type will be ignored. And so to the types which implements <see cref="IDictionary"/>.
		/// To serialize those members, return a non-empty name and the members will be serialized into a field named returned by this method.</remarks>
		public virtual string GetCollectionContainerName (Type type) { return null; }

		/// <summary>
		/// This method is called to determine whether a field or a property is serializable.
		/// If false is returned, the member will be excluded from serialization.
		/// If true is returned, the member will always get serialized.
		/// If null is returned, the serialization of the member will be determined by the settings in <see cref="JSONParameters"/>.
		/// </summary>
		/// <param name="member">The member to be serialized.</param>
		/// <param name="info">Reflection information for the member.</param>
		/// <returns>False is returned if the member is private, otherwise, null is returned.</returns>
		public virtual bool? IsMemberSerializable (MemberInfo member, IMemberInfo info) {
			var p = member as PropertyInfo;
			if (p != null) {
				var g = p.GetGetMethod (true);
				if (g == null || g.IsPublic == false)
					return false;
				return null;
			}
			var f = member as FieldInfo;
			if (f != null) {
				if (f.IsPublic == false)
					return false;
				return null;
			}
			throw new ArgumentException ("member should be property or field", member.Name);
		}

		/// <summary>
		/// This method is called to determine whether a field or a property is deserializable. If false is returned, the member will be excluded from deserialization. By default, writable fields or properties are deserializable.
		/// </summary>
		/// <param name="member">The member to be serialized.</param>
		/// <param name="info">Reflection information for the member.</param>
		/// <returns>True is returned if the member is public, otherwise, false.</returns>
		public virtual bool IsMemberDeserializable (MemberInfo member, IMemberInfo info) {
			return info.IsReadOnly == false && info.IsPublic;
		}

		/// <summary>
		/// This method returns possible names for corresponding types of a field or a property. This enables polymorphic serialization and deserialization for abstract, interface, or object types, with predetermined concrete types. If polymorphic serialization is not used, null or an empty <see cref="SerializedNames"/> could be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The dictionary contains types and their corresponding names.</returns>
		public virtual SerializedNames GetSerializedNames (MemberInfo member) { return null; }

		/// <summary>
		/// This method returns a series of values that will not be serialized for a field or a property. When the value of the member matches those values, it will not be serialized. If all values can be serialized, null should be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The values which are not serialized for <paramref name="member"/>.</returns>
		public virtual IEnumerable GetNonSerializedValues (MemberInfo member) { return null; }

		/// <summary>
		/// This method returns the <see cref="IJsonConverter"/> to convert values for a field or a property during serialization and deserialization. If no converter is used, null can be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The converter.</returns>
		public virtual IJsonConverter GetMemberConverter (MemberInfo member) { return null; }

		/// <summary>
		/// This method returns an <see cref="IJsonConverter"/> instance to convert item values for a field or a property which is of <see cref="IEnumerable"/> type during serialization and deserialization. If no converter is used, null can be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The converter.</returns>
		public virtual IJsonConverter GetMemberItemConverter (MemberInfo member) { return null; }
	}

	/// <summary>
	/// The controller interface to control type reflections for serialization and deserialization.
	/// </summary>
	/// <remarks>
	/// <para>The interface works in the reflection phase. Its methods are executed typically once and the result will be cached. Consequently, changes occur after the reflection phase will not take effect.</para>
	/// <para>It is recommended to inherit from <see cref="DefaultReflectionController"/> or <see cref="JsonReflectionController"/>.</para>
	/// </remarks>
	/// <preliminary />
	public interface IReflectionController
	{
		/// <summary>
		/// This method is called to determine how to format values of the given <see cref="Enum"/> type.
		/// </summary>
		/// <param name="type">An <see cref="Enum"/> value type.</param>
		/// <returns>The format of the enum value.</returns>
		EnumValueFormat GetEnumValueFormat (Type type);

		/// <summary>
		/// This method is called to override the serialized name of an enum value. If null or empty string is returned, the original name of the enum value is used.
		/// </summary>
		/// <param name="member">The enum value member.</param>
		/// <returns>The name of the enum value.</returns>
		string GetEnumValueName (MemberInfo member);

		/// <summary>
		/// This method is called before the constructor of a type is built for deserialization to detect whether the type is deserializable.
		/// When this method returns true, the type can be deserialized regardless it is a non-public type.
		/// Public types are always deserializable and not affected by the value returned from this method.
		/// If the type contains generic parameters (for generic types) or an element type (for array types), the parameters and element types will be checked first.
		/// </summary>
		/// <param name="type">The type to be deserialized.</param>
		/// <returns>Whether the type can be deserialized even if it is a non-public type.</returns>
		bool IsAlwaysDeserializable (Type type);

		/// <summary>
		/// This method is called to get the <see cref="IJsonInterceptor"/> for the type. If no interceptor, null should be returned.
		/// </summary>
		/// <param name="type">The type to be checked for <see cref="IJsonInterceptor"/>.</param>
		/// <returns>The interceptor.</returns>
		IJsonInterceptor GetInterceptor (Type type);

		/// <summary>
		/// This method is called to get the <see cref="IJsonConverter"/> for the type. If no converter, null should be returned.
		/// </summary>
		/// <param name="type">The type to be checked for <see cref="IJsonConverter"/>.</param>
		/// <returns>The interceptor.</returns>
		IJsonConverter GetConverter (Type type);

		/// <summary>
		/// Gets the name of the collection container for customized types which implement <see cref="IEnumerable"/> and have extra members to be serialized.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> which implements <see cref="IEnumerable"/> and contains extra members to be serialized.</param>
		/// <returns>The name of the collection container. If null is returned, no container will be created.</returns>
		/// <remarks>By default, the serializer serializes types which implement <see cref="IEnumerable"/> interface as JSON arrays.
		/// Members of those types will be ignored. And so to the types which implements <see cref="IDictionary"/>.
		/// To serialize those members, return a non-empty name for those types and the members will be serialized into a field with the returned name.</remarks>
		string GetCollectionContainerName (Type type);

		/// <summary>
		/// This method is called to determine whether a field or a property is serializable.
		/// If false is returned, the member will be excluded from serialization.
		/// If true is returned, the member will always get serialized.
		/// If null is returned, the serialization of the member will be determined by the settings in <see cref="JSONParameters"/>.
		/// </summary>
		/// <param name="member">The member to be serialized.</param>
		/// <param name="info">Reflection information for the member.</param>
		/// <returns>True is returned if the member is serializable, otherwise, false.</returns>
		bool? IsMemberSerializable (MemberInfo member, IMemberInfo info);

		/// <summary>
		/// This method is called to determine whether a field or a property is deserializable. If false is returned, the member will be excluded from deserialization. By default, writable fields or properties are deserializable.
		/// </summary>
		/// <param name="member">The member to be serialized.</param>
		/// <param name="info">Reflection information for the member.</param>
		/// <returns>True is returned if the member is serializable, otherwise, false.</returns>
		bool IsMemberDeserializable (MemberInfo member, IMemberInfo info);

		/// <summary>
		/// This method returns possible names for corresponding types of a field or a property. This enables polymorphic serialization and deserialization for abstract, interface, or object types, with predetermined concrete types. If polymorphic serialization is not used, null or an empty dictionary could be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The dictionary contains types and their corresponding names.</returns>
		SerializedNames GetSerializedNames (MemberInfo member);

		/// <summary>
		/// This method returns a series of values that will not be serialized for a field or a property. When the value of the member matches those values, it will not be serialized. If all values can be serialized, null should be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The values which are not serialized for <paramref name="member"/>.</returns>
		IEnumerable GetNonSerializedValues (MemberInfo member);

		/// <summary>
		/// This method returns an <see cref="IJsonConverter"/> instance to convert values for a field or a property during serialization and deserialization. If no converter is used, null can be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The converter.</returns>
		IJsonConverter GetMemberConverter (MemberInfo member);

		/// <summary>
		/// This method returns an <see cref="IJsonConverter"/> instance to convert item values for a field or a property which is of <see cref="IEnumerable"/> type during serialization and deserialization. If no converter is used, null can be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The converter.</returns>
		IJsonConverter GetMemberItemConverter (MemberInfo member);

	}

    /// <summary>
    /// Contains the names for a serialized member.
    /// </summary>
    /// <preliminary />
    [Serializable]
    public sealed class SerializedNames : Dictionary<Type, string>, ISerializable
	{
		/// <summary>
		/// Gets the default name for the serialized member.
		/// </summary>
		public string DefaultName { get; set; }

		[SecurityPermission (SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context) {
			if (info == null)
				throw new ArgumentNullException ("info");

			info.AddValue ("$DefaultName", DefaultName);
			GetObjectData (info, context);
		}
	}

}
