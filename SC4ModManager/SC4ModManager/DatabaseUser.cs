using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Reflection;

namespace SC4ModManager {
    public class DatabaseUser {
        public SqliteConnection conn;

        public DatabaseUser() {
            string path = "C:\\Users\\Administrator\\OneDrive\\Documents\\SC4ModManager\\SC4ModManager\\SC4ModManager\\Catalog.db";
            if (!File.Exists(path)) {
                throw new Exception();
            }

            conn = new SqliteConnection($"Data Source={path};");
            conn.Open();
            SqliteCommand cmd = conn.CreateCommand();

            StringBuilder query = new StringBuilder();
            query.AppendLine("SELECT Packs.Title, Packs.Author, TGIs.TGI, TGIs.ExmpName FROM TGITable TGIs");
            query.AppendLine("INNER JOIN PackTable Packs");
            query.AppendLine("on TGIs.PathID = Packs.PackID");
            query.AppendLine("WHERE TGI like '%7abc%'");

            //cmd.CommandText = "SELECT * FROM TGITable WHERE TGI like '%7abc%'";
            cmd.CommandText= query.ToString();

            using (var reader = cmd.ExecuteReader()) {

                while (reader.Read()) {
                    var tgi = reader.GetString(2);
                    var name = reader.GetString(3);

                }
            }
        }
    }
}
