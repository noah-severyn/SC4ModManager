using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using CsvHelper;
using System.Text;
using csDBPF;
using csDBPF.Properties;
using csDBPF.Entries;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using FSHLib;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace SC4ModManager {
	public static class Analysis {
        /// <summary>
        /// Creates a CSV of Rep13s (props, buildings, textures, flora) used on a series of lots.
        /// </summary>
        public class ListRep13IIDs {
            private const string Rep13sCSVpath = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\rep13IIDs.csv";

            /// <summary>
            /// Generates a CSV file of all props, textures, buildings, and flora used on lots (Rep 13 of the LotConfigPropertyLotObjectData properties).
            /// </summary>
            /// <param name="dbpfFiles">List of file paths to iterate through</param>
            /// <remarks>
            /// CSV Format is: {ParentTGI}, {Rep0IID}, 0x{Rep13IID}
            /// </remarks>
            public static void CreateCSV(List<string> dbpfFiles) {
                List<Rep13IIDs> listOfRep13IIDs = new List<Rep13IIDs>();

                //Loop through each file
                foreach (string filePath in dbpfFiles) {
                    DBPFFile dbpf = new DBPFFile(filePath);
                    List<DBPFEntryEXMP> exemplars = (List<DBPFEntryEXMP>) (from entry in dbpf.GetEntries()
                                                                           where entry.MatchesKnownEntryType(DBPFTGI.EXEMPLAR)
                                                                           select entry);

                    //Loop through each subfile
                    foreach (DBPFEntryEXMP entry in exemplars) {

                        //Initialize the list of properties for this entry
                        entry.DecodeEntry();

                        //Check the exemplar type and skip to next exemplar file if not a match
                        int exType = entry.GetExemplarType();
                        if (!(exType == (int) DBPFProperty.ExemplarTypes.LotConfiguration)) {
                            continue;
                        }

                        //We know this exemplar is type 0x10 (Lot Configuration) so continue on to snag the LotConfigPropertyLotObjectData properties
                        foreach (DBPFProperty property in entry.ListOfProperties.Values) {
                            //LotConfigPropertyLotObjectData must be between 0x88edc900 and 0x88edce00 - first one is 0x88edc900 and can continue on for max 1280 repetitions total
                            if (property.ID >= 0x88edc900 && property.ID <= 0x88edce00) {
                                List<long> lotObjects = (List<long>) property.GetData();

                                //First rep determines the type. We want to add only if it is 0x0 (building) or 0x1 (prop) or 0x2 (texture) or 0x4 (flora)
                                uint lotObjectType = (uint) lotObjects[0];
                                if (lotObjectType == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Building || lotObjectType == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Prop ||
                                    lotObjectType == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Texture || lotObjectType == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Flora) {
                                    listOfRep13IIDs.Add(new Rep13IIDs { ParentTGI = entry.TGI.ToString(), Rep0IID = lotObjectType, Rep13IID = (uint) lotObjects[12] });
                                }
                            }
                        }
                    }
                }

                WriteListToCSV(listOfRep13IIDs, Rep13sCSVpath);
            }

            /// <summary>
            /// Helper class to establish the format of a Rep13IID item.
            /// </summary>
            private class Rep13IIDs {
                public string ParentTGI { get; set; }
                public uint Rep0IID { get; set; }
                public uint Rep13IID { get; set; }

                public override string ToString() {
                    return $"{ParentTGI}, {Rep0IID}, 0x{DBPFUtil.ToHexString(Rep13IID)}";
                }
            }
        }


		/// <summary>
        /// Creates a CSV of TGIs found in the list of files.
        /// </summary>
        public class ListTGIs {
            private const string TGIsCSVpath = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\foundTGIs.csv";

            /// <summary>
            /// Generates a CSV file of all TGIs.
            /// </summary>
            /// <param name="dbpfFiles">List of file paths to iterate through</param>
            /// <remarks>
            /// CSV format: {FilePath}, {TGI}
            /// </remarks>
            public static void CreateCSV(List<string> dbpfFiles) {
                List<TGIs> allTGIs = new List<TGIs>();

                foreach (string filePath in dbpfFiles) {
                    DBPFFile dbpf = new DBPFFile(filePath);
                    foreach (DBPFTGI tgi in dbpf.GetTGIs()) {

                        //Add all Base/Overlay textures. Look at the least significant 4 bits and only add if it is 0, 5, or A: And the Instance by 0b1111 (0xF) and check the modulus result.
                        if (tgi.MatchesKnownTGI(DBPFTGI.FSH_BASE_OVERLAY) && ((tgi.InstanceID & 0xF) % 5) == 0) {
                            allTGIs.Add(new TGIs { FilePath = filePath, TGI = tgi.ToString() });
                        }

                        //Add all Exemplars
                        else if (tgi.MatchesKnownTGI(DBPFTGI.EXEMPLAR)) {
                            allTGIs.Add(new TGIs { FilePath = filePath, TGI = tgi.ToString() });
                        }

                        //Add all Cohorts. Note the Building/prop family of the Cohort is always 0x10000000 less than the Cohort's Index.
                        else if (tgi.MatchesKnownTGI(DBPFTGI.COHORT)) {
                            DBPFTGI family = new DBPFTGI((uint) tgi.TypeID, (uint) tgi.GroupID, (uint) (tgi.InstanceID - 0x10000000));
                            allTGIs.Add(new TGIs { FilePath = filePath, TGI = family.ToString() });
                        }

                    }
                }
                WriteListToCSV(allTGIs, TGIsCSVpath);
            }

            /// <summary>
            /// Simple helper class to hold fields for writing TGIs to CSV file.
            /// </summary>
            private class TGIs {
                public string FilePath { get; set; }
                public string TGI { get; set; }
                public override string ToString() {
                    return $"{FilePath}, {TGI}";
                }
            }
        }


        /// <summary>
        /// Collection of functions related to the creation of the Prop & Texture Catalog database.
        /// </summary>
        public class PropTextureCatalog {
            /// <summary>
            /// Creates a SQLite DB of props and lot textures from the list of files.
            /// </summary>
            /// <param name="dbpfFiles">List of file paths to iterate through</param>
            public static void BuildDB(List<FileInfo> dbpfFiles) {
                StringBuilder log = new StringBuilder("FileName,Type,Group,Instance,TGIType,TGISubtype,Message");

                string filename;
                string exemplarname;
                long? exemplartype;
                DBPFFile dbpf;
                DBPFEntryEXMP exmp;
                DBPFProperty prop;
                DBPFTGI family;
                var db = new DatabaseBuilder("C:\\source\\repos\\SC4ModManager\\SC4ModManager\\SC4ModManager");

                foreach (FileInfo file in dbpfFiles) {
                    Debug.WriteLine(file.Name);
                    dbpf = new DBPFFile(file);
                    log.AppendLine(dbpf.GetIssueLog());
                    foreach (DBPFEntry entry in dbpf.GetEntries()) {
                        filename = Path.GetFileNameWithoutExtension(file.Name);

                        //Add Base/Overlay textures
                        //Look at the least significant 4 bits and only add if it is 0, 5, or A: AND the Instance by 0b1111 (0xF) and examine the modulus result.
                        if (entry.MatchesKnownEntryType(DBPFTGI.FSH_BASE_OVERLAY) && ((entry.TGI.InstanceID & 0xF) % 5) == 0) {
                            db.AddTGI(filename, entry.TGI.ToString().Substring(0, 34), 2, null);
                        }

                        //Add Exemplars
                        else if (entry.MatchesKnownEntryType(DBPFTGI.EXEMPLAR)) {
                            exmp = (DBPFEntryEXMP) entry;
                            entry.DecodeEntry();
                            if (exmp.ListOfProperties.Count == 0) continue;
                            
                            exemplartype = exmp.GetExemplarType();
                            if (exemplartype == -1) {
                                log.AppendLine($"{filename}, {exmp.TGI.ToString().Replace(" ", "")},missing property: ExemplarType");
                                exemplartype = 0;
                            }

                            prop = exmp.GetProperty("ExemplarName");
                            if (prop is null) {
                                log.AppendLine($"{filename}, {exmp.TGI.ToString().Replace(" ", "")},missing property: ExemplarName");
                                exemplarname = "";
                            } else {
                                exemplarname = exemplarname = (string) prop.GetData();
                            }
                            
                            switch (exemplartype) {
                                case -1: //Unknown
                                    db.AddTGI(filename, exmp.TGI.ToString().Substring(0, 34), null, exemplarname);
                                    break;
                                case (long?) DBPFProperty.ExemplarTypes.Building: //0x02 Building
                                    db.AddTGI(filename, exmp.TGI.ToString().Substring(0, 34), 0, exemplarname);
                                    break;
                                case (long?) DBPFProperty.ExemplarTypes.Prop: //0x1E Prop
                                    db.AddTGI(filename, exmp.TGI.ToString().Substring(0, 34), 1, exemplarname);
                                    break;
                                case (long?) DBPFProperty.ExemplarTypes.FloraFauna: //0x0F Flora
                                    db.AddTGI(filename, exmp.TGI.ToString().Substring(0, 34), 4, exemplarname);
                                    break;
                                default:
                                    break;
                            }
                        }

                        //Add Cohorts
                        //Note the Building/prop family of the Cohort is always 0x10000000 less than the Cohort's Index.
                        else if (entry.MatchesKnownEntryType(DBPFTGI.COHORT)) {
                            exmp = (DBPFEntryEXMP) entry;
                            family = new DBPFTGI((uint) entry.TGI.TypeID, (uint) entry.TGI.GroupID, (uint) (entry.TGI.InstanceID - 0x10000000));
                            entry.DecodeEntry();
                            if (exmp.ListOfProperties.Count == 0) continue;

                            exemplarname = (string) exmp.GetProperty("ExemplarName").GetData();
                            db.AddTGI(filename, family.ToString().Substring(0, 34), 10, exemplarname);
                        }
                    }
                }

                File.WriteAllText("C:\\source\\repos\\SC4ModManager\\SC4ModManager\\SC4ModManager\\log.csv", log.ToString());
            }

            public static void fshTest() {
                //https://github.com/dmo2118/fsh2png/blob/master/fsh2png.c
                string directory = "C:\\Users\\Administrator\\OneDrive\\SC4 Deps\\tmp\\";
                IEnumerable<string> filePaths = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly);

                FileStream fs;
                string filename;
                foreach (string path in filePaths) {
                    if (Path.GetExtension(path) != ".fsh") continue;

                    filename = Path.GetFileNameWithoutExtension(path);
                    fs = new FileStream(path, FileMode.Open);

                    //FSHLib.dll. code yoinked from https://www.sc4devotion.com/forums/index.php?topic=7127.msg303057#msg303057
                    FSHImage fsh = new FSHImage(fs); //FOR FSH FILES ONLY
                    fs.Close();
                    BitmapItem bi = (BitmapItem) fsh.Bitmaps[0];
                    //Bitmap bmp = Blend(bi.Alpha, bi.Bitmap);
                    //bmp.MakeTransparent();
                    BlendBitmap bl = new BlendBitmap();
                    Bitmap bmp = bl.BlendBmp(bi);


                    //bmp.MakeTransparent(); //FOR OVERLAY TEXTURES ONLY - OR TEXTURES WITH AN ALPHA COMPONENT
                    bmp.Save(Path.Combine(directory, filename+".png"), ImageFormat.Png);
                }

            }


            //for each pixel in the alpha that is black, set that to transparent in the new bitmap
            private static Bitmap Blend(Bitmap alpha, Bitmap color) {
                //both bitmap must be same size
                if (alpha.Width != color.Width || alpha.Height != color.Height) {
                    throw new ArgumentException("Provided bitmaps are different sizes.");
                }
                for (int x = 0; x < color.Width; x++) {
                    for (int y = 0; y < color.Height; y++) {
                        if (alpha.GetPixel(x,y) == Color.Black) {
                            color.SetPixel(x, y, Color.Transparent);
                        }
                    }
                }
                return color;
            }
        }


        /// <summary>
        /// Create LotList and BldgList CSV files, used to replace the LotList spreadsheet to analyze a plugins folder.
        /// </summary>
		public class CreateLotList {
            private const string LotListCSVPath = "C:\\Users\\Administrator\\Documents\\SimCity 4\\Plugins\\IRM\\LotList.csv";
            private const string BldgListCSVPath = "C:\\Users\\Administrator\\Documents\\SimCity 4\\Plugins\\IRM\\BldgList.csv";

            /// <summary>
            /// Create LotList and BldgList CSV files, used to replace the LotList spreadsheet to analyze a plugins folder.
            /// </summary>
            /// <param name="dbpfFiles">List of file paths to iterate through</param>
            /// <remarks>
            /// Creates LotList CSV: {RCIType}, {Stage}, {LotSizeX}x{LotSizeY}, {BuildingInstance}, {Name} <br/> 
            /// Also creates BldgList CSV: {Instance}, {BuildingName}, {Tilesets}
            /// </remarks>
            public static void CreateCSVs(List<FileInfo> dbpfFiles) {
                //create a new dictionary to store the scanned lot items
                List<LotList> lotList = new List<LotList>();
                List<BuildingList> bldgList = new List<BuildingList>();

                foreach (FileInfo file in dbpfFiles) {
                    DBPFFile dbpf = new DBPFFile(file);
                    dbpf.DecodeAllEntries();

                    //In a DBPF file, the indicies of TGIs corresponds directly to the indicies of Entries
                    //Filter down list of entries to only target the desired ones using LINQ
                    List<DBPFEntryEXMP> filteredEntries = (List<DBPFEntryEXMP>) (from entry in dbpf.GetEntries()
                                                                                 where entry.MatchesKnownEntryType(DBPFTGI.EXEMPLAR)
                                                                                 select entry);

                    foreach (DBPFEntryEXMP entry in filteredEntries) {
                        entry.DecodeEntry();

                        //Loop through LotConfigs to get lot info like stage, lot dims, wealth, etc.
                        if (entry.GetExemplarType() == (int) DBPFProperty.ExemplarTypes.LotConfiguration) {

                            LotList listItem = new LotList();
                            listItem.Name = (string) entry.GetProperty("Exemplar Name").GetData();
                            listItem.Stage = (byte) entry.GetProperty("Growth Stage").GetData(0);
                            listItem.LotSizeX = (byte) entry.GetProperty("LotConfigPropertySize").GetData(0);
                            listItem.LotSizeY = (byte) entry.GetProperty("LotConfigPropertySize").GetData(0);

                            //TOdo - for some reason this does not return the correct result
                            DBPFProperty prop = entry.GetProperty(0x88edc900);
                            if (prop is null) {
                                continue;
                            }
                            listItem.BuildingInstance = (uint) prop.GetData(12);

                            prop = entry.GetProperty("LotConfigPropertyPurposeTypes");
                            if (prop is null) {
                                continue;
                            }
                            if (prop.NumberOfReps == 0) { //have to account for some properties that might not have any values set
                                continue;
                            }
                            int purposeType = (byte) prop.GetData(0);
                            switch (purposeType) {
                                case 1:
                                    listItem.RCIType = "R";
                                    break;
                                case 2:
                                    listItem.RCIType = "CS";
                                    break;
                                case 3:
                                    listItem.RCIType = "CO";
                                    break;
                                case 5:
                                    listItem.RCIType = "IR";
                                    break;
                                case 6:
                                    listItem.RCIType = "ID";
                                    break;
                                case 7:
                                    listItem.RCIType = "IM";
                                    break;
                                case 8:
                                    listItem.RCIType = "IHT";
                                    break;
                                default:
                                    break;
                            }

                            int wealthType = (byte) entry.GetProperty("LotConfigPropertyWealthTypes").GetData(0);
                            string wealth = new string('$', wealthType);
                            listItem.RCIType += wealth;

                            lotList.Add(listItem);
                        }

                        //Loop through Building exemplars to get tileset info
                        else if (entry.GetExemplarType() == (int) DBPFProperty.ExemplarTypes.Building) {

                            BuildingList bldgItem = new BuildingList();
                            bldgItem.BuildingName = (string) entry.GetProperty("Exemplar Name").GetData();
                            bldgItem.Instance = (uint) entry.TGI.InstanceID;

                            DBPFProperty prop = entry.GetProperty("OccupantGroups");
                            if (prop is null) { //no guarantee that a building will have this property (it could be in a cohort)
                                continue;
                            }
                            var vals = (List<long>) prop.GetData();
                            bldgItem.Tilesets = ArrayToString(vals);
                            bldgList.Add(bldgItem);
                        }
                    }
                }

                WriteListToCSV(lotList, LotListCSVPath);
                WriteListToCSV(bldgList, BldgListCSVPath);
            }

            private static string ArrayToString(List<long> vals) {
                StringBuilder sb = new StringBuilder();
                foreach (uint item in vals) {
                    if (item >= 0x2000 && item <= 0x2003) {
                        sb.Append(DBPFUtil.ToHexString(item, 4) + ";");
                    }

                }
                return sb.ToString();
            }

            /// <summary>
            /// Helper class to establish the format of a LotList item.
            /// </summary>
            private class LotList {
                public string RCIType { get; set; }
                public byte LotSizeX { get; set; }
                public byte LotSizeY { get; set; }
                public byte Stage { get; set; }
                public string Name { get; set; }
                public uint BuildingInstance { get; set; }

                public override string ToString() {
                    return $"{RCIType}, {Stage}, {LotSizeX}x{LotSizeY}, {DBPFUtil.ToHexString(BuildingInstance)}, {Name}";
                }
            }

            /// <summary>
            /// Helper class to establish the format of a BuildingList item.
            /// </summary>
            private class BuildingList {
                public uint Instance { get; set; }
                public string BuildingName { get; set; }
                public string Tilesets { get; set; }

                public override string ToString() {
                    return $"{DBPFUtil.ToHexString(Instance)}, {BuildingName}, {Tilesets}";
                }

            }
        }




		/// <summary>
		/// Writes the dictionary of all scanned Rep13s to CSV file
		/// </summary>
		/// <param name="dict"></param>
		/// <see cref="https://joshclose.github.io/CsvHelper/getting-started/#writing-a-csv-file"/>
		private static void WriteListToCSV<T>(List<T> list, string filePath) {
			using (var writer = new StreamWriter(filePath))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture)) {
				csv.WriteRecords(list);
			}
		}
	}
}
