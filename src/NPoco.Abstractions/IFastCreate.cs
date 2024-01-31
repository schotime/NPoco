using System.Data.Common;

namespace NPoco
{
    public interface IFastCreate
    {
        object Create(DbDataReader dataReader);
    }
}