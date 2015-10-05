using System;
using System.Net;

namespace NPoco.FastJSON.BonusPack
{
	/// <summary>
	/// An <see cref="IJsonConverter"/> which converts between <see cref="IPAddress"/> and string.
	/// </summary>
	class IPAddressConverter : JsonConverter<System.Net.IPAddress, string>
	{
		protected override string Convert (string fieldName, IPAddress fieldValue) {
			return fieldValue != null ? fieldValue.ToString () : null;
		}

		protected override IPAddress Revert (string fieldName, string fieldValue) {
			try {
				return fieldValue != null ? IPAddress.Parse (fieldValue) : null;
			}
			catch (Exception) {
				throw new JsonSerializationException ("Error parsing IP Address: " + fieldValue);
			}

		}
	}
}
