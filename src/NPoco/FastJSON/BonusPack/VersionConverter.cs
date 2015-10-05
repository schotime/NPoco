using System;

namespace NPoco.FastJSON.BonusPack
{
	/// <summary>
	/// An <see cref="IJsonConverter"/> which converts between <see cref="Version"/> and string.
	/// </summary>
	class VersionConverter : JsonConverter<Version, string>
	{
		protected override string Convert (string fieldName, Version fieldValue) {
			return fieldValue != null ? fieldValue.ToString () : null;
		}

		protected override Version Revert (string fieldName, string fieldValue) {
			try {
				return fieldValue != null ? new Version (fieldValue) : null;
			}
			catch (Exception) {
				throw new JsonSerializationException ("Error parsing version string: " + fieldValue);
			}
		}
	}
}
