using System;
using System.Collections.Generic;

namespace NPoco.Compiled
{
    public class CompiledQuery
    {
        public Sql Template {get;set; }
        public List<CompiledData> Data {  get; set; }
        public Func<object> CompiledExpression { get; set; }
    }
}