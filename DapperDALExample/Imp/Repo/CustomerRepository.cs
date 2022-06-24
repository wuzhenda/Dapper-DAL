using Dapper.DAL.Infrastructure;
using Dapper.DAL.Infrastructure.EnumQueriesStoredProcedures;
using Dapper.DAL.Infrastructure.Interfaces;


namespace DapperDALExample.Imp.Repo
{
    public class CustomerRepository : Repository<Customer, CustomerEnum>
    {
        public CustomerRepository(IDapperContext context) : base(context)
        {
        }
    }
}