using System;
using System.Collections.Generic;
using System.Linq;

namespace NPoco.RowMappers
{
    public static class PropertyMapperNameConvention
    {
        public static string SplitPrefix = "npoco_";

        public static IEnumerable<PosName> ConvertFromNewConvention(this IEnumerable<PosName> posNames)
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

        public static IEnumerable<PosName> ConvertFromOldConvention(this IEnumerable<PosName> posNames, List<PocoMember> pocoMembers)
        {
            var used = new Dictionary<PocoMember, bool>();
            var members = FlattenPocoMembers(pocoMembers).ToList();

            foreach (var posName in posNames)
            {
                var unusedPocoMembers = members.Where(x => !used.ContainsKey(x)).ToList();
                var member = PropertyMapper.FindMember(unusedPocoMembers, posName.Name);
                if (member != null && member.PocoColumn != null)
                {
                    used.Add(member, true);
                    posName.Name = member.PocoColumn.MemberInfoKey;
                }

                yield return posName;
            }
        }

        private static IEnumerable<PocoMember> FlattenPocoMembers(List<PocoMember> pocoMembers)
        {
            foreach (var pocoMember in pocoMembers.OrderBy(x => x.PocoMemberChildren.Count != 0))
            {
                if (pocoMember.PocoColumn != null)
                {
                    yield return pocoMember;
                }

                foreach (var pocoMemberChild in FlattenPocoMembers(pocoMember.PocoMemberChildren))
                {
                    yield return pocoMemberChild;
                }
            }
        }
    }
}