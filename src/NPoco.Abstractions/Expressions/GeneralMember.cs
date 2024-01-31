using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPoco.Expressions
{

    public class GeneralMember
    {
        public Type EntityType { get; set; }
        public PocoColumn PocoColumn { get; set; }
        public PocoColumn[] PocoColumns { get; set; }
    }
}
