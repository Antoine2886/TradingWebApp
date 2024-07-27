using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bd.Infrastructure
{
    public class Balance
    {
        public Guid Id { get; set; }
        public float TotalBalance { get; set; }
        public float AvailableBalance { get; set; }

        public Balance()
        {
            Id = Guid.NewGuid();
            TotalBalance = 10000;
            AvailableBalance = TotalBalance;
        }
    }

}
