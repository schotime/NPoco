namespace NPoco.FluentMappings
{
    public static class ConventionExtensions
    {
        public static IColumnsBuilderConventions IgnoreComplex(this IColumnsBuilderConventions conventions)
        {
            return conventions.IgnoreWhere(y => !(y.PropertyType.IsValueType || y.PropertyType == typeof(string) || y.PropertyType == typeof(byte[])));
        }

        public static void WithSmartConventions(this IConventionScanner scanner)
        {
            scanner.PrimaryKeysNamed(y => y.Name + "Id");
            scanner.TablesNamed(y => Inflector.MakePlural(y.Name));
            scanner.Columns.IgnoreComplex();
        }
    }
}