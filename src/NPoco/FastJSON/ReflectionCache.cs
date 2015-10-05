using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace NPoco.FastJSON
{
	delegate object CreateObject ();
	delegate object GenericGetter (object obj);
	delegate void WriteJsonValue (JsonSerializer serializer, object value);
	delegate object GenericSetter (object target, object value);
	delegate void AddCollectionItem (object target, object value);
	delegate object RevertJsonValue (JsonDeserializer deserializer, object value, ReflectionCache targetType);

	struct CompoundDeserializer
	{
		readonly string CollectionName;
		readonly RevertJsonValue DeserializeMethod;
		public CompoundDeserializer (string collectionName, RevertJsonValue deserializeMethod) {
			CollectionName = collectionName;
			DeserializeMethod = deserializeMethod;
		}
		internal object Deserialize (JsonDeserializer deserializer, object value, ReflectionCache targetType) {
			var d = value as JsonDict;
			var o = DeserializeMethod (deserializer, d[CollectionName], targetType);
			return deserializer.CreateObject (d, targetType, o);
		}
	}
	[DebuggerDisplay ("{TypeName} ({JsonDataType})")]
	class ReflectionCache
	{
		internal readonly string TypeName;
		internal readonly string AssemblyName;
		internal readonly Type Type;
		internal readonly JsonDataType JsonDataType;

		#region Definition for Generic or Array Types
		internal readonly Type[] ArgumentTypes;
		internal readonly ReflectionCache[] ArgumentReflections;
		internal readonly ComplexType CommonType;
		internal readonly WriteJsonValue ItemSerializer;
		internal readonly RevertJsonValue ItemDeserializer;
		internal readonly AddCollectionItem AppendItem;
		#endregion

		#region Object Serialization and Deserialization Info
		internal readonly ConstructorTypes ConstructorInfo;
		internal readonly CreateObject Constructor;
		internal readonly WriteJsonValue SerializeMethod;
		internal RevertJsonValue DeserializeMethod;
		internal readonly MemberCache[] Members;
		internal JsonMemberGetter[] Getters;
		// denotes the collection name for extended IEnumerable types
		internal string CollectionName;
		// a member could have several setters because of the result of typed serialization
		internal Dictionary<string, JsonMemberSetter> Setters;
		internal bool AlwaysDeserializable;
		internal IJsonConverter Converter;
		internal IJsonInterceptor Interceptor;
		#endregion

		#region Enum Info
		internal readonly bool IsFlaggedEnum;
		internal Dictionary<string, Enum> EnumNames;
		#endregion

		internal ReflectionCache (Type type) {
			Type = type;
			TypeName = type.FullName;
			AssemblyName = type.AssemblyQualifiedName;

			JsonDataType = Reflection.GetJsonDataType (type);
			SerializeMethod = JsonSerializer.GetWriteJsonMethod (type);
			DeserializeMethod = JsonDeserializer.GetReadJsonMethod (type);

			if (JsonDataType == JsonDataType.Enum) {
				IsFlaggedEnum = AttributeHelper.HasAttribute<FlagsAttribute> (type, false);
				return;
			}

			if (type.IsArray) {
				ArgumentTypes = new Type[] { type.GetElementType () };
				CommonType = type.GetArrayRank () == 1 ? ComplexType.Array : ComplexType.MultiDimensionalArray;
			}
			else {
				var t = type;
				if (t.IsGenericType == false) {
					while ((t = t.BaseType) != null) {
						if (t.IsGenericType) {
							break;
						}
					}
				}
				if (t != null) {
					ArgumentTypes = t.GetGenericArguments ();
					var gt = t.GetGenericTypeDefinition ();
					if (gt.Equals (typeof (Dictionary<,>))) {
						CommonType = ComplexType.Dictionary;
					}
					else if (gt.Equals (typeof (List<>))) {
						CommonType = ComplexType.List;
					}
					else if (gt.Equals (typeof (Nullable<>))) {
						CommonType = ComplexType.Nullable;
						SerializeMethod = JsonSerializer.GetWriteJsonMethod (ArgumentTypes[0]);
					}
				}
			}
			if (typeof(IEnumerable).IsAssignableFrom (type)) {
				if (typeof(Array).IsAssignableFrom (type) == false) {
					AppendItem = Reflection.CreateWrapperMethod<AddCollectionItem> (Reflection.FindMethod (type, "Add", new Type[1] { null }));
				}
				if (ArgumentTypes != null && ArgumentTypes.Length == 1) {
					ItemSerializer = JsonSerializer.GetWriteJsonMethod (ArgumentTypes[0]);
					ItemDeserializer = JsonDeserializer.GetReadJsonMethod (ArgumentTypes[0]);
				}
			}
			if (ArgumentTypes != null) {
				ArgumentReflections = new ReflectionCache[ArgumentTypes.Length];
			}
			if (CommonType != ComplexType.Array
				&& CommonType != ComplexType.MultiDimensionalArray
				&& CommonType != ComplexType.Nullable) {
				var t = type;
				if (type.IsNested == false && type.IsPublic == false) {
					ConstructorInfo |= ConstructorTypes.NonPublic;
				}
				else {
					while (t != null && t.IsNested) {
						if (t.IsNestedPublic == false) {
							ConstructorInfo |= ConstructorTypes.NonPublic;
						}
						t = t.DeclaringType;
					}
				}
				if (type.IsClass || type.IsValueType) {
					Constructor = Reflection.CreateConstructorMethod (type, type.IsVisible == false || typeof (DatasetSchema).Equals (type));
					if (Constructor != null && Constructor.Method.IsPublic == false) {
						ConstructorInfo |= ConstructorTypes.NonPublic;
					}
					if (Constructor == null) {
						var c = type.GetConstructors (BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
						if (c != null && c.Length > 0) {
							ConstructorInfo |= ConstructorTypes.Parametric;
						}
					}

					Members = Reflection.GetMembers (type);
				}
			}
			//if (typeof (IEnumerable).IsAssignableFrom (type)) {
			//	return;
			//}
			//if (JsonDataType != JsonDataType.Undefined) {
			//	return;
			//}
		}

		/// <summary>
		/// Creates an instance of the type by calling its parameterless constructor.
		/// </summary>
		/// <returns>The created instance.</returns>
		public object Instantiate () {
			if (Constructor == null) {
				return null;
			}
			if (ConstructorInfo != ConstructorTypes.Default && AlwaysDeserializable == false) {
				throw new JsonSerializationException ("The constructor of type \"" + TypeName + "\" from assembly \"" + AssemblyName + "\" is not publicly visible.");
			}
			try {
				return Constructor ();
			}
			catch (Exception ex) {
				throw new JsonSerializationException (string.Format (@"Failed to fast create instance for type ""{0}"" from assembly ""{1}""", TypeName, AssemblyName), ex);
			}
		}

		internal JsonMemberGetter FindGetters (string memberName) {
			return Array.Find (Getters, (i) => { return i.MemberName == memberName; });
		}

		internal MemberCache FindMemberCache (string memberName) {
			return Array.Find (Members, (i) => { return i.MemberName == memberName; });
		}

		internal List<JsonMemberSetter> FindProperties (string memberName) {
			var r = new List<JsonMemberSetter> ();
			foreach (var item in Setters) {
				if (item.Value.Member.MemberName == memberName) {
					r.Add (item.Value);
				}
			}
			return r;
		}
	}

	/// <summary>
	/// Contains information about a member, used in reflection phase before serialization.
	/// </summary>
	/// <preliminary/>
	public interface IMemberInfo
	{
		/// <summary>
		/// The name of the field or property.
		/// </summary>
		string MemberName { get; }
		/// <summary>
		/// The type of the member.
		/// </summary>
		Type MemberType { get; }
		/// <summary>
		/// True if the member is a property, false for a field.
		/// </summary>
		bool IsProperty { get; }
		/// <summary>
		/// Indicates whether the member is read-only. Read-only properties or initialized-only fields returns true.
		/// </summary>
		bool IsReadOnly { get; }
		/// <summary>
		/// Indicates whether the member is static.
		/// </summary>
		bool IsStatic { get; }
		/// <summary>
		/// Indicates whether the member is publicly visible.
		/// </summary>
		bool IsPublic { get; }
	}

	/// <summary>
	/// Caches reflection information for a member
	/// </summary>
	[DebuggerDisplay ("{MemberName} ({MemberType.Name}, public={HasPublicGetter},{HasPublicSetter})")]
	sealed class MemberCache : IMemberInfo
	{
		internal readonly string MemberName;
		internal readonly Type MemberType;
		internal readonly MemberInfo MemberInfo; // PropertyInfo or FieldInfo
		internal ReflectionCache MemberTypeReflection;

		internal readonly bool HasPublicGetter;
		internal readonly bool HasPublicSetter;
		internal readonly bool IsStatic;
		internal readonly bool IsProperty;
		internal readonly bool IsReadOnly;
		internal readonly bool IsCollection;
		internal readonly bool IsClass;
		internal readonly bool IsValueType;
		internal readonly bool IsStruct;
		internal readonly bool IsNullable;

		internal readonly JsonDataType JsonDataType;
		internal readonly Type ElementType; // bt
		internal readonly Type ChangeType; // nullable ? elementtype : membertype
		internal readonly WriteJsonValue SerializeMethod;
		internal readonly RevertJsonValue DeserializeMethod;
		internal readonly GenericGetter Getter;
		internal readonly GenericSetter Setter;

		#region IMemberInfo
		string IMemberInfo.MemberName { get { return MemberName; } }
		Type IMemberInfo.MemberType { get { return MemberType; } }
		bool IMemberInfo.IsProperty { get { return IsProperty; } }
		bool IMemberInfo.IsReadOnly { get { return IsReadOnly; } }
		bool IMemberInfo.IsPublic { get { return HasPublicGetter || HasPublicSetter; } }
		bool IMemberInfo.IsStatic { get { return IsStatic; } }
		#endregion

		public MemberCache (PropertyInfo property)
			: this (property, property.PropertyType, property.Name) {
			Getter = Reflection.CreateGetProperty (property);
			Setter = Reflection.CreateSetProperty (property);
			HasPublicGetter = property.GetGetMethod () != null;
			HasPublicSetter = property.GetSetMethod () != null;
			IsProperty = true;
			IsStatic = (property.GetGetMethod (true) ?? property.GetSetMethod (true)).IsStatic;
			IsReadOnly = property.GetSetMethod () == null; // property.CanWrite can return true if the setter is non-public
		}
		public MemberCache (FieldInfo field)
			: this (field, field.FieldType, field.Name) {
			Getter = Reflection.CreateGetField (field);
			Setter = Reflection.CreateSetField (field);
			HasPublicGetter = HasPublicSetter = field.IsPublic;
			IsStatic = field.IsStatic;
			IsReadOnly = field.IsInitOnly;
		}
		public MemberCache (Type type, string name, MemberCache baseInfo)
			: this (baseInfo.MemberInfo, type, name) {
			Getter = baseInfo.Getter;
			Setter = baseInfo.Setter;
			IsProperty = baseInfo.IsProperty;
			IsStatic = baseInfo.IsStatic;
			IsReadOnly = baseInfo.IsReadOnly;
		}

		MemberCache (MemberInfo memberInfo, Type memberType, string name) {
			MemberName = name;
			MemberType = memberType;
			MemberInfo = memberInfo;
			JsonDataType dt = Reflection.GetJsonDataType (memberType);
			DeserializeMethod = JsonDeserializer.GetReadJsonMethod (memberType);
			SerializeMethod = JsonSerializer.GetWriteJsonMethod (memberType);

			if (dt == JsonDataType.Array || dt == JsonDataType.MultiDimensionalArray) {
				ElementType = memberType.GetElementType ();
			}

			IsValueType = memberType.IsValueType;
			IsStruct = (IsValueType && !memberType.IsPrimitive && !memberType.IsEnum && typeof (decimal).Equals (memberType) == false);
			IsClass = memberType.IsClass;
			IsCollection = typeof (ICollection).IsAssignableFrom (memberType) && typeof (byte[]).Equals (memberType) == false;
			if (memberType.IsGenericType) {
				ElementType = memberType.GetGenericArguments ()[0];
				IsNullable = memberType.GetGenericTypeDefinition ().Equals (typeof (Nullable<>));
			}
			if (IsValueType) {
				ChangeType = IsNullable ? ElementType : memberType;
			}
			JsonDataType = dt;
		}
	}

	[DebuggerDisplay ("{MemberName} ({SerializedName})")]
	sealed class JsonMemberGetter
	{
		internal readonly MemberCache Member;
		internal readonly string MemberName;

		internal TriState Serializable;

		internal bool SpecificName;
		internal string SerializedName;
		internal IDictionary<Type, string> TypedNames;

		internal bool HasNonSerializedValue;
		internal object[] NonSerializedValues;

		internal IJsonConverter Converter;
		internal IJsonConverter ItemConverter;

		public JsonMemberGetter (MemberCache cache) {
			Member = cache;
			MemberName = cache.MemberName;
			SerializedName = cache.MemberName;
		}
	}

	[DebuggerDisplay ("{MemberName} ({JsonDataType})")]
	sealed class JsonMemberSetter // myPropInfo
	{
		internal readonly string MemberName;
		internal readonly MemberCache Member;

		internal bool CanWrite;
		internal IJsonConverter Converter;
		internal IJsonConverter ItemConverter;

		public JsonMemberSetter (MemberCache member) {
			MemberName = member.MemberName;
			Member = member;
			CanWrite = member.IsReadOnly == false;
		}
	}

}
