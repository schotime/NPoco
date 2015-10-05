using System;

namespace NPoco.FastJSON
{
	/// <summary>
	/// Controls the naming format of serialized enum values.
	/// </summary>
	public enum EnumValueFormat
	{
		/// <summary>
		/// The serialized names will be the same as the field name.
		/// </summary>
		Default,
		/// <summary>
		/// All letters in the serialized names will be changed to lowercase.
		/// </summary>
		LowerCase,
		/// <summary>
		/// The first letter of each serialized names will be changed to lowercase.
		/// </summary>
		CamelCase,
		/// <summary>
		/// All letters in the serialized names will be changed to uppercase.
		/// </summary>
		UpperCase,
		/// <summary>
		/// Enum fields will be serialized numerically.
		/// </summary>
		Numeric
	}

	/// <summary>
	/// Controls the letter case of serialized field names.
	/// </summary>
	public enum NamingConvention
	{
		/// <summary>
		/// The letter case of the serialized field names will be the same as the field or member name.
		/// </summary>
		Default,
		/// <summary>
		/// All letters in the serialized field names will be changed to lowercase.
		/// </summary>
		LowerCase,
		/// <summary>
		/// The first letter of each serialized field names will be changed to lowercase.
		/// </summary>
		CamelCase,
		/// <summary>
		/// All letters in the serialized field names will be changed to uppercase.
		/// </summary>
		UpperCase
	}

	enum JsonDataType // myPropInfoType
	{
		Undefined,
		Int,
		Long,
		String,
		Bool,
		Single,
		Double,
		DateTime,
		Enum,
		Guid,
		TimeSpan,

		Array,
		List,
		ByteArray,
		MultiDimensionalArray,
		Dictionary,
		StringKeyDictionary,
		NameValue,
		StringDictionary,
#if !SILVERLIGHT
		Hashtable,
		DataSet,
		DataTable,
#endif
		Custom,
		Primitive,
		Object
	}

	/// <summary>Indicates the state of a setting.</summary>
	enum TriState
	{
		/// <summary>Represents the normal behavior.</summary>
		Default,
		/// <summary>Represents a positive setting. Actions should be taken to the object.</summary>
		True,
		/// <summary>Represents a negative setting. Actions may not be taken to the object.</summary>
		False
	}

	[Flags]
	enum ConstructorTypes
	{
		// public, parameterless
		Default = 0,
		NonPublic = 1,
		Parametric = 2
	}

	enum ComplexType
	{
		General,
		Array,
		MultiDimensionalArray,
		Dictionary,
		List,
		Nullable
	}

	static class Constants
	{
		internal static TriState ToTriState (bool? value) {
			return value.HasValue
				? (bool)value ? TriState.True : TriState.False
				: TriState.Default;
		}
		internal static bool? ToBoolean (TriState value) {
			return value == TriState.True ? true
				: value == TriState.False ? false
				: (bool?)null;
		}
	}
}
