using Dapper.DAL.General;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DapperDALExample.Imp
{
    public class CustomerEnum : EnumBase<CustomerEnum, string>
    {
        public static readonly CustomerEnum GetCustomerByPage = new CustomerEnum("GetCustomerByPage", "[dbo].[spCustomerListByPageGet]", CommandType.StoredProcedure);

        public CustomerEnum(string Name, string EnumValue, CommandType? cmdType)
            : base(Name, EnumValue, cmdType)
        {
        }
    }
}
