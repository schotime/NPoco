using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPoco
{
    public interface IDataContext
    {
        object Poco { get; }
        string TableName { get; }
        string PrimaryKeyName { get; }
        
    }
}
