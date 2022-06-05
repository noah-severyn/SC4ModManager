using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using CsvHelper;
using System.Text;
using csDBPF;
using csDBPF.Properties;

namespace SC4ModManager {
	public static class Analysis {
		private const string Rep13sCSVpath = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\rep13IIDs.csv";
		private const string TGIsCSVpath = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\allTGIs.csv";

		/// <summary>
		/// Generates a CSV file of all props, textures, buildings, and flora used on lots (Rep 13 of the LotConfigPropertyLotObjectData properties).
		/// </summary>
		/// <param name="dbpfFiles">List of file paths to iterate through</param>
		public static void GetRep13IIDs(List<string> dbpfFiles) {
			Dictionary<uint, Rep13IIDs> listOfRep13IIDs = new Dictionary<uint, Rep13IIDs>();
			uint idx = 0;

			//Loop through each file
			foreach (string filePath in dbpfFiles) {
				DBPFFile dbpf = new DBPFFile(filePath);
				OrderedDictionary listOfEntries = dbpf.ListOfEntries;

				//Loop through each subfile
				foreach (DBPFEntry entry in listOfEntries.Values) {
					if (entry.TGI.MatchesKnownTGI(DBPFTGI.EXEMPLAR)) {
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
								property.DecodeValues();
								Array lotObjects = Array.CreateInstance(property.DataType.PrimitiveDataType, property.NumberOfReps);
								lotObjects = property.DecodedValues;

								//First rep determines the type. We want to add only if it is 0x0 (building) or 0x1 (prop) or 0x2 (texture) or 0x4 (flora)
								uint lotObjectType = (uint) lotObjects.GetValue(0);
								if (lotObjectType == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Building || lotObjectType == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Prop ||
									lotObjectType == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Texture || lotObjectType == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Flora) {
									listOfRep13IIDs.Add(idx, new Rep13IIDs { FilePath = filePath, Rep0IID = lotObjectType, Rep13IID = (uint) lotObjects.GetValue(12) });
									idx++;
								}
							}
						}
					}
				}
			}

			WriteRep13sToCSV(listOfRep13IIDs, Rep13sCSVpath);
		}
		/// <summary>
		/// Simple helper class to hold fields for writing Rep13s to CSV file.
		/// </summary>
		private class Rep13IIDs {
			public string FilePath { get; set; }
			public uint Rep0IID { get; set; }
			public uint Rep13IID { get; set; }

			public override string ToString() {
				return $"{FilePath}, {Rep0IID}, 0x{DBPFUtil.UIntToHexString(Rep13IID)}";
			}
		}
		/// <summary>
		/// Writes the dictionary of all scanned Rep13s to CSV file
		/// </summary>
		/// <param name="dict"></param>
		/// <see cref="https://joshclose.github.io/CsvHelper/getting-started/#writing-a-csv-file"/>
		private static void WriteRep13sToCSV(Dictionary<uint, Rep13IIDs> dict, string filePath) {
			using (var writer = new StreamWriter(filePath))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture)) {
				csv.WriteRecords(dict);
			}
		}


		/// <summary>
		/// Generates a CSV file of all TGIs.
		/// </summary>
		/// <param name="dbpfFiles">List of file paths to iterate through</param>
		public static void GetTGIs(List<string> dbpfFiles) {
			Dictionary<uint, TGIs> allTGIs = new Dictionary<uint, TGIs>();
			uint idx = 0;

			foreach (string filePath in dbpfFiles) {
				DBPFFile dbpf = new DBPFFile(filePath);
				foreach (DBPFTGI tgi in dbpf.ListOfTGIs.Values) {

					//If TGI is Base/Overlay Texture, look at the least significant 4 bits and only add if it is 0, 5, or A (And the Instance by 0b1111 (0xF) and check the modulus result)
					if (tgi.MatchesKnownTGI(DBPFTGI.FSH_BASE_OVERLAY) && ((tgi.Instance & 0xF) % 5) != 0) {
						continue;
					}
					//Do not index S3D either
					if (tgi.MatchesKnownTGI(DBPFTGI.S3D)) {
						continue;
					}

					//for now, only do exemplar
					if (tgi.MatchesKnownTGI(DBPFTGI.EXEMPLAR)) {
						allTGIs.Add(idx, new TGIs { FilePath = filePath, TGI = tgi.ToString() });
						idx++;
					}
					
				}
			}
			WriteTGIsToCSV(allTGIs, TGIsCSVpath);
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
		/// <summary>
		/// Writes the dictionary of all scanned Rep13s to CSV file
		/// </summary>
		/// <param name="dict"></param>
		private static void WriteTGIsToCSV(Dictionary<uint, TGIs> dict, string filePath) {
			using (var writer = new StreamWriter(filePath))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture)) {
				csv.WriteRecords(dict);
			}
		}
	}
}
