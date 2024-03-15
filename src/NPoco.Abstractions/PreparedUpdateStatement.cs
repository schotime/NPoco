using System.Collections.Generic;

namespace NPoco
{
    public class PreparedUpdateStatement
    {
        public PocoData PocoData { get; set; }
        public string VersionName { get; set; }
        public object VersionValue { get; set; }
        public VersionColumnType VersionColumnType { get; set; }
        public string Sql { get; set; }
        public List<object> Rawvalues { get; set; }
        public Dictionary<string, object> PrimaryKeyValuePairs { get; set; }
    }
}