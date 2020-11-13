using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NPoco.RowMappers
{
    public static class PropertyMapperNameConvention
    {
        public static string SplitPrefix = "npoco_";

        internal static IEnumerable<PosName> ConvertFromNewConvention(this IEnumerable<PosName> posNames, PocoData pocoData)
        {
            var allMembers = pocoData.GetAllMembers().ToList();
            var scopedPocoMembers = pocoData.Members;
            string prefix = null;

            foreach (var posName in posNames)
            {
                if (posName.Name == "npoco")
                {
                    scopedPocoMembers.Clear();
                    prefix = null;
                    continue;
                }

                if (posName.Name.StartsWith(SplitPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    prefix = posName.Name.Substring(SplitPrefix.Length);
                    var relevantMembers = allMembers.SingleOrDefault(x => string.Equals(PocoColumn.GenerateKey(x.MemberInfoChain), prefix, StringComparison.OrdinalIgnoreCase));
                    if (relevantMembers != null)
                    {
                        scopedPocoMembers = relevantMembers.PocoMemberChildren;
                    }
                    
                    continue;
                }

                var member = FindMember(scopedPocoMembers, posName.Name);
                if (member != null && member.PocoColumn != null)
                {
                    posName.Name = member.PocoColumn.MemberInfoKey;
                }
                else
                {
                    posName.Name = PocoDataBuilder.JoinStrings(prefix, posName.Name);
                }

                yield return posName;
            }
        }

        internal static IEnumerable<PosName> ConvertFromOldConvention(this IEnumerable<PosName> posNames, List<PocoMember> pocoMembers)
        {
            var used = new Dictionary<PocoMember, int>();
            var members = FlattenPocoMembers(pocoMembers).ToList();
            var level = 0;

            foreach (var posName in posNames)
            {
                var pocoMemberLevels = members.Where(x => !used.ContainsKey(x.PocoMember) && x.Level >= level);
                var member = FindMember(pocoMemberLevels, posName.Name);
                
                if (member?.PocoMember?.PocoColumn != null)
                {
                    level = member.Level;
                    used.Add(member.PocoMember, level);
                    posName.Name = member.PocoMember.PocoColumn.MemberInfoKey;
                }

                yield return posName;
            }
        }

        internal static PocoMemberLevel FindMember(IEnumerable<PocoMemberLevel> pocoMembers, string name)
        {
            return pocoMembers
                .Where(x => x.PocoMember.ReferenceType == ReferenceType.None)
                .FirstOrDefault(x => IsPocoMemberEqual(x.PocoMember, name));
        }

        internal static PocoMember FindMember(IEnumerable<PocoMember> pocoMembers, string name)
        {
            return pocoMembers
                .Where(x => x.ReferenceType == ReferenceType.None)
                .FirstOrDefault(x => IsPocoMemberEqual(x, name));
        }

        private static bool IsPocoMemberEqual(PocoMember pocoMember, string name)
        {
            if (pocoMember.PocoColumn == null)
                return PropertyMapper.IsEqual(name, pocoMember.Name, false);

            if (pocoMember.PocoColumn.MemberInfoKey == name)
                return true;

            if (PropertyMapper.IsEqual(name, pocoMember.PocoColumn.ColumnAlias ?? pocoMember.PocoColumn.ColumnName, pocoMember.PocoColumn.ExactColumnNameMatch))
                return true;

            return PropertyMapper.IsEqual(name, pocoMember.Name, pocoMember.PocoColumn.ExactColumnNameMatch);
        }

        private static IEnumerable<PocoMemberLevel> FlattenPocoMembers(List<PocoMember> pocoMembers, int levelMonitor = 1)
        {
            foreach (var pocoMember in pocoMembers.OrderBy(x => x.PocoMemberChildren.Count != 0))
            {
                if (pocoMember.PocoColumn != null)
                {
                    yield return new PocoMemberLevel { PocoMember = pocoMember, Level = levelMonitor };
                }

                if (pocoMember.PocoMemberChildren.Count == 0)
                    continue;

                levelMonitor++;
                foreach (var pocoMemberLevel in FlattenPocoMembers(pocoMember.PocoMemberChildren, levelMonitor))
                {
                    yield return pocoMemberLevel;
                }
            }
        }

        internal class PocoMemberLevel
        {
            public PocoMember PocoMember { get; set; }
            public int Level { get; set; }
        }
    }
}