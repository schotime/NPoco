using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.IO;

namespace NPoco.FastJSON
{
	sealed class JsonDeserializer
	{
		static readonly RevertJsonValue[] _revertMethods = RegisterMethods ();

		readonly JSONParameters _params;
		readonly SerializationManager _manager;
		readonly Dictionary<object, int> _circobj = new Dictionary<object, int> ();
		readonly Dictionary<int, object> _cirrev = new Dictionary<int, object> ();
		bool _usingglobals = false;
		Dictionary<string,object> globaltypes;

		static RevertJsonValue[] RegisterMethods () {
			var r = new RevertJsonValue[Enum.GetNames (typeof (JsonDataType)).Length];
			r[(int)JsonDataType.Array] = RevertArray;
			r[(int)JsonDataType.Bool] = RevertPrimitive;
			r[(int)JsonDataType.ByteArray] = RevertByteArray;
			r[(int)JsonDataType.Custom] = RevertCustom;
			r[(int)JsonDataType.DataSet] = RevertDataSet;
			r[(int)JsonDataType.DataTable] = RevertDataTable;
			r[(int)JsonDataType.DateTime] = RevertDateTime;
			r[(int)JsonDataType.Dictionary] = RevertDictionary;
			r[(int)JsonDataType.Double] = RevertPrimitive;
			r[(int)JsonDataType.Enum] = RevertEnum;
			r[(int)JsonDataType.List] = RevertList;
			r[(int)JsonDataType.Guid] = RevertGuid;
			r[(int)JsonDataType.Hashtable] = RevertHashTable;
			r[(int)JsonDataType.Int] = RevertInt32;
			r[(int)JsonDataType.Long] = RevertPrimitive;
			r[(int)JsonDataType.MultiDimensionalArray] = RevertMultiDimensionalArray;
			r[(int)JsonDataType.NameValue] = RevertNameValueCollection;
			r[(int)JsonDataType.Object] = RevertUndefined;
			r[(int)JsonDataType.Single] = RevertSingle;
			r[(int)JsonDataType.String] = RevertPrimitive;
			r[(int)JsonDataType.StringDictionary] = RevertStringDictionary;
			r[(int)JsonDataType.StringKeyDictionary] = RevertStringKeyDictionary;
			r[(int)JsonDataType.TimeSpan] = RevertTimeSpan;
			r[(int)JsonDataType.Undefined] = RevertUndefined;
			return r;
		}

		public JsonDeserializer (JSONParameters param, SerializationManager manager) {
			_params = param;
			_manager = manager;
		}

		public T ToObject<T>(string json) {
			Type t = typeof (T);
			return (T)ToObject (json, t);
		}

		public object ToObject (string json) {
			return ToObject (json, null);
		}

		public object ToObject (string json, Type type) {
			object o = new JsonParser (json).Decode ();
			if (o == null)
				return null;

			ReflectionCache c = null;
			if (type != null) {
				c = _manager.GetReflectionCache (type);
				var cv = c.Converter;
				if (cv != null) {
					var ji = new JsonItem (String.Empty, o, false);
					ConvertObject (cv, ji, type);
					if (ReferenceEquals (ji._Value, o) == false) {
						return ji._Value;
					}
					if (c.CommonType == ComplexType.Dictionary
						|| c.CommonType == ComplexType.List) {
						_usingglobals = false;
					}
				}
				else if (c.DeserializeMethod != null) {
					return c.DeserializeMethod (this, o, c);
				}
			}
			else {
				_usingglobals = _params.UsingGlobalTypes;
			}

			var d = o as JsonDict;
			if (d != null) {
				if (type != null) {
#if !SILVERLIGHT
					if (c.JsonDataType == JsonDataType.DataSet)
						return CreateDataSet (d);

					if (c.JsonDataType == JsonDataType.DataTable)
						return CreateDataTable (d);
#endif
					if (c.CommonType == ComplexType.Dictionary) // deserialize a dictionary
						return RootDictionary (o, c);
				}
				return CreateObject (d, c, null);
			}
			var a = o as JsonArray;
			if (a != null) {
				if (type != null) {
					if (c.CommonType == ComplexType.Dictionary) // k/v format
						return RootDictionary (o, c);

					if (c.CommonType == ComplexType.List) // deserialize to generic list
						return RootList (o, c);

					if (c.JsonDataType == JsonDataType.Hashtable)
						return RootHashTable (a);

					if (c.CommonType == ComplexType.Array) {
						return CreateArray (a, c);
					}
				}
				return a.ToArray ();
			}

			if (type != null && o.GetType ().Equals (type) == false)
				return ChangeType (o, type);

			return o;
		}

		internal static RevertJsonValue GetReadJsonMethod (Type type) {
			if (type == null) {
				return RevertUndefined;
			}
			var d = Reflection.GetJsonDataType (type);
			if (d != JsonDataType.Primitive) {
				return GetRevertMethod (d);
			}
			if (type.IsGenericType && type.GetGenericTypeDefinition ().Equals (typeof (Nullable<>))) {
				type = type.GetGenericArguments ()[0];
			}
			return typeof (byte).Equals (type) ? RevertByte
				: typeof (decimal).Equals (type) ? RevertDecimal
				: typeof (char).Equals (type) ? RevertChar
				: typeof (sbyte).Equals (type) ? RevertSByte
				: typeof (short).Equals (type) ? RevertShort
				: typeof (ushort).Equals (type) ? RevertUShort
				: typeof (uint).Equals (type) ? RevertUInt32
				: typeof (ulong).Equals (type) ? RevertUInt64
				: (RevertJsonValue)RevertUndefined;
		}

		#region [   p r i v a t e   m e t h o d s   ]
		void ConvertObject (IJsonConverter converter, JsonItem ji, Type sourceType) {
			var rt = converter.GetReversiveType (ji);
			var xv = ji._Value;
			if (xv != null && rt != null && sourceType.Equals (xv.GetType ()) == false) {
				var c = _manager.GetReflectionCache (rt);
				var jt = Reflection.GetJsonDataType (rt);
				if (jt != JsonDataType.Undefined) {
					xv = c.DeserializeMethod (this, xv, c);
				}
				else if (xv is JsonDict) {
					xv = CreateObject ((JsonDict)xv, c, null);
				}
			}
			ji._Value = xv;
			converter.DeserializationConvert (ji);
		}

		object RootHashTable (JsonArray o) {
			Hashtable h = new Hashtable ();
			var c = _manager.GetReflectionCache (typeof (object));
			foreach (JsonDict values in o) {
				object key = values["k"];
				object val = values["v"];
				if (key is JsonDict)
					key = CreateObject ((JsonDict)key, c, null);

				if (val is JsonDict)
					val = CreateObject ((JsonDict)val, c, null);

				h.Add (key, val);
			}

			return h;
		}

		object RootList (object parse, ReflectionCache type) {
			var ec = type.ArgumentReflections[0];
			var m = ec.DeserializeMethod;
			IList o = (IList)type.Instantiate ();
			foreach (var k in (IList)parse) {
				_usingglobals = false;
				object v = m (this, k, ec);
				o.Add (v);
			}
			return o;
		}

		object RootDictionary (object parse, ReflectionCache type) {
			var g = type.ArgumentReflections;
			ReflectionCache c1 = g[0], c2 = g[1];
			var mk = c1.DeserializeMethod;
			var m = c2.DeserializeMethod;
			var d = parse as JsonDict;
			if (d != null) {
				IDictionary o = (IDictionary)type.Instantiate ();

				foreach (var kv in d) {
					o.Add (mk (this, kv.Key, c1), m (this, kv.Value, c2));
				}

				return o;
			}
			var a = parse as JsonArray;
			if (a != null)
				return CreateDictionary (a, type);

			return null;
		}

		/// <summary>
		/// Deserializes an object.
		/// </summary>
		/// <param name="data">The data to be deserialized.</param>
		/// <param name="type">The reflection cache of the type.</param>
		/// <param name="input">The data container. If this value is not null, deserialized members will be written to it. If null, new object will be created.</param>
		/// <returns>The deserialized object.</returns>
		/// <exception cref="JsonSerializationException">Cannot determine type from <paramref name="data"/>.</exception>
		internal object CreateObject (JsonDict data, ReflectionCache type, object input) {
			if (data.RefIndex > 0) {
				object v = null;
				_cirrev.TryGetValue (data.RefIndex, out v);
				return v;
			}

			if (data.Types != null && data.Types.Count > 0) {
				_usingglobals = true;
				globaltypes = new Dictionary<string,object> ();
				foreach (var kv in data.Types) {
					globaltypes.Add ((string)kv.Value, kv.Key);
				}
			}

			var tn = data.Type;
			bool found = (tn != null && tn.Length > 0);
#if !SILVERLIGHT
			if (found == false && type != null && typeof (object).Equals (type.Type)) {
				return data;
			}
#endif
			if (found) {
				if (_usingglobals) {
					object tname = "";
					if (globaltypes != null && globaltypes.TryGetValue (data.Type, out tname))
						tn = (string)tname;
				}
				type = _manager.GetReflectionCache (Reflection.Instance.GetTypeFromCache (tn));
			}

			if (type == null)
				throw new JsonSerializationException ("Cannot determine type");

			object o = input;
			if (o == null) {
				o = _params.ParametricConstructorOverride
					? System.Runtime.Serialization.FormatterServices.GetUninitializedObject (type.Type)
					: type.Instantiate ();
			}
			int circount = 0;
			if (_circobj.TryGetValue (o, out circount) == false) {
				circount = _circobj.Count + 1;
				_circobj.Add (o, circount);
				_cirrev.Add (circount, o);
			}

			var si = type.Interceptor;
			if (si != null) {
				si.OnDeserializing (o);
			}
			Dictionary<string, JsonMemberSetter> props = type.Setters;
			//TODO: Candidate to removal of unknown use of map
			//if (data.Map != null) {
			//	ProcessMap (o, props, data.Map);
			//}
			foreach (var kv in data) {
				var n = kv.Key;
				var v = kv.Value;
				JsonMemberSetter pi;
				if (props.TryGetValue (n, out pi) == false || pi.CanWrite == false && pi.Member.JsonDataType != JsonDataType.List)
					continue;
				MemberCache m = pi.Member;
				var ji = new JsonItem (n, v, false);
				bool converted = false;
				// TODO: Convert items for types implements IEnumerable and Add(?) method
				if (v is IList && pi.ItemConverter != null) {
					converted = ConvertItems (pi, ji);
				}
				if (pi.Converter != null || m.MemberTypeReflection.Converter != null) {
					ConvertProperty (o, pi, ji);
				}

				object oset = null;
				// use the converted value
				if (converted || ReferenceEquals (ji._Value, v) == false) {
					if (pi.CanWrite == false && m.JsonDataType == JsonDataType.List) {
						ji._Value = CreateList ((JsonArray)ji._Value, m.MemberTypeReflection, m.Getter (o));
					}
					if (ji._Value != null || m.IsClass || m.IsNullable) {
						oset = ji._Value;
						goto SET_VALUE;
					}
					continue;
				}
				// process null value
				if (ji._Value == null) {
					var i = new JsonItem (n, null, false);
					if (si != null && si.OnDeserializing (o, i) == false) {
						continue;
					}
					if (i.Value != null || m.IsClass || m.IsNullable) {
						o = m.Setter (o, i.Value);
					}
					continue;
				}
				v = ji._Value;
				// set member value
				switch (m.JsonDataType) {
					case JsonDataType.Undefined: goto default;
					case JsonDataType.Int: oset = (int)(long)v; break;
					case JsonDataType.String:
					case JsonDataType.Bool:
					case JsonDataType.Long:
					case JsonDataType.Double: oset = v; break;
					case JsonDataType.Single: oset = (float)(double)v; break;
					case JsonDataType.DateTime: oset = CreateDateTime (this, v); break;
					case JsonDataType.Guid: oset = CreateGuid (v); break;
					case JsonDataType.ByteArray: oset = Convert.FromBase64String ((string)v); break;
					case JsonDataType.List:
						if (m.MemberTypeReflection.CollectionName != null) {
							goto default;
						}
						oset = CreateList ((JsonArray)v, m.MemberTypeReflection, pi.CanWrite && (m.IsClass || m.IsStruct) ? null : m.Getter (o));
						break;
					case JsonDataType.Object: oset = v; break;
					default:
						if (m.DeserializeMethod != null) {
							oset = m.MemberTypeReflection.DeserializeMethod (this, ji._Value, m.MemberTypeReflection);
							goto SET_VALUE;
						}
						if ((m.IsClass || m.IsStruct) && v is JsonDict)
							oset = CreateObject ((JsonDict)v, m.MemberTypeReflection, m.Getter (o));

						else if (v is JsonArray)
							oset = CreateArray ((JsonArray)v, _manager.GetReflectionCache (typeof (object[])));

						else if (m.IsValueType)
							oset = ChangeType (v, m.ChangeType);

						else
							oset = v;

						break;
				}
				SET_VALUE:
				ji.Value = oset;
				if (si != null) {
					if (si.OnDeserializing (o, ji) == false) {
						continue;
					}
				}
				if (m.Setter != null) {
					o = m.Setter (o, ji.Value);
				}
			}
			if (si != null) {
				si.OnDeserialized (o);
			}
			return o;
		}

		void ConvertProperty (object o, JsonMemberSetter pi, JsonItem ji) {
			var pc = pi.Converter ?? pi.Member.MemberTypeReflection.Converter;
			var rt = pc.GetReversiveType (ji);
			var xv = ji._Value;
			if (xv != null && rt != null && pi.Member.MemberType.Equals (xv.GetType ()) == false) {
				var c = _manager.GetReflectionCache (rt);
				var jt = Reflection.GetJsonDataType (rt);
				if (jt != JsonDataType.Undefined) {
					xv = c.DeserializeMethod (this, xv, c);
				}
				else if (xv is JsonDict) {
					xv = CreateObject ((JsonDict)xv, c, pi.Member.Getter (o));
				}
			}
			ji._Value = xv;
			pc.DeserializationConvert (ji);
		}

		static bool ConvertItems (JsonMemberSetter pi, JsonItem ji) {
			var vl = ji._Value as IList;
			var l = vl.Count;
			var converted = false;
			var ai = new JsonItem (ji._Name, null, false);
			for (int i = 0; i < l; i++) {
				var vi = vl[i];
				ai._Value = vi;
				pi.ItemConverter.DeserializationConvert (ai);
				if (ReferenceEquals (vi, ai._Value) == false) {
					vl[i] = ai._Value;
					converted = true;
				}
			}
			if (converted) {
				if (pi.Member.JsonDataType == JsonDataType.Array) {
					ji._Value = Array.CreateInstance (pi.Member.ElementType, l);
					vl.CopyTo ((Array)ji._Value, 0);
				}
				else if (pi.Member.JsonDataType == JsonDataType.List) {
					ji._Value = pi.Member.MemberTypeReflection.Instantiate ();
					var gl = ji._Value as IList;
					for (int i = 0; i < l; i++) {
						gl.Add (vl[i]);
					}
				}
			}

			return converted;
		}

		static StringDictionary CreateStringDictionary (JsonDict d) {
			StringDictionary nv = new StringDictionary ();

			foreach (var o in d)
				nv.Add (o.Key, (string)o.Value);

			return nv;
		}

		static NameValueCollection CreateNameValueCollection (JsonDict d) {
			NameValueCollection nv = new NameValueCollection ();

			foreach (var o in d) {
				var k = o.Key;
				var ov = o.Value;
				if (ov == null) {
					nv.Add (k, null);
					continue;
				}
				var s = ov as string;
				if (s != null) {
					nv.Add (k, s);
					continue;
				}
				var sa = ov as IList;
				if (sa != null) {
					foreach (string item in sa) {
						nv.Add (k, item);
					}
					continue;
				}
				nv.Add (k, ov.ToString ());
			}

			return nv;
		}

		object ChangeType (object value, Type conversionType) {
			var c = _manager.GetReflectionCache (conversionType);
			if (c.DeserializeMethod != null) {
				return c.DeserializeMethod (this, value, c);
			}
			// 8-30-2014 - James Brooks - Added code for nullable types.
			if (conversionType.IsGenericType) {
				if (c.CommonType == ComplexType.Nullable) {
					if (value == null) {
						return value;
					}
					conversionType = c.ArgumentTypes[0];
				}
			}

			// 8-30-2014 - James Brooks - Nullable Guid is a special case so it was moved after the "IsNullable" check.
			if (typeof (Guid).Equals (conversionType))
				return CreateGuid (value);

			return Convert.ChangeType (value, conversionType, CultureInfo.InvariantCulture);
		}

		object CreateEnum (object value, Type enumType) {
			var s = value as string;
			if (s != null) {
				return _manager.GetEnumValue (enumType, s);
			}
			else {
				return Enum.ToObject (enumType, value);
			}
		}

		object CreateArray (JsonArray data, ReflectionCache arrayType) {
			var l = data.Count;
			var ec = arrayType.ArgumentReflections[0];
			Array col = Array.CreateInstance (ec.Type, l);
			var r = arrayType.ItemDeserializer;
			if (r != null) {
				for (int i = 0; i < l; i++) {
					var ob = data[i];
					if (ob == null) {
						continue;
					}
					col.SetValue (r (this, ob, ec), i);
				}
				return col;
			}

			// TODO: candidate of code clean-up
			// create an array of objects
			for (int i = 0; i < l; i++) {
				var ob = data[i];
				if (ob == null) {
					continue;
				}
				if (ob is IDictionary)
					col.SetValue (CreateObject ((JsonDict)ob, ec, null), i);
				// support jagged array
				else if (ob is ICollection) {
					col.SetValue (CreateArray ((JsonArray)ob, ec), i);
				}
				else
					col.SetValue (ChangeType (ob, ec.Type), i);
			}

			return col;
		}

		object CreateMultiDimensionalArray (JsonArray data, ReflectionCache arrayType) {
			ReflectionCache ec = arrayType.ArgumentReflections[0];
			Type et = ec.Type;
			var ar = arrayType.Type.GetArrayRank ();
			var ub = new int[ar];
			var d = data;
			// get upper bounds
			for (int i = 0; i < ar; i++) {
				var l = d.Count;
				ub[i] = l;
				if (i == ar - 1) {
					break;
				}
				JsonArray a = null;
				for (int j = 0; j < l; j++) {
					a = d[j] as JsonArray;
					if (d != null) {
						d = a;
						break;
					}
				}
				if (a == null) {
					throw new JsonSerializationException ("The rank of the multi-dimensional array does not match.");
				}
			}
			var mdi = new int[ar];
			Array col = Array.CreateInstance (ec.Type, ub);
			var m = arrayType.ItemDeserializer;
			var ri = 0;
			SetMultiDimensionalArrayValue (data, ec, ub, mdi, col, m, ri);
			return col;
		}

		void SetMultiDimensionalArrayValue (JsonArray data, ReflectionCache et, int[] upperBounds, int[] indexes, Array array, RevertJsonValue m, int rankIndex) {
			if (rankIndex + 1 == upperBounds.Length) {
				foreach (var item in data) {
					array.SetValue (m (this, item, et), indexes);
					++indexes[rankIndex];
				}
				return;
			}
			for (int i = 0; i < upperBounds[rankIndex]; i++) {
				var ob = data[indexes[rankIndex]] as JsonArray;
				if (ob == null) {
					continue;
				}

				else {
					for (int j = indexes.Length - 1; j > rankIndex; j--) {
						indexes[j] = 0;
					}
					SetMultiDimensionalArrayValue (ob, et, upperBounds, indexes, array, m, rankIndex + 1);
					++indexes[rankIndex];
				}
			}
		}

		object CreateList (JsonArray data, ReflectionCache listType, object input) {
			var ec = listType.ArgumentReflections != null ? listType.ArgumentReflections[0] : null;
			var r = listType.ItemDeserializer;
			object l = input ?? listType.Instantiate ();
			IList col = l as IList;
			if (col != null) {
				if (r != null) {
					foreach (var item in data) {
						// TODO: determine whether item type is nullable
						col.Add (item != null ? r (this, item, ec) : null);
					}
					return col;
				}
			}
			var a = listType.AppendItem;
			if (a != null) {
				if (l == null) {
					throw new JsonSerializationException ("The collection member typed \"" + listType.AssemblyName + "\" was null and could not be instantiated");
				}
				foreach (var item in data) {
					a (l, r (this, item, ec));
				}
				return l;
			}
			// TODO: candidate of code clean-up.
			Type et = listType.ArgumentTypes != null ? listType.ArgumentTypes[0] : null;
			// create an array of objects
			foreach (var o in data) {
				if (o is IDictionary)
					col.Add (CreateObject ((JsonDict)o, ec, null));

				else if (o is JsonArray) {
					if (et.IsGenericType)
						col.Add (o);//).ToArray());
					else
						col.Add (((JsonArray)o).ToArray ());
				}
				else
					col.Add (ChangeType (o, et));
			}
			return col;
		}

		object CreateStringKeyDictionary (JsonDict reader, ReflectionCache pt) {
			var col = (IDictionary)pt.Instantiate ();
			// NOTE: argument 0 is not used
			ReflectionCache ec = pt.ArgumentReflections != null ? pt.ArgumentReflections[1] : null;
			var m = ec != null ? ec.DeserializeMethod : RevertUndefined;
			foreach (KeyValuePair<string, object> values in reader) {
				col.Add (values.Key, m (this, values.Value, ec));
			}
			return col;
		}

		object CreateDictionary (JsonArray reader, ReflectionCache pt) {
			IDictionary col = (IDictionary)pt.Instantiate ();
			ReflectionCache c1 = null, c2 = null;
			if (pt.ArgumentReflections != null) {
				c1 = pt.ArgumentReflections[0];
				c2 = pt.ArgumentReflections[1];
			}
			var mk = c1.DeserializeMethod;
			var mv = c2.DeserializeMethod;

			foreach (JsonDict values in reader) {
				col.Add (mk (this, values["k"], c1), mv (this, values["v"], c2));
			}

			return col;
		}

#if !SILVERLIGHT
		DataSet CreateDataSet (JsonDict reader) {
			DataSet ds = new DataSet ();
			ds.EnforceConstraints = false;
			ds.BeginInit ();

			// read dataset schema here
			var schema = reader.Schema;

			if (schema is string) {
				TextReader tr = new StringReader ((string)schema);
				ds.ReadXmlSchema (tr);
			}
			else {
				DatasetSchema ms = (DatasetSchema)CreateObject ((JsonDict)schema, _manager.GetReflectionCache (typeof (DatasetSchema)), null);
				ds.DataSetName = ms.Name;
				for (int i = 0; i < ms.Info.Count; i += 3) {
					if (ds.Tables.Contains (ms.Info[i]) == false)
						ds.Tables.Add (ms.Info[i]);
					ds.Tables[ms.Info[i]].Columns.Add (ms.Info[i + 1], Type.GetType (ms.Info[i + 2]));
				}
			}

			foreach (KeyValuePair<string, object> pair in reader) {
				//if (pair.Key == "$type" || pair.Key == "$schema") continue;

				JsonArray rows = (JsonArray)pair.Value;
				if (rows == null) continue;

				DataTable dt = ds.Tables[pair.Key];
				ReadDataTable (rows, dt);
			}

			ds.EndInit ();

			return ds;
		}

		void ReadDataTable (JsonArray rows, DataTable dataTable) {
			dataTable.BeginInit ();
			dataTable.BeginLoadData ();
			List<int> guidcols = new List<int> ();
			List<int> datecol = new List<int> ();

			foreach (DataColumn c in dataTable.Columns) {
				if (typeof (Guid).Equals (c.DataType) || typeof (Guid?).Equals (c.DataType))
					guidcols.Add (c.Ordinal);
				if (_params.UseUTCDateTime && (typeof (DateTime).Equals (c.DataType) || typeof (DateTime?).Equals (c.DataType)))
					datecol.Add (c.Ordinal);
			}
			var gc = guidcols.Count > 0;
			var dc = datecol.Count > 0;

			foreach (JsonArray row in rows) {
				object[] v = new object[row.Count];
				row.CopyTo (v, 0);
				if (gc) {
					foreach (int i in guidcols) {
						string s = (string)v[i];
						if (s != null && s.Length < 36)
							v[i] = new Guid (Convert.FromBase64String (s));
					}
				}
				if (dc) {
					foreach (int i in datecol) {
						var s = v[i];
						if (s != null)
							v[i] = CreateDateTime (this, s);
					}
				}
				dataTable.Rows.Add (v);
			}

			dataTable.EndLoadData ();
			dataTable.EndInit ();
		}

		DataTable CreateDataTable (JsonDict reader) {
			var dt = new DataTable ();

			// read dataset schema here
			var schema = reader.Schema;

			if (schema is string) {
				TextReader tr = new StringReader ((string)schema);
				dt.ReadXmlSchema (tr);
			}
			else {
				var ms = (DatasetSchema)CreateObject ((JsonDict)schema, _manager.GetReflectionCache (typeof (DatasetSchema)), null);
				dt.TableName = ms.Info[0];
				for (int i = 0; i < ms.Info.Count; i += 3) {
					dt.Columns.Add (ms.Info[i + 1], Reflection.Instance.GetTypeFromCache (ms.Info[i + 2]));
				}
			}

			foreach (var pair in reader) {
				//if (pair.Key == "$type" || pair.Key == "$schema")
				//	continue;

				var rows = (JsonArray)pair.Value;
				if (rows == null)
					continue;

				if (!dt.TableName.Equals (pair.Key, StringComparison.InvariantCultureIgnoreCase))
					continue;

				ReadDataTable (rows, dt);
			}

			return dt;
		}
#endif
		#endregion

		internal static RevertJsonValue GetRevertMethod (JsonDataType type) {
			return _revertMethods[(int)type];
		}
		#region RevertJsonValue delegate methods
		internal static object RevertPrimitive (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return value;
		}
		internal static object RevertInt32 (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return value is double ? (int)(double)value : (int)(long)value;
		}
		internal static object RevertByte (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return value is double ? (byte)(double)value : (byte)(long)value;
		}
		internal static object RevertSByte (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return value is double ? (sbyte)(double)value : (sbyte)(long)value;
		}
		internal static object RevertShort (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return value is double ? (short)(double)value : (short)(long)value;
		}
		internal static object RevertUShort (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return value is double ? (ushort)(double)value : (ushort)(long)value;
		}
		internal static object RevertUInt32 (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return value is double ? (uint)(double)value : (uint)(long)value;
		}
		internal static object RevertUInt64 (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return value is double ? (ulong)(double)value : (ulong)(long)value;
		}
		internal static object RevertSingle (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return value is double ? (float)(double)value : (float)(long)value;
		}
		internal static object RevertDecimal (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return value is double ? (decimal)(double)value : (decimal)(long)value;
		}
		internal static object RevertChar (JsonDeserializer deserializer, object value, ReflectionCache type) {
			var s = value as string;
			return s.Length > 0 ? s[0] : '\0';
		}
		internal static object RevertGuid (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return CreateGuid (value);
		}
		static object CreateGuid (object value) {
			var s = (string)value;
			return s.Length > 30 ? new Guid (s) : new Guid (Convert.FromBase64String (s));
		}
		internal static object RevertTimeSpan (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return CreateTimeSpan (value);
		}
		static object CreateTimeSpan (object value) {
			// TODO: Optimize TimeSpan
			return TimeSpan.Parse ((string)value);
		}
		internal static object RevertByteArray (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return Convert.FromBase64String ((string)value);
		}
		internal static object RevertDateTime (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return CreateDateTime (deserializer, value);
		}
		internal static object CreateDateTime (JsonDeserializer deserializer, object value) {
			string t = (string)value;
			//                   0123456789012345678 9012 9/3
			// datetime format = yyyy-MM-ddTHH:mm:ss .nnn  Z
			int year = ValueConverter.ToInt32 (t, 0, 4);
			int month = ValueConverter.ToInt32 (t, 5, 2);
			int day = ValueConverter.ToInt32 (t, 8, 2);
			int hour = ValueConverter.ToInt32 (t, 11, 2);
			int min = ValueConverter.ToInt32 (t, 14, 2);
			int sec = ValueConverter.ToInt32 (t, 17, 2);
			int ms = (t.Length > 21 && t[19] == '.') ? ValueConverter.ToInt32 (t, 20, 3) : 0;
			bool utc = (t[t.Length - 1] == 'Z');

			if (deserializer._params.UseUTCDateTime == false || utc == false)
				return new DateTime (year, month, day, hour, min, sec, ms);
			else
				return new DateTime (year, month, day, hour, min, sec, ms, DateTimeKind.Utc).ToLocalTime ();
		}
		internal static object RevertUndefined (JsonDeserializer deserializer, object value, ReflectionCache type) {
			if (value == null) return null;
			var d = value as JsonDict;
			if (d != null) {
				return deserializer.CreateObject (d, type, null);
			}
			var a = value as JsonArray;
			if (a != null) {
				return deserializer.CreateList (a, type, null);
			}
			return value;
		}
		internal static object RevertArray (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return deserializer.CreateArray ((JsonArray)value, type);
		}
		internal static object RevertMultiDimensionalArray (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return deserializer.CreateMultiDimensionalArray ((JsonArray)value, type);
		}
		internal static object RevertList (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return deserializer.CreateList ((JsonArray)value, type, null);
		}
		internal static object RevertDataSet (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return deserializer.CreateDataSet ((JsonDict)value);
		}
		internal static object RevertDataTable (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return deserializer.CreateDataTable ((JsonDict)value);
		}
		internal static object RevertHashTable (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return deserializer.RootHashTable ((JsonArray)value);
		}
		internal static object RevertDictionary (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return deserializer.RootDictionary (value, type);
		}
		internal static object RevertNameValueCollection (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return CreateNameValueCollection ((JsonDict)value);
		}
		internal static object RevertStringDictionary (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return CreateStringDictionary ((JsonDict)value);
		}
		internal static object RevertStringKeyDictionary (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return deserializer.CreateStringKeyDictionary ((JsonDict)value, type);
		}
		internal static object RevertEnum (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return deserializer.CreateEnum (value, type.Type);
		}
		internal static object RevertCustom (JsonDeserializer deserializer, object value, ReflectionCache type) {
			return deserializer._manager.CreateCustom ((string)value, type.Type);
		}
		//internal static object ChangeType (JsonDeserializer deserializer, object value, ReflectionCache type) {
		//	return deserializer.ChangeType (value, type);
		//}
		#endregion
	}
}
