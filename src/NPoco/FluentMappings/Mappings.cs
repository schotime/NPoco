using System;
using System.Collections.Generic;

namespace NPoco.FluentMappings
{
    public class Mappings
    {
        public Dictionary<Type, TypeDefinition> Config = new Dictionary<Type, TypeDefinition>();

        public Map<T> For<T>()
        {
            var definition = new TypeDefinition(typeof(T));
            var petaPocoMap = new Map<T>(definition);
            Config[typeof (T)] = definition;
            return petaPocoMap;
        }

        public static Mappings BuildMappingsFromMaps(params IMap[] petaPocoMaps)
        {
            var petaPocoConfig = new Mappings();
            foreach (var petaPocoMap in petaPocoMaps)
            {
                var type = petaPocoMap.TypeDefinition.Type;
                petaPocoConfig.Config[type] = petaPocoMap.TypeDefinition;
            }
            return petaPocoConfig;
        }
    }
}