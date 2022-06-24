using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Dapper.DAL.Infrastructure;
using Dapper.DAL.Infrastructure.Enum;
using Dapper.DAL.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Dapper.DAL.Infrastructure.EnumQueriesStoredProcedures;

namespace DapperDALExample.Imp
{
    public class Example
    {
        /// <summary>
        /// Example UnitOfWork property
        /// </summary>
        public IUnitOfWork UnitOfWork { get; set; }

        /// <summary>   
        /// Constructor injection
        /// </summary>
        /// <param name="unitOfWork">reference to data access layer</param>
        public Example(IConfiguration configuration)
        {  
            IDapperContext context = new DapperContext(configuration);
            IFactoryRepository repoFactory = new FactoryRepository();
            IUnitOfWork unitOfWork = new UnitOfWork(context, repoFactory);
            UnitOfWork = unitOfWork;
        }

        //public void TestDtu()
        //{
        //    // Get Repository
        //    var  dtuRepo = UnitOfWork.GetRepository<ByDtu,EmptyEnum>();

        //    var stopwatch = new Stopwatch();
        //    stopwatch.Start();
        //    var dtuModel = dtuRepo.GetByKey(1061);
        //    stopwatch.Stop();
        //    Console.WriteLine($"dtu_bianhao:"+((dtuModel is ByDtu)? dtuModel.Bianhao:"")+" take time seconds:"+stopwatch.Elapsed.TotalSeconds);

        //    stopwatch.Restart();
        //    var etorModel = UnitOfWork.GetRepository<ByEtor, EmptyEnum>().GetBy(
        //            where: new { dtu_id = dtuModel.Id},
        //            order: new { create_time = SortAs.Desc}
        //        ).FirstOrDefault();
        //    stopwatch.Stop();

        //    Console.WriteLine($"etor_bianhao:" + ((etorModel is ByEtor) ? etorModel.Bianhao : "") + " take time seconds:" + stopwatch.Elapsed.TotalSeconds);

        //}


        public void GetDataByStoredProcedure()
        {
            // Get Repository
            IRepository<Customer, CustomerEnum> repo = UnitOfWork.GetRepository<Customer, CustomerEnum>();
            // Executing stored procedure
            var param = new DynamicParameters();
            param.Add("@startIndex", 10);
            param.Add("@endIndex", 20);
            param.Add("@count", dbType: DbType.Int32, direction: ParameterDirection.Output);
            //Example for string return / out param
            //param.Add("@errorMsg", dbType: DbType.String, size: 4000, direction: ParameterDirection.ReturnValue);
            IEnumerable<Customer> customers = repo.Exec<Customer>(CustomerEnum.GetCustomerByPage, param);
            int count = param.Get<int>("@count");
        }

        public void GetDataByGetByMethod()
        {
            // Get Repository
            IRepository<Customer, CustomerEnum> repo = UnitOfWork.GetRepository<Customer, CustomerEnum>();
            // Get data with filtering and ordering
            IEnumerable<Customer> customers =
                repo.GetBy(
                    where: new { Zip = "12345", Registered = new DateTime(year: 2013, month: 7, day: 7) },
                    order: new { Registered = SortAs.Desc, Name = SortAs.Asc }
                );
            // Generated SQL Query : 
            // 'SELECT * FROM Customer WHERE Zip = @Zip AND Registered = @Registered ORDER BY Registered DESC, Name ASC'
            // and setup parameters @Zip, @Registered
        }
    
    }
}