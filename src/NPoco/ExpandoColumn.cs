using System.Collections;
using System.Collections.Generic;

namespace NPoco
{
    public class ExpandoColumn : PocoColumn
    {
        public override void SetValue(object target, object val)
        {
            ((IDictionary) target)[ColumnName] = val;
        }

        public override object GetValue(object target) 
        {
            var val = ((IDictionary)target).Contains(ColumnName) ? ((IDictionary)target)[ColumnName] : null;
            return val;
        }

        public override object ChangeType(object val) { return val; }
    }
}