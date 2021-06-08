using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ConcurrencyProject.Models
{
    public class BankAccount
    {
        public int BankAccountId { get; set; }
        public string AccountName { get; set; }
        [ConcurrencyCheck]
        public int Balance { get; set; }
    }
}
