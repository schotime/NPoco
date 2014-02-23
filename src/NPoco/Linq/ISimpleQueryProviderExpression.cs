using NPoco.Expressions;

namespace NPoco.Linq
{
    public interface ISimpleQueryProviderExpression<TModel>
    {
        SqlExpression<TModel> AtlasSqlExpression { get; }
    }
}