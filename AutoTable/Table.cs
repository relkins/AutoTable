using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTable
{
    public class Table
    {
        public string Name { get; set; }
        public HashSet<string> Columns { get; set; }

        public Table(string name, HashSet<string> columns = null)
        {
            //TODO: Validation

            this.Name = name;
            this.Columns = columns ?? new HashSet<string>();
        }
    }
}
