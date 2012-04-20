using System;
using System.Collections.Generic;

namespace NPoco.FluentMappings
{
    public class PetaPocoMappings
    {
        public Dictionary<Type, PetaPocoTypeDefinition> Config = new Dictionary<Type, PetaPocoTypeDefinition>();

        public PetaPocoMap<T> For<T>()
        {
            var definition = new PetaPocoTypeDefinition(typeof(T));
            var petaPocoMap = new PetaPocoMap<T>(definition);
            Config.Add(typeof(T), definition);
            return petaPocoMap;
        }

        public static PetaPocoMappings BuildMappingsFromMaps(params IPetaPocoMap[] petaPocoMaps)
        {
            var petaPocoConfig = new PetaPocoMappings();
            foreach (var petaPocoMap in petaPocoMaps)
            {
                var type = petaPocoMap.TypeDefinition.Type;
                petaPocoConfig.Config[type] = petaPocoMap.TypeDefinition;
            }
            return petaPocoConfig;
        }
    }
}