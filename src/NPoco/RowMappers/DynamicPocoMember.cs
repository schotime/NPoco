using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace NPoco.RowMappers
{
    public class DynamicPocoMember : PocoMember
    {
        public override void SetValue(object target, object value)
        {
            ((IDictionary) target)[Name] = value;
        }

        public override object GetValue(object target)
        {
            var val = ((IDictionary)target).Contains(Name) ? ((IDictionary)target)[Name] : null;
            return val;
        }
    }
}