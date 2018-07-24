using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot.Models
{
    public class AggregatedScore
    {
        public string Author { get; set; }

        public ulong AuthorId { get; set; }

        public double Score { get; set; }

        public int Count { get; set; }
    }
}
