using System;
using System.Collections.Generic;
using System.Linq;

namespace NPoco.RowMappers
{
    public static class MyEnumerableExtensions
    {
        public static IEnumerable<GroupResult<TKey>> GroupByMany<TKey>(this IEnumerable<TKey> elements, Func<TKey, string> stringFunc, string splitBy, int i = 0)
        {
            return elements
                .Select(x => new { Item = x, Parts = stringFunc(x).Split(new[] { splitBy }, StringSplitOptions.RemoveEmptyEntries) })
                .GroupBy(x => x.Parts.Skip(i).FirstOrDefault())
                .Where(x => x.Key != null)
                .Select(g => new GroupResult<TKey>
                {
                    Item = g.Key,
                    Key = g.Select(x => x.Item).First(),
                    Count = g.Count(),
                    SubItems = g.Select(x => x.Item).GroupByMany(stringFunc, splitBy, i + 1).ToList()
                });
        }
    }
}