using System;

namespace NPoco.FastJSON
{
	static class ValueConverter
	{
		internal static char ParseUnicode (char c1, char c2, char c3, char c4) {
			return (char)((ParseSingleChar (c1) << 12)
				+ (ParseSingleChar (c2) << 8)
				+ (ParseSingleChar (c3) << 4)
				+ ParseSingleChar (c4));
		}
		static int ParseSingleChar (char c1) {
			if (c1 >= '0' && c1 <= '9')
				return (c1 - '0');
			else if (c1 >= 'A' && c1 <= 'F')
				return ((c1 - 'A') + 10);
			else if (c1 >= 'a' && c1 <= 'f')
				return ((c1 - 'a') + 10);
			return 0;
		}

		internal static long CreateLong (string s, int index, int count) {
			long num = 0;
			bool neg = false;
			for (int x = 0; x < count; x++, index++) {
				char cc = s[index];

				if (cc == '-')
					neg = true;
				else if (cc == '+')
					neg = false;
				else {
					num = (num << 3) + (num << 1); // *= 10
					num += (cc - '0');
				}
			}
			if (neg) num = -num;

			return num;
		}

		internal static int ToInt32 (string s, int index, int count) {
			int num = 0;
			bool neg = false;
			for (int x = 0; x < count; x++, index++) {
				char cc = s[index];

				if (cc == '-')
					neg = true;
				else if (cc == '+')
					neg = false;
				else {
					num = (num << 3) + (num << 1); // *= 10;
					num += (cc - '0');
				}
			}
			if (neg) num = -num;

			return num;
		}
		internal static string ToFixedWidthString (int value, int digits) {
			var chs = new char[digits];
			for (int i = chs.Length - 1; i >= 0; i--) {
				chs[i] = (char)('0' + (value % 10));
				value /= 10;
			}
			return new string (chs);
		}

		internal static string Int64ToString (long value) {
			var n = false;
			var d = 20;
			if (value < 0) {
				if (value == Int64.MinValue) {
					return "-9223372036854775808";
				}
				n = true;
				value = -value;
			}
			if (value < 10L) {
				d = 2;
			}
			else if (value < 1000L) {
				d = 4;
			}
			else if (value < 1000000L) {
				d = 7;
			}
			var chs = new char[d];
			var i = d;
			while (--i > 0) {
				chs[i] = (char)('0' + (value % 10L));
				value /= 10L;
				if (value == 0L) {
					break;
				}
			}
			if (n) {
				chs[--i] = '-';
			}
			return new string (chs, i, d - i);
		}
		internal static string UInt64ToString (ulong value) {
			var d = 20;
			if (value < 10UL) {
				d = 2;
			}
			else if (value < 1000UL) {
				d = 4;
			}
			else if (value < 1000000UL) {
				d = 7;
			}
			var chs = new char[d];
			var i = d;
			while (--i > 0) {
				chs[i] = (char)('0' + (value % 10UL));
				value /= 10UL;
				if (value == 0UL) {
					break;
				}
			}
			return new string (chs, i, d - i);
		}
		internal static string Int32ToString (int value) {
			var n = false;
			var d = 11;
			if (value < 0) {
				if (value == Int32.MinValue) {
					return "-2147483648";
				}
				n = true;
				value = -value;
			}
			if (value < 10) {
				d = 2;
			}
			else if (value < 1000) {
				d = 4;
			}
			var chs = new char[d];
			var i = d;
			while (--i > 0) {
				chs[i] = (char)('0' + (value % 10));
				value /= 10;
				if (value == 0) {
					break;
				}
			}
			if (n) {
				chs[--i] = '-';
			}
			return new string (chs, i, d - i);
		}
	}
}
