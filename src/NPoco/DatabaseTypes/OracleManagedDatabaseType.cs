using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPoco.DatabaseTypes
{
    public class OracleManagedDatabaseType : OracleDatabaseType
    {
        public override string GetProviderName()
        {
            return "Oracle.ManagedDataAccess.Client";
        }
    }
}
