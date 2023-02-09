using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SQLite;

namespace SC4ModManager {
    /// <summary>
    /// An item representing a record in the Prop Texture catalog table.
    /// </summary>
    [Table("records")]
    public class Record {
        public string Title { get; set; }
        public string TGI { get; set; }
        public string Author { get; set; }
        public string ExmpName { get; set; }
    }


    /// <summary>
    /// Open and process the Prop Texture Catalog database.
    /// </summary>
    public class DatabaseReader {
        private SQLiteConnection _conn;

        public DatabaseReader() {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Catalog.db");
            if (!File.Exists(path)) {
                throw new Exception();
            }
            SQLiteConnectionString options = new SQLiteConnectionString(path, false);
            _conn=  new SQLiteConnection(options);
        }

        public void GetRecords() {
			string search = "7abc";

			StringBuilder query = new StringBuilder();
			query.AppendLine("SELECT PackTable.Title, TGITable.TGI, PackTable.Author, TGITable.ExmpName FROM TGITable");
			query.AppendLine("LEFT JOIN PathTable ON TGITable.PathID = PathTable.PathID");
			query.AppendLine("LEFT JOIN PackTable ON TGITable.PathID = PackTable.PackID");
			query.AppendLine($"WHERE Title like '%{search}%' OR TGI like '%{search}%' OR Author like '%{search}%' OR ExmpName like '%{search}%'");

			var results = _conn.Query<Record>(query.ToString());
            _conn.Close();

		}

    }
}
