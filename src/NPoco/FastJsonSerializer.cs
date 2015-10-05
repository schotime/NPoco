using System;
using NPoco.FastJSON;

namespace NPoco
{
    public class FastJsonColumnSerializer : IColumnSerializer
    {
        public static JSONParameters JsonParameters = new JSONParameters() { UseUTCDateTime = false, UseExtensions = false };
        
        public string Serialize(object value)
        {
            return JSON.ToJSON(value, JsonParameters);
        }

        public object Deserialize(string value, Type targeType)
        {
            return new JsonDeserializer(JsonParameters, JSON.Manager).ToObject(value, targeType);
        }
    }
}