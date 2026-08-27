#if NET10_0_OR_GREATER
using System;
using System.Text.Json;

namespace NPoco
{
    /// <summary>
    /// Uses System.Text.Json to serialize/deserialize [ComplexMapping] columns. Only compiled for
    /// net10.0+, where System.Text.Json ships in the shared framework, so opting into this costs no
    /// extra package reference (unlike NPoco.JsonNet, which brings in Newtonsoft.Json).
    /// </summary>
    public class SystemTextJsonColumnSerializer : IColumnSerializer
    {
        public JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions();

        public string Serialize(object value)
        {
            return JsonSerializer.Serialize(value, SerializerOptions);
        }

        public object Deserialize(string value, Type targetType)
        {
            return JsonSerializer.Deserialize(value, targetType, SerializerOptions);
        }
    }
}
#endif
