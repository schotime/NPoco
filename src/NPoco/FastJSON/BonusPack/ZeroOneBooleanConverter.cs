using System;

namespace NPoco.FastJSON.BonusPack
{
	/// <summary>
	/// A <see cref="IJsonConverter"/> converts boolean values to 1/0 or "1"/"0", rather than the default "true" and "false" values.
	/// </summary>
	class ZeroOneBooleanConverter : IJsonConverter
	{
		/// <summary>
		/// Creates an instance of <see cref="ZeroOneBooleanConverter"/>.
		/// </summary>
		public ZeroOneBooleanConverter () { }

		/// <summary>
		/// Creates an instance of <see cref="ZeroOneBooleanConverter"/>, specifying whether the boolean values should be serialized to textual "1"/"0" values.
		/// </summary>
		/// <param name="useTextualForm">When this value is true, the boolean values will be serialized to textual "1"/"0" values.</param>
		public ZeroOneBooleanConverter (bool useTextualForm) {
			UseTextualForm = useTextualForm;
		}

		/// <summary>
		/// Gets whether the boolean values should be serialized to textual "1"/"0" values.
		/// </summary>
		public bool UseTextualForm { get; private set; }

		void IJsonConverter.DeserializationConvert (JsonItem item) {
			var v = item._Value;
			if (v == null) {
				item._Value = false;
			}
			var s = v as string;
			if (s != null) {
				item._Value = s.Trim () != "0";
			}
			if (v is long) {
				item._Value = (long)v != 0L;
			}
		}

		Type IJsonConverter.GetReversiveType (JsonItem item) {
			return null;
		}

		void IJsonConverter.SerializationConvert (JsonItem item) {
			if (UseTextualForm) {
				item._Value = (bool)item._Value ? "1" : "0";
			}
			else {
				item._Value = (bool)item._Value ? 1 : 0;
			}
		}
	}
}
