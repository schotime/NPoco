using System;
using Newtonsoft.Json;

namespace NPoco
{
    public class JsonNetColumnSerializer : IColumnSerializer
    {
        public static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { DateFormatHandling = DateFormatHandling.IsoDateFormat };
        
        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, SerializerSettings);
        }

        public object Deserialize(string value, Type targeType)
        {
            return JsonConvert.DeserializeObject(value, targeType, SerializerSettings);
        }
    }
}