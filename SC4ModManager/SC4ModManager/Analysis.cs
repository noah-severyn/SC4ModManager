using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using CsvHelper;
using System.Text;
using csDBPF;
using csDBPF.Properties;
using System.Linq;
using System.Diagnostics;

namespace SC4ModManager {
	public static class Analysis {
		#region Rep13IIDs
		private const string Rep13sCSVpath = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\rep13IIDs.csv";
		/// <summary>
		/// Generates a CSV file of all props, textures, buildings, and flora used on lots (Rep 13 of the LotConfigPropertyLotObjectData properties).
		/// </summary>
		/// <param name="dbpfFiles">List of file paths to iterate through</param>
		public static void GetRep13IIDs(List<string> dbpfFiles) {
			List<Rep13IIDs> listOfRep13IIDs = new List<Rep13IIDs>();

			//Loop through each file
			foreach (string filePath in dbpfFiles) {
				DBPFFile dbpf = DBPFFile.CreateIfValidDBPF(new FileInfo(filePath));
				List<DBPFEntry> listOfEntries = dbpf.ListOfEntries;

				//Loop through each subfile
				foreach (DBPFEntry entry in listOfEntries) {
					if (entry.TGI.MatchesKnownTGI(DBPFTGI.EXEMPLAR)) {
						//Initialize the list of properties for this entry
						entry.DecodeEntry();

						//Check the exemplar type and skip to next exemplar file if not a match
						int exType = entry.GetExemplarType();
						if (!(exType == (int) DBPFProperty.ExemplarTypes.LotConfiguration)) {
							continue;
						}

						//We know this exemplar is type 0x10 (Lot Configuration) so continue on to snag the LotConfigPropertyLotObjectData properties
						foreach (DBPFProperty property in entry.ListOfProperties) {
							//LotConfigPropertyLotObjectData must be between 0x88edc900 and 0x88edce00 - first one is 0x88edc900 and can continue on for max 1280 repetitions total
							if (property.ID >= 0x88edc900 && property.ID <= 0x88edce00) {
								property.DecodeValues();
								Array lotObjects = Array.CreateInstance(property.DataType.PrimitiveDataType, property.NumberOfReps);
								lotObjects = property.DecodedValues;

								//First rep determines the type. We want to add only if it is 0x0 (building) or 0x1 (prop) or 0x2 (texture) or 0x4 (flora)
								uint lotObjectType = (uint) lotObjects.GetValue(0);
								if (lotObjectType == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Building || lotObjectType == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Prop ||
									lotObjectType == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Texture || lotObjectType == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Flora) {
									listOfRep13IIDs.Add(new Rep13IIDs { ParentTGI = entry.TGI.ToString(), Rep0IID = lotObjectType, Rep13IID = (uint) lotObjects.GetValue(12) });
								}
							}
						}
					}
				}
			}

			WriteListToCSV(listOfRep13IIDs, Rep13sCSVpath);
		}
		/// <summary>
		/// Simple helper class to hold fields for writing Rep13s to CSV file.
		/// </summary>
		private class Rep13IIDs {
			public string ParentTGI { get; set; }
			public uint Rep0IID { get; set; }
			public uint Rep13IID { get; set; }

			public override string ToString() {
				return $"{ParentTGI}, {Rep0IID}, 0x{DBPFUtil.UIntToHexString(Rep13IID)}";
			}
		}
		#endregion Rep13IIDs


		#region GetTGIs
		private const string TGIsCSVpath = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\foundTGIs.csv";
		/// <summary>
		/// Generates a CSV file of all TGIs.
		/// </summary>
		/// <param name="dbpfFiles">List of file paths to iterate through</param>
		public static void GetTGIs(List<string> dbpfFiles) {
			List<TGIs> allTGIs = new List<TGIs>();

			foreach (string filePath in dbpfFiles) {
				DBPFFile dbpf = DBPFFile.CreateIfValidDBPF(new FileInfo(filePath));
				foreach (DBPFTGI tgi in dbpf.ListOfTGIs) {

					//Add all Base/Overlay textures. Look at the least significant 4 bits and only add if it is 0, 5, or A: And the Instance by 0b1111 (0xF) and check the modulus result.
					if (tgi.MatchesKnownTGI(DBPFTGI.FSH_BASE_OVERLAY) && ((tgi.Instance & 0xF) % 5) == 0) {
						allTGIs.Add(new TGIs { FilePath = filePath, TGI = tgi.ToString() });
					}

					//Add all Exemplars
					else if (tgi.MatchesKnownTGI(DBPFTGI.EXEMPLAR)) {
						allTGIs.Add(new TGIs { FilePath = filePath, TGI = tgi.ToString() });
					}

					//Add all Cohorts. Note the Building/prop family of the Cohort is always 0x10000000 less than the Cohort's Index.
					else if (tgi.MatchesKnownTGI(DBPFTGI.COHORT)) {
						DBPFTGI family = new DBPFTGI((uint) tgi.Type, (uint) tgi.Group, (uint) (tgi.Instance - 0x10000000));
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
		#endregion GetTGIs


		#region PropTextureCatalog
		//TODO - FilePath should be a FileIdentifier instead:
		//the path can change, filename can change - what will remain constant such that a file will be recognized even if it is edited - or do we care?
		//possibly be some sort of combination of the date created unix timestamp, and first n and last n rows in the DIR table?

		//ORRRRR do we not care at all, and just directly look at the TGIs contained in an upload by STEX and LEX id and use those as the identifiers <<<<<<<<<<<<<<

		//index offset in the header might be a good measurement to tell if a file has been modified or not (but we have date modified so ????)


		/// <summary>
		/// Generates a CSV file of all TGIs.
		/// </summary>
		/// <param name="dbpfFiles">List of file paths to iterate through</param>
		public static void GenerateMainPropTextureCatalogList(List<FileInfo> dbpfFiles) {
			string exName;

			var db = new DatabaseBuilder.DatabaseHandler("C:\\Users\\Administrator\\Desktop\\");

			foreach (FileInfo file in dbpfFiles) {
				Debug.WriteLine(file.Name);
				DBPFFile dbpf = DBPFFile.CreateIfValidDBPF(file);
				foreach (DBPFEntry entry in dbpf.ListOfEntries) {

					//Add all Base/Overlay textures. Look at the least significant 4 bits and only add if it is 0, 5, or A: And the Instance by 0b1111 (0xF) and check the modulus result.
					if (entry.MatchesKnownEntryType(DBPFTGI.FSH_BASE_OVERLAY) && ((entry.TGI.Instance & 0xF) % 5) == 0) {
						db.AddTGI(file.Name, entry.TGI.ToString(), null);
					}

					//Add all Exemplars
					else if (entry.MatchesKnownEntryType(DBPFTGI.EXEMPLAR)) {
						entry.DecodeEntry();
						if (entry.ListOfProperties.Count == 0) continue;

						DBPFProperty p = entry.GetProperty("ExemplarName");
						p.DecodeValues();
						exName = (string) p.DecodedValues.GetValue(0); //Decoded value of exemplar name is a string array of length 1
						db.AddTGI(file.Name, entry.TGI.ToString(), exName);
					}

					//Add all Cohorts. Note the Building/prop family of the Cohort is always 0x10000000 less than the Cohort's Index.
					else if (entry.MatchesKnownEntryType(DBPFTGI.COHORT)) {
						DBPFTGI family = new DBPFTGI((uint) entry.TGI.Type, (uint) entry.TGI.Group, (uint) (entry.TGI.Instance - 0x10000000));
						entry.DecodeEntry();
						if (entry.ListOfProperties.Count == 0) continue;

						DBPFProperty p = entry.GetProperty("ExemplarName");
						p.DecodeValues();
						exName = (string) p.DecodedValues.GetValue(0); //Decoded value of exemplar name is a string array of length 1
						db.AddTGI(file.Name, entry.TGI.ToString(), exName);
					}
				}
			}
		}
		#endregion PropTextureCatalog


		#region LotList
		private const string LotListCSVPath = "C:\\Users\\Administrator\\Documents\\SimCity 4\\Plugins\\IRM\\LotList.csv";
		private const string BldgListCSVPath = "C:\\Users\\Administrator\\Documents\\SimCity 4\\Plugins\\IRM\\BldgList.csv";
		//scan exemplars for lot size, growth stage, lot name, jmyers
		public static void GenerateLotListTables(List<FileInfo> dbpfFiles) {
			//create a new dictionary to store the scanned lot items
			List<LotList> lotList = new List<LotList>();
			List<BuildingList> bldgList = new List<BuildingList>();

			foreach (FileInfo file in dbpfFiles) {
				DBPFFile dbpf = DBPFFile.CreateIfValidDBPF(file);
				dbpf.DecodeAllEntries();

				//In a DBPF file, the indicies of TGIs corresponds dicrectly to the indicies of Entries
				//Filter down list of entries to only target the desired ones using LINQ
				var filteredEntries = from entry in dbpf.ListOfEntries
									  where entry.MatchesKnownEntryType(DBPFTGI.EXEMPLAR)
									  select entry;

				foreach (DBPFEntry entry in filteredEntries) {
					entry.DecodeEntry();

					//Loop through LotConfigs to get lot info like stage, lot dims, wealth, etc.
					if (entry.GetExemplarType() == (int) DBPFProperty.ExemplarTypes.LotConfiguration) {
						entry.DecodeAllProperties();

						LotList listItem = new LotList();
						listItem.Name = (string) entry.GetProperty("Exemplar Name").DecodedValues.GetValue(0);
						listItem.Stage = (byte) entry.GetProperty("Growth Stage").DecodedValues.GetValue(0);
						listItem.LotSizeX = (byte) entry.GetProperty("LotConfigPropertySize").DecodedValues.GetValue(0);
						listItem.LotSizeY = (byte) entry.GetProperty("LotConfigPropertySize").DecodedValues.GetValue(1);

						//TOdo - for some reason this does not return the correct result
						DBPFProperty prop = entry.GetProperty(0x88edc900);
						Array vals = Array.CreateInstance(prop.DataType.PrimitiveDataType,prop.NumberOfReps);
						vals = prop.DecodedValues;
						if (prop is null) {
							continue;
						}
						listItem.BuildingInstance = (uint) vals.GetValue(12);

						prop = entry.GetProperty("LotConfigPropertyPurposeTypes");
						if (prop is null) {
							continue;
						}
						if (prop.DecodedValues.Length == 0) { //have to account for some properties that might not have any values set
							continue;
						}
						int purposeType = (byte) prop.DecodedValues.GetValue(0);
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

						int wealthType = (byte) entry.GetProperty("LotConfigPropertyWealthTypes").DecodedValues.GetValue(0);
						string wealth = new string('$', wealthType);
						listItem.RCIType += wealth;

						lotList.Add(listItem);
					} 
					
					//Loop through Building exemplars to get tileset info
					else if (entry.GetExemplarType() == (int) DBPFProperty.ExemplarTypes.Building) {
						entry.DecodeAllProperties();

						BuildingList bldgItem = new BuildingList();
						bldgItem.BuildingName = (string) entry.GetProperty("Exemplar Name").DecodedValues.GetValue(0);
						bldgItem.Instance = (uint) entry.TGI.Instance;

						DBPFProperty prop = entry.GetProperty("OccupantGroups");
						if (prop is null) { //no guarantee that a building will have this property (it could be in a cohort)
							continue;
						}
						Array vals = Array.CreateInstance(prop.DataType.PrimitiveDataType, prop.NumberOfReps);
						vals = prop.DecodedValues;
						bldgItem.Tilesets = ArrayToString(vals);
						bldgList.Add(bldgItem);
					}
				}
			}

			WriteListToCSV(lotList, LotListCSVPath);
			WriteListToCSV(bldgList, BldgListCSVPath);
		}

		private static string ArrayToString(Array vals) {
			StringBuilder sb = new StringBuilder();
			foreach (uint item in vals) {
				if (item >= 0x2000 && item <= 0x2003) {
					sb.Append(DBPFUtil.UIntToHexString(item, 4) + ";");
				}
				
			}
			return sb.ToString();
		}

		private class LotList {
			public string RCIType { get; set; }
			public byte LotSizeX { get; set; }
			public byte LotSizeY { get; set; }
			public byte Stage { get; set; }
			public string Name { get; set; }
			public uint BuildingInstance { get; set; }

			public override string ToString() {
				return $"{RCIType}, {Stage}, {LotSizeX}x{LotSizeY}, {DBPFUtil.UIntToHexString(BuildingInstance)}, {Name}";
			}
		}

		private class BuildingList {
			public uint Instance { get; set; }
			public string BuildingName { get; set; }
			public string Tilesets { get; set; }

			public override string ToString() {
				return $"{DBPFUtil.UIntToHexString(Instance)}, {BuildingName}, {Tilesets}";
			}

		}
		#endregion LotList




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
