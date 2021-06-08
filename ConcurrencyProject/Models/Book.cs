using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConcurrencyProject.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public string Author { get; set; }
        public int NbrOfPages { get; set; }
    }
}
