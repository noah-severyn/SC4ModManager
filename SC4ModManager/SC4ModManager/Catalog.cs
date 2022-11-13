using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SQLite;
using SQLitePCL;
using System.IO;

namespace SC4ModManager {
	class Catalog {


		/// <summary>
		/// Table to track which TGIs are in which dependency pack. This class represents one item (row) in the TGITable.
		/// </summary>
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


		public class DatabaseHandler {
			private SQLiteConnection _db;
			public DatabaseHandler(string dbpath) {
				dbpath = Path.Combine(dbpath, "Catalog.db");
				bool exists = File.Exists(dbpath);
				bool isFirstInit = !exists;


				if (isFirstInit) {
					//make sure the specified folder exists and create if not
					var folderPath = Path.GetDirectoryName(dbpath);
					Directory.CreateDirectory(folderPath);
					File.CreateText(dbpath).Dispose(); //what does this do???
				}

				_db = new SQLiteConnection(dbpath);
				if (isFirstInit) {
					_db.CreateTable<TGIItem>();
					_db.CreateTable<PathItem>();
				}
			}



			public void AddTGI(string path, string tgi, string exmpName) {
				//check if we already have a matching pathid, create new pathitem if not, otherwise use found pathid
				int pathID = GetPathID(path);
				if (pathID <= 0) {
					PathItem newPath = new PathItem {
						PathName = path
					};
					_db.Insert(newPath);
					//pathID = _db.Query<PathItem>("SELECT * FROM PathTable WHERE PathID = (SELECT Max(PathID) FROM PathTable)")[0].PathID;
					pathID = newPath.PathID;
				}
				
				//once we know our pathitem we can then add the new tgi with that pathitem
				TGIItem newTGI = new TGIItem {
					PathID = pathID,
					TGI = tgi,
					ExemplarName = exmpName
				};
				_db.Insert(newTGI);
				
			}



			//return -1 if item is not found or if multiple matches were found
			//PathItems table should always have unique items, so we might have an issue if the return is more than one item
			public int GetPathID(string path) {
				string searchPath = path.Replace(@"\\", @"\");
				List<PathItem> result = _db.Query<PathItem>($"SELECT * FROM PathTable WHERE PathName = '{searchPath}'");

				if (result.Count == 1) {
					return result[0].PathID;
				} else {
					return -1;
				}
			}




			//public void AddItem(TGIItem item) {
			//	_db.Insert(item);
			//}

				//public void WriteAllItems() {
				//	var tgis = _db.Query<TGIItem>("SELECT * FROM DependencyTGIs");
				//	foreach (var item in tgis) {
				//		Debug.WriteLine(item);
				//	}
				//}

				//public void GetItem(int id) {
				//	var item = _db.Query
				//}
		}

		
	}
}
