using NPoco;

namespace NPoco.Tests.NewMapper.Models
{
    [TableName("Users")]
    public class UsersNameProjection
    {
        [Column("Name")]
        public virtual string _TheName { get; set; }
    }
}