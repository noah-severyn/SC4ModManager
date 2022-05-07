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

namespace SC4ModManager {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private string pluginsFolderDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SimCity 4\\Plugins";
		private string folderPath = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\B62";
		private string folderPath2 = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\B62\\B62-Albertsons 60's Retro v2.0";


		public MainWindow() {
			InitializeComponent();
			string[] files = Directory.GetFiles(folderPath2, "*", SearchOption.TopDirectoryOnly);
			List<string> dbpfFiles = new List<string>();
			foreach (string file in files) {
				if (DBPFUtil.IsFileDBPF(file)) {
					dbpfFiles.Add(file);
				}
			}


			foreach (string filePath in dbpfFiles) {
				DBPFFile dbpf = new DBPFFile(filePath);
				OrderedDictionary entries = dbpf.ListOfEntries;

				foreach (DBPFEntry entry in entries.Values) {
					//Looking for exemplars subfiles that have type 0x010 (LotConfiguration), and then all properties 0x88EDC903 (LotConfigPropertyLotObjectData)
					if (entry.TGI.MatchesKnownTGI(DBPFTGI.EXEMPLAR)) {
						Dictionary<int, DBPFProperty> properties = (Dictionary<int, DBPFProperty>) entry.DecodeEntry();

						List<uint> listOfRep13IIDs = new List<uint>();
						foreach (DBPFProperty property in properties.Values) {
							if (property.ID == 0x88edc903) {
								//XMLProperties.AllProperties.TryGetValue(0x88EDC903, out XMLExemplarProperty xmlProp);
								Array vals = Array.CreateInstance(property.DataType.PrimitiveDataType, property.NumberOfReps);

								//Want only rep 1 = 0x0 (building) or 0x1 (prop) or 0x2 (texture) or 0x4 (flora)
								Trace.WriteLine(vals.GetValue(0));
							}
						}
					}
				}

			}
		}
	}
}
