using System.Data;
using Dapper.DAL.General;

namespace Dapper.DAL.Infrastructure.EnumQueriesStoredProcedures
{
    public sealed class EmptyEnum : EnumBase<EmptyEnum, string>
    {
        public EmptyEnum(string Name, string EnumValue, CommandType? cmdType)
            : base(Name, EnumValue, cmdType)
        {
        }
    }
}