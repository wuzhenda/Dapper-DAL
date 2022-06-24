using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Dapper.DAL.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Dapper.DAL.Infrastructure
{
    public class DapperContext : IDapperContext
    {
        private readonly string _connectionString;
        private IDbConnection _connection;

        public DapperContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Dapper");
        }

        public IDbConnection Connection 
        {
            get
            {
                if (_connection == null)
                {
                    _connection = new SqlConnection(_connectionString);
                }
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }
                return _connection;
            }
        }

        public void Dispose()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
                _connection.Close();
        }
    }
}