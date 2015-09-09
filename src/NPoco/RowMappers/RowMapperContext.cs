using System;

namespace NPoco.RowMappers
{
    public struct RowMapperContext
    {
        public object Instance { get; set; }
        public PocoData PocoData { get; set; }
        public Type Type { get { return PocoData.Type; } }
    }
}