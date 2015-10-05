using System;

namespace NPoco.FastJSON.BonusPack
{
	class UriConverter : JsonConverter<Uri, string>
	{
		protected override string Convert (string fieldName, Uri fieldValue) {
			return fieldValue.OriginalString;
		}

		protected override Uri Revert (string fieldName, string fieldValue) {
			return new Uri (fieldValue);
		}
	}
}
