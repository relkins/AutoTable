using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTable
{
    public class Entry
    {
        /// <summary>
        /// Table Name
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// Unique Identifier
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Data
        /// </summary>
        public Dictionary<string, string> Data { get; set; }

        /// <summary>
        /// Child Entries
        /// </summary>
        public List<Entry> Entries { get; set; }

        public Entry(string table, string key, Dictionary<string, string> data = null)
        {
            //TODO: Validation (Detect forbidden data keys)

            this.Table = table;
            this.Key = key;
            this.Data = data ?? new Dictionary<string, string>();
        }
    }
}
