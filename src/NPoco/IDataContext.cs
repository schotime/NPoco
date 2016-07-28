
namespace NPoco
{
    public interface IDataContext
    {
        object Poco { get; }
        string TableName { get; }
        string PrimaryKeyName { get; }
        
    }
}
