using System;
using System.Collections.Generic;

namespace NPoco.RowMappers
{
    public static class PropertyMapperNameConvention
    {
        public static string SplitPrefix = "npoco_";

        public static IEnumerable<PropertyMapper.PosName> ConvertFromConvention(this IEnumerable<PropertyMapper.PosName> posNames)
        {
            string prefix = null;
            foreach (var posName in posNames)
            {
                if (posName.Name.StartsWith(SplitPrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    prefix = posName.Name.Substring(SplitPrefix.Length);
                    continue;
                }

                if (prefix != null)
                {
                    posName.Name = PocoDataBuilder.JoinStrings(prefix, posName.Name);
                }

                yield return posName;
            }
        }
    }
}