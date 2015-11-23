using NPoco.FluentMappings;
using NPoco.Tests.Common;

namespace NPoco.Tests.FluentMappings
{
    public class UserMap : Map<User>
    {
        public UserMap(TypeDefinition t) : base(t)
        {
            Columns(x => x.Column(y => y.Age).Ignore());
        }
    }
}