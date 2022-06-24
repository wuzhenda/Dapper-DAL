using System.Data;
using Dapper.DAL.General;

namespace Dapper.DAL.Infrastructure.Interfaces
{
    public interface IUnitOfWork
    {
        IDapperContext Context { get; }
        IDbTransaction Transaction { get; }
        IRepository<TSet, TEnumSp> GetRepository<TSet, TEnumSp>() where TSet : class where TEnumSp : EnumBase<TEnumSp, string>;
        IDbTransaction BeginTransaction();
        void Commit();
    }
}