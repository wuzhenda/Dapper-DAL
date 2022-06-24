using Dapper.DAL.General;

namespace Dapper.DAL.Infrastructure.Interfaces
{
    public interface IFactoryRepository
    {
        IRepository<T, TRepoSp> CreateRepository<T, TRepoSp>(IDapperContext context)
            where T : class
            where TRepoSp : EnumBase<TRepoSp, string>;
    }
}