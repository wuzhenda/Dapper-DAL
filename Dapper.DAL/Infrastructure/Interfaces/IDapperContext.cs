using System;
using System.Data;

namespace Dapper.DAL.Infrastructure.Interfaces
{
    public interface IDapperContext : IDisposable
    {
        IDbConnection Connection { get; }
    }
}