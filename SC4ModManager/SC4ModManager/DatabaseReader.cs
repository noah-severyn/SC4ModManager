using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SQLite;

namespace SC4ModManager {
    public class DatabaseReader {
        [Table("records")]
        public class Record {
            public string Title { get; set; }
            public string TGI { get; set; }
            public string Author { get; set; }
            public string ExmpName { get; set; }
        }




        private SQLiteConnection _conn;

        public DatabaseReader() {
            string path = "C:\\Users\\Administrator\\OneDrive\\Documents\\SC4ModManager\\SC4ModManager\\SC4ModManager\\Catalog.db";
            if (!File.Exists(path)) {
                throw new Exception();
            }
            _conn = InitialiseConnection();
        }

        private SQLiteConnection InitialiseConnection() {
            var DataSource = "C:\\Users\\Administrator\\OneDrive\\Documents\\SC4ModManager\\SC4ModManager\\SC4ModManager\\Catalog.db";
            SQLiteConnectionString options = new SQLiteConnectionString(DataSource, false);
            return new SQLiteConnection(options);

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
