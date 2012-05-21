using System;
using System.Collections.Generic;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public class ConventionScannerSettings
    {
        public ConventionScannerSettings()
        {
            Assemblies = new HashSet<Assembly>();
            IgnorePropertiesWhere = new List<Func<PropertyInfo, bool>>();
            IncludeTypes = new List<Func<Type, bool>>();
        }

        public Mappings MappingOverrides { get; set; }

        public HashSet<Assembly> Assemblies { get; set; }
        public bool TheCallingAssembly { get; set; }
        public List<Func<Type, bool>> IncludeTypes { get; set; }

        public Func<Type, string> TablesNamed { get; set; }
        public Func<Type, string> PrimaryKeysNamed { get; set; }
        public Func<Type, bool> PrimaryKeysAutoIncremented { get; set; }
        public Func<Type, string> SequencesNamed { get; set; }

        public Func<PropertyInfo, string> PropertiesNamed { get; set; }
        public List<Func<PropertyInfo, bool>> IgnorePropertiesWhere { get; set; }
        public Func<PropertyInfo, bool> VersionPropertiesWhere { get; set; }
        public Func<PropertyInfo, bool> ResultPropertiesWhere { get; set; }
        public Func<PropertyInfo, bool> ForceDateTimesToUtcWhere { get; set; }

        public bool Lazy { get; set; }
    }
}