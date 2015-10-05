#if !SILVERLIGHT
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;

namespace NPoco.FastJSON
{
	sealed class JsonSerializer
	{
		static readonly WriteJsonValue[] _convertMethods = RegisterMethods ();

		StringBuilder _output = new StringBuilder ();
		readonly int _maxDepth = 20;
		int _currentDepth;
		int _before;
		readonly Dictionary<string, int> _globalTypes = new Dictionary<string, int> ();
		readonly Dictionary<object, int> _cirobj = new Dictionary<object, int> ();
		readonly JSONParameters _params;
		readonly SerializationManager _manager;
		bool _useGlobalTypes;
		readonly bool _useEscapedUnicode, _useExtensions, _showReadOnlyProperties, _showReadOnlyFields;
		readonly NamingStrategy _naming;

		internal JsonSerializer (JSONParameters param, SerializationManager manager) {
			_manager = manager;
			_params = param;
			_useEscapedUnicode = _params.UseEscapedUnicode;
			_maxDepth = _params.SerializerMaxDepth;
			_naming = _params.NamingStrategy;
			if (_params.EnableAnonymousTypes) {
				_useExtensions = _useGlobalTypes = false;
				_showReadOnlyFields = _showReadOnlyProperties = true;
			}
			else {
				_useExtensions = _params.UseExtensions;
				_useGlobalTypes = _params.UsingGlobalTypes && _useExtensions;
				_showReadOnlyProperties = _params.ShowReadOnlyProperties;
				_showReadOnlyFields = _params.ShowReadOnlyFields;
			}
		}

		internal string ConvertToJSON (object obj, ReflectionCache cache) {
			if (cache.CommonType == ComplexType.Dictionary || cache.CommonType == ComplexType.List) {
				_useGlobalTypes = false;
			}

			var cv = cache.Converter;
			if (cv != null) {
				var ji = new JsonItem (String.Empty, obj, false);
				cv.SerializationConvert (ji);
				if (ReferenceEquals (obj, ji._Value) == false) {
					obj = ji._Value;
				}
				cache = _manager.GetReflectionCache (obj.GetType ());
			}
			var m = cache.SerializeMethod;
			if (m != null) {
				if (cache.CollectionName != null) {
					WriteObject (obj);
				}
				else {
					m (this, obj);
				}
			}
			else {
				WriteValue (obj);
			}

			if (_useGlobalTypes && _globalTypes != null && _globalTypes.Count > 0) {
				var sb = new StringBuilder ();
				sb.Append ("\"" + JsonDict.ExtTypes + "\":{");
				var pendingSeparator = false;
				foreach (var kv in _globalTypes) {
					sb.Append (pendingSeparator ? ",\"" : "\"");
					sb.Append (kv.Key);
					sb.Append ("\":\"");
					sb.Append (ValueConverter.Int32ToString (kv.Value));
					sb.Append ('\"');
					pendingSeparator = true;
				}
				sb.Append ("},");
				_output.Insert (_before, sb.ToString ());
			}
			return _output.ToString ();
		}

		static WriteJsonValue[] RegisterMethods () {
			var r = new WriteJsonValue[Enum.GetNames (typeof (JsonDataType)).Length];
			r[(int)JsonDataType.Array] = WriteArray;
			r[(int)JsonDataType.Bool] = WriteBoolean;
			r[(int)JsonDataType.ByteArray] = WriteByteArray;
			r[(int)JsonDataType.Custom] = WriteCustom;
			r[(int)JsonDataType.DataSet] = WriteDataSet;
			r[(int)JsonDataType.DataTable] = WriteDataTable;
			r[(int)JsonDataType.DateTime] = WriteDateTime;
			r[(int)JsonDataType.Dictionary] = WriteDictionary;
			r[(int)JsonDataType.Double] = WriteDouble;
			r[(int)JsonDataType.Enum] = WriteEnum;
			r[(int)JsonDataType.List] = WriteArray;
			r[(int)JsonDataType.Guid] = WriteGuid;
			r[(int)JsonDataType.Hashtable] = WriteDictionary;
			r[(int)JsonDataType.Int] = WriteInt32;
			r[(int)JsonDataType.Long] = WriteInt64;
			r[(int)JsonDataType.MultiDimensionalArray] = WriteMultiDimensionalArray;
			r[(int)JsonDataType.NameValue] = WriteNameValueCollection;
			r[(int)JsonDataType.Object] = WriteUnknown;
			r[(int)JsonDataType.Single] = WriteSingle;
			r[(int)JsonDataType.String] = WriteString;
			r[(int)JsonDataType.StringDictionary] = WriteStringDictionary;
			r[(int)JsonDataType.StringKeyDictionary] = WriteDictionary;
			r[(int)JsonDataType.TimeSpan] = WriteTimeSpan;
			r[(int)JsonDataType.Undefined] = WriteObject;
			return r;
		}
		void WriteValue (object obj) {
			if (obj == null || obj is DBNull)
				_output.Append ("null");

			else if (obj is string || obj is char) {
				if (_useEscapedUnicode) {
					WriteStringEscapeUnicode (_output, obj.ToString ());
				}
				else {
					WriteString (_output, obj.ToString ());
				}
			}
			else if (obj is bool)
				_output.Append (((bool)obj) ? "true" : "false"); // conform to standard
			else if (obj is int) {
				_output.Append (ValueConverter.Int32ToString ((int)obj));
			}
			else if (obj is long) {
				_output.Append (ValueConverter.Int64ToString ((long)obj));
			}
			else if (obj is double || obj is float || obj is decimal || obj is byte)
				_output.Append (((IConvertible)obj).ToString (NumberFormatInfo.InvariantInfo));

			else if (obj is DateTime)
				WriteDateTime (this, obj);

			else if (obj is Guid)
				WriteGuid (this, obj);

			else {
				var t = obj.GetType ();
				var c = _manager.GetReflectionCache (t);
				if (c.SerializeMethod != null) {
					if (c.CollectionName != null) {
						WriteObject (obj);
					}
					else {
						c.SerializeMethod (this, obj);
					}
				}
				else if (_manager.IsTypeRegistered (obj.GetType ())) {
					WriteCustom (obj);
				}
				else {
					WriteObject (obj);
				}
			}
		}

		void WriteSD (StringDictionary stringDictionary) {
			_output.Append ('{');

			var pendingSeparator = false;

			foreach (DictionaryEntry entry in stringDictionary) {
				if (_params.SerializeNullValues == false && entry.Value == null) {
				}
				else {
					if (pendingSeparator) _output.Append (',');

					_naming.WriteName (_output, (string)entry.Key);
					WriteString (_output, (string)entry.Value);
					pendingSeparator = true;
				}
			}
			_output.Append ('}');
		}

		void WriteCustom (object obj) {
			Serialize s = _manager.GetCustomSerializer (obj.GetType ());
			WriteStringFast (s (obj));
		}

		void WriteBytes (byte[] bytes) {
#if !SILVERLIGHT
			WriteStringFast (Convert.ToBase64String (bytes, 0, bytes.Length, Base64FormattingOptions.None));
#else
			WriteStringFast(Convert.ToBase64String(bytes, 0, bytes.Length));
#endif
		}

#if !SILVERLIGHT
		static DatasetSchema GetSchema (DataTable ds) {
			if (ds == null) return null;

			var m = new DatasetSchema {
				Info = new List<string> (),
				Name = ds.TableName
			};

			foreach (DataColumn c in ds.Columns) {
				m.Info.Add (ds.TableName);
				m.Info.Add (c.ColumnName);
				m.Info.Add (c.DataType.ToString ());
			}
			// FEATURE : serialize relations and constraints here

			return m;
		}

		static DatasetSchema GetSchema (DataSet ds) {
			if (ds == null) return null;

			var m = new DatasetSchema {
				Info = new List<string> (),
				Name = ds.DataSetName
			};

			foreach (DataTable t in ds.Tables) {
				foreach (DataColumn c in t.Columns) {
					m.Info.Add (t.TableName);
					m.Info.Add (c.ColumnName);
					m.Info.Add (c.DataType.ToString ());
				}
			}
			// FEATURE : serialize relations and constraints here

			return m;
		}

		static string GetXmlSchema (DataTable dt) {
			using (var writer = new StringWriter ()) {
				dt.WriteXmlSchema (writer);
				return dt.ToString ();
			}
		}

		void WriteDataset (DataSet ds) {
			_output.Append ('{');
			if (_useExtensions) {
				WritePair (JsonDict.ExtSchema, _params.UseOptimizedDatasetSchema ? (object)GetSchema (ds) : ds.GetXmlSchema ());
				_output.Append (',');
			}
			var tablesep = false;
			foreach (DataTable table in ds.Tables) {
				if (tablesep) _output.Append (',');
				tablesep = true;
				WriteDataTableData (table);
			}
			// end dataset
			_output.Append ('}');
		}

		void WriteDataTableData (DataTable table) {
			_output.Append ('\"');
			_output.Append (table.TableName);
			_output.Append ("\":[");
			var cols = table.Columns;
			var rowseparator = false;
			var cl = cols.Count;
			var w = new WriteJsonValue[cl];
			if (table.Rows.Count > 3) {
				for (int i = w.Length - 1; i >= 0; i--) {
					w[i] = GetWriteJsonMethod (cols[i].DataType);
				}
			}
			else {
				w = null;
			}
			foreach (DataRow row in table.Rows) {
				if (rowseparator) _output.Append (',');
				rowseparator = true;
				_output.Append ('[');

				for (int j = 0; j < cl; j++) {
					if (j > 0) {
						_output.Append (',');
					}
					if (w != null) {
						w[j] (this, row[j]);
					}
					else {
						WriteValue (row[j]);
					}
				}
				_output.Append (']');
			}

			_output.Append (']');
		}

		void WriteDataTable (DataTable dt) {
			_output.Append ('{');
			if (_useExtensions) {
				WritePair (JsonDict.ExtSchema, _params.UseOptimizedDatasetSchema ? (object)GetSchema (dt) : GetXmlSchema (dt));
				_output.Append (',');
			}

			WriteDataTableData (dt);

			_output.Append ('}');
		}
#endif

		// HACK: This is a very long function, individual parts in regions are made inline for better performance
		void WriteObject (object obj) {
			#region Detect Circular Reference
			var ci = 0;
			if (_cirobj.TryGetValue (obj, out ci) == false)
				_cirobj.Add (obj, _cirobj.Count + 1);
			else {
				if (_currentDepth > 0 && _useExtensions && _params.InlineCircularReferences == false) {
					//_circular = true;
					_output.Append ("{\"" + JsonDict.ExtRefIndex + "\":");
					_output.Append (ValueConverter.Int32ToString (ci));
					_output.Append ("}");
					return;
				}
			}
			#endregion
			var def = _manager.GetReflectionCache (obj.GetType ());
			var si = def.Interceptor;
			if (si != null && si.OnSerializing (obj) == false) {
				return;
			}
			#region Locate Extension Insertion Position
			if (_useGlobalTypes == false)
				_output.Append ('{');
			else {
				if (_before == 0) {
					_output.Append ('{');
					_before = _output.Length;
				}
				else
					_output.Append ('{');
			}
			#endregion

			_currentDepth++;
			if (_currentDepth > _maxDepth)
				throw new JsonSerializationException ("Serializer encountered maximum depth of " + _maxDepth);

			//var map = new Dictionary<string, string> ();
			var append = false;
			#region Write Type Reference
			if (_useExtensions) {
				if (_useGlobalTypes == false)
					WritePairFast (JsonDict.ExtType, def.AssemblyName);
				else {
					var dt = 0;
					var ct = def.AssemblyName;
					if (_globalTypes.TryGetValue (ct, out dt) == false) {
						dt = _globalTypes.Count + 1;
						_globalTypes.Add (ct, dt);
					}
					WritePairFast (JsonDict.ExtType, ValueConverter.Int32ToString (dt));
				}
				append = true;
			}
			#endregion

			var g = def.Getters;
			var c = g.Length;
			for (int ii = 0; ii < c; ii++) {
				var p = g[ii];
				var m = p.Member;
				#region Skip Members Not For Serialization
				if (p.Serializable == TriState.False) {
					continue;
				}
				if (p.Serializable == TriState.Default) {
					if (m.IsStatic && _params.SerializeStaticMembers == false
						|| m.IsReadOnly && m.MemberTypeReflection.AppendItem == null
							&& (m.IsProperty && _showReadOnlyProperties == false || m.IsProperty == false && _showReadOnlyFields == false)) {
						continue;
					}
				}
				#endregion
				var ji = new JsonItem (m.MemberName, m.Getter (obj), true);
				if (si != null && si.OnSerializing (obj, ji) == false) {
					continue;
				}
				var cv = p.Converter ?? m.MemberTypeReflection.Converter;
				if (cv != null) {
					cv.SerializationConvert (ji);
				}
				#region Convert Items
				if (p.ItemConverter != null) {
					var ev = ji._Value as IEnumerable;
					if (ev != null) {
						var ai = new JsonItem (ji.Name, null, false);
						var ol = new List<object> ();
						foreach (var item in ev) {
							ai.Value = item;
							p.ItemConverter.SerializationConvert (ai);
							ol.Add (ai.Value);
						}
						ji._Value = ol;
					}
				}
				#endregion
				#region Determine Serialized Field Name
				if (p.SpecificName) {
					if (ji._Value == null || p.TypedNames == null || p.TypedNames.TryGetValue (ji._Value.GetType (), out ji._Name) == false) {
						ji._Name = p.SerializedName;
					}
				}
				else {
					ji._Name = p.SerializedName;
				}
				#endregion
				#region Skip Null, Default Value or Empty Collection
				if (_params.SerializeNullValues == false && (ji._Value == null || ji._Value is DBNull)) {
					continue;
				}
				if (p.HasNonSerializedValue && Array.IndexOf (p.NonSerializedValues, ji._Value) != -1) {
					// ignore fields with default value
					continue;
				}
				if (m.IsCollection && _params.SerializeEmptyCollections == false) {
					var vc = ji._Value as ICollection;
					if (vc != null && vc.Count == 0) {
						continue;
					}
				}
				#endregion
				if (append)
					_output.Append (',');

				#region Write Name
				if (p.SpecificName) {
					WriteStringFast (ji._Name);
					_output.Append (':');
				}
				else {
					_naming.WriteName (_output, ji._Name);
				}
				#endregion
				#region Write Value
				if (m.SerializeMethod != null && cv == null) {
					var v = ji._Value;
					if (v == null || v is DBNull) {
						_output.Append ("null");
					}
					else if (m.MemberTypeReflection.CollectionName != null) {
						WriteObject (v);
					}
					else {
						m.SerializeMethod (this, v);
					}
				}
				else {
					WriteValue (ji._Value);
				}
				#endregion

				append = true;
			}
			#region Write Inherited Collection
			if (def.CollectionName != null && def.SerializeMethod != null) {
				if (append)
					_output.Append (',');
				WriteStringFast (def.CollectionName);
				_output.Append (':');
				def.SerializeMethod (this, obj);
				append = true;
			}
			#endregion
			#region Write Extra Values
			if (si != null) {
				var ev = si.SerializeExtraValues (obj);
				if (ev != null) {
					foreach (var item in ev) {
						if (append)
							_output.Append (',');
						WritePair (item._Name, item._Value);
						append = true;
					}
				}
				si.OnSerialized (obj);
			}
			#endregion
			_currentDepth--;
			_output.Append ('}');
		}


		void WritePairFast (string name, string value) {
			WriteStringFast (name);
			_output.Append (':');
			WriteStringFast (value);
		}

		void WritePair (string name, object value) {
			WriteStringFast (name);
			_output.Append (':');
			WriteValue (value);
		}

		static void WriteArray (JsonSerializer serializer, object value) {
			IEnumerable array = value as IEnumerable;
			var o = serializer._output;
			if (array == null) {
				o.Append ("null");
				return;
			}
			//if (_params.SerializeEmptyCollections == false) {
			//	var c = array as ICollection;
			//	if (c.Count == 0) {
			//		return;
			//	}
			//}

			var list = array as IList;
			if (list != null) {
				var c = list.Count;
				if (c == 0) {
					o.Append ("[]");
					return;
				}

				var t = list.GetType ();
				if (t.IsArray && t.GetArrayRank () > 1) {
					WriteMultiDimensionalArray (serializer, list);
					return;
				}
				var d = serializer._manager.GetReflectionCache (t);
				var w = d.ItemSerializer;
				if (w != null) {
					o.Append ('[');
					var v = list[0];
					if (v == null) {
						o.Append ("null");
					}
					else {
						w (serializer, v);
					}
					for (int i = 1; i < c; i++) {
						o.Append (',');
						v = list[i];
						if (v == null) {
							o.Append ("null");
						}
						else {
							w (serializer, v);
						}
					}
					o.Append (']');
					return;
				}

				o.Append ('[');
				serializer.WriteValue (list[0]);
				for (int i = 1; i < c; i++) {
					o.Append (',');
					serializer.WriteValue (list[i]);
				}
				o.Append (']');
				return;
			}

			var pendingSeperator = false;
			o.Append ('[');
			foreach (object obj in array) {
				if (pendingSeperator) o.Append (',');

				serializer.WriteValue (obj);

				pendingSeperator = true;
			}
			o.Append (']');
		}

		static void WriteMultiDimensionalArray (JsonSerializer serializer, object value) {
			var a = value as Array;
			if (a == null) {
				serializer._output.Append ("null");
				return;
			}
			var m = serializer._manager.GetReflectionCache (a.GetType ().GetElementType ()).SerializeMethod;
			serializer.WriteMultiDimensionalArray (m, a);
		}

		void WriteMultiDimensionalArray (WriteJsonValue m, Array md) {
			var r = md.Rank;
			var lb = new int[r];
			var ub = new int[r];
			var mdi = new int[r];
			for (int i = 0; i < r; i++) {
				lb[i] = md.GetLowerBound (i);
				ub[i] = md.GetUpperBound (i) + 1;
			}
			Array.Copy (lb, 0, mdi, 0, r);
			WriteMultiDimensionalArray (m, md, r, lb, ub, mdi, 0);
		}

		void WriteMultiDimensionalArray (WriteJsonValue m, Array array, int rank, int[] lowerBounds, int[] upperBounds, int[] indexes, int rankIndex) {
			var u = upperBounds[rankIndex];
			if (rankIndex < rank - 1) {
				_output.Append ('[');
				bool s = false;
				var d = rankIndex;
				do {
					if (s) {
						_output.Append (',');
					}
					Array.Copy (lowerBounds, d + 1, indexes, d + 1, rank - d - 1);
					WriteMultiDimensionalArray (m, array, rank, lowerBounds, upperBounds, indexes, ++d);
					d = rankIndex;
					s = true;
				} while (++indexes[rankIndex] < u);
				_output.Append (']');
			}
			else if (rankIndex == rank - 1) {
				_output.Append ('[');
				bool s = false;
				do {
					if (s) {
						_output.Append (',');
					}
					var v = array.GetValue (indexes);
					if (v == null || v is DBNull) {
						_output.Append ("null");
					}
					else {
						m (this, v);
					}
					s = true;
				} while (++indexes[rankIndex] < u);
				_output.Append (']');
			}
		}

		void WriteStringDictionary (IDictionary dic) {
			_output.Append ('{');
			var pendingSeparator = false;
			foreach (DictionaryEntry entry in dic) {
				if (_params.SerializeNullValues == false && entry.Value == null) {
					continue;
				}
				if (pendingSeparator) _output.Append (',');
				_naming.WriteName (_output, (string)entry.Key);
				WriteValue (entry.Value);
				pendingSeparator = true;
			}
			_output.Append ('}');
		}

		void WriteNameValueCollection (NameValueCollection collection) {
			_output.Append ('{');
			var pendingSeparator = false;
			var length = collection.Count;
			for (int i = 0; i < length; i++) {
				var v = collection.GetValues (i);
				if (v == null && _params.SerializeNullValues == false) {
					continue;
				}
				if (pendingSeparator) _output.Append (',');
				pendingSeparator = true;
				_naming.WriteName (_output, collection.GetKey (i));
				if (v == null) {
					_output.Append ("null");
					continue;
				}
				var vl = v.Length;
				if (vl == 0) {
					_output.Append ("\"\"");
					continue;
				}
				if (vl == 1) {
					if (_useEscapedUnicode) {
						WriteStringEscapeUnicode (_output, v[0]);
					}
					else {
						WriteString (_output, v[0]);
					}
				}
				else {
					_output.Append ('[');
					if (_useEscapedUnicode) {
						WriteStringEscapeUnicode (_output, v[0]);
					}
					else {
						WriteString (_output, v[0]);
					}
					for (int vi = 1; vi < vl; vi++) {
						_output.Append (',');
						if (_useEscapedUnicode) {
							WriteStringEscapeUnicode (_output, v[vi]);
						}
						else {
							WriteString (_output, v[vi]);
						}
					}
					_output.Append (']');
				}
			}
			_output.Append ('}');
		}

		void WriteStringDictionary (IDictionary<string, object> dic) {
			_output.Append ('{');
			var pendingSeparator = false;
			foreach (KeyValuePair<string, object> entry in dic) {
				if (_params.SerializeNullValues == false && entry.Value == null) {
					continue;
				}
				if (pendingSeparator) _output.Append (',');
				_naming.WriteName (_output, entry.Key);
				WriteValue (entry.Value);
				pendingSeparator = true;
			}
			_output.Append ('}');
		}

		void WriteKvStyleDictionary (IDictionary dic) {
			_output.Append ('[');

			var pendingSeparator = false;

			foreach (DictionaryEntry entry in dic) {
				if (pendingSeparator) _output.Append (',');
				_output.Append ('{');
				WritePair ("k", entry.Key);
				_output.Append (",");
				WritePair ("v", entry.Value);
				_output.Append ('}');

				pendingSeparator = true;
			}
			_output.Append (']');
		}

		void WriteStringFast (string s) {
			_output.Append ('\"');
			_output.Append (s);
			_output.Append ('\"');
		}

		internal static void WriteStringEscapeUnicode (StringBuilder output, string s) {
			output.Append ('\"');

			var runIndex = -1;
			var l = s.Length;
			for (var index = 0; index < l; ++index) {
				var c = s[index];
				if (c >= ' ' && c < 128 && c != '\"' && c != '\\') {
					if (runIndex == -1)
						runIndex = index;

					continue;
				}

				if (runIndex != -1) {
					output.Append (s, runIndex, index - runIndex);
					runIndex = -1;
				}

				switch (c) {
					case '\t': output.Append ("\\t"); break;
					case '\r': output.Append ("\\r"); break;
					case '\n': output.Append ("\\n"); break;
					case '"':
					case '\\': output.Append ('\\'); output.Append (c); break;
					default:
						output.Append ("\\u");
						// hard-code this line to improve performance:
						// output.Append (((int)c).ToString ("X4", NumberFormatInfo.InvariantInfo));
						var n = (c >> 12) & 0x0F;
						output.Append ((char)(n > 9 ? n + ('A' - 10) : n + '0'));
						n = (c >> 8) & 0x0F;
						output.Append ((char)(n > 9 ? n + ('A' - 10) : n + '0'));
						n = (c >> 4) & 0x0F;
						output.Append ((char)(n > 9 ? n + ('A' - 10) : n + '0'));
						n = c & 0x0F;
						output.Append ((char)(n > 9 ? n + ('A' - 10) : n + '0'));
						break;
				}
			}

			if (runIndex != -1)
				output.Append (s, runIndex, s.Length - runIndex);

			output.Append ('\"');
		}

		internal static void WriteString (StringBuilder output, string s) {
			output.Append ('\"');

			var runIndex = -1;
			var l = s.Length;
			for (var index = 0; index < l; ++index) {
				var c = s[index];
				if (c != '\t' && c != '\n' && c != '\r' && c != '\"' && c != '\\')// && c != ':' && c!=',')
				{
					if (runIndex == -1)
						runIndex = index;

					continue;
				}

				if (runIndex != -1) {
					output.Append (s, runIndex, index - runIndex);
					runIndex = -1;
				}

				switch (c) {
					case '\t': output.Append ("\\t"); break;
					case '\r': output.Append ("\\r"); break;
					case '\n': output.Append ("\\n"); break;
					case '"':
					case '\\': output.Append ('\\'); output.Append (c); break;
					default:
						output.Append (c);
						break;
				}
			}

			if (runIndex != -1)
				output.Append (s, runIndex, s.Length - runIndex);

			output.Append ('\"');
		}


		#region WriteJsonValue delegate methods
		internal static WriteJsonValue GetWriteJsonMethod (Type type) {
			var t = Reflection.GetJsonDataType (type);
			if (t == JsonDataType.Primitive) {
				return typeof (decimal).Equals (type) ? WriteDecimal
						: typeof (byte).Equals (type) ? WriteByte
						: typeof (sbyte).Equals (type) ? WriteSByte
						: typeof (short).Equals (type) ? WriteInt16
						: typeof (ushort).Equals (type) ? WriteUInt16
						: typeof (uint).Equals (type) ? WriteUInt32
						: typeof (ulong).Equals (type) ? WriteUInt64
						: typeof (char).Equals (type) ? WriteChar
						: (WriteJsonValue)WriteUnknown;
			}
			else if (t == JsonDataType.Undefined) {
				return type.IsSubclassOf (typeof (Array)) && type.GetArrayRank () > 1 ? WriteMultiDimensionalArray
					: type.IsSubclassOf (typeof (Array)) && typeof (byte[]).Equals (type) == false ? WriteArray
					: typeof (KeyValuePair<string,object>).Equals (type) ? WriteKeyObjectPair
					: typeof (KeyValuePair<string,string>).Equals (type) ? WriteKeyValuePair
					: (WriteJsonValue)WriteObject;
			}
			else {
				return _convertMethods[(int)t];
			}
		}

		static void WriteByte (JsonSerializer serializer, object value) {
			serializer._output.Append (ValueConverter.Int32ToString ((byte)value));
		}
		static void WriteSByte (JsonSerializer serializer, object value) {
			serializer._output.Append (ValueConverter.Int32ToString ((sbyte)value));
		}
		static void WriteInt16 (JsonSerializer serializer, object value) {
			serializer._output.Append (ValueConverter.Int32ToString ((short)value));
		}
		static void WriteUInt16 (JsonSerializer serializer, object value) {
			serializer._output.Append (ValueConverter.Int32ToString ((ushort)value));
		}
		static void WriteInt32 (JsonSerializer serializer, object value) {
			serializer._output.Append (ValueConverter.Int32ToString ((int)value));
		}
		static void WriteUInt32 (JsonSerializer serializer, object value) {
			serializer._output.Append (ValueConverter.Int64ToString ((uint)value));
		}
		static void WriteInt64 (JsonSerializer serializer, object value) {
			serializer._output.Append (ValueConverter.Int64ToString ((long)value));
		}
		static void WriteUInt64 (JsonSerializer serializer, object value) {
			serializer._output.Append (ValueConverter.UInt64ToString ((ulong)value));
		}
		static void WriteSingle (JsonSerializer serializer, object value) {
			serializer._output.Append (((float)value).ToString (NumberFormatInfo.InvariantInfo));
		}
		static void WriteDouble (JsonSerializer serializer, object value) {
			serializer._output.Append (((double)value).ToString (NumberFormatInfo.InvariantInfo));
		}
		static void WriteDecimal (JsonSerializer serializer, object value) {
			serializer._output.Append (((decimal)value).ToString (NumberFormatInfo.InvariantInfo));
		}
		static void WriteBoolean (JsonSerializer serializer, object value) {
			serializer._output.Append ((bool)value ? "true" : "false");
		}
		static void WriteChar (JsonSerializer serializer, object value) {
			WriteString (serializer, ((char)value).ToString ());
		}

		static void WriteDateTime (JsonSerializer serializer, object value) {
			// datetime format standard : yyyy-MM-dd HH:mm:ss
			var dt = (DateTime)value;
			var parameter = serializer._params;
			var output = serializer._output;
			if (parameter.UseUTCDateTime)
				dt = dt.ToUniversalTime ();

			output.Append ('"');
			output.Append (ValueConverter.ToFixedWidthString (dt.Year, 4));
			output.Append ('-');
			output.Append (ValueConverter.ToFixedWidthString (dt.Month, 2));
			output.Append ('-');
			output.Append (ValueConverter.ToFixedWidthString (dt.Day, 2));
			output.Append ('T'); // strict ISO date compliance
			output.Append (ValueConverter.ToFixedWidthString (dt.Hour, 2));
			output.Append (':');
			output.Append (ValueConverter.ToFixedWidthString (dt.Minute, 2));
			output.Append (':');
			output.Append (ValueConverter.ToFixedWidthString (dt.Second, 2));
			if (parameter.DateTimeMilliseconds) {
				output.Append ('.');
				output.Append (ValueConverter.ToFixedWidthString (dt.Millisecond, 3));
			}
			if (parameter.UseUTCDateTime)
				output.Append ('Z');

			output.Append ('\"');
		}

		static void WriteTimeSpan (JsonSerializer serializer, object timeSpan) {
			serializer.WriteStringFast ((((TimeSpan)timeSpan).ToString ()));
		}

		static void WriteString (JsonSerializer serializer, object value) {
			if (value == null) {
				serializer._output.Append ("null");
				return;
			}
			var s = (string)value;
			if (s.Length == 0) {
				serializer._output.Append ("\"\"");
				return;
			}
			if (serializer._useEscapedUnicode) {
				WriteStringEscapeUnicode (serializer._output, s);
			}
			else {
				WriteString (serializer._output, s);
			}
		}

		static void WriteGuid (JsonSerializer serializer, object guid) {
			if (serializer._params.UseFastGuid == false)
				serializer.WriteStringFast (((Guid)guid).ToString ());
			else
				serializer.WriteBytes (((Guid)guid).ToByteArray ());
		}

		static void WriteEnum (JsonSerializer serializer, object value) {
			Enum e = (Enum)value;
			// TODO : optimize enum write
			if (serializer._params.UseValuesOfEnums) {
				serializer._output.Append (Convert.ToInt64 (e).ToString (NumberFormatInfo.InvariantInfo));
				return;
			}
			var n = serializer._manager.GetEnumName (e);
			if (n != null) {
				serializer.WriteStringFast (n);
			}
			else {
				serializer._output.Append (Convert.ToInt64 (e).ToString (NumberFormatInfo.InvariantInfo));
			}
		}
		static void WriteByteArray (JsonSerializer serializer, object value) {
			serializer.WriteStringFast (Convert.ToBase64String ((byte[])value));
		}
		static void WriteCustom (JsonSerializer serializer, object value) {
			serializer.WriteCustom (value);
		}
		static void WriteDataSet (JsonSerializer serializer, object value) {
			serializer.WriteDataset ((DataSet)value);
		}
		static void WriteDataTable (JsonSerializer serializer, object value) {
			serializer.WriteDataTable ((DataTable)value);
		}
		static void WriteDictionary (JsonSerializer serializer, object value) {
			if (serializer._params.KVStyleStringDictionary == false) {
				if (value is IDictionary<string, object>) {
					serializer.WriteStringDictionary ((IDictionary<string, object>)value);
					return;
				}
				else if (value is IDictionary
					&& value.GetType ().IsGenericType
					&& typeof (string).Equals (value.GetType ().GetGenericArguments ()[0])) {
					serializer.WriteStringDictionary ((IDictionary)value);
					return;
				}
#if NET_40_OR_GREATER
				else if (value is System.Dynamic.ExpandoObject) {
					serializer.WriteStringDictionary ((IDictionary<string, object>)value);
					return;
				}
#endif
			}
			if (value is IDictionary)
				serializer.WriteKvStyleDictionary ((IDictionary)value);
		}
		static void WriteStringDictionary (JsonSerializer serializer, object value) {
			serializer.WriteSD ((StringDictionary)value);
		}
		static void WriteNameValueCollection (JsonSerializer serializer, object value) {
			serializer.WriteNameValueCollection ((NameValueCollection)value);
		}
		static void WriteKeyObjectPair (JsonSerializer serializer, object value) {
			var p = (KeyValuePair<string, object>)value;
			serializer._output.Append ('{');
			serializer.WriteStringFast (p.Key);
			serializer._output.Append (':');
			WriteObject (serializer, p.Value);
			serializer._output.Append ('}');
		}
		static void WriteKeyValuePair (JsonSerializer serializer, object value) {
			var p = (KeyValuePair<string, string>)value;
			serializer._output.Append ('{');
			serializer.WriteStringFast (p.Key);
			serializer._output.Append (':');
			WriteString (serializer, p.Value);
			serializer._output.Append ('}');
		}
		static void WriteObject (JsonSerializer serializer, object value) {
			serializer.WriteObject (value);
		}
		static void WriteUnknown (JsonSerializer serializer, object value) {
			serializer.WriteValue (value);
		}
		#endregion
	}
}
