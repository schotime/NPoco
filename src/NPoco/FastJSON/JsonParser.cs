using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NPoco.FastJSON
{
	/// <summary>
	/// This class encodes and decodes JSON strings.
	/// Spec. details, see http://www.json.org/
	/// </summary>
	sealed class JsonParser
	{
		enum Token
		{
			None,           // Used to denote no Lookahead available
			Curly_Open,
			Curly_Close,
			Squared_Open,
			Squared_Close,
			Colon,
			Comma,
			String,
			Number,
			True,
			False,
			Null
		}
		readonly static Token[] _CharTokenMap = InitCharTokenMap ();
		readonly string _json;
		readonly StringBuilder _sb = new StringBuilder ();
		Token _lookAheadToken = Token.None;
		int _index;

		static Token[] InitCharTokenMap () {
			var t = new Token[0x7F];
			t['{'] = Token.Curly_Open;
			t['}'] = Token.Curly_Close;
			t[':'] = Token.Colon;
			t[','] = Token.Comma;
			t['.'] = Token.Number;
			t['['] = Token.Squared_Open;
			t[']'] = Token.Squared_Close;
			t['\"'] = Token.String;
			for (int i = '0'; i <= '9'; i++) {
				t[(char)i] = Token.Number;
			}
			t['-'] = t['+'] = t['.'] = Token.Number;
			return t;
		}
		internal JsonParser (string json) {
			_json = json;
		}

		public object Decode () {
			return ParseValue ();
		}

		JsonDict ParseObject () {
			var table = new JsonDict ();

			ConsumeToken (); // {

			while (true) {
				switch (LookAhead ()) {

					case Token.Comma:
						ConsumeToken ();
						break;

					case Token.Curly_Close:
						ConsumeToken ();
						return table;

					default:
						{
							// name
							string name = ParseString ();

							// :
							if (NextToken () != Token.Colon) {
								throw new JsonParserException ("Expected colon at index ", _index, GetContextText ());
							}

							// value
							object value = ParseValue ();

							if (name.Length == 0) {
								// ignores unnamed item
								continue;
							}
							if (name[0] == '$') {
								switch (name) {
									case JsonDict.ExtTypes: table.Types = (JsonDict)value; continue;
									case JsonDict.ExtType: table.Type = (string)value; continue;
									case JsonDict.ExtRefIndex: table.RefIndex = (int)(long)value; continue;
									case JsonDict.ExtSchema: table.Schema = value; continue;
									default:
										break;
								}
							}
							table.Add (new KeyValuePair<string, object> (name, value));
						}
						break;
				}
			}
		}

		string GetContextText () {
			const int ContextLength = 20;
			var s = _index < ContextLength ? _index : ContextLength;
			var e = _index + ContextLength > _json.Length ? _json.Length - _index : ContextLength;
			return string.Concat (_json.Substring (_index - s, s), "^ERROR^", _json.Substring (_index, e));
		}

		JsonArray ParseArray () {
			var array = new JsonArray ();
			ConsumeToken (); // [

			while (true) {
				switch (LookAhead ()) {
					case Token.Comma:
						ConsumeToken ();
						break;

					case Token.Squared_Close:
						ConsumeToken ();
						return array;

					default:
						array.Add (ParseValue ());
						break;
				}
			}
		}

		object ParseValue () {
			switch (LookAhead ()) {
				case Token.Number:
					return ParseNumber ();

				case Token.String:
					return ParseString ();

				case Token.Curly_Open:
					return ParseObject ();

				case Token.Squared_Open:
					return ParseArray ();

				case Token.True:
					ConsumeToken ();
					return true;

				case Token.False:
					ConsumeToken ();
					return false;

				case Token.Null:
					ConsumeToken ();
					return null;
			}

			throw new JsonParserException ("Unrecognized token at index ", _index, GetContextText ());
		}

		string ParseString () {
			ConsumeToken (); // "

			_sb.Length = 0;

			int runIndex = -1;

			while (_index < _json.Length) {
				var c = _json[_index++];

				if (c == '"') {
					if (runIndex != -1) {
						if (_sb.Length == 0)
							return _json.Substring (runIndex, _index - runIndex - 1);

						_sb.Append (_json, runIndex, _index - runIndex - 1);
					}
					return _sb.ToString ();
				}

				if (c != '\\') {
					if (runIndex == -1)
						runIndex = _index - 1;

					continue;
				}

				if (_index == _json.Length) break;

				if (runIndex != -1) {
					_sb.Append (_json, runIndex, _index - runIndex - 1);
					runIndex = -1;
				}

				switch (_json[_index++]) {
					case '"':
						_sb.Append ('"');
						break;

					case '\\':
						_sb.Append ('\\');
						break;

					case '/':
						_sb.Append ('/');
						break;

					case 'b':
						_sb.Append ('\b');
						break;

					case 'f':
						_sb.Append ('\f');
						break;

					case 'n':
						_sb.Append ('\n');
						break;

					case 'r':
						_sb.Append ('\r');
						break;

					case 't':
						_sb.Append ('\t');
						break;

					case 'u':
						{
							int remainingLength = _json.Length - _index;
							if (remainingLength < 4) break;

							// parse the 32 bit hex into an integer code point
							// skip 4 chars
							_sb.Append (ValueConverter.ParseUnicode (_json[_index], _json[++_index], _json[++_index], _json[++_index]));
							++_index;
						}
						break;
				}
			}

			throw new JsonParserException ("Unexpectedly reached end of string: ", _json.Length, GetContextText ());
		}

		object ParseNumber () {
			ConsumeToken ();

			// Need to start back one place because the first digit is also a token and would have been consumed
			var startIndex = _index - 1;
			bool dec = false;
			do {
				if (_index == _json.Length)
					break;
				var c = _json[_index];

				if ((c >= '0' && c <= '9') || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E') {
					if (c == '.' || c == 'e' || c == 'E')
						dec = true;
					if (++_index == _json.Length)
						break;//throw new Exception("Unexpected end of string whilst parsing number");
					continue;
				}
				break;
			} while (true);

			if (dec) {
				string s = _json.Substring (startIndex, _index - startIndex);
				return double.Parse (s, NumberFormatInfo.InvariantInfo);
			}
			return ValueConverter.CreateLong (_json, startIndex, _index - startIndex);
		}

		Token LookAhead () {
			if (_lookAheadToken != Token.None) return _lookAheadToken;

			return _lookAheadToken = NextTokenCore ();
		}

		void ConsumeToken () {
			_lookAheadToken = Token.None;
		}

		Token NextToken () {
			var result = _lookAheadToken != Token.None ? _lookAheadToken : NextTokenCore ();

			_lookAheadToken = Token.None;

			return result;
		}

		Token NextTokenCore () {
			char c;

			// Skip past whitespace
			while (_index < _json.Length) {
				c = _json[_index];

				if (c > ' ') break;
				if (c != ' ' && c != '\t' && c != '\n' && c != '\r') break;

				++_index;
			}

			if (_index == _json.Length) {
				throw new JsonParserException ("Reached end of string unexpectedly: ", _json.Length, GetContextText ());
			}

			c = _json[_index];

			_index++;

			var t = _CharTokenMap[c];
			if (t != Token.None) {
				return t;
			}

			switch (c) {
				case 'f':
					if (_json.Length - _index >= 4 &&
						_json[_index + 0] == 'a' &&
						_json[_index + 1] == 'l' &&
						_json[_index + 2] == 's' &&
						_json[_index + 3] == 'e') {
						_index += 4;
						return Token.False;
					}
					break;
				case 't':
					if (_json.Length - _index >= 3 &&
						_json[_index + 0] == 'r' &&
						_json[_index + 1] == 'u' &&
						_json[_index + 2] == 'e') {
						_index += 3;
						return Token.True;
					}
					break;
				case 'n':
					if (_json.Length - _index >= 3 &&
						_json[_index + 0] == 'u' &&
						_json[_index + 1] == 'l' &&
						_json[_index + 2] == 'l') {
						_index += 3;
						return Token.Null;
					}
					break;
			}
			throw new JsonParserException ("Could not find token at index ", --_index, GetContextText ());
		}
	}

	sealed class JsonArray : List<object> { }
	sealed class JsonDict : List<KeyValuePair<string, object>>, IDictionary<string, object>
	{
		internal const string ExtRefIndex = "$i";
		internal const string ExtTypes = "$types";
		internal const string ExtType = "$type";
		internal const string ExtSchema = "$schema";

		internal int RefIndex;
		internal JsonDict Types;
		internal string Type;
		internal object Schema;

		public object this[string key] {
			get {
				foreach (var item in this) {
					if (item.Key == key) {
						return item.Value;
					}
				}
				return null;
			}
			set {
				for (int i = Count - 1; i >= 0; i--) {
					if (this[i].Key == key) {
						this[i] = new KeyValuePair<string, object> (key, value);
						return;
					}
				}
			}
		}

		public void Add (string key, object value) {
			Add (new KeyValuePair<string, object> (key, value));
		}

		ICollection<string> IDictionary<string, object>.Keys {
			get {
				var k = new string[Count];
				for (int i = Count - 1; i >= 0; i--) {
					k[i] = this[i].Key;
				}
				return k;
			}
		}

		ICollection<object> IDictionary<string, object>.Values {
			get {
				var v = new object[Count];
				for (int i = Count - 1; i >= 0; i--) {
					v[i] = this[i].Value;
				}
				return v;
			}
		}

		public bool ContainsKey (string key) {
			foreach (var item in this) {
				if (item.Key == key) {
					return true;
				}
			}
			return false;
		}

		public bool Remove (string key) {
			for (int i = Count - 1; i >= 0; i--) {
				if (this[i].Key == key) {
					RemoveAt (i);
					return true;
				}
			}
			return false;
		}

		bool IDictionary<string, object>.TryGetValue (string key, out object value) {
			foreach (var item in this) {
				if (item.Key == key) {
					value = item.Value;
					return true;
				}
			}
			value = null;
			return false;
		}

		public static explicit operator Dictionary<string, object>(JsonDict dict) {
			return new Dictionary<string, object> (dict);
		}
	}

	sealed class DatasetSchema
	{
		public List<string> Info;//{ get; set; }
		public string Name;//{ get; set; }
	}
}
