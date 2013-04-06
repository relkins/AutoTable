using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AutoTable
{
    public class Engine
    {
        private string ConnectionString { get; set; }
        private string Schema { get; set; }

        //Table Schema Cache
        private ConcurrentDictionary<string, Table> Tables = new ConcurrentDictionary<string, Table>();

        public Engine(string connectionString, string schema = "dbo")
        {
            //TODO: Validation

            this.ConnectionString = connectionString;
            this.Schema = schema;

            //TODO: Pre-load the table cache by querying INFORMATION_SCHEMA
        }

        /// <summary>
        /// Insert a new entry
        /// </summary>
        public void Insert(Entry entry)
        {
            //Sync the schema of the entry with the database and cache
            SyncTable(entry);

            //Insert the new entry into the database
            InsertEntry(entry);
        }

        /// <summary>
        /// Updated a previously recorded entry
        /// </summary>
        public void Update(Entry entry)
        {
            //Sync the schema of the entry with the database and cache
            SyncTable(entry);

            //Update the entry based on a matching Key value
            UpdateEntry(entry);
        }

        /// <summary>
        /// Syncronizes the database and cache with any new tables or columns based on entry data
        /// </summary>
        private void SyncTable(Entry entry)
        {
            Table table;

            //Attempt to find the cached fields for the table
            if (!Tables.TryGetValue(entry.Table, out table))
            {
                //Create the table in the database
                CreateTable(entry.Table);

                //Cache the table
                table = Tables.GetOrAdd(entry.Table, new Table(entry.Table));
            }

            //Check if there are any new columns on the entry which the cache doesn't have
            var newColumns = entry.Data.Where(d => !table.Columns.Contains(d.Key)).Select(c => c.Key).ToList();
            if (newColumns.Any())
            {
                //Alter the existing table with any new fields that are found and add to cache
                AlterTable(entry.Table, newColumns);

                //Cache the columns
                newColumns.ForEach((c) => table.Columns.Add(c));
            }
        }

        /// <summary>
        /// Creates a new table with predefined columns
        /// </summary>
        /// <param name="name">Table name</param>
        private void CreateTable(string name)
        {
            using (SqlConnection conn = new SqlConnection(this.ConnectionString))
            {
                conn.Open();

                string query = string.Format(@"
                    IF NOT EXISTS (SELECT * FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_SCHEMA] = @Schema AND [TABLE_NAME] = @Table)
                    BEGIN
	                    CREATE TABLE [{0}].[{1}]
                        ( 
                            [Id] INT IDENTITY(1, 1) NOT NULL,
                            [Key] NVARCHAR(200) NOT NULL, 
                            [CreatedDate] DATETIME2 NOT NULL,
                            [UpdatedDate] DATETIME2 NULL,
                            CONSTRAINT [PK_{1}_Id] PRIMARY KEY CLUSTERED ([Id]),
                            CONSTRAINT [UQ_{1}_Key] UNIQUE ([Key]) 
                        );
                        CREATE INDEX [IX_{1}_CreatedDate] ON [{0}].[{1}] ([CreatedDate] DESC);
                        CREATE INDEX [IX_{1}_UpdatedDate] ON [{0}].[{1}] ([UpdatedDate] DESC);
                    END
                ", this.Schema, name);

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.Add(new SqlParameter("@Schema", this.Schema));
                cmd.Parameters.Add(new SqlParameter("@Table", name));
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Performs a table alter to add additional columns to an existing table 
        /// </summary>
        /// <param name="name">Table name</param>
        /// <param name="newColumns">Collection of columns names</param>
        private void AlterTable(string name, List<string> newColumns)
        {
            if (newColumns.Any())
            {
                using (SqlConnection conn = new SqlConnection(this.ConnectionString))
                {
                    conn.Open();

                    StringBuilder query = new StringBuilder();
                    foreach (string newColumn in newColumns)
                    {
                        query.AppendLine(string.Format(@"
                        IF NOT EXISTS (SELECT * FROM [INFORMATION_SCHEMA].[COLUMNS] WHERE [TABLE_SCHEMA] = @Schema AND [TABLE_NAME] = @Table AND [COLUMN_NAME] = '{2}')
                        BEGIN
	                        ALTER TABLE [{0}].[{1}] ADD {2} NVARCHAR(500) NULL;
                        END
                    ", this.Schema, name, newColumn));
                    }

                    SqlCommand cmd = new SqlCommand(query.ToString(), conn);
                    cmd.Parameters.Add(new SqlParameter("@Schema", this.Schema));
                    cmd.Parameters.Add(new SqlParameter("@Table", name));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Inserts a new entry into an existing table
        /// </summary>
        private void InsertEntry(Entry entry)
        {
            using (SqlConnection conn = new SqlConnection(this.ConnectionString))
            {
                conn.Open();

                string[] columnNames = entry.Data.Select(d => "[" + d.Key + "]").ToArray();
                string[] dataParameters = entry.Data.Select(d => "@" + d.Key).ToArray();
                string query = string.Format(@"INSERT INTO [{0}].[{1}] ([Key],[CreatedDate],{2}) VALUES (@Key,@CreatedDate,{3});", this.Schema, entry.Table, string.Join(",", columnNames), string.Join(",", dataParameters));

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.Add(new SqlParameter("@Key", entry.Key));
                cmd.Parameters.Add(new SqlParameter("@CreatedDate", DateTime.UtcNow));
                cmd.Parameters.AddRange(entry.Data.Select(d => new SqlParameter("@" + d.Key, d.Value)).ToArray());
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Updates a previously recorded entry using the Key value as the identifier
        /// </summary>
        private void UpdateEntry(Entry entry)
        {
            using (SqlConnection conn = new SqlConnection(this.ConnectionString))
            {
                conn.Open();

                string[] parameters = entry.Data.Select(d => string.Format("[{0}] = @{1}", d.Key, d.Key)).ToArray();
                string query = string.Format(@"UPDATE [{0}].[{1}] SET [UpdatedDate] = @UpdatedDate,{2} WHERE [Key] = @Key;", this.Schema, entry.Table, string.Join(",", parameters));

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.Add(new SqlParameter("@UpdatedDate", DateTime.UtcNow));
                cmd.Parameters.Add(new SqlParameter("@Key", entry.Key));
                cmd.Parameters.AddRange(entry.Data.Select(d => new SqlParameter("@" + d.Key, d.Value)).ToArray());
                cmd.ExecuteNonQuery();
            }
        }
    }
}
