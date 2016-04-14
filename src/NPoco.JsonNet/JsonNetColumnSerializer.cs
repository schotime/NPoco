using System;
using Newtonsoft.Json;

namespace NPoco
{
    public class JsonNetColumnSerializer : IColumnSerializer
    {
        public JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { DateFormatHandling = DateFormatHandling.IsoDateFormat };
        
        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, SerializerSettings);
        }

        public object Deserialize(string value, Type targetType)
        {
            return JsonConvert.DeserializeObject(value, targetType, SerializerSettings);
        }
    }
}