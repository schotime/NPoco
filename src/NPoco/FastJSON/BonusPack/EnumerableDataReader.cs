using System;
using System.Collections.Generic;
using System.Data;

namespace NPoco.FastJSON.BonusPack
{
	/// <summary>
	/// Turns an <see cref="IEnumerable{T}"/> collection into an <see cref="EnumerableDataReader"/>.
	/// </summary>
	public static class EnumerableDataReader
	{
		/// <summary>
		/// Creates an <see cref="EnumerableDataReader&lt;T&gt;"/> instance from a given <see cref="IEnumerable&lt;T&gt;"/> instance.
		/// </summary>
		/// <typeparam name="T">The type of the data.</typeparam>
		/// <param name="collection">The data to be read.</param>
		/// <returns>An <see cref="EnumerableDataReader&lt;T&gt;"/> instance.</returns>
		public static EnumerableDataReader<T> Create<T> (IEnumerable<T> collection) {
			return new EnumerableDataReader<T> (collection);
		}
	}

	/// <summary>
	/// Experimental Feature:
	/// Converts <see cref="IEnumerable{T}"/> instances into <see cref="IDataReader"/> for <see cref="System.Data.SqlClient.SqlBulkCopy.WriteToServer(IDataReader)"/>.
	/// </summary>
	/// <remarks>References:
	/// 1) https://github.com/matthewschrager/Repository/blob/master/Repository.EntityFramework/EntityDataReader.cs;
	/// 2) http://www.codeproject.com/Articles/876276/Bulk-Insert-Into-SQL-From-Csharp</remarks>
	/// <typeparam name="T">The data type in the data source.</typeparam>
	public class EnumerableDataReader<T> : IDataReader
	{
		readonly static Dictionary<Type, byte> _scalarTypes = InitScalarTypes ();

		static Dictionary<Type, byte> InitScalarTypes () {
			return new Dictionary<Type, byte> () {
				{ typeof(string), 0 },

				{ typeof(byte), 0 },
				{ typeof(short), 0 },
				{ typeof(ushort), 0 },
				{ typeof(bool), 0 },
				{ typeof(int), 0 },
				{ typeof(uint), 0 },
				{ typeof(long), 0 },
				{ typeof(ulong), 0 },
				{ typeof(char), 0 },
				{ typeof(float), 0 },
				{ typeof(double), 0 },
				{ typeof(decimal), 0 },
				{ typeof(Guid), 0 },
				{ typeof(DateTime), 0 },
				{ typeof(TimeSpan), 0 },

				{ typeof(byte?), 0 },
				{ typeof(short?), 0 },
				{ typeof(ushort?), 0 },
				{ typeof(bool?), 0 },
				{ typeof(int?), 0 },
				{ typeof(uint?), 0 },
				{ typeof(long?), 0 },
				{ typeof(ulong?), 0 },
				{ typeof(char?), 0 },
				{ typeof(float?), 0 },
				{ typeof(double?), 0 },
				{ typeof(decimal?), 0 },
				{ typeof(Guid?), 0 },
				{ typeof(DateTime?), 0 },
				{ typeof(TimeSpan?), 0 }
			};
		}

		IEnumerator<T> _enumerator;
		readonly int _fieldCount;
		readonly string[] _memberNames;
		readonly MemberCache[] _members;

		/// <summary>
		/// Initializes a new instance of the <see cref="EnumerableDataReader{T}"/> class.
		/// </summary>
		/// <param name="collection">The collection to be exported.</param>
		public EnumerableDataReader (IEnumerable<T> collection) : this (collection, false) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="EnumerableDataReader{T}"/> class.
		/// </summary>
		/// <param name="collection">The collection.</param>
		/// <param name="showReadOnlyValues">if set to <c>true</c>, read-only values will be exported to the <see cref="IDataReader"/>.</param>
		/// <exception cref="NotSupportedException">This exception will be thrown when <typeparamref name="T"/> is a scalar type.</exception>
		public EnumerableDataReader (IEnumerable<T> collection, bool showReadOnlyValues) {
			if (collection == null) {
				throw new ArgumentNullException ("collection");
			}
			var t = typeof(T);
			if (_scalarTypes.ContainsKey (t)) {
				throw new NotSupportedException (t.FullName + " is not supported.");
			}
			_enumerator = collection.GetEnumerator ();
			var p = SerializationManager.Instance.GetReflectionCache (t).Getters;
			_fieldCount = p.Length;
			_memberNames = new string[_fieldCount];
			_members = new MemberCache[_fieldCount];
			int c = 0;
			for (int i = 0; i < _fieldCount; i++) {
				var g = p[i];
				if (showReadOnlyValues == false && g.Serializable == TriState.False) {
					continue;
				}
				_memberNames[c] = p[i].SerializedName;
				_members[c] = p[i].Member;
				++c;
			}
			if (c < _fieldCount) {
				_fieldCount = c;
				Array.Resize (ref _memberNames, c);
				Array.Resize (ref _members, c);
			}
		}

		/// <summary>
		/// Closes the reader.
		/// </summary>
		public void Close () {
			_enumerator.Dispose ();
		}

		/// <summary>
		/// Gets the depth of the reader (0 is always returned).
		/// </summary>
		public int Depth {
			get { return 0; }
		}

		/// <summary>
		/// Gets the schema table.
		/// </summary>
		/// <returns>The schema table containing the following columns for each member: ColumnName, ColumnOrdinal, DataType, DataTypeName, ColumnSize.</returns>
		public DataTable GetSchemaTable () {
			DataTable t = new DataTable ();
			for (int i = 0; i < _fieldCount; i++) {
				DataRow row = t.NewRow ();
				row["ColumnName"] = GetName (i);
				row["ColumnOrdinal"] = i;
				Type type = GetFieldType (i);
				var c = SerializationManager.Instance.GetReflectionCache (type);
				if (c.CommonType == ComplexType.Nullable) {
					type = c.ArgumentTypes[0];
				}
				row["DataType"] = type;
				row["DataTypeName"] = GetDataTypeName (i);
				row["ColumnSize"] = -1;
				t.Rows.Add (row);
			}
			return t;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is closed.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is closed; otherwise, <c>false</c>.
		/// </value>
		public bool IsClosed {
			get { return _enumerator == null; }
		}

		/// <summary>
		/// Proceed to the next result.
		/// </summary>
		/// <returns>Always returns false.</returns>
		public bool NextResult () {
			return false;
		}

		/// <summary>
		/// Reads an object in the collection.
		/// </summary>
		/// <returns>True if there is an object being read, otherwise, false.</returns>
		/// <exception cref="System.ObjectDisposedException">The instance is disposed.</exception>
		public bool Read () {
			if (_enumerator == null) {
				throw new ObjectDisposedException ("EnumerableDataReader");
			}

			return _enumerator.MoveNext ();
		}

		/// <summary>
		/// Gets the records affected (always returns -1).
		/// </summary>
		public int RecordsAffected {
			get { return -1; }
		}

		/// <summary>
		/// Dispose the internal collection enumerator.
		/// </summary>
		public void Dispose () {
			if (_enumerator != null) {
				_enumerator.Dispose ();
				_enumerator = null;
			}
		}

		/// <summary>
		/// Gets the field count.
		/// </summary>
		public int FieldCount {
			get { return _fieldCount; }
		}

		/// <summary>
		/// Gets a boolean value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public bool GetBoolean (int i) {
			return Convert.ToBoolean (GetValue (i));
		}

		/// <summary>
		/// Gets a byte value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public byte GetByte (int i) {
			return Convert.ToByte (GetValue (i));
		}

		/// <summary>
		/// Gets the bytes at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <param name="fieldOffset">The field offset.</param>
		/// <param name="buffer">The buffer.</param>
		/// <param name="bufferoffset">The buffer offset.</param>
		/// <param name="length">The length to read.</param>
		/// <returns>The number of bytes copied into the buffer.</returns>
		public long GetBytes (int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) {
			var buf = (byte[])GetValue (i);
			int bytes = Math.Min (length, buf.Length - (int)fieldOffset);
			Buffer.BlockCopy (buf, (int)fieldOffset, buffer, bufferoffset, bytes);
			return bytes;
		}

		/// <summary>
		/// Gets a <see cref="char"/> value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public char GetChar (int i) {
			return Convert.ToChar (GetValue (i));
		}

		/// <summary>
		/// Gets a <see cref="char"/> array at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <param name="fieldoffset">The field offset.</param>
		/// <param name="buffer">The buffer.</param>
		/// <param name="bufferoffset">The buffer offset.</param>
		/// <param name="length">The length to read.</param>
		/// <returns>The number of bytes copied into the buffer.</returns>
		public long GetChars (int i, long fieldoffset, char[] buffer, int bufferoffset, int length) {
			string s = GetString (i);
			int chars = Math.Min (length, s.Length - (int)fieldoffset);
			s.CopyTo ((int)fieldoffset, buffer, bufferoffset, chars);
			return chars;
		}

		/// <summary>
		/// This method is not implemented.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns></returns>
		/// <exception cref="System.NotImplementedException"></exception>
		public IDataReader GetData (int i) {
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Gets the name of the data type.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns></returns>
		public string GetDataTypeName (int i) {
			var a = _members[i];
			return a.MemberType.Name;
		}

		/// <summary>
		/// Gets a <see cref="DateTime"/> value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public DateTime GetDateTime (int i) {
			return Convert.ToDateTime (GetValue (i));
		}

		/// <summary>
		/// Gets a <see cref="decimal"/> value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public decimal GetDecimal (int i) {
			return Convert.ToDecimal (GetValue (i));
		}

		/// <summary>
		/// Gets a <see cref="double"/> value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public double GetDouble (int i) {
			return Convert.ToDouble (GetValue (i));
		}

		/// <summary>
		/// Gets the type of the field.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The <see cref="Type"/> of the member at specific field index.</returns>
		public Type GetFieldType (int i) {
			var a = _members[i];
			return a.MemberType;
		}

		/// <summary>
		/// Gets a <see cref="float"/> value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public float GetFloat (int i) {
			return Convert.ToSingle (GetValue (i));
		}

		/// <summary>
		/// Gets a <see cref="Guid"/> value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public Guid GetGuid (int i) {
			var v = GetValue (i);
			if (v is Guid || v  is Guid?) {
				return (Guid)v;
			}
			if (v is string) {
				return new Guid ((string)v);
			}
			if (v is byte[]) {
				return new Guid ((byte[])v);
			}
			return Guid.Empty;
		}

		/// <summary>
		/// Gets a <see cref="short"/> value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public short GetInt16 (int i) {
			return Convert.ToInt16 (GetValue (i));
		}

		/// <summary>
		/// Gets a <see cref="int"/> value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public int GetInt32 (int i) {
			return Convert.ToInt32 (GetValue (i));
		}

		/// <summary>
		/// Gets a <see cref="long"/> value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public long GetInt64 (int i) {
			return Convert.ToInt64 (GetValue (i));
		}

		/// <summary>
		/// Gets the name of the field.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The member name at the specific index.</returns>
		public string GetName (int i) {
			return _memberNames[i];
		}

		/// <summary>
		/// Gets the ordinal index of a member.
		/// </summary>
		/// <param name="name">The name of the member.</param>
		/// <returns>The field index of the member.</returns>
		public int GetOrdinal (string name) {
			return Array.IndexOf (_memberNames, name);
		}

		/// <summary>
		/// Gets a <see cref="string"/> value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public string GetString (int i) {
			return Convert.ToString (GetValue (i));
		}

		/// <summary>
		/// Gets the value at the specific index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value.</returns>
		public object GetValue (int i) {
			return _members[i].Getter (_enumerator.Current);
		}

		/// <summary>
		/// Loads all values into the <paramref name="values"/> array.
		/// </summary>
		/// <param name="values">The array which holds the field values.</param>
		/// <returns>The number of fields loaded into the array.</returns>
		public int GetValues (object[] values) {
            if (values == null) {
                throw new ArgumentNullException ("values");
            }
			var vl = values.Length;
			var l = _fieldCount > vl ? vl : _fieldCount;
			for (int i = l - 1; i >= 0; i--) {
				values[i] = GetValue (i);
			}
			return l;
		}

		/// <summary>
		/// Determines whether the field at the specific index is <see cref="DBNull"/>.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>True if the value is <see cref="DBNull"/></returns>
		public bool IsDBNull (int i) {
			return Convert.IsDBNull (GetValue (i));
		}

		/// <summary>
		/// Gets the value of a member with the specified name.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <returns>The value of the member with the specific name.</returns>
		public object this[string name] {
			get { return GetValue (Array.IndexOf (_memberNames, name)); }
		}

		/// <summary>
		/// Gets the value of a member at the specified index.
		/// </summary>
		/// <param name="i">The index of the field.</param>
		/// <returns>The value of a member at the specified index.</returns>
		public object this[int i] {
			get { return GetValue (i); }
		}
	}
}
