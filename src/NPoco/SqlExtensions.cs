namespace NPoco
{
    public static class SqlExtensions
    {
        public static Sql ToSql(this SqlBuilder.Template template)
        {
            return new Sql(true, template.RawSql, template.Parameters);
        }
    }
}