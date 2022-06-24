using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DapperDALExample.Imp
{
    public class Customer
    {
		[Key]
		public int Id { get; set; }

		[Required]
		public string Name { get; set; }

		public string? Description { get; set; }
		public string? Address { get; set; }

		public string? Zip { get; set; }

		public int Balance { get; set; }

		public DateTime Registered { get; set; }

    }
}
