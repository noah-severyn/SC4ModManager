using System;
using System.IO;
using System.Collections.Generic;
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

namespace SC4ModManager {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private string pluginsFolderDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SimCity 4\\Plugins";
		private string folderPath = "C:\\Users\\Administrator\\OneDrive\\SC4 MODPACC\\B62";


		public MainWindow() {
			InitializeComponent();

			string[] files = Directory.GetFiles(folderPath);
			csDBPF.DBPFUtil.FilterFilesByExtension

		}
	}
}
