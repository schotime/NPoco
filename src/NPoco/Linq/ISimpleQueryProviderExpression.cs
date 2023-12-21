using NPoco.Expressions;

namespace NPoco.Linq
{
    public interface ISimpleQueryProviderExpression<TModel>
    {
        ISqlExpression<TModel> AtlasSqlExpression { get; }
    }
}