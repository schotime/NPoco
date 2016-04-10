using System;

namespace NPoco
{
    public class FastJSONColumnSerializer : IColumnSerializer
    {
        public string Serialize(object value)
        {
            return fastJSON.JSON.ToJSON(value);
        }

        public object Deserialize(string value, Type targetType)
        {
            return fastJSON.JSON.ToObject(value, targetType);
        }
    }
}