using System;
using System.Collections;
using System.Data;
using System.Data.Common;

namespace NPoco.Tests.NewMapper
{
    public class FakeReader : DbDataReader
    {
#if !DNXCORE50
        public override void Close()
        {
            throw new NotImplementedException();
        }

        public override DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }
#endif

        public override bool NextResult()
        {
            throw new NotImplementedException();
        }

        public override bool Read()
        {
            throw new NotImplementedException();
        }

        public override int Depth { get; }
        public override bool IsClosed { get; }
        public override int RecordsAffected { get; }

        public override bool GetBoolean(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override byte GetByte(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override Guid GetGuid(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override short GetInt16(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetInt32(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetInt64(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override string GetName(int i)
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

        public override int GetValues(object[] values)
        {
            values[0] = "Name";
            values[1] = 5;
            values[2] = 23m;
            values[3] = "AUD";
            values[4] = 24m;
            values[5] = "USD";
            return 1;
        }

        public override int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override Type GetFieldType(int i)
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

        public override object GetValue(int i)
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

        public override double GetDouble(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override float GetFloat(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override string GetString(int i)
        {
            return (string)GetValue(i);
        }

        public override decimal GetDecimal(int i)
        {
            return (decimal)GetValue(i);
        }

        public override DateTime GetDateTime(int i)
        {
            throw new NotImplementedException();
        }

        public override bool IsDBNull(int i)
        {
            return false;
        }

        public override int FieldCount { get { return 6; } }

        public override object this[int ordinal]
        {
            get { throw new NotImplementedException(); }
        }

        public override object this[string name]
        {
            get { throw new NotImplementedException(); }
        }

        public override bool HasRows { get; }
    }
}