using System;

namespace NPoco
{
    public interface IColumnSerializer
    {
        string Serialize(object value);
        object Deserialize(string value, Type targetType);
    }
}