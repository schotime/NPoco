using System.Text;

namespace NPoco.FastJSON
{
	static class Formatter
	{
		public static string Indent = "   ";

		public static void AppendIndent(StringBuilder sb, int count)
		{
			for (; count > 0; --count) sb.Append(Indent);
		}

		public static string PrettyPrint (string input) {
			return PrettyPrint (input, false);
		}

		public static string PrettyPrint(string input, bool decodeUnicode)
		{
			var output = new StringBuilder();
			int depth = 0;
			int len = input.Length;
			char[] chars = input.ToCharArray();
			for (int i = 0; i < len; ++i)
			{
				char ch = chars[i];

				if (ch == '\"') // found string span
				{
					bool str = true;
					while (str)
					{
						output.Append(ch);
						ch = chars[++i];
						if (ch == '\\')
						{
							if (decodeUnicode && chars[i + 1] == 'u' && i + 6 < len) {
								ch = (char)System.Convert.ToUInt16 (new string(chars, i + 2, 4), 16);
								i += 5;
							}
							else {
								output.Append (ch);
								ch = chars[++i];
							}
						}
						else if (ch == '\"')
							str = false;
					}
				}

				switch (ch)
				{
					case '{':
					case '[':
						output.Append(ch);
						//if (chars[i+1] == ch+2) {
						//	output.Append ((char)(ch + 2));
						//	++i;
						//	break;
						//}
						output.AppendLine();
						AppendIndent(output, ++depth);
						break;
					case '}':
					case ']':
						output.AppendLine();
						AppendIndent(output, --depth);
						output.Append(ch);
						break;
					case ',':
						output.Append(ch);
						output.AppendLine();
						AppendIndent(output, depth);
						break;
					case ':':
						output.Append(" : ");
						break;
					default:
						if (!char.IsWhiteSpace(ch))
							output.Append(ch);
						break;
				}
			}

			return output.ToString();
		}
	}
}