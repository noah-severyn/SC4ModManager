using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using csDBPF;
using csDBPF.Properties;
using System.Diagnostics;
using CsvHelper;
using System.Globalization;

namespace SC4ModManager {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private readonly string pluginsFolderDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SimCity 4\\Plugins";
		private readonly string folderPath = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\B62";
		private readonly string folderPath2 = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\B62\\B62-Albertsons 60's Retro v2.0";


		public MainWindow() {
			InitializeComponent();
			//string[] files = Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly);
			string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
			List<string> dbpfFiles = new List<string>();
			foreach (string file in files) {
				if (DBPFUtil.IsFileDBPF(file)) {
					dbpfFiles.Add(file);
				}
			}

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
						Dictionary<int, DBPFProperty> listOfProperties = (Dictionary<int, DBPFProperty>) entry.DecodeEntry();

						//Check the exemplar type and skip to next exemplar file if not a match
						DBPFProperty exemplarType = entry.GetProperty(0x00000010,listOfProperties);
						Array exemplarTypeVals = Array.CreateInstance(exemplarType.DataType.PrimitiveDataType, exemplarType.NumberOfReps); //Create new array to hold the values
						exemplarTypeVals = (Array) exemplarType.DecodeValues(); //Set the values from the decoded property
						uint exemplarTypeValue = (uint) exemplarTypeVals.GetValue(0); //Exemplar type can only hold one value, so grab the first one.
						if (!(exemplarTypeValue == (int) DBPFProperty.ExemplarTypes.LotConfiguration)) {
							continue;
						}

						//We know this exemplar is type 0x10 (Lot Configuration) so continue on to snag the LotConfigPropertyLotObjectData properties - first one ix 0x88edc900 and can continue on for max 1028 repetitions total
						foreach (DBPFProperty property in listOfProperties.Values) {
							//LotConfigPropertyLotObjectData must be between 0x88edc900 and 0x88edce00
							if (property.ID >= 0x88edc900 && property.ID <= 0x88edce00) {
								Array lcplodVals = Array.CreateInstance(property.DataType.PrimitiveDataType, property.NumberOfReps);
								lcplodVals = (Array) property.DecodeValues();
								//Want only rep 1 = 0x0 (building) or 0x1 (prop) or 0x2 (texture) or 0x4 (flora)
								uint rep0 = (uint) lcplodVals.GetValue(0);

								if (rep0 == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Building || rep0 == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Prop ||
									rep0 == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Texture || rep0 == (int) DBPFProperty.LotConfigPropertyLotObjectTypes.Flora) {
									listOfRep13IIDs.Add(idx, new Rep13IIDs { FilePath = filePath, Rep0IID = rep0, Rep13IID = (uint) lcplodVals.GetValue(12) });
									idx++;
								}
							}
						}
					}
				}
			}

			WriteCSV(listOfRep13IIDs);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dict"></param>
		/// <see cref="https://joshclose.github.io/CsvHelper/getting-started/#writing-a-csv-file"/>
		private void WriteCSV(Dictionary<uint,Rep13IIDs> dict) {
			using (var writer = new StreamWriter("C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\rep13IIDs.csv"))
			using (var csv = new CsvWriter(writer,CultureInfo.InvariantCulture)) {
				csv.WriteRecords(dict);
			}
		}

		private class Rep13IIDs {
			public string FilePath { get; set; }
			public uint Rep0IID { get; set; }
			public uint Rep13IID { get; set; }

			public override string ToString() {
				return $"{FilePath}, {Rep0IID}, 0x{DBPFUtil.UIntToHexString(Rep13IID)}";
			}
		}
	}
}
