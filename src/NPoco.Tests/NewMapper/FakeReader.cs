using System;
using System.Data;

namespace NPoco.Tests.NewMapper
{
    public class FakeReader : IDataReader
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public virtual string GetName(int i)
        {
            switch (i)
            {
                case 0: return "Name";
                case 1: return "MoneyId";
                case 2: return "Money__Value";
                case 3: return "Money__Currency";
                case 4: return "Money__Money2__Value";
                case 5: return "Money__Money2__Currency";
            }
            return null;
        }

        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public Type GetFieldType(int i)
        {
            switch (i)
            {
                case 0: return typeof(string);
                case 1: return typeof(int);
                case 2: return typeof(decimal);
                case 3: return typeof(string);
                case 4: return typeof(decimal);
                case 5: return typeof(string);
            }
            return null;
        }

        public object GetValue(int i)
        {
            switch (i)
            {
                case 0: return "Name";
                case 1: return 5;
                case 2: return 23m;
                case 3: return "AUD";
                case 4: return 24m;
                case 5: return "USD";
            }
            return null;
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public bool GetBoolean(int i)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            throw new NotImplementedException();
        }

        public int GetInt32(int i)
        {
            return (int)GetValue(i);
        }

        public long GetInt64(int i)
        {
            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            throw new NotImplementedException();
        }

        public double GetDouble(int i)
        {
            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            return (string)GetValue(i);
        }

        public decimal GetDecimal(int i)
        {
            return (decimal)GetValue(i);
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            return false;
        }

        public int FieldCount { get { return 4; } }

        object IDataRecord.this[int i]
        {
            get { throw new NotImplementedException(); }
        }

        object IDataRecord.this[string name]
        {
            get { throw new NotImplementedException(); }
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public bool NextResult()
        {
            throw new NotImplementedException();
        }

        public bool Read()
        {
            throw new NotImplementedException();
        }

        public int Depth { get; private set; }
        public bool IsClosed { get; private set; }
        public int RecordsAffected { get; private set; }
    }
}