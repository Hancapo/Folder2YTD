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
using System.Windows.Shapes;
using System.Windows.Forms;
using Ookii.Dialogs.Wpf;


namespace Folder2YTD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TransparencyTypes.ItemsSource = new List<string>() {"Off", "By flags (PNG only)", "By pixels" };
            FormatOutput.ItemsSource = new List<string>() {"GTA V (.YTD)" };
            FormatOutput.SelectedIndex = 0;
            TransparencyTypes.SelectedIndex = 0;    
        }

        private void btnTesteo_Click(object sender, RoutedEventArgs e)
        {

            
            
        }

        private void btnSelectFolders_Click(object sender, RoutedEventArgs e)
        {
           
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Multiselect = true;

            if ((bool)dialog.ShowDialog())
            {
                lbFolderView.ItemsSource = dialog.SelectedPaths;
            }
            
        }
    }
}
