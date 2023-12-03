using System;
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
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using csDBPF;

namespace SC4ModManager {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void LoadFolderButton_Click(object sender, RoutedEventArgs e) {

            string? folderpath;
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                Title = "Choose Folder",
                InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\Plugins"),
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                folderpath = dialog.FileName;
            } else {
                return;
            }

            FolderInput.Text = folderpath;
            PopulateFolderListbox(folderpath);
        }


        private void PopulateFolderListbox(string folderpath) {
            string[] files = Directory.GetFiles(folderpath);
            foreach (string file in files) {
                DBPFFile dbpf = new DBPFFile(file);
                if (dbpf.t) {

                }
            }
        }
    }
}
