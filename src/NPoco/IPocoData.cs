using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPoco
{
    public interface IPocoData
    {
        Dictionary<string, PocoColumn> QueryColumns { get; }
        TableInfo TableInfo { get; }
        Dictionary<string, PocoColumn> Columns { get; }
    }
}
