using System;
using Microsoft.Extensions.Configuration;


namespace DapperDALExample // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            IConfiguration configuration = configurationBuilder.Build();

            var ex = new Imp.Example(configuration);
            
            //ex.TestDtu();

            Console.WriteLine("===done===");
            Console.ReadLine();
        }
    }
}