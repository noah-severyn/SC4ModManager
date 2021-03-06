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
		private readonly string DocsPlugins = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SimCity 4\\Plugins";
		private readonly string DocsPluginsDeps = "C:\\Users\\Administrator\\Documents\\SimCity 4\\Plugins\\!Deps";
		private readonly string DocsPluginsDeps2 = "C:\\Users\\Administrator\\Documents\\SimCity 4\\Plugins\\!Deps 2";

		private readonly string GameDir = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\SimCity 4 Deluxe";
		private readonly string GamePlugins = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\SimCity 4 Deluxe\\Plugins";

		private readonly string B62Files = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\B62\\b62 unzipped";
		private readonly string AllDeps = "C:\\Users\\Administrator\\OneDrive\\SC4 Deps";
		private readonly string STEX_Sample = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\STEX Entries Sample - 2021.10.22\\Files";

		


		public MainWindow() {
			InitializeComponent();
			string[] files = Directory.GetFiles(DocsPluginsDeps, "*", SearchOption.TopDirectoryOnly);
			List<string> dbpfFiles = new List<string>();
			foreach (string file in files) {
				if (DBPFUtil.IsFileDBPF(file)) {
					dbpfFiles.Add(file);
				}
			}

			//Analysis.GetRep13IIDs(dbpfFiles); //creates "rep13IIDs.csv"

			Analysis.GetTGIs(dbpfFiles); //creates "foundTGIs.csv"

			//Analysis.GenerateMainPropTextureCatalogList(dbpfFiles); //creates "depTGIs.csv"

			//LEX_Access.AccessLEXFileInfo(2987);
		}
	}
}
