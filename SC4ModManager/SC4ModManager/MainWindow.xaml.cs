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
using System.Diagnostics;



namespace SC4ModManager {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private readonly string pluginsFolderDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SimCity 4\\Plugins";
		private readonly string B62_Files = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\B62";
		private readonly string folderPath2 = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\B62\\B62-Albertsons 60's Retro v2.0";
		private readonly string SC4_Deps = "C:\\Users\\Administrator\\OneDrive\\SC4 Deps";
		private readonly string Deps = "C:\\Users\\Administrator\\Documents\\SimCity 4\\Plugins\\!Deps";
		private readonly string Deps2 = "C:\\Users\\Administrator\\Documents\\SimCity 4\\Plugins\\!Deps 2";
		private readonly string STEX_Sample = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\STEX Entries Sample - 2021.10.22\\Files";
		private readonly string GAME_DIR = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\SimCity 4 Deluxe";


		public MainWindow() {
			InitializeComponent();
			string[] files = Directory.GetFiles(SC4_Deps, "*", SearchOption.TopDirectoryOnly);
			List<string> dbpfFiles = new List<string>();
			foreach (string file in files) {
				if (DBPFUtil.IsFileDBPF(file)) {
					dbpfFiles.Add(file);
				}
			}


			//Analysis.GetRep13IIDs(dbpfFiles);
			//Analysis.GetTGIs(dbpfFiles);
			Analysis.GenerateMainPropTextureCatalogList(dbpfFiles);
		}
	}
}
