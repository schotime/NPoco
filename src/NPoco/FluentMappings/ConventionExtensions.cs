using System;

namespace NPoco.FluentMappings
{
    public static class ConventionExtensions
    {
        public static IColumnsBuilderConventions IgnoreComplex(this IColumnsBuilderConventions conventions)
        {
            return conventions.IgnoreWhere(y => !(y.GetMemberInfoType().IsValueType || y.GetMemberInfoType() == typeof(string) || y.GetMemberInfoType() == typeof(byte[])));
        }

        public static void WithSmartConventions(this IConventionScanner scanner)
        {
            scanner.WithSmartConventions(false);
        }

        public static void WithSmartConventions(this IConventionScanner scanner, bool lowercase)
        {
            scanner.PrimaryKeysNamed(y => ToLowerIf(y.Name + "Id", lowercase));
            scanner.TablesNamed(y => ToLowerIf(Inflector.MakePlural(y.Name), lowercase));
            scanner.Columns.Named(x => ToLowerIf(x.Name, lowercase));
            scanner.Columns.IgnoreComplex();
            scanner.Columns.ForceDateTimesToUtcWhere(x => x.GetMemberInfoType() == typeof(DateTime) || x.GetMemberInfoType() == typeof(DateTime?));
        }

        private static string ToLowerIf(string s, bool clause)
        {
            return clause ? s.ToLowerInvariant() : s;
        }
    }
}