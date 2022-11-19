﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SQLite;
using SQLitePCL;
using System.IO;
using Microsoft.Data.Sqlite;

namespace SC4ModManager {
    /// <summary>
    /// Table to track which TGIs are in which dependency pack. This class represents one item (row) in the TGITable.
    /// </summary>s
    [Table("TGITable")]
    public class TGIItem {
        [PrimaryKey, AutoIncrement]
        [Column("TGIID")]
        public int ID { get; set; }

        [Column("PathID")]
        public int PathID { get; set; }

        [Column("TGI")]
        public string TGI { get; set; }

        [Column("ExmpName")]
        public string ExemplarName { get; set; }

        public override string ToString() {
            return $"{ID}, {PathID}, {TGI}, {ExemplarName}";
        }
    }

    /// <summary>
    /// Table to track the file path each PathID maps to. This class represents one item (row) in the PathTable.
    /// </summary>
    [Table("PathTable")]
    public class PathItem {
        [PrimaryKey, AutoIncrement]
        [Column("PathID")]
        public int PathID { get; set; }

        [Column("PathName")]
        public string PathName { get; set; }

        public override string ToString() {
            return $"{PathID}, {PathName}";
        }
    }

    /// <summary>
    /// Table containing information on each dependency pack (aka download or file).
    /// </summary>
    [Table("PackTable")]
    public class PackItem {
        [PrimaryKey, AutoIncrement]
        [Column("PackID")]
        public int PackID { get; set; }

        [Column("Title")]
        public string Title { get; set; }

        [Column("Hyperlink")]
        public string Hyperlink { get; set; }

        [Column("Author")]
        public string Author { get; set; }

        [Column("Type")]
        public string Type { get; set; }

        [Column("PrimaryCat")]
        public string PrimaryCat { get; set; }

        [Column("SecondaryCat")]
        public string SecondaryCat { get; set; }

        public override string ToString() {
            return $"{PackID}: {Title}, by {Author}. Type:{Type}, Primary:{PrimaryCat}, Secondary:{SecondaryCat}";
        }
    }


    public class DatabaseBuilder {
		
		/// <summary>
		/// Create and operate on the Prop Texture Catalog database.
		/// </summary>
		public class DatabaseHandler {
			private SQLiteConnection _db;
			public DatabaseHandler(string dbpath) {
				dbpath = Path.Combine(dbpath, "Catalog.db");
				bool exists = File.Exists(dbpath);
				bool isFirstInit = !exists;

                //make sure the specified folder + file exists and create if not
                if (isFirstInit) {
					var folderPath = Path.GetDirectoryName(dbpath);
					Directory.CreateDirectory(folderPath);
					File.CreateText(dbpath).Dispose(); //Opens new StreamWriter to create the file then closes the writer - writing is handled by the SQLite functions
				}

				_db = new SQLiteConnection(dbpath);
				if (isFirstInit) {
					_db.CreateTable<TGIItem>();
					_db.CreateTable<PathItem>();
					_db.CreateTable<PackItem>();
				}
			}


			/// <summary>
			/// Add a TGI item with associated information to the database.
			/// </summary>
			/// <param name="path">File path the TGI is contained in</param>
			/// <param name="tgi">String representation of the TGI in the format 0x00000000 0x00000000 0x00000000 </param>
			/// <param name="exmpName">Name of the exemplar; null if the TGI is a texture</param>
			/// <remarks>The path in TGITable is stored as a reference to the full path in PathTable. This dramatically reduces file size as the long path string only needs to be stored once.</remarks>
			public void AddTGI(string path, string tgi, string exmpName) {
				//check if we already have a matching pathid, create new pathitem if not, otherwise use found pathid
				int pathID = GetPathID(path);
				if (pathID <= 0) {
					PathItem newPath = new PathItem {
						PathName = path
					};
					_db.Insert(newPath);
					pathID = newPath.PathID;
				}

				//once we know our pathitem (if it already exists) we can then add the new tgi with that pathitem
				//but first check if this item already exists
				if (!DoesTGIExist(tgi)) {
					TGIItem newTGI = new TGIItem {
						PathID = pathID,
						TGI = tgi,
						ExemplarName = exmpName
					};
					_db.Insert(newTGI);
				}
			}


			//return -1 if item is not found or if multiple matches were found
			//PathItems table should always have unique items, so we might have an issue if the return is more than one item
			private int GetPathID(string path) {
				string searchPath = path.Replace(@"\\", @"\").Replace("'","''");
				List<PathItem> result = _db.Query<PathItem>($"SELECT * FROM PathTable WHERE PathName = '{searchPath}'");

				if (result.Count == 1) {
					return result[0].PathID;
				} else {
					return -1;
				}
			}

			private bool DoesTGIExist(string tgi) {
				List<TGIItem> result = _db.Query<TGIItem>($"SELECT * FROM TGIItable WHERE TGI = '{tgi}'");
				return result.Count == 0;
			}
		}
	}
}
