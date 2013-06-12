using NPoco.FluentMappings;
using NPoco.Tests.Common;

namespace NPoco.Tests.FluentMappings
{
    public class SupervisorMap : Map<Supervisor>
    {
        public SupervisorMap(TypeDefinition t) : base(t)
        {
            UseMap<UserMap>();
            Columns(x => x.Column(y => y.IsSupervisor).Result());
        }
    }
}