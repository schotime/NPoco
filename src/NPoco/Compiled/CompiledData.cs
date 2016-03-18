using System;
using System.Reflection;

namespace NPoco.Compiled
{
    public class CompiledData
    {
        public int? Index { get; set; }
        public Guid UniqueValue { get; set; }
        public MemberAccessor MemberAccessor { get; set; }
        public string Name { get; set; }
    }
}