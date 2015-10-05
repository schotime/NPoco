using System.Text.RegularExpressions;

namespace NPoco.FastJSON.BonusPack
{
	class RegexConverter : JsonConverter<Regex, RegexConverter.RegexInfo>
	{
		protected override RegexInfo Convert (string fieldName, Regex fieldValue) {
			return new RegexInfo () { Pattern = fieldValue.ToString (), Options = fieldValue.Options };
		}

		protected override Regex Revert (string fieldName, RegexInfo fieldValue) {
			return new Regex (fieldValue.Pattern, fieldValue.Options);
		}

		[JsonSerializable]
		internal struct RegexInfo
		{
			public string Pattern;
			public RegexOptions Options;
		}
	}
}
